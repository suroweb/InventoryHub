namespace ServerApp.Services;

public interface ITenantService
{
    Guid GetTenantId();
    void SetTenant(Guid tenantId);
    Task<string> GetTenantConnectionStringAsync();
}
