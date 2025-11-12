using Shared.Models;

namespace ServerApp.Domain.Entities;

public class Tenant : BaseEntity
{
    public required string Name { get; set; }
    public required string Subdomain { get; set; }
    public required string ConnectionString { get; set; }
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Free;
    public DateTime SubscriptionExpiresAt { get; set; } = DateTime.UtcNow.AddMonths(1);
    public bool IsActive { get; set; } = true;
    public string? CompanyEmail { get; set; }
    public string? CompanyPhone { get; set; }
    public int MaxUsers { get; set; }
    public int MaxProducts { get; set; }
    public int ApiRateLimit { get; set; }

    // Navigation properties
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();

    public bool IsSubscriptionActive() => IsActive && SubscriptionExpiresAt > DateTime.UtcNow;

    public void UpdateLimitsBasedOnTier()
    {
        MaxUsers = SubscriptionTierLimits.GetMaxUsers(SubscriptionTier);
        MaxProducts = SubscriptionTierLimits.GetMaxProducts(SubscriptionTier);
        ApiRateLimit = SubscriptionTierLimits.GetApiRateLimitPerMinute(SubscriptionTier);
    }
}
