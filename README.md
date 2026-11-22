# DbDemo -- работа с базами данных в .NET

| Проект | Демонстрирует                                                                                 |
|---|-----------------------------------------------------------------------------------------------|
| `DbDemo.AdoNet` | аномалии и уровни изоляции, пул подключений, ADO.NET, инъекции, NpgsqlDataSource              |
| `DbDemo.EfCore` | генерация SQL, отложенное выполнение, N+1, ChangeTracker и снимки, ExecuteUpdate, миграции EF |
| `DbDemo.Dapper` | репозиторий на Dapper, multi-mapping, транзакция                                              |
| `DbDemo.Migrations` | миграции FluentMigrator, таблица `VersionInfo`                                                |
| `DbDemo.DbUp` | те же изменения схемы SQL-скриптами, таблица `schemaversions`                                 |
| `DbDemo.IntegrationTests` | xUnit + Testcontainers: репозиторий, ограничения, маппинг, миграции                           |

## Требования

- .NET SDK 10
- Docker (для базы и для интеграционных тестов)
- для миграций EF: `dotnet tool install --global dotnet-ef`

Версии всех пакетов собраны в `Directory.Packages.props`.

## Быстрый старт

```bash
docker compose up -d                                # PostgreSQL: localhost:5432, demo/demo, база shop
dotnet run --project DbDemo.Migrations          # схема и данные через FluentMigrator
dotnet run --project DbDemo.AdoNet -- help      # список сценариев
```

Строки подключения по умолчанию заданы в коде. Переопределить можно переменными окружения
`DBDEMO_CS` (база `shop`), `DBDEMO_EF_CS` (`shop_ef`), `DBDEMO_DBUP_CS` (`shop_dbup`).
