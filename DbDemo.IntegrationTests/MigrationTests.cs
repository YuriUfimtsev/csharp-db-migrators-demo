using System.Reflection;
using Dapper;
using DbDemo.Migrations;
using FluentMigrator;
using Npgsql;

namespace DbDemo.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class MigrationTests(DbFixture db)
{
    [Fact]
    public async Task All_migrations_are_recorded_in_VersionInfo()
    {
        var expected = typeof(Migrator).Assembly.GetTypes()
            .Select(t => t.GetCustomAttribute<MigrationAttribute>())
            .OfType<MigrationAttribute>()
            .Select(a => a.Version)
            .Order()
            .ToList();

        await using var conn = new NpgsqlConnection(db.ConnectionString);
        var applied = (await conn.QueryAsync<long>(
            "SELECT \"Version\" FROM \"VersionInfo\" ORDER BY \"Version\"")).ToList();

        Assert.Equal(expected, applied);
    }

    [Fact]
    public async Task Rating_migration_can_be_rolled_back_and_applied_again()
    {
        Migrator.MigrateDown(db.ConnectionString, Versions.SeedData);
        Assert.False(await ColumnExistsAsync("products", "rating"));

        Migrator.MigrateUp(db.ConnectionString);
        Assert.True(await ColumnExistsAsync("products", "rating"));
    }

    [Fact]
    public async Task All_migrations_go_down_to_zero_and_up_again()
    {
        Migrator.MigrateDown(db.ConnectionString, 0);
        Assert.False(await TableExistsAsync("products"));

        Migrator.MigrateUp(db.ConnectionString);
        Assert.True(await TableExistsAsync("products"));
        Assert.True(await TableExistsAsync("accounts"));
    }

    private async Task<bool> TableExistsAsync(string table)
    {
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        return await conn.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = @table)", new { table });
    }

    private async Task<bool> ColumnExistsAsync(string table, string column)
    {
        await using var conn = new NpgsqlConnection(db.ConnectionString);
        return await conn.ExecuteScalarAsync<bool>("""
            SELECT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_name = @table AND column_name = @column)
            """, new { table, column });
    }
}
