using System.ComponentModel.DataAnnotations;
using Shared.Models;

namespace Shared.DTOs;

public class CreateTenantRequest
{
    [Required]
    [StringLength(200)]
    public required string Name { get; set; }

    [Required]
    [StringLength(100)]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Subdomain must contain only lowercase letters, numbers, and hyphens")]
    public required string Subdomain { get; set; }

    [Required]
    [EmailAddress]
    public required string AdminEmail { get; set; }

    [Required]
    public required string AdminPassword { get; set; }

    [Required]
    public required string AdminFirstName { get; set; }

    [Required]
    public required string AdminLastName { get; set; }

    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Free;
}

public class TenantDTO
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Subdomain { get; set; }
    public SubscriptionTier SubscriptionTier { get; set; }
    public DateTime SubscriptionExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public int MaxUsers { get; set; }
    public int MaxProducts { get; set; }
    public DateTime CreatedAt { get; set; }
}
