namespace DbDemo.EfCore;

// Навигационные свойства virtual: это нужно только для сценария с ленивой загрузкой (прокси).

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public virtual List<Product> Products { get; set; } = [];
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int? CategoryId { get; set; }
    public virtual Category? Category { get; set; }

    //   dotnet ef migrations add AddProductRating
    // public int Rating { get; set; }
}
