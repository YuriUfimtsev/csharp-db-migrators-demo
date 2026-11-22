using DbDemo.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;

namespace DbDemo.IntegrationTests;

/// <summary>
/// Один контейнер PostgreSQL на все тесты коллекции.
/// Схема создаётся теми же миграциями, что и в продакшне.
/// </summary>
public sealed class DbFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Migrator.MigrateUp(ConnectionString);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    /// <summary>Очистить данные перед тестом. Таблица истории миграций не трогается.</summary>
    public async Task ResetAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "TRUNCATE products, categories RESTART IDENTITY CASCADE", conn);
        await cmd.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(Name)]
public class DatabaseCollection : ICollectionFixture<DbFixture>
{
    public const string Name = "database";
}
