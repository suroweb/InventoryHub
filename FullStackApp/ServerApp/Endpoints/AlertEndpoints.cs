using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerApp.Services;

namespace ServerApp.Endpoints;

public static class AlertEndpoints
{
    public static void MapAlertEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/alerts")
            .WithTags("Alerts")
            .RequireAuthorization();

        group.MapGet("/unread", GetUnreadAlertsAsync)
            .WithName("GetUnreadAlerts")
            .WithOpenApi();

        group.MapGet("/", GetAlertsAsync)
            .WithName("GetAlerts")
            .WithOpenApi();

        group.MapPost("/{alertId:guid}/read", MarkAsReadAsync)
            .WithName("MarkAlertAsRead")
            .WithOpenApi();

        group.MapPost("/{alertId:guid}/dismiss", DismissAlertAsync)
            .WithName("DismissAlert")
            .WithOpenApi();

        group.MapPost("/check-stock", CheckStockAlertsAsync)
            .WithName("CheckStockAlerts")
            .WithOpenApi();
    }

    private static async Task<IResult> GetUnreadAlertsAsync(IAlertService alertService)
    {
        try
        {
            var alerts = await alertService.GetUnreadAlertsAsync();
            return Results.Ok(new { alerts, count = alerts.Count });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetAlertsAsync(
        [FromQuery] int skip,
        [FromQuery] int take,
        IAlertService alertService)
    {
        try
        {
            if (take <= 0) take = 20;
            if (take > 100) take = 100;

            var alerts = await alertService.GetAlertsAsync(skip, take);
            return Results.Ok(alerts);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> MarkAsReadAsync(
        Guid alertId,
        IAlertService alertService,
        HttpContext context)
    {
        try
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Results.Unauthorized();

            await alertService.MarkAsReadAsync(alertId, userId);
            return Results.Ok(new { message = "Alert marked as read" });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> DismissAlertAsync(
        Guid alertId,
        IAlertService alertService)
    {
        try
        {
            await alertService.DismissAsync(alertId);
            return Results.Ok(new { message = "Alert dismissed" });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> CheckStockAlertsAsync(IAlertService alertService)
    {
        try
        {
            await alertService.CheckLowStockAlertsAsync();
            return Results.Ok(new { message = "Stock alerts checked and created" });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
