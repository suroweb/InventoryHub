using Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace ServerApp.Domain.Entities;

public class Category : BaseEntity
{
    public Guid TenantId { get; set; }

    [Required]
    [StringLength(100)]
    public required string Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    // Navigation properties
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
