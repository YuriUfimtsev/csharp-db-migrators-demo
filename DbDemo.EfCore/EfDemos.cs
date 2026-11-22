using Microsoft.EntityFrameworkCore;

namespace DbDemo.EfCore;

public static class EfDemos
{
    public static async Task SeedAsync()
    {
        await using var db = Db.Create(log: false);
        if (await db.Categories.AnyAsync())
        {
            Console.WriteLine("Данные уже есть.");
            return;
        }

        var periphery = new Category { Name = "Периферия" };
        var monitors = new Category { Name = "Мониторы" };
        var storage = new Category { Name = "Хранение данных" };

        db.Products.AddRange(
            new Product { Name = "Мышь", Price = 990, Category = periphery },
            new Product { Name = "Клавиатура", Price = 2490, Category = periphery },
            new Product { Name = "Монитор 27\"", Price = 21990, Category = monitors },
            new Product { Name = "SSD 1 ТБ", Price = 6990, Category = storage },
            new Product { Name = "Флешка 64 ГБ", Price = 590, Category = storage });

        await db.SaveChangesAsync();
        Console.WriteLine("Добавлено 3 категории и 5 товаров.");
    }

    public static async Task QueryAsync()
    {
        await using var db = Db.Create();

        var query = db.Products
            .Where(p => p.Price < 5000)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Name, p.Price })
            .Take(10);

        Console.WriteLine("ToQueryString:\n" + query.ToQueryString() + "\n");

        foreach (var row in await query.ToListAsync())
            Console.WriteLine($"{row.Name,-16} {row.Price,10:N2}");
    }

    public static async Task DeferredAsync()
    {
        await using var db = Db.Create();

        var query = db.Products.Where(p => p.Price > 1000);
        Console.WriteLine("Запрос построен, в базу ещё ничего не ушло.\n");

        Console.WriteLine($"CountAsync: {await query.CountAsync()}\n");

        Console.WriteLine("Теперь foreach, ещё один запрос:");
        await foreach (var p in query.AsAsyncEnumerable())
            Console.WriteLine($"  {p.Name}");
    }

    public static async Task NPlusOneAsync()
    {
        Console.WriteLine("== Ленивая загрузка: смотрите, сколько SELECT ушло ==");
        await using (var db = Db.Create(lazy: true))
        {
            foreach (var p in await db.Products.ToListAsync())
                Console.WriteLine($"{p.Name} -> {p.Category?.Name}");
        }

        Console.WriteLine("\n== Include: один запрос с JOIN ==");
        await using (var db = Db.Create())
        {
            var products = await db.Products.Include(p => p.Category).ToListAsync();
            foreach (var p in products)
                Console.WriteLine($"{p.Name} -> {p.Category?.Name}");
        }
    }

    public static async Task TrackingAsync()
    {
        await using var db = Db.Create();

        var p = await db.Products.OrderBy(x => x.Id).FirstAsync();
        var entry = db.Entry(p);
        Console.WriteLine($"\nПосле загрузки: {entry.State}");

        var oldPrice = p.Price;
        p.Price = oldPrice + 100;

        entry = db.Entry(p);   // Entry вызывает DetectChanges для этой сущности
        Console.WriteLine($"После изменения: {entry.State}");
        Console.WriteLine($"Price: исходное = {entry.Property(x => x.Price).OriginalValue}, " +
                          $"текущее = {entry.Property(x => x.Price).CurrentValue}, " +
                          $"изменено = {entry.Property(x => x.Price).IsModified}");
        Console.WriteLine($"Name изменено = {entry.Property(x => x.Name).IsModified}\n");

        await db.SaveChangesAsync();   // в UPDATE попадёт только price
        Console.WriteLine($"\nПосле SaveChanges: {entry.State}");

        p.Price = oldPrice;            // вернуть как было
        await db.SaveChangesAsync();
    }

    public static async Task SameValueAsync()
    {
        await using var db = Db.Create();
        var p = await db.Products.OrderBy(x => x.Id).FirstAsync();
        var oldName = p.Name;

        p.Price = p.Price;
        p.Name = oldName + " (новое имя)";
        await db.SaveChangesAsync();   // UPDATE только name

        p.Name = oldName;
        await db.SaveChangesAsync();
    }

    public static async Task ModifyAsync()
    {
        await using var db = Db.Create();

        var added = new Product { Name = "Веб-камера", Price = 3490 };
        db.Products.Add(added);
        Console.WriteLine($"До SaveChanges: Id = {added.Id}");

        await db.SaveChangesAsync();
        Console.WriteLine($"После SaveChanges: Id = {added.Id}\n");

        db.Products.Remove(added);
        Console.WriteLine($"После Remove: {db.Entry(added).State}");
        await db.SaveChangesAsync();
        Console.WriteLine($"После SaveChanges: {db.Entry(added).State}");
    }

    public static async Task BulkAsync()
    {
        await using var db = Db.Create();

        var updated = await db.Products
            .Where(p => p.Category!.Name == "Периферия")
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Price, p => p.Price * 0.9m));
        Console.WriteLine($"ExecuteUpdate: обновлено {updated}\n");

        db.Products.Add(new Product { Name = "Бракованный товар", Price = 0 });
        await db.SaveChangesAsync();

        var deleted = await db.Products.Where(p => p.Price == 0).ExecuteDeleteAsync();
        Console.WriteLine($"ExecuteDelete: удалено {deleted}");

        // вернуть цены
        await db.Products
            .Where(p => p.Category!.Name == "Периферия")
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Price, p => Math.Round(p.Price / 0.9m)));
    }

    public static async Task NoTrackingAsync()
    {
        await using var db = Db.Create();

        var p = await db.Products.AsNoTracking().OrderBy(x => x.Id).FirstAsync();
        p.Price = 0;

        int n = await db.SaveChangesAsync();
        Console.WriteLine($"\nSaveChanges вернул {n}: объект не отслеживается, снимка нет.");
    }

    public static async Task RawSqlAsync()
    {
        await using var db = Db.Create();

        var min = 1000m;
        var list = await db.Products
            .FromSql($"SELECT * FROM products WHERE price > {min}")
            .OrderBy(p => p.Price)   // можно продолжать LINQ поверх своего SQL
            .ToListAsync();

        foreach (var p in list)
            Console.WriteLine($"{p.Name,-16} {p.Price,10:N2}");
    }

    public static async Task MigrationsAsync()
    {
        await using var db = Db.Create(log: false);

        Console.WriteLine("Применённые (из __EFMigrationsHistory):");
        foreach (var m in await db.Database.GetAppliedMigrationsAsync())
            Console.WriteLine($"  {m}");

        Console.WriteLine("Ожидающие:");
        foreach (var m in await db.Database.GetPendingMigrationsAsync())
            Console.WriteLine($"  {m}");
    }
}
