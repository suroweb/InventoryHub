using Microsoft.EntityFrameworkCore;
using ServerApp.Data.Contexts;

namespace ServerApp.Services;

public class TenantService : ITenantService
{
    private Guid _tenantId;
    private readonly MasterDbContext _masterDbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantService(MasterDbContext masterDbContext, IHttpContextAccessor httpContextAccessor)
    {
        _masterDbContext = masterDbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetTenantId()
    {
        if (_tenantId != Guid.Empty)
        {
            return _tenantId;
        }

        // Try to get from HTTP context
        if (_httpContextAccessor.HttpContext?.Items.ContainsKey("TenantId") == true)
        {
            _tenantId = (Guid)_httpContextAccessor.HttpContext.Items["TenantId"]!;
            return _tenantId;
        }

        // Try to get from claims
        var tenantIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim, out var parsedTenantId))
        {
            _tenantId = parsedTenantId;
            return _tenantId;
        }

        throw new InvalidOperationException("Tenant context not set");
    }

    public void SetTenant(Guid tenantId)
    {
        _tenantId = tenantId;
    }

    public async Task<string> GetTenantConnectionStringAsync()
    {
        var tenantId = GetTenantId();
        var tenant = await _masterDbContext.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId && t.IsActive);

        if (tenant == null || !tenant.IsSubscriptionActive())
        {
            throw new InvalidOperationException("Tenant not found or subscription expired");
        }

        return tenant.ConnectionString;
    }
}
