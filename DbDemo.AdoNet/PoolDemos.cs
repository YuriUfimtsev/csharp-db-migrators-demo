using System.Diagnostics;
using Npgsql;

namespace DbDemo.AdoNet;

public static class PoolDemos
{
    /// <summary>Открыть и закрыть 200 подключений без пула и с пулом.</summary>
    public static async Task CostAsync(string cs)
    {
        foreach (var pooling in new[] { false, true })
        {
            var connectionString = new NpgsqlConnectionStringBuilder(cs) { Pooling = pooling }.ConnectionString;
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < 200; i++)
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand("SELECT 1", conn);
                await cmd.ExecuteScalarAsync();
            }

            Console.WriteLine($"Pooling={pooling,-5}: {sw.ElapsedMilliseconds,6} мс");
        }
    }

    /// <summary>После Dispose физическое подключение остаётся открытым и выдаётся снова.</summary>
    public static async Task ReuseAsync(string cs)
    {
        const string app = "dbdemo-reuse";
        var connectionString = new NpgsqlConnectionStringBuilder(cs) { ApplicationName = app }.ConnectionString;

        for (int i = 1; i <= 3; i++)
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            // ProcessID: номер серверного процесса PostgreSQL, обслуживающего подключение
            Console.WriteLine($"Открытие {i}: PID на сервере = {conn.ProcessID}");
        }

        Console.WriteLine($"Подключений '{app}' на сервере после Dispose: {await CountConnectionsAsync(cs, app)}");
    }

    /// <summary>Пул ищется по строке подключения: другая строка, другой пул.</summary>
    public static async Task KeysAsync(string cs)
    {
        const string app = "dbdemo-keys";
        var a = new NpgsqlConnectionStringBuilder(cs) { ApplicationName = app, MaxPoolSize = 100 }.ConnectionString;
        var b = new NpgsqlConnectionStringBuilder(cs) { ApplicationName = app, MaxPoolSize = 50 }.ConnectionString;

        foreach (var (name, connectionString) in new[] { ("A", a), ("B", b), ("A", a), ("B", b) })
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            Console.WriteLine($"Строка {name}: PID на сервере = {conn.ProcessID}");
        }

        Console.WriteLine($"Физических подключений '{app}': {await CountConnectionsAsync(cs, app)}");
    }

    /// <summary>Подключения не возвращаются в пул: шестое ждёт Timeout и падает.</summary>
    public static async Task LeakAsync(string cs)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(cs) { MaxPoolSize = 5, Timeout = 3 }.ConnectionString;
        var leaked = new List<NpgsqlConnection>();

        try
        {
            for (int i = 0; i < 10; i++)
            {
                var conn = new NpgsqlConnection(connectionString);   // нет using: ошибка
                await conn.OpenAsync();
                leaked.Add(conn);
                Console.WriteLine($"Открыто подключение {i}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            foreach (var conn in leaked)
                await conn.DisposeAsync();
        }
    }

    private static async Task<long> CountConnectionsAsync(string cs, string applicationName)
    {
        await using var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT count(*) FROM pg_stat_activity WHERE application_name = @app", conn);
        cmd.Parameters.AddWithValue("app", applicationName);
        return (long)(await cmd.ExecuteScalarAsync())!;
    }
}
