using Microsoft.EntityFrameworkCore;
using ServerApp.Data.Contexts;
using ServerApp.Domain.Entities;
using Shared.Models;

namespace ServerApp.Services;

public interface ISubscriptionService
{
    Task<Tenant> CreateTenantAsync(string name, string subdomain, string adminEmail, SubscriptionTier tier);
    Task<bool> UpgradeTenantAsync(Guid tenantId, SubscriptionTier newTier);
    Task<bool> ExtendSubscriptionAsync(Guid tenantId, int months);
    Task<bool> CheckUsageLimitsAsync(Guid tenantId, string resourceType);
    Task<TenantUsage> GetTenantUsageAsync(Guid tenantId);
}

public class TenantUsage
{
    public int UserCount { get; set; }
    public int MaxUsers { get; set; }
    public int ProductCount { get; set; }
    public int MaxProducts { get; set; }
    public int ApiCallsToday { get; set; }
    public int ApiRateLimit { get; set; }
}

public class SubscriptionService : ISubscriptionService
{
    private readonly MasterDbContext _masterDbContext;
    private readonly IConfiguration _configuration;

    public SubscriptionService(MasterDbContext masterDbContext, IConfiguration configuration)
    {
        _masterDbContext = masterDbContext;
        _configuration = configuration;
    }

    public async Task<Tenant> CreateTenantAsync(string name, string subdomain, string adminEmail, SubscriptionTier tier)
    {
        // Check if subdomain is already taken
        var existing = await _masterDbContext.Tenants
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain);

        if (existing != null)
        {
            throw new InvalidOperationException($"Subdomain '{subdomain}' is already taken");
        }

        // Generate tenant-specific connection string
        var baseConnectionString = _configuration.GetConnectionString("TenantTemplate")
            ?? "Host=localhost;Database=InventoryHub_{TenantId};Username=postgres;Password=postgres";

        var tenant = new Tenant
        {
            Name = name,
            Subdomain = subdomain.ToLower(),
            ConnectionString = baseConnectionString,
            SubscriptionTier = tier,
            SubscriptionExpiresAt = DateTime.UtcNow.AddMonths(1),
            IsActive = true,
            CompanyEmail = adminEmail
        };

        tenant.UpdateLimitsBasedOnTier();

        // Update connection string with actual tenant ID
        tenant.ConnectionString = tenant.ConnectionString.Replace("{TenantId}", tenant.Id.ToString());

        await _masterDbContext.Tenants.AddAsync(tenant);
        await _masterDbContext.SaveChangesAsync();

        // TODO: Create tenant database and run migrations
        // await CreateTenantDatabaseAsync(tenant);

        return tenant;
    }

    public async Task<bool> UpgradeTenantAsync(Guid tenantId, SubscriptionTier newTier)
    {
        var tenant = await _masterDbContext.Tenants.FindAsync(tenantId);
        if (tenant == null) return false;

        var oldTier = tenant.SubscriptionTier;

        // Only allow upgrades
        if (newTier <= oldTier)
        {
            throw new InvalidOperationException("Can only upgrade to a higher tier");
        }

        tenant.SubscriptionTier = newTier;
        tenant.UpdateLimitsBasedOnTier();
        tenant.UpdatedAt = DateTime.UtcNow;

        await _masterDbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ExtendSubscriptionAsync(Guid tenantId, int months)
    {
        var tenant = await _masterDbContext.Tenants.FindAsync(tenantId);
        if (tenant == null) return false;

        // Extend from current expiration or now, whichever is later
        var baseDate = tenant.SubscriptionExpiresAt > DateTime.UtcNow
            ? tenant.SubscriptionExpiresAt
            : DateTime.UtcNow;

        tenant.SubscriptionExpiresAt = baseDate.AddMonths(months);
        tenant.UpdatedAt = DateTime.UtcNow;

        await _masterDbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CheckUsageLimitsAsync(Guid tenantId, string resourceType)
    {
        var tenant = await _masterDbContext.Tenants.FindAsync(tenantId);
        if (tenant == null) return false;

        var usage = await GetTenantUsageAsync(tenantId);

        return resourceType.ToLower() switch
        {
            "user" => usage.UserCount < usage.MaxUsers,
            "product" => usage.ProductCount < usage.MaxProducts,
            _ => true
        };
    }

    public async Task<TenantUsage> GetTenantUsageAsync(Guid tenantId)
    {
        var tenant = await _masterDbContext.Tenants.FindAsync(tenantId);
        if (tenant == null)
        {
            throw new InvalidOperationException("Tenant not found");
        }

        var userCount = await _masterDbContext.Users.CountAsync(u => u.TenantId == tenantId);

        // Note: For product count, we'd need to connect to tenant DB
        // This is a simplified version
        return new TenantUsage
        {
            UserCount = userCount,
            MaxUsers = tenant.MaxUsers,
            ProductCount = 0, // TODO: Query tenant database
            MaxProducts = tenant.MaxProducts,
            ApiCallsToday = 0, // TODO: Implement API call tracking
            ApiRateLimit = tenant.ApiRateLimit
        };
    }
}
