using ServerApp.Data.Contexts;
using ServerApp.Services;
using Microsoft.EntityFrameworkCore;

namespace ServerApp.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, MasterDbContext masterDbContext, ITenantService tenantService)
    {
        // Skip tenant resolution for auth endpoints
        if (context.Request.Path.StartsWithSegments("/api/auth"))
        {
            await _next(context);
            return;
        }

        // Try to get tenant from subdomain first
        var host = context.Request.Host.Host;
        var subdomain = GetSubdomain(host);

        Guid? tenantId = null;

        // Option 1: Get tenant from subdomain
        if (!string.IsNullOrEmpty(subdomain))
        {
            var tenant = await masterDbContext.Tenants
                .FirstOrDefaultAsync(t => t.Subdomain == subdomain && t.IsActive);

            if (tenant != null && tenant.IsSubscriptionActive())
            {
                tenantId = tenant.Id;
            }
        }

        // Option 2: Get tenant from JWT claims (if authenticated)
        if (tenantId == null && context.User.Identity?.IsAuthenticated == true)
        {
            var tenantIdClaim = context.User.FindFirst("TenantId")?.Value;
            if (Guid.TryParse(tenantIdClaim, out var parsedTenantId))
            {
                tenantId = parsedTenantId;
            }
        }

        // Option 3: Get tenant from X-Tenant-Id header (for API clients)
        if (tenantId == null && context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerTenantId))
        {
            if (Guid.TryParse(headerTenantId, out var parsedTenantId))
            {
                tenantId = parsedTenantId;
            }
        }

        if (tenantId.HasValue)
        {
            // Set tenant context
            tenantService.SetTenant(tenantId.Value);
            context.Items["TenantId"] = tenantId.Value;
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Tenant not found or subscription expired",
                message = "Unable to resolve tenant context"
            });
            return;
        }

        await _next(context);
    }

    private string? GetSubdomain(string host)
    {
        // Extract subdomain from host
        // Example: tenant1.inventoryhub.com -> tenant1
        var parts = host.Split('.');
        if (parts.Length > 2)
        {
            return parts[0];
        }
        return null;
    }
}

public static class TenantResolutionMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<TenantResolutionMiddleware>();
    }
}
