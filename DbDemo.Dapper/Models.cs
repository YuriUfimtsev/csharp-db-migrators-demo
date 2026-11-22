namespace DbDemo.Dapper;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int? CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int Rating { get; set; }

    public Category? Category { get; set; }
}

public record ProductFilter(int[]? CategoryIds = null, decimal? MaxPrice = null);
