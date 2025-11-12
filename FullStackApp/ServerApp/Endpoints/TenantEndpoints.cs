using Microsoft.AspNetCore.Mvc;
using ServerApp.Services;
using Shared.DTOs;

namespace ServerApp.Endpoints;

public static class TenantEndpoints
{
    public static void MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenants").WithTags("Tenants");

        group.MapPost("/", CreateTenantAsync)
            .WithName("CreateTenant")
            .WithOpenApi();

        group.MapPost("/{tenantId:guid}/upgrade", UpgradeTenantAsync)
            .WithName("UpgradeTenant")
            .RequireAuthorization()
            .WithOpenApi();

        group.MapGet("/{tenantId:guid}/usage", GetTenantUsageAsync)
            .WithName("GetTenantUsage")
            .RequireAuthorization()
            .WithOpenApi();
    }

    private static async Task<IResult> CreateTenantAsync(
        [FromBody] CreateTenantRequest request,
        ISubscriptionService subscriptionService,
        IAuthService authService)
    {
        try
        {
            // Create tenant
            var tenant = await subscriptionService.CreateTenantAsync(
                request.Name,
                request.Subdomain,
                request.AdminEmail,
                request.Tier);

            // Create admin user
            var authResult = await authService.RegisterAsync(
                request.AdminEmail,
                request.AdminPassword,
                request.AdminFirstName,
                request.AdminLastName,
                tenant.Id);

            if (!authResult.Success)
            {
                return Results.BadRequest(new
                {
                    error = "Tenant created but admin user registration failed",
                    details = authResult.Error
                });
            }

            var tenantDTO = new TenantDTO
            {
                Id = tenant.Id,
                Name = tenant.Name,
                Subdomain = tenant.Subdomain,
                SubscriptionTier = tenant.SubscriptionTier,
                SubscriptionExpiresAt = tenant.SubscriptionExpiresAt,
                IsActive = tenant.IsActive,
                MaxUsers = tenant.MaxUsers,
                MaxProducts = tenant.MaxProducts,
                CreatedAt = tenant.CreatedAt
            };

            return Results.Created($"/api/tenants/{tenant.Id}", new
            {
                tenant = tenantDTO,
                adminToken = authResult.Token
            });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> UpgradeTenantAsync(
        Guid tenantId,
        [FromBody] UpgradeTenantRequest request,
        ISubscriptionService subscriptionService,
        HttpContext httpContext)
    {
        // Verify user belongs to this tenant
        var userTenantId = Guid.Parse(httpContext.User.FindFirst("TenantId")!.Value);
        if (userTenantId != tenantId)
        {
            return Results.Forbid();
        }

        try
        {
            var success = await subscriptionService.UpgradeTenantAsync(tenantId, request.NewTier);

            if (!success)
            {
                return Results.NotFound(new { error = "Tenant not found" });
            }

            return Results.Ok(new { message = "Tenant upgraded successfully" });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetTenantUsageAsync(
        Guid tenantId,
        ISubscriptionService subscriptionService,
        HttpContext httpContext)
    {
        // Verify user belongs to this tenant
        var userTenantId = Guid.Parse(httpContext.User.FindFirst("TenantId")!.Value);
        if (userTenantId != tenantId)
        {
            return Results.Forbid();
        }

        try
        {
            var usage = await subscriptionService.GetTenantUsageAsync(tenantId);
            return Results.Ok(usage);
        }
        catch (Exception ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }
}

public class UpgradeTenantRequest
{
    public Shared.Models.SubscriptionTier NewTier { get; set; }
}
