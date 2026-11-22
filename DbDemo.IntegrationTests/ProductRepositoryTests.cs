using DbDemo.Dapper;
using Npgsql;

namespace DbDemo.IntegrationTests;

[Collection(DatabaseCollection.Name)]
public class ProductRepositoryTests(DbFixture db) : IAsyncLifetime
{
    private readonly ProductRepository _repo = new(db.ConnectionString);

    public Task InitializeAsync() => db.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Add_then_GetById_returns_same_product()
    {
        var id = await _repo.AddAsync(new Product { Name = "Мышь", Price = 990m });

        var loaded = await _repo.GetByIdAsync(id);

        Assert.NotNull(loaded);
        Assert.Equal("Мышь", loaded.Name);
        Assert.Equal(990m, loaded.Price);
        Assert.NotEqual(default, loaded.CreatedAt);   // значение по умолчанию из базы дошло до объекта
    }

    [Fact]
    public async Task GetById_returns_null_for_missing_id()
    {
        Assert.Null(await _repo.GetByIdAsync(12345));
    }

    [Fact]
    public async Task UpdatePrice_changes_only_target_row()
    {
        var a = await _repo.AddAsync(new Product { Name = "A", Price = 100m });
        var b = await _repo.AddAsync(new Product { Name = "B", Price = 200m });

        var affected = await _repo.UpdatePriceAsync(a, 150m);

        Assert.Equal(1, affected);
        Assert.Equal(150m, (await _repo.GetByIdAsync(a))!.Price);
        Assert.Equal(200m, (await _repo.GetByIdAsync(b))!.Price);
    }

    [Fact]
    public async Task Delete_removes_row()
    {
        var id = await _repo.AddAsync(new Product { Name = "Мышь", Price = 990m });

        Assert.Equal(1, await _repo.DeleteAsync(id));
        Assert.Null(await _repo.GetByIdAsync(id));
    }

    [Fact]
    public async Task FindByName_is_case_insensitive_substring_search()
    {
        await _repo.AddAsync(new Product { Name = "Мышь", Price = 990m });
        await _repo.AddAsync(new Product { Name = "Клавиатура", Price = 2490m });

        var found = await _repo.FindByNameAsync("мыш");

        Assert.Single(found);
        Assert.Equal("Мышь", found[0].Name);
    }

    [Fact]
    public async Task FindByName_treats_input_as_value_not_sql()
    {
        await _repo.AddAsync(new Product { Name = "Мышь", Price = 990m });
        await _repo.AddAsync(new Product { Name = "Клавиатура", Price = 2490m });

        var found = await _repo.FindByNameAsync("' OR '1'='1");

        Assert.Empty(found);
    }

    [Fact]
    public async Task GetWithCategory_maps_both_objects()
    {
        var categoryId = await _repo.AddCategoryAsync("Периферия");
        await _repo.AddAsync(new Product { Name = "Мышь", Price = 990m, CategoryId = categoryId });

        var list = await _repo.GetWithCategoryAsync();

        var product = Assert.Single(list);
        Assert.Equal("Мышь", product.Name);
        Assert.NotNull(product.Category);
        Assert.Equal(categoryId, product.Category.Id);
        Assert.Equal("Периферия", product.Category.Name);
    }

    [Fact]
    public async Task Duplicate_category_name_violates_unique_constraint()
    {
        await _repo.AddCategoryAsync("Периферия");

        var ex = await Assert.ThrowsAsync<PostgresException>(() => _repo.AddCategoryAsync("Периферия"));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, ex.SqlState);
    }

    [Fact]
    public async Task Product_with_unknown_category_violates_foreign_key()
    {
        var ex = await Assert.ThrowsAsync<PostgresException>(() =>
            _repo.AddAsync(new Product { Name = "Мышь", Price = 990m, CategoryId = 999 }));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, ex.SqlState);
    }

    [Fact]
    public async Task AddWithNewCategory_saves_both_in_one_transaction()
    {
        var id = await _repo.AddWithNewCategoryAsync(new Product { Name = "SSD", Price = 6990m }, "Хранение");

        var product = Assert.Single(await _repo.GetWithCategoryAsync());
        Assert.Equal(id, product.Id);
        Assert.Equal("Хранение", product.Category!.Name);
    }

    [Fact]
    public async Task AddMany_inserts_all_rows_in_one_query()
    {
        var products = Enumerable.Range(1, 100)
            .Select(i => new Product { Name = $"Товар {i}", Price = i })
            .ToArray();

        var ids = await _repo.AddManyAsync(products);

        Assert.Equal(100, ids.Length);
        Assert.All(ids, id => Assert.True(id > 0));
        var first = await _repo.GetByIdAsync(ids[0]);
        Assert.Equal("Товар 1", first!.Name);
    }

    [Fact]
    public async Task Search_combines_only_given_conditions()
    {
        var periphery = await _repo.AddCategoryAsync("Периферия");
        var monitors = await _repo.AddCategoryAsync("Мониторы");
        await _repo.AddAsync(new Product { Name = "Мышь", Price = 990m, CategoryId = periphery });
        await _repo.AddAsync(new Product { Name = "Клавиатура", Price = 2490m, CategoryId = periphery });
        await _repo.AddAsync(new Product { Name = "Монитор", Price = 21990m, CategoryId = monitors });

        Assert.Equal(3, (await _repo.SearchAsync(new ProductFilter())).Length);
        Assert.Equal(2, (await _repo.SearchAsync(new ProductFilter(CategoryIds: [periphery]))).Length);
        var cheap = Assert.Single(await _repo.SearchAsync(new ProductFilter([periphery], MaxPrice: 1000m)));
        Assert.Equal("Мышь", cheap.Name);
    }
}
