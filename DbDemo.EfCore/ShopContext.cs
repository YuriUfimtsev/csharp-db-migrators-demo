using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;

namespace DbDemo.EfCore;

public class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Category>(e =>
        {
            e.ToTable("categories");
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.HasIndex(c => c.Name).IsUnique();
        });

        b.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            e.Property(p => p.Price).HasColumnName("price").HasPrecision(10, 2);
            e.Property(p => p.CategoryId).HasColumnName("category_id");
            e.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId);
            // e.Property(p => p.Rating).HasColumnName("rating");
        });
    }
}

public static class Db
{
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("DBDEMO_EF_CS")
        ?? "Host=localhost;Port=5432;Database=shop_ef;Username=demo;Password=demo";

    /// <param name="log">печатать SQL, который уходит в базу</param>
    /// <param name="lazy">включить ленивую загрузку через прокси</param>
    public static ShopContext Create(bool log = true, bool lazy = false)
    {
        var options = new DbContextOptionsBuilder<ShopContext>()
            .UseNpgsql(ConnectionString);

        if (lazy)
            options.UseLazyLoadingProxies();

        if (log)
            options
                .LogTo(Console.WriteLine, new[] { DbLoggerCategory.Database.Command.Name }, LogLevel.Information)
                .EnableSensitiveDataLogging();   // значения параметров, только для разработки

        return new ShopContext(options.Options);
    }
}

/// <summary>Нужна инструменту dotnet ef, чтобы создать контекст при генерации миграций.</summary>
public class DesignTimeFactory : IDesignTimeDbContextFactory<ShopContext>
{
    public ShopContext CreateDbContext(string[] args) => Db.Create(log: false);
}
