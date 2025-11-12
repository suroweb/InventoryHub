using Microsoft.AspNetCore.Identity;

namespace ServerApp.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    public string FullName => $"{FirstName} {LastName}".Trim();
}
