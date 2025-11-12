using Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace ServerApp.Domain.Entities;

public class Product : BaseEntity
{
    public Guid TenantId { get; set; }

    [Required]
    [StringLength(200)]
    public required string Name { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    public int Stock { get; set; }

    public bool Available { get; set; } = true;

    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }

    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public string? SKU { get; set; }
    public string? Barcode { get; set; }
    public decimal? CostPrice { get; set; }
    public int? ReorderLevel { get; set; }
}
