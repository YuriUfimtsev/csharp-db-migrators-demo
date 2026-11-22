using DbDemo.EfCore;

// Запуск:
//   dotnet run --project DbDemo.EfCore -- <сценарий>
// Перед первым запуском создайте базу миграциями EF (см. README):
//   dotnet ef migrations add Initial --project DbDemo.EfCore
//   dotnet ef database update --project DbDemo.EfCore

var scenario = args.FirstOrDefault() ?? "help";

switch (scenario)
{
    case "seed":         await EfDemos.SeedAsync(); break;
    case "query":        await EfDemos.QueryAsync(); break;
    case "deferred":     await EfDemos.DeferredAsync(); break;
    case "n1":           await EfDemos.NPlusOneAsync(); break;
    case "tracking":     await EfDemos.TrackingAsync(); break;
    case "same-value":   await EfDemos.SameValueAsync(); break;
    case "modify":       await EfDemos.ModifyAsync(); break;
    case "bulk":         await EfDemos.BulkAsync(); break;
    case "no-tracking":  await EfDemos.NoTrackingAsync(); break;
    case "raw-sql":      await EfDemos.RawSqlAsync(); break;
    case "migrations":   await EfDemos.MigrationsAsync(); break;

    default:
        Console.WriteLine("""
            Сценарии:
              seed         заполнить базу тестовыми данными
              query        LINQ, ToQueryString и сгенерированный SQL
              deferred     когда запрос уходит в базу
              n1           ленивая загрузка и N+1, затем Include
              tracking     ChangeTracker, снимок и UPDATE изменённых столбцов
              same-value   присвоение того же значения (задача 4)
              modify       Add, Remove и один SaveChanges
              bulk         ExecuteUpdate и ExecuteDelete
              no-tracking  AsNoTracking и SaveChanges (задача 5)
              raw-sql      FromSql с интерполяцией
              migrations   применённые и ожидающие миграции
            """);
        break;
}
