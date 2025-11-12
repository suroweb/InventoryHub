using Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace ServerApp.Domain.Entities;

public class Supplier : BaseEntity
{
    public Guid TenantId { get; set; }

    [Required]
    [StringLength(200)]
    public required string Name { get; set; }

    [StringLength(100)]
    public string? ContactName { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? Phone { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    // Navigation properties
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
