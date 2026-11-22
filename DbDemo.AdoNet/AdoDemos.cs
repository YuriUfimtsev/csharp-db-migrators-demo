using Npgsql;

namespace DbDemo.AdoNet;

public static class AdoDemos
{
    /// <summary>ExecuteReader: курсор только вперёд, маппинг вручную.</summary>
    public static async Task ReadAsync(string cs)
    {
        await using var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT id, name, price, category_id FROM products WHERE price > @min ORDER BY price", conn);
        cmd.Parameters.AddWithValue("min", 500m);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            int id = reader.GetInt32(0);
            string name = reader.GetString(1);
            decimal price = reader.GetDecimal(2);
            int? categoryId = reader.IsDBNull(3) ? null : reader.GetInt32(3);

            Console.WriteLine($"{id,3}  {name,-16} {price,10:N2}  категория {categoryId?.ToString() ?? "-"}");
        }
    }

    /// <summary>ExecuteScalar и ExecuteNonQuery.</summary>
    public static async Task CommandsAsync(string cs)
    {
        await using var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync();

        await using (var insert = new NpgsqlCommand(
            "INSERT INTO products (name, price) VALUES (@name, @price) RETURNING id", conn))
        {
            insert.Parameters.AddWithValue("name", "Коврик для мыши");
            insert.Parameters.AddWithValue("price", 390m);
            var id = (int)(await insert.ExecuteScalarAsync())!;
            Console.WriteLine($"ExecuteScalar: новый id = {id}");

            await using var delete = new NpgsqlCommand("DELETE FROM products WHERE id = @id", conn);
            delete.Parameters.AddWithValue("id", id);
            Console.WriteLine($"ExecuteNonQuery: удалено строк = {await delete.ExecuteNonQueryAsync()}");
        }

        await using var count = new NpgsqlCommand("SELECT count(*) FROM products", conn);
        Console.WriteLine($"ExecuteScalar: всего товаров = {await count.ExecuteScalarAsync()}");
    }

    /// <summary>Склейка строк против параметров.</summary>
    public static async Task InjectionAsync(string cs, string input)
    {
        await using var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync();
        Console.WriteLine($"Ввод пользователя: {input}");

        // ПЛОХО: значение вклеено в текст запроса
        var unsafeSql = $"SELECT name FROM products WHERE name = '{input}'";
        Console.WriteLine($"\nСклейка строк:\n  {unsafeSql}");
        await using (var cmd = new NpgsqlCommand(unsafeSql, conn))
            Console.WriteLine($"  найдено строк: {await CountRowsAsync(cmd)}");

        // ХОРОШО: значение передаётся параметром
        Console.WriteLine("\nПараметр:\n  SELECT name FROM products WHERE name = @name");
        await using (var cmd = new NpgsqlCommand("SELECT name FROM products WHERE name = @name", conn))
        {
            cmd.Parameters.AddWithValue("name", input);
            Console.WriteLine($"  найдено строк: {await CountRowsAsync(cmd)}");
        }
    }

    /// <summary>Транзакция без Commit откатывается при Dispose.</summary>
    public static async Task TransactionAsync(string cs)
    {
        await using var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync();
        Console.WriteLine($"До транзакции: {await CountProductsAsync(conn, null)} товаров");

        await using (var tx = await conn.BeginTransactionAsync())
        {
            await using var cmd = new NpgsqlCommand(
                "INSERT INTO products (name, price) VALUES ('Временный товар', 1)", conn, tx);
            await cmd.ExecuteNonQueryAsync();
            Console.WriteLine($"Внутри транзакции: {await CountProductsAsync(conn, tx)} товаров");
            // CommitAsync не вызываем
        }

        Console.WriteLine($"После Dispose без Commit: {await CountProductsAsync(conn, null)} товаров");
    }

    /// <summary>NpgsqlDataSource: один объект на приложение, владеет пулом.</summary>
    public static async Task DataSourceAsync(string cs)
    {
        await using var dataSource = NpgsqlDataSource.Create(cs);

        await using var cmd = dataSource.CreateCommand("SELECT count(*) FROM products");
        long count = (long)(await cmd.ExecuteScalarAsync())!;
        Console.WriteLine($"Товаров: {count}");

        await using var conn = await dataSource.OpenConnectionAsync();
        Console.WriteLine($"Подключение из DataSource, PID = {conn.ProcessID}");
    }

    private static async Task<int> CountRowsAsync(NpgsqlCommand cmd)
    {
        int rows = 0;
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) rows++;
        return rows;
    }

    private static async Task<long> CountProductsAsync(NpgsqlConnection conn, NpgsqlTransaction? tx)
    {
        await using var cmd = new NpgsqlCommand("SELECT count(*) FROM products", conn, tx);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }
}
