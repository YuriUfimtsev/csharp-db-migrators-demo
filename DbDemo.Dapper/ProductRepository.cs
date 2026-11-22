using System.Text;
using Dapper;
using Npgsql;

namespace DbDemo.Dapper;

/// <summary>Репозиторий на Dapper. SQL пишем сами, маппинг делает Dapper.</summary>
public class ProductRepository(string connectionString)
{
    // Столбцы с подчёркиванием не совпадают с именами свойств, поэтому нужны псевдонимы.
    // Без них CreatedAt и CategoryId останутся значениями по умолчанию (задача 6).
    private const string Columns =
        "id, name, price, category_id AS CategoryId, created_at AS CreatedAt, rating";

    // Подключение закрыто: Dapper сам откроет его на время команды и закроет.
    private NpgsqlConnection Connection() => new(connectionString);

    public async Task<int> AddAsync(Product product)
    {
        await using var conn = Connection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO products (name, price, category_id) VALUES (@Name, @Price, @CategoryId) RETURNING id",
            new { product.Name, product.Price, product.CategoryId });
    }

    /// <summary>Пачка товаров одним запросом: массивы передаются параметрами и разворачиваются unnest.</summary>
    public async Task<int[]> AddManyAsync(IReadOnlyCollection<Product> products)
    {
        await using var conn = Connection();
        var ids = await conn.QueryAsync<int>("""
            INSERT INTO products (name, price)
            SELECT * FROM unnest(@Names, @Prices)
            RETURNING id
            """,
            new
            {
                Names = products.Select(p => p.Name).ToArray(),
                Prices = products.Select(p => p.Price).ToArray(),
            });
        return ids.ToArray();   // материализуем до закрытия подключения
    }

    /// <summary>Динамические условия: текст собирается из констант, значения только параметрами.</summary>
    public async Task<Product[]> SearchAsync(ProductFilter filter)
    {
        var sql = new StringBuilder($"SELECT {Columns} FROM products");
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (filter.CategoryIds is { Length: > 0 })
        {
            conditions.Add("category_id = ANY(@CategoryIds)");
            parameters.Add("CategoryIds", filter.CategoryIds);
        }
        if (filter.MaxPrice is not null)
        {
            conditions.Add("price <= @MaxPrice");
            parameters.Add("MaxPrice", filter.MaxPrice);
        }
        if (conditions.Count > 0)
            sql.Append(" WHERE ").AppendJoin(" AND ", conditions);
        sql.Append(" ORDER BY id");

        await using var conn = Connection();
        var rows = await conn.QueryAsync<Product>(sql.ToString(), parameters);
        return rows.ToArray();
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        await using var conn = Connection();
        return await conn.QuerySingleOrDefaultAsync<Product>(
            $"SELECT {Columns} FROM products WHERE id = @id",   // в строку вставлена константа, не ввод
            new { id });
    }

    public async Task<IReadOnlyList<Product>> FindByNameAsync(string text)
    {
        await using var conn = Connection();
        var rows = await conn.QueryAsync<Product>(
            $"SELECT {Columns} FROM products WHERE name ILIKE '%' || @text || '%' ORDER BY id",
            new { text });
        return rows.AsList();
    }

    /// <summary>Multi-mapping: товар и его категория из одного запроса.</summary>
    public async Task<IReadOnlyList<Product>> GetWithCategoryAsync()
    {
        const string sql = """
            SELECT p.id, p.name, p.price, p.category_id AS CategoryId, p.created_at AS CreatedAt, p.rating,
                   c.id, c.name
            FROM products p
            JOIN categories c ON c.id = p.category_id
            ORDER BY p.id
            """;

        await using var conn = Connection();
        var rows = await conn.QueryAsync<Product, Category, Product>(
            sql,
            (p, c) => { p.Category = c; return p; },
            splitOn: "id");
        return rows.AsList();
    }

    public async Task<int> UpdatePriceAsync(int id, decimal price)
    {
        await using var conn = Connection();
        return await conn.ExecuteAsync(
            "UPDATE products SET price = @price WHERE id = @id", new { id, price });
    }

    public async Task<int> DeleteAsync(int id)
    {
        await using var conn = Connection();
        return await conn.ExecuteAsync("DELETE FROM products WHERE id = @id", new { id });
    }

    public async Task<int> AddCategoryAsync(string name)
    {
        await using var conn = Connection();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO categories (name) VALUES (@name) RETURNING id", new { name });
    }

    /// <summary>Два действия в одной транзакции: категория и товар в ней.</summary>
    public async Task<int> AddWithNewCategoryAsync(Product product, string categoryName)
    {
        await using var conn = Connection();
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        var categoryId = await conn.ExecuteScalarAsync<int>(
            "INSERT INTO categories (name) VALUES (@categoryName) RETURNING id",
            new { categoryName }, tx);

        var id = await conn.ExecuteScalarAsync<int>(
            "INSERT INTO products (name, price, category_id) VALUES (@Name, @Price, @categoryId) RETURNING id",
            new { product.Name, product.Price, categoryId }, tx);

        await tx.CommitAsync();
        return id;
    }
}
