using Dapper;
using DbDemo.Dapper;
using Npgsql;

namespace DbDemo.IntegrationTests;

/// <summary>компилятор такую ошибку не видит, тест видит.</summary>
[Collection(DatabaseCollection.Name)]
public class MappingPitfallTests(DbFixture db) : IAsyncLifetime
{
    public Task InitializeAsync() => db.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Column_with_underscore_is_silently_not_mapped_without_alias()
    {
        var id = await new ProductRepository(db.ConnectionString)
            .AddAsync(new Product { Name = "Мышь", Price = 990m });

        await using var conn = new NpgsqlConnection(db.ConnectionString);

        var wrong = await conn.QuerySingleAsync<Product>(
            "SELECT id, created_at FROM products WHERE id = @id", new { id });
        var right = await conn.QuerySingleAsync<Product>(
            "SELECT id, created_at AS CreatedAt FROM products WHERE id = @id", new { id });

        Assert.Equal(default, wrong.CreatedAt);   // ошибки нет, значение просто не заполнено
        Assert.NotEqual(default, right.CreatedAt);
    }
}
