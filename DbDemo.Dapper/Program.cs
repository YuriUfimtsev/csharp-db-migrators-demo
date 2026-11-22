using DbDemo.Dapper;

//   dotnet run --project DbDemo.Dapper
// Перед запуском: docker compose up -d и dotnet run --project DbDemo.Migrations

var cs = Environment.GetEnvironmentVariable("DBDEMO_CS")
         ?? "Host=localhost;Port=5432;Database=shop;Username=demo;Password=demo";

var repo = new ProductRepository(cs);

Console.WriteLine("== Товары с категориями (multi-mapping) ==");
foreach (var p in await repo.GetWithCategoryAsync())
    Console.WriteLine($"{p.Id,3}  {p.Name,-16} {p.Price,10:N2}  {p.Category?.Name}");

Console.WriteLine("\n== Добавление, чтение, изменение, удаление ==");
var id = await repo.AddAsync(new Product { Name = "Наушники", Price = 4990m });
var loaded = await repo.GetByIdAsync(id);
Console.WriteLine($"Добавлен: {loaded!.Id} {loaded.Name} {loaded.Price:N2}, создан {loaded.CreatedAt:u}");

await repo.UpdatePriceAsync(id, 4490m);
Console.WriteLine($"Новая цена: {(await repo.GetByIdAsync(id))!.Price:N2}");

Console.WriteLine($"Удалено строк: {await repo.DeleteAsync(id)}");

Console.WriteLine("\n== Поиск: ввод пользователя передаётся параметром ==");
foreach (var input in new[] { "мы", "' OR '1'='1" })
    Console.WriteLine($"'{input}': найдено {(await repo.FindByNameAsync(input)).Count}");
