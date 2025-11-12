using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerApp.Services.AI;

namespace ServerApp.Endpoints;

public static class AIEndpoints
{
    public static void MapAIEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ai")
            .WithTags("AI & Machine Learning")
            .RequireAuthorization();

        // Demand Forecasting
        group.MapGet("/forecast/{productId:guid}", ForecastDemandAsync)
            .WithName("ForecastDemand")
            .WithOpenApi()
            .WithDescription("AI-powered demand forecasting using open source models (DeepSeek, Llama, etc.)");

        group.MapGet("/reorder-recommendation/{productId:guid}", GetReorderRecommendationAsync)
            .WithName("GetReorderRecommendation")
            .WithOpenApi()
            .WithDescription("Smart reorder recommendations based on AI forecasting");

        // Smart Recommendations
        group.MapGet("/recommendations/{customerId:guid}", GetSmartRecommendationsAsync)
            .WithName("GetSmartRecommendations")
            .WithOpenApi()
            .WithDescription("AI-powered product recommendations for customers");

        // Seasonality Analysis
        group.MapGet("/seasonality/{productId:guid}", AnalyzeSeasonalityAsync)
            .WithName("AnalyzeSeasonality")
            .WithOpenApi()
            .WithDescription("Analyze seasonal demand patterns");

        // Stock Optimization
        group.MapGet("/optimize-stock/{locationId:guid}", OptimizeStockLevelsAsync)
            .WithName("OptimizeStockLevels")
            .WithOpenApi()
            .WithDescription("AI-powered stock level optimization");

        // Natural Language Query
        group.MapPost("/query", ProcessNaturalLanguageQueryAsync)
            .WithName("ProcessNLQuery")
            .WithOpenApi()
            .WithDescription("Query inventory data using natural language");

        // Data Analysis
        group.MapPost("/analyze", AnalyzeDataAsync)
            .WithName("AnalyzeData")
            .WithOpenApi()
            .WithDescription("AI-powered data analysis and insights");
    }

    private static async Task<IResult> ForecastDemandAsync(
        Guid productId,
        [FromQuery] int days,
        IForecastingService forecastingService)
    {
        try
        {
            if (days <= 0) days = 30;
            if (days > 365) days = 365;

            var forecast = await forecastingService.ForecastDemandAsync(productId, days);
            return Results.Ok(forecast);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetReorderRecommendationAsync(
        Guid productId,
        IForecastingService forecastingService)
    {
        try
        {
            var recommendation = await forecastingService.GetReorderRecommendationAsync(productId);
            return Results.Ok(recommendation);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetSmartRecommendationsAsync(
        Guid customerId,
        IForecastingService forecastingService)
    {
        try
        {
            var recommendations = await forecastingService.GetSmartRecommendationsAsync(customerId);
            return Results.Ok(recommendations);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> AnalyzeSeasonalityAsync(
        Guid productId,
        IForecastingService forecastingService)
    {
        try
        {
            var analysis = await forecastingService.AnalyzeSeasonalityAsync(productId);
            return Results.Ok(analysis);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> OptimizeStockLevelsAsync(
        Guid locationId,
        IForecastingService forecastingService)
    {
        try
        {
            var optimization = await forecastingService.OptimizeStockLevelsAsync(locationId);
            return Results.Ok(optimization);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> ProcessNaturalLanguageQueryAsync(
        [FromBody] NLQueryRequest request,
        IAIService aiService,
        IAnalyticsService analyticsService)
    {
        try
        {
            // Get context data
            var dashboardMetrics = await analyticsService.GetDashboardMetricsAsync();

            var aiRequest = new AIRequest
            {
                Prompt = $@"User question: {request.Query}

Current inventory context:
- Total products: {dashboardMetrics.TotalProducts}
- Low stock items: {dashboardMetrics.LowStockProducts}
- Out of stock items: {dashboardMetrics.OutOfStockProducts}
- Total orders: {dashboardMetrics.TotalOrders}
- Revenue today: ${dashboardMetrics.RevenueToday:F2}
- Revenue this month: ${dashboardMetrics.RevenueThisMonth:F2}

Provide a clear, concise answer to the user's question based on this data.",
                SystemPrompt = "You are an inventory management assistant. Answer questions clearly and provide actionable insights.",
                Temperature = 0.5
            };

            var response = await aiService.GenerateTextAsync(aiRequest);

            if (!response.Success)
                return Results.BadRequest(new { error = response.Error });

            return Results.Ok(new
            {
                query = request.Query,
                answer = response.Content,
                confidence = 0.85,
                model = response.Model
            });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> AnalyzeDataAsync(
        [FromBody] AnalyzeRequest request,
        IAIService aiService)
    {
        try
        {
            var response = await aiService.AnalyzeDataAsync(request.Data, request.AnalysisType);

            if (!response.Success)
                return Results.BadRequest(new { error = response.Error });

            return Results.Ok(new
            {
                analysisType = request.AnalysisType,
                result = response.Content,
                model = response.Model,
                duration = response.Duration.TotalSeconds
            });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}

public class NLQueryRequest
{
    public required string Query { get; set; }
}

public class AnalyzeRequest
{
    public required string Data { get; set; }
    public string AnalysisType { get; set; } = "insights"; // sentiment, summary, insights, trends
}
