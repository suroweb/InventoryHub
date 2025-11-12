using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs;

public class ProductDTO
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool Available { get; set; }
    public CategoryDTO? Category { get; set; }
    public SupplierDTO? Supplier { get; set; }
    public string? SKU { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProductRequest
{
    [Required]
    [StringLength(200)]
    public required string Name { get; set; }

    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    public int Stock { get; set; }

    public bool Available { get; set; } = true;

    [Required]
    public Guid CategoryId { get; set; }

    [Required]
    public Guid SupplierId { get; set; }

    public string? SKU { get; set; }
    public decimal? CostPrice { get; set; }
}

public class UpdateProductRequest : CreateProductRequest
{
}

public class CategoryDTO
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public class SupplierDTO
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Country { get; set; }
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
