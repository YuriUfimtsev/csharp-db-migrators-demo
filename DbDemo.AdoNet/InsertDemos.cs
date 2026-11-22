using System.Diagnostics;
using Npgsql;
using NpgsqlTypes;

namespace DbDemo.AdoNet;

/// <summary>Три способа вставить много строк: построчно, пачкой через unnest, bulk через COPY.</summary>
public static class InsertDemos
{
    public static async Task CompareAsync(string cs, int count = 5_000)
    {
        var names = Enumerable.Range(1, count).Select(i => $"bulk-{i}").ToArray();
        var prices = Enumerable.Range(1, count).Select(i => (decimal)i).ToArray();

        await using var conn = new NpgsqlConnection(cs);
        await conn.OpenAsync();

        await MeasureAsync("Построчно      ", conn, async () =>
        {
            for (int i = 0; i < count; i++)
            {
                await using var cmd = new NpgsqlCommand(
                    "INSERT INTO products (name, price) VALUES (@n, @p)", conn);
                cmd.Parameters.AddWithValue("n", names[i]);
                cmd.Parameters.AddWithValue("p", prices[i]);
                await cmd.ExecuteNonQueryAsync();
            }
        });

        await MeasureAsync("Пачкой: unnest ", conn, async () =>
        {
            await using var cmd = new NpgsqlCommand(
                "INSERT INTO products (name, price) SELECT * FROM unnest(@names, @prices)", conn);
            cmd.Parameters.AddWithValue("names", names);
            cmd.Parameters.AddWithValue("prices", prices);
            await cmd.ExecuteNonQueryAsync();
        });

        await MeasureAsync("Bulk: COPY     ", conn, async () =>
        {
            await using var writer = await conn.BeginBinaryImportAsync(
                "COPY products (name, price) FROM STDIN (FORMAT BINARY)");
            for (int i = 0; i < count; i++)
            {
                await writer.StartRowAsync();
                await writer.WriteAsync(names[i], NpgsqlDbType.Varchar);
                await writer.WriteAsync(prices[i], NpgsqlDbType.Numeric);
            }
            await writer.CompleteAsync();
        });
    }

    private static async Task MeasureAsync(string title, NpgsqlConnection conn, Func<Task> insert)
    {
        var sw = Stopwatch.StartNew();
        await insert();
        Console.WriteLine($"{title}: {sw.ElapsedMilliseconds,6} мс");

        await using var cleanup = new NpgsqlCommand("DELETE FROM products WHERE name LIKE 'bulk-%'", conn);
        await cleanup.ExecuteNonQueryAsync();
    }
}
