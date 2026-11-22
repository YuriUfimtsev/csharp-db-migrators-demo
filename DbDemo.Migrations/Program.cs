using DbDemo.Migrations;

// dotnet run --project DbDemo.Migrations             применить все миграции
// dotnet run --project DbDemo.Migrations -- list     список и статус
// dotnet run --project DbDemo.Migrations -- down 202611010920   откатить новее версии

var cs = Environment.GetEnvironmentVariable("DBDEMO_CS")
         ?? "Host=localhost;Port=5432;Database=shop;Username=demo;Password=demo";

switch (args.FirstOrDefault())
{
    case "list":
        Migrator.List(cs);
        break;
    case "down" when args.Length > 1:
        Migrator.MigrateDown(cs, long.Parse(args[1]));
        break;
    default:
        Migrator.MigrateUp(cs);
        break;
}

// История применённых миграций:
//   SELECT * FROM "VersionInfo" ORDER BY "Version";
