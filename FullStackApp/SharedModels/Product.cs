namespace InventoryHub.Shared.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Price { get; set; }
    public int Stock { get; set; }
    public bool Available { get; set; }
    public Category Category { get; set; } = new();
    public Supplier Supplier { get; set; } = new();
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class Supplier
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}
