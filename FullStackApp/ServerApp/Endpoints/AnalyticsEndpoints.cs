using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerApp.Services;

namespace ServerApp.Endpoints;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/analytics")
            .WithTags("Analytics")
            .RequireAuthorization();

        group.MapGet("/dashboard", GetDashboardMetricsAsync)
            .WithName("GetDashboardMetrics")
            .WithOpenApi()
            .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(2)));

        group.MapGet("/revenue", GetRevenueAnalyticsAsync)
            .WithName("GetRevenueAnalytics")
            .WithOpenApi();

        group.MapGet("/top-products", GetTopProductsAsync)
            .WithName("GetTopProducts")
            .WithOpenApi()
            .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)));

        group.MapGet("/stock", GetStockAnalyticsAsync)
            .WithName("GetStockAnalytics")
            .WithOpenApi()
            .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(1)));

        group.MapGet("/sales-trend", GetSalesTrendAsync)
            .WithName("GetSalesTrend")
            .WithOpenApi();
    }

    private static async Task<IResult> GetDashboardMetricsAsync(IAnalyticsService analyticsService)
    {
        try
        {
            var metrics = await analyticsService.GetDashboardMetricsAsync();
            return Results.Ok(metrics);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetRevenueAnalyticsAsync(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        IAnalyticsService analyticsService)
    {
        try
        {
            var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
            var toDate = to ?? DateTime.UtcNow;

            var analytics = await analyticsService.GetRevenueAnalyticsAsync(fromDate, toDate);
            return Results.Ok(analytics);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetTopProductsAsync(
        [FromQuery] int count,
        IAnalyticsService analyticsService)
    {
        try
        {
            if (count <= 0) count = 10;
            if (count > 100) count = 100;

            var topProducts = await analyticsService.GetTopProductsAsync(count);
            return Results.Ok(topProducts);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetStockAnalyticsAsync(IAnalyticsService analyticsService)
    {
        try
        {
            var analytics = await analyticsService.GetStockAnalyticsAsync();
            return Results.Ok(analytics);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetSalesTrendAsync(
        [FromQuery] int days,
        IAnalyticsService analyticsService)
    {
        try
        {
            if (days <= 0) days = 30;
            if (days > 365) days = 365;

            var trend = await analyticsService.GetSalesTrendAsync(days);
            return Results.Ok(trend);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
