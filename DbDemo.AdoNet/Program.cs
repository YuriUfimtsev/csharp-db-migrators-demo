using DbDemo.AdoNet;

// Запуск:
//   dotnet run --project DbDemo.AdoNet -- <сценарий>
// Перед запуском: docker compose up -d и dotnet run --project DbDemo.Migrations

var cs = Environment.GetEnvironmentVariable("DBDEMO_CS")
         ?? "Host=localhost;Port=5432;Database=shop;Username=demo;Password=demo";

var scenario = args.FirstOrDefault() ?? "help";

switch (scenario)
{
    // 02. Транзакции и изоляция
    case "lost-update":     await IsolationDemos.LostUpdateAsync(cs); break;
    case "dirty-read":      await IsolationDemos.DirtyReadAsync(cs); break;
    case "non-repeatable":  await IsolationDemos.NonRepeatableReadAsync(cs); break;
    case "phantom":         await IsolationDemos.PhantomAsync(cs); break;
    case "serialization":   await IsolationDemos.SerializationAsync(cs); break;

    // 03. Пул подключений
    case "pool-cost":   await PoolDemos.CostAsync(cs); break;
    case "pool-reuse":  await PoolDemos.ReuseAsync(cs); break;
    case "pool-keys":   await PoolDemos.KeysAsync(cs); break;
    case "pool-leak":   await PoolDemos.LeakAsync(cs); break;

    // 04. ADO.NET
    case "read":        await AdoDemos.ReadAsync(cs); break;
    case "commands":    await AdoDemos.CommandsAsync(cs); break;
    case "injection":   await AdoDemos.InjectionAsync(cs, args.ElementAtOrDefault(1) ?? "' OR '1'='1"); break;
    case "tx":          await AdoDemos.TransactionAsync(cs); break;
    case "datasource":  await AdoDemos.DataSourceAsync(cs); break;
    case "insert-modes": await InsertDemos.CompareAsync(cs); break;

    default:
        Console.WriteLine("""
            Сценарии:
              lost-update     потерянное обновление и как его избежать
              dirty-read      Read Uncommitted в PostgreSQL
              non-repeatable  неповторяющееся чтение: Read Committed и Repeatable Read
              phantom         фантомное чтение: Read Committed и Repeatable Read
              serialization   аномалия сериализации: Repeatable Read и Serializable
              pool-cost    время 200 подключений без пула и с пулом
              pool-reuse   один и тот же серверный процесс после Dispose
              pool-keys    разные строки подключения дают разные пулы
              pool-leak    исчерпание пула без Dispose
              read         ExecuteReader и ручной маппинг
              commands     ExecuteScalar и ExecuteNonQuery
              injection    SQL-инъекция и параметры (можно передать свою строку)
              tx           транзакция без Commit откатывается
              datasource   NpgsqlDataSource
              insert-modes 5000 строк: построчно, unnest, COPY
            """);
        break;
}
