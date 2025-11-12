using Microsoft.EntityFrameworkCore;
using ServerApp.Data.Contexts;
using ServerApp.Domain.Entities;
using ServerApp.Services.AI;

namespace ServerApp.Services.AI;

/// <summary>
/// AI-Powered Inventory Forecasting using open source models
/// </summary>
public interface IForecastingService
{
    Task<DemandForecast> ForecastDemandAsync(Guid productId, int daysAhead = 30);
    Task<ReorderRecommendation> GetReorderRecommendationAsync(Guid productId);
    Task<List<ProductRecommendation>> GetSmartRecommendationsAsync(Guid customerId);
    Task<SeasonalityAnalysis> AnalyzeSeasonalityAsync(Guid productId);
    Task<StockOptimization> OptimizeStockLevelsAsync(Guid locationId);
}

public class DemandForecast
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public List<DailyForecast> Forecast { get; set; } = new();
    public double Confidence { get; set; } // 0-1
    public string Trend { get; set; } = string.Empty; // "increasing", "decreasing", "stable"
    public Dictionary<string, object> Insights { get; set; } = new();
}

public class DailyForecast
{
    public DateTime Date { get; set; }
    public int PredictedDemand { get; set; }
    public int LowerBound { get; set; }
    public int UpperBound { get; set; }
}

public class ReorderRecommendation
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public bool ShouldReorder { get; set; }
    public int RecommendedQuantity { get; set; }
    public DateTime SuggestedOrderDate { get; set; }
    public DateTime ExpectedStockoutDate { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public decimal EstimatedCost { get; set; }
}

public class ProductRecommendation
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public double RelevanceScore { get; set; } // 0-1
    public string Reason { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class SeasonalityAnalysis
{
    public Guid ProductId { get; set; }
    public bool HasSeasonality { get; set; }
    public List<SeasonalPeak> Peaks { get; set; } = new();
    public string Pattern { get; set; } = string.Empty;
    public Dictionary<string, double> MonthlyMultipliers { get; set; } = new();
}

public class SeasonalPeak
{
    public string Period { get; set; } = string.Empty; // "December", "Q4", etc.
    public double Multiplier { get; set; } // 1.0 = average, 2.0 = double demand
}

public class StockOptimization
{
    public Guid LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public List<ProductOptimization> Products { get; set; } = new();
    public decimal TotalSavings { get; set; }
    public int ItemsToReduce { get; set; }
    public int ItemsToIncrease { get; set; }
}

public class ProductOptimization
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int OptimalStock { get; set; }
    public string Action { get; set; } = string.Empty; // "reduce", "increase", "maintain"
    public string Reasoning { get; set; } = string.Empty;
}

public class ForecastingService : IForecastingService
{
    private readonly TenantDbContext _context;
    private readonly IAIService _aiService;
    private readonly ILogger<ForecastingService> _logger;

    public ForecastingService(
        TenantDbContext context,
        IAIService aiService,
        ILogger<ForecastingService> logger)
    {
        _context = context;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<DemandForecast> ForecastDemandAsync(Guid productId, int daysAhead = 30)
    {
        // Get historical sales data
        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null)
            throw new InvalidOperationException("Product not found");

        var historicalOrders = await _context.OrderItems
            .Include(i => i.Order)
            .Where(i => i.ProductId == productId &&
                        i.Order != null &&
                        i.Order.Type == OrderType.Sales &&
                        i.Order.Status == OrderStatus.Completed &&
                        i.Order.OrderDate >= DateTime.UtcNow.AddDays(-90))
            .GroupBy(i => i.Order!.OrderDate.Date)
            .Select(g => new { Date = g.Key, Quantity = g.Sum(i => i.Quantity) })
            .OrderBy(x => x.Date)
            .ToListAsync();

        // Prepare data for AI analysis
        var historicalData = string.Join("\n", historicalOrders.Select(h => $"{h.Date:yyyy-MM-dd}: {h.Quantity}"));

        var aiRequest = new AIRequest
        {
            Prompt = $@"Analyze this sales data and forecast demand for the next {daysAhead} days:

Product: {product.Name}
Category: {product.Category?.Name}
Current Stock: {product.Stock}

Historical Sales (last 90 days):
{historicalData}

Provide a JSON response with:
1. Daily forecasts for next {daysAhead} days
2. Confidence level (0-1)
3. Trend (increasing/decreasing/stable)
4. Key insights

Format: {{"forecasts": [{{"date": "YYYY-MM-DD", "demand": N}}], "confidence": 0.X, "trend": "...", "insights": ""...""}}",
            SystemPrompt = "You are an expert in demand forecasting and inventory optimization. Analyze patterns, seasonality, and trends. Return only valid JSON.",
            Temperature = 0.3
        };

        var aiResponse = await _aiService.GenerateTextAsync(aiRequest);

        if (!aiResponse.Success || string.IsNullOrEmpty(aiResponse.Content))
        {
            // Fallback to simple moving average
            return GenerateFallbackForecast(product, historicalOrders, daysAhead);
        }

        try
        {
            // Parse AI response
            var jsonDoc = System.Text.Json.JsonDocument.Parse(aiResponse.Content);
            var root = jsonDoc.RootElement;

            var forecast = new DemandForecast
            {
                ProductId = productId,
                ProductName = product.Name,
                CurrentStock = product.Stock,
                Confidence = root.GetProperty("confidence").GetDouble(),
                Trend = root.GetProperty("trend").GetString() ?? "stable"
            };

            foreach (var item in root.GetProperty("forecasts").EnumerateArray())
            {
                var date = DateTime.Parse(item.GetProperty("date").GetString()!);
                var demand = item.GetProperty("demand").GetInt32();

                forecast.Forecast.Add(new DailyForecast
                {
                    Date = date,
                    PredictedDemand = demand,
                    LowerBound = (int)(demand * 0.8), // 20% variance
                    UpperBound = (int)(demand * 1.2)
                });
            }

            if (root.TryGetProperty("insights", out var insights))
            {
                forecast.Insights["ai_analysis"] = insights.GetString() ?? string.Empty;
            }

            // Save forecast to database
            var forecastModel = new ForecastModel
            {
                ProductId = productId,
                TenantId = product.TenantId,
                PeriodStart = DateTime.UtcNow.Date,
                PeriodEnd = DateTime.UtcNow.Date.AddDays(daysAhead),
                PredictedDemand = forecast.Forecast.Sum(f => f.PredictedDemand),
                ModelParameters = aiResponse.Content,
                GeneratedAt = DateTime.UtcNow
            };

            _context.ForecastModels.Add(forecastModel);
            await _context.SaveChangesAsync();

            return forecast;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse AI forecast response");
            return GenerateFallbackForecast(product, historicalOrders, daysAhead);
        }
    }

    public async Task<ReorderRecommendation> GetReorderRecommendationAsync(Guid productId)
    {
        var forecast = await ForecastDemandAsync(productId, 30);
        var product = await _context.Products.FindAsync(productId);

        if (product == null)
            throw new InvalidOperationException("Product not found");

        var totalDemand = forecast.Forecast.Sum(f => f.PredictedDemand);
        var averageDailyDemand = totalDemand / 30.0;

        // Calculate days until stockout
        var daysUntilStockout = product.Stock > 0
            ? (int)(product.Stock / averageDailyDemand)
            : 0;

        var shouldReorder = product.Stock <= (product.ReorderLevel ?? 0);
        var recommendedQuantity = 0;
        var reasoning = "";

        if (shouldReorder)
        {
            // Calculate Economic Order Quantity (EOQ) or use AI
            var leadTime = 7; // Assume 7 days lead time
            var safetyStock = (int)(averageDailyDemand * leadTime * 1.5); // 50% safety margin
            recommendedQuantity = (int)(averageDailyDemand * 30) + safetyStock;

            // Use AI for reasoning
            var aiRequest = new AIRequest
            {
                Prompt = $@"Explain why this product needs reordering:
Product: {product.Name}
Current Stock: {product.Stock}
Reorder Point: {product.ReorderLevel}
Average Daily Demand: {averageDailyDemand:F2}
Days Until Stockout: {daysUntilStockout}
Recommended Quantity: {recommendedQuantity}

Provide a brief explanation (2-3 sentences).",
                Temperature = 0.5
            };

            var aiResponse = await _aiService.GenerateTextAsync(aiRequest);
            reasoning = aiResponse.Success ? aiResponse.Content ?? "Reorder recommended based on current demand." : "Reorder recommended based on current demand.";
        }
        else
        {
            reasoning = $"Stock levels are healthy. Current stock will last approximately {daysUntilStockout} days.";
        }

        return new ReorderRecommendation
        {
            ProductId = productId,
            ProductName = product.Name,
            ShouldReorder = shouldReorder,
            RecommendedQuantity = recommendedQuantity,
            SuggestedOrderDate = DateTime.UtcNow.Date,
            ExpectedStockoutDate = DateTime.UtcNow.Date.AddDays(daysUntilStockout),
            Reasoning = reasoning,
            EstimatedCost = recommendedQuantity * (product.CostPrice ?? product.Price)
        };
    }

    public async Task<List<ProductRecommendation>> GetSmartRecommendationsAsync(Guid customerId)
    {
        // Get customer's order history
        var customerOrders = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Where(o => o.CustomerId == customerId &&
                        o.Type == OrderType.Sales &&
                        o.Status == OrderStatus.Completed)
            .OrderByDescending(o => o.OrderDate)
            .Take(10)
            .ToListAsync();

        if (!customerOrders.Any())
            return new List<ProductRecommendation>();

        var purchasedProducts = customerOrders
            .SelectMany(o => o.Items)
            .Select(i => i.Product?.Name)
            .Distinct()
            .Take(20);

        var productList = string.Join(", ", purchasedProducts);

        // Use AI to recommend similar/complementary products
        var aiRequest = new AIRequest
        {
            Prompt = $@"Based on these purchased products: {productList}

Recommend 5 complementary or similar products that the customer might be interested in.
Consider product relationships, common purchase patterns, and business logic.

Return JSON: [{{"productName": "...", "reason": "...", "relevance": 0.X}}]",
            SystemPrompt = "You are a product recommendation expert. Suggest relevant, practical products.",
            Temperature = 0.7
        };

        var aiResponse = await _aiService.GenerateTextAsync(aiRequest);

        if (!aiResponse.Success || string.IsNullOrEmpty(aiResponse.Content))
            return new List<ProductRecommendation>();

        try
        {
            var jsonDoc = System.Text.Json.JsonDocument.Parse(aiResponse.Content);
            var recommendations = new List<ProductRecommendation>();

            foreach (var item in jsonDoc.RootElement.EnumerateArray())
            {
                var productName = item.GetProperty("productName").GetString();
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Name.Contains(productName!));

                if (product != null)
                {
                    recommendations.Add(new ProductRecommendation
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        RelevanceScore = item.GetProperty("relevance").GetDouble(),
                        Reason = item.GetProperty("reason").GetString() ?? "",
                        Price = product.Price
                    });
                }
            }

            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse AI recommendations");
            return new List<ProductRecommendation>();
        }
    }

    public async Task<SeasonalityAnalysis> AnalyzeSeasonalityAsync(Guid productId)
    {
        // Get 1 year of historical data
        var salesData = await _context.OrderItems
            .Include(i => i.Order)
            .Where(i => i.ProductId == productId &&
                        i.Order != null &&
                        i.Order.Type == OrderType.Sales &&
                        i.Order.Status == OrderStatus.Completed &&
                        i.Order.OrderDate >= DateTime.UtcNow.AddYears(-1))
            .GroupBy(i => new { i.Order!.OrderDate.Year, i.Order.OrderDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Quantity = g.Sum(i => i.Quantity) })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

        if (salesData.Count < 6) // Need at least 6 months
        {
            return new SeasonalityAnalysis
            {
                ProductId = productId,
                HasSeasonality = false,
                Pattern = "Insufficient data for seasonality analysis"
            };
        }

        var monthlyData = string.Join("\n", salesData.Select(s => $"{s.Year}-{s.Month:D2}: {s.Quantity}"));

        var aiRequest = new AIRequest
        {
            Prompt = $@"Analyze this monthly sales data for seasonality patterns:

{monthlyData}

Identify:
1. Is there seasonality? (true/false)
2. Peak periods
3. Pattern description
4. Monthly multipliers (compared to average)

Return JSON with structure.",
            SystemPrompt = "You are a time series analysis expert. Identify seasonal patterns and trends.",
            Temperature = 0.2
        };

        var aiResponse = await _aiService.GenerateTextAsync(aiRequest);

        // Parse and return (simplified for now)
        return new SeasonalityAnalysis
        {
            ProductId = productId,
            HasSeasonality = salesData.Max(s => s.Quantity) > salesData.Average(s => s.Quantity) * 1.5,
            Pattern = aiResponse.Content ?? "Analysis in progress"
        };
    }

    public async Task<StockOptimization> OptimizeStockLevelsAsync(Guid locationId)
    {
        var stockLevels = await _context.StockLevels
            .Include(s => s.Product)
            .Where(s => s.LocationId == locationId)
            .ToListAsync();

        var location = await _context.Locations.FindAsync(locationId);

        var optimization = new StockOptimization
        {
            LocationId = locationId,
            LocationName = location?.Name ?? "Unknown"
        };

        foreach (var stock in stockLevels.Take(20)) // Limit for performance
        {
            var forecast = await ForecastDemandAsync(stock.ProductId, 30);
            var avgDailyDemand = forecast.Forecast.Average(f => f.PredictedDemand);
            var optimalStock = (int)(avgDailyDemand * 30 * 1.2); // 30 days + 20% safety

            string action;
            if (stock.Quantity < optimalStock * 0.8)
                action = "increase";
            else if (stock.Quantity > optimalStock * 1.3)
                action = "reduce";
            else
                action = "maintain";

            optimization.Products.Add(new ProductOptimization
            {
                ProductId = stock.ProductId,
                ProductName = stock.Product?.Name ?? "Unknown",
                CurrentStock = stock.Quantity,
                OptimalStock = optimalStock,
                Action = action,
                Reasoning = $"Based on {avgDailyDemand:F1} units/day demand forecast"
            });
        }

        optimization.ItemsToIncrease = optimization.Products.Count(p => p.Action == "increase");
        optimization.ItemsToReduce = optimization.Products.Count(p => p.Action == "reduce");

        return optimization;
    }

    private DemandForecast GenerateFallbackForecast(Product product, List<dynamic> historicalOrders, int daysAhead)
    {
        // Simple moving average fallback
        var avgDemand = historicalOrders.Any() ? (int)historicalOrders.Average(h => (int)h.Quantity) : 0;

        var forecast = new DemandForecast
        {
            ProductId = product.Id,
            ProductName = product.Name,
            CurrentStock = product.Stock,
            Confidence = 0.5,
            Trend = "stable"
        };

        for (int i = 0; i < daysAhead; i++)
        {
            forecast.Forecast.Add(new DailyForecast
            {
                Date = DateTime.UtcNow.Date.AddDays(i + 1),
                PredictedDemand = avgDemand,
                LowerBound = (int)(avgDemand * 0.7),
                UpperBound = (int)(avgDemand * 1.3)
            });
        }

        forecast.Insights["method"] = "Moving average (AI unavailable)";

        return forecast;
    }
}
