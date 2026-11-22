using System.Data;
using Npgsql;

namespace DbDemo.AdoNet;

/// <summary>
/// Аномалии и уровни изоляции. Две транзакции имитируются двумя подключениями,
/// шаги выполняются строго по очереди, поэтому результат воспроизводим.
/// </summary>
public static class IsolationDemos
{
    private const int AccountId = 1;

    public static async Task LostUpdateAsync(string cs)
    {
        await using var c1 = await OpenAsync(cs);
        await using var c2 = await OpenAsync(cs);

        Console.WriteLine("== Чтение в приложении, запись вычисленного значения (Read Committed) ==");
        await SetBalanceAsync(c1, 100);
        var b1 = await GetBalanceAsync(c1, null);
        var b2 = await GetBalanceAsync(c2, null);
        Console.WriteLine($"T1 прочитала {b1}, T2 прочитала {b2}");
        await ExecAsync(c1, null, "UPDATE accounts SET balance = @b WHERE id = 1", ("b", b1 + 50));
        await ExecAsync(c2, null, "UPDATE accounts SET balance = @b WHERE id = 1", ("b", b2 - 30));
        Console.WriteLine($"Итог: {await GetBalanceAsync(c1, null)} (ожидали 120)\n");

        Console.WriteLine("== Атомарный UPDATE: считаем в базе ==");
        await SetBalanceAsync(c1, 100);
        await ExecAsync(c1, null, "UPDATE accounts SET balance = balance + 50 WHERE id = 1");
        await ExecAsync(c2, null, "UPDATE accounts SET balance = balance - 30 WHERE id = 1");
        Console.WriteLine($"Итог: {await GetBalanceAsync(c1, null)}\n");

        Console.WriteLine("== Repeatable Read: PostgreSQL не даст потерять обновление ==");
        await SetBalanceAsync(c1, 100);
        await using var t1 = await c1.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        await using var t2 = await c2.BeginTransactionAsync(IsolationLevel.RepeatableRead);
        b1 = await GetBalanceAsync(c1, t1);
        b2 = await GetBalanceAsync(c2, t2);
        await ExecAsync(c1, t1, "UPDATE accounts SET balance = @b WHERE id = 1", ("b", b1 + 50));
        await t1.CommitAsync();
        Console.WriteLine("T1 записала 150 и зафиксировала");
        try
        {
            await ExecAsync(c2, t2, "UPDATE accounts SET balance = @b WHERE id = 1", ("b", b2 - 30));
            await t2.CommitAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            Console.WriteLine($"T2: {ex.SqlState} {ex.MessageText}. Транзакцию нужно повторить.");
            await t2.RollbackAsync();
        }
        Console.WriteLine($"Итог: {await GetBalanceAsync(c1, null)}");
    }

    public static async Task DirtyReadAsync(string cs)
    {
        await using var c1 = await OpenAsync(cs);
        await using var c2 = await OpenAsync(cs);
        await SetBalanceAsync(c1, 100);

        await using var t1 = await c1.BeginTransactionAsync();
        await ExecAsync(c1, t1, "UPDATE accounts SET balance = 0 WHERE id = 1");
        Console.WriteLine("T1 изменила баланс на 0, но не зафиксировала");

        await using var t2 = await c2.BeginTransactionAsync(IsolationLevel.ReadUncommitted);
        Console.WriteLine($"T2 (Read Uncommitted) читает: {await GetBalanceAsync(c2, t2)}");
        Console.WriteLine("В PostgreSQL Read Uncommitted работает как Read Committed: грязного чтения нет.");

        await t2.CommitAsync();
        await t1.RollbackAsync();
    }

    public static async Task NonRepeatableReadAsync(string cs)
    {
        foreach (var level in new[] { IsolationLevel.ReadCommitted, IsolationLevel.RepeatableRead })
        {
            await using var c1 = await OpenAsync(cs);
            await using var c2 = await OpenAsync(cs);
            await SetBalanceAsync(c1, 100);

            await using var t1 = await c1.BeginTransactionAsync(level);
            var first = await GetBalanceAsync(c1, t1);
            await ExecAsync(c2, null, "UPDATE accounts SET balance = 50 WHERE id = 1");   // T2, сразу COMMIT
            var second = await GetBalanceAsync(c1, t1);
            await t1.CommitAsync();

            Console.WriteLine($"{level,-15}: первое чтение {first}, второе {second}");
        }
    }

    public static async Task PhantomAsync(string cs)
    {
        foreach (var level in new[] { IsolationLevel.ReadCommitted, IsolationLevel.RepeatableRead })
        {
            await using var c1 = await OpenAsync(cs);
            await using var c2 = await OpenAsync(cs);

            await using var t1 = await c1.BeginTransactionAsync(level);
            var first = await ScalarAsync<long>(c1, t1, "SELECT count(*) FROM accounts WHERE balance > 0");
            var newId = await ScalarAsync<int>(c2, null,
                "INSERT INTO accounts (owner, balance) VALUES ('Фантом', 80) RETURNING id");
            var second = await ScalarAsync<long>(c1, t1, "SELECT count(*) FROM accounts WHERE balance > 0");
            await t1.CommitAsync();

            Console.WriteLine($"{level,-15}: первый count {first}, второй {second}");
            await ExecAsync(c2, null, "DELETE FROM accounts WHERE id = @id", ("id", newId));
        }
        Console.WriteLine("В PostgreSQL Repeatable Read работает на снимке транзакции, фантомов нет.");
    }

    /// <summary>Пример из документации PostgreSQL: таблица class | value.</summary>
    public static async Task SerializationAsync(string cs)
    {
        foreach (var level in new[] { IsolationLevel.RepeatableRead, IsolationLevel.Serializable })
        {
            await using var c1 = await OpenAsync(cs);
            await using var c2 = await OpenAsync(cs);
            await ExecAsync(c1, null, """
                CREATE TABLE IF NOT EXISTS class_values (class int, value int);
                TRUNCATE class_values;
                INSERT INTO class_values VALUES (1, 10), (1, 20), (2, 100), (2, 200);
                """);

            Console.WriteLine($"== {level} ==");
            await using var t1 = await c1.BeginTransactionAsync(level);
            await using var t2 = await c2.BeginTransactionAsync(level);
            try
            {
                var s1 = await ScalarAsync<long>(c1, t1, "SELECT sum(value) FROM class_values WHERE class = 1");
                var s2 = await ScalarAsync<long>(c2, t2, "SELECT sum(value) FROM class_values WHERE class = 2");
                await ExecAsync(c1, t1, "INSERT INTO class_values VALUES (2, @v)", ("v", (int)s1));
                await ExecAsync(c2, t2, "INSERT INTO class_values VALUES (1, @v)", ("v", (int)s2));
                await t1.CommitAsync();
                Console.WriteLine($"T1 вставила (2, {s1}) и зафиксировала");
                await t2.CommitAsync();
                Console.WriteLine($"T2 вставила (1, {s2}) и зафиксировала: аномалия сериализации");
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.SerializationFailure)
            {
                Console.WriteLine($"Ошибка {ex.SqlState}: {ex.MessageText}");
            }
        }
    }

    private static async Task<NpgsqlConnection> OpenAsync(string cs)
    {
        var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync();
        return conn;
    }

    private static Task SetBalanceAsync(NpgsqlConnection conn, decimal balance) =>
        ExecAsync(conn, null, "UPDATE accounts SET balance = @b WHERE id = 1", ("b", balance));

    private static Task<decimal> GetBalanceAsync(NpgsqlConnection conn, NpgsqlTransaction? tx) =>
        ScalarAsync<decimal>(conn, tx, $"SELECT balance FROM accounts WHERE id = {AccountId}");

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection conn, NpgsqlTransaction? tx, string sql)
    {
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        return (T)(await cmd.ExecuteScalarAsync())!;
    }

    private static async Task ExecAsync(NpgsqlConnection conn, NpgsqlTransaction? tx, string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value);
        await cmd.ExecuteNonQueryAsync();
    }
}
