using System.Reflection;
using DbUp;

// Те же изменения схемы, что и в FluentMigrator, но SQL-скриптами.
//   dotnet run --project DbDemo.DbUp             применить скрипты
//   dotnet run --project DbDemo.DbUp -- list     какие скрипты ещё не выполнены
// История: SELECT * FROM schemaversions;

var cs = Environment.GetEnvironmentVariable("DBDEMO_DBUP_CS")
         ?? "Host=localhost;Port=5432;Database=shop_dbup;Username=demo;Password=demo";

EnsureDatabase.For.PostgresqlDatabase(cs);   // создать базу, если её нет

var upgrader = DeployChanges.To
    .PostgresqlDatabase(cs)
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .WithTransactionPerScript()
    .LogToConsole()
    .Build();

if (args.FirstOrDefault() == "list")
{
    foreach (var script in upgrader.GetScriptsToExecute())
        Console.WriteLine(script.Name);
    return 0;
}

var result = upgrader.PerformUpgrade();
if (!result.Successful)
{
    Console.WriteLine(result.Error);
    return 1;
}

Console.WriteLine("Готово.");
return 0;
