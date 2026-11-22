using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;

namespace DbDemo.Migrations;

/// <summary>Запуск миграций FluentMigrator. Используется приложением и интеграционными тестами.</summary>
public static class Migrator
{
    public static void MigrateUp(string connectionString)
    {
        using var services = Build(connectionString);
        services.GetRequiredService<IMigrationRunner>().MigrateUp();
    }

    /// <summary>Откатить все миграции новее указанной версии.</summary>
    public static void MigrateDown(string connectionString, long version)
    {
        using var services = Build(connectionString);
        services.GetRequiredService<IMigrationRunner>().MigrateDown(version);
    }

    public static void List(string connectionString)
    {
        using var services = Build(connectionString);
        services.GetRequiredService<IMigrationRunner>().ListMigrations();
    }

    private static ServiceProvider Build(string connectionString) =>
        new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(r => r
                .AddPostgres()
                .WithGlobalConnectionString(connectionString)
                .ScanIn(typeof(Migrator).Assembly).For.Migrations())
            .AddLogging(l => l.AddFluentMigratorConsole())
            .BuildServiceProvider(false);
}
