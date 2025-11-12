using Microsoft.EntityFrameworkCore;
using ServerApp.Data.Contexts;

namespace ServerApp.Services;

public interface IAnalyticsService
{
    Task<DashboardMetrics> GetDashboardMetricsAsync();
    Task<RevenueAnalytics> GetRevenueAnalyticsAsync(DateTime from, DateTime to);
    Task<List<TopProduct>> GetTopProductsAsync(int count = 10);
    Task<StockAnalytics> GetStockAnalyticsAsync();
    Task<List<SalesT rend>> GetSalesTrendAsync(int days = 30);
}

public class DashboardMetrics
{
    public int TotalProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal RevenueToday { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalSuppliers { get; set; }
    public int ActiveLocations { get; set; }
    public decimal InventoryValue { get; set; }
}

public class RevenueAnalytics
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal ProfitMargin { get; set; }
    public List<CategoryRevenue> ByCategory { get; set; } = new();
    public List<MonthlyRevenue> ByMonth { get; set; } = new();
}

public class CategoryRevenue
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public class MonthlyRevenue
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Cost { get; set; }
    public decimal Profit { get; set; }
}

public class TopProduct
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class StockAnalytics
{
    public int TotalItems { get; set; }
    public decimal TotalValue { get; set; }
    public decimal AverageStockLevel { get; set; }
    public int ItemsBelowReorderPoint { get; set; }
    public List<ABCAnalysis> ABCItems { get; set; } = new();
    public decimal StockTurnoverRatio { get; set; }
}

public class ABCAnalysis
{
    public string Category { get; set; } = string.Empty; // A, B, or C
    public int ItemCount { get; set; }
    public decimal ValuePercentage { get; set; }
}

public class SalesTrend
{
    public DateTime Date { get; set; }
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
}

public class AnalyticsService : IAnalyticsService
{
    private readonly TenantDbContext _context;
    private readonly ITenantService _tenantService;

    public AnalyticsService(TenantDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<DashboardMetrics> GetDashboardMetricsAsync()
    {
        var tenantId = _tenantService.GetTenantId();
        var today = DateTime.UtcNow.Date;
        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

        var metrics = new DashboardMetrics
        {
            TotalProducts = await _context.Products.CountAsync(),
            LowStockProducts = await _context.StockLevels
                .Where(s => s.Quantity <= s.ReorderPoint && s.Quantity > 0)
                .CountAsync(),
            OutOfStockProducts = await _context.StockLevels
                .Where(s => s.Quantity == 0)
                .CountAsync(),
            TotalOrders = await _context.Orders.CountAsync(),
            PendingOrders = await _context.Orders
                .Where(o => o.Status == Domain.Entities.OrderStatus.Pending ||
                            o.Status == Domain.Entities.OrderStatus.Confirmed)
                .CountAsync(),
            TotalCustomers = await _context.Customers.CountAsync(),
            TotalSuppliers = await _context.Suppliers.CountAsync(),
            ActiveLocations = await _context.Locations.Where(l => l.IsActive).CountAsync(),
        };

        // Calculate revenue
        var salesOrders = await _context.Orders
            .Where(o => o.Type == Domain.Entities.OrderType.Sales &&
                        o.Status == Domain.Entities.OrderStatus.Completed)
            .ToListAsync();

        metrics.TotalRevenue = salesOrders.Sum(o => o.TotalAmount);
        metrics.RevenueToday = salesOrders
            .Where(o => o.OrderDate.Date == today)
            .Sum(o => o.TotalAmount);
        metrics.RevenueThisMonth = salesOrders
            .Where(o => o.OrderDate >= firstDayOfMonth)
            .Sum(o => o.TotalAmount);

        // Calculate inventory value
        var stockLevels = await _context.StockLevels
            .Include(s => s.Product)
            .ToListAsync();

        metrics.InventoryValue = stockLevels.Sum(s => s.Quantity * (s.Product?.Price ?? 0));

        return metrics;
    }

    public async Task<RevenueAnalytics> GetRevenueAnalyticsAsync(DateTime from, DateTime to)
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p!.Category)
            .Where(o => o.Type == Domain.Entities.OrderType.Sales &&
                        o.Status == Domain.Entities.OrderStatus.Completed &&
                        o.OrderDate >= from && o.OrderDate <= to)
            .ToListAsync();

        var analytics = new RevenueAnalytics
        {
            TotalRevenue = orders.Sum(o => o.TotalAmount),
            TotalCost = orders.SelectMany(o => o.Items)
                .Sum(i => (i.Product?.CostPrice ?? 0) * i.Quantity)
        };

        analytics.GrossProfit = analytics.TotalRevenue - analytics.TotalCost;
        analytics.ProfitMargin = analytics.TotalRevenue > 0
            ? (analytics.GrossProfit / analytics.TotalRevenue) * 100
            : 0;

        // By category
        analytics.ByCategory = orders
            .SelectMany(o => o.Items)
            .GroupBy(i => i.Product?.Category?.Name ?? "Uncategorized")
            .Select(g => new CategoryRevenue
            {
                CategoryName = g.Key,
                Revenue = g.Sum(i => i.LineTotal),
                OrderCount = g.Select(i => i.OrderId).Distinct().Count()
            })
            .OrderByDescending(c => c.Revenue)
            .ToList();

        // By month
        analytics.ByMonth = orders
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .Select(g => new MonthlyRevenue
            {
                Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                Revenue = g.Sum(o => o.TotalAmount),
                Cost = g.SelectMany(o => o.Items).Sum(i => (i.Product?.CostPrice ?? 0) * i.Quantity),
                Profit = g.Sum(o => o.TotalAmount) - g.SelectMany(o => o.Items).Sum(i => (i.Product?.CostPrice ?? 0) * i.Quantity)
            })
            .OrderBy(m => m.Month)
            .ToList();

        return analytics;
    }

    public async Task<List<TopProduct>> GetTopProductsAsync(int count = 10)
    {
        return await _context.OrderItems
            .Include(i => i.Product)
            .Where(i => i.Order != null &&
                        i.Order.Type == Domain.Entities.OrderType.Sales &&
                        i.Order.Status == Domain.Entities.OrderStatus.Completed)
            .GroupBy(i => new { i.ProductId, i.Product!.Name })
            .Select(g => new TopProduct
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                QuantitySold = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.LineTotal)
            })
            .OrderByDescending(p => p.Revenue)
            .Take(count)
            .ToListAsync();
    }

    public async Task<StockAnalytics> GetStockAnalyticsAsync()
    {
        var stockLevels = await _context.StockLevels
            .Include(s => s.Product)
            .ToListAsync();

        var analytics = new StockAnalytics
        {
            TotalItems = stockLevels.Sum(s => s.Quantity),
            TotalValue = stockLevels.Sum(s => s.Quantity * (s.Product?.Price ?? 0)),
            AverageStockLevel = stockLevels.Count > 0 ? stockLevels.Average(s => s.Quantity) : 0,
            ItemsBelowReorderPoint = stockLevels.Count(s => s.Quantity <= s.ReorderPoint)
        };

        // ABC Analysis
        var productValues = stockLevels
            .Select(s => new
            {
                s.ProductId,
                Value = s.Quantity * (s.Product?.Price ?? 0)
            })
            .OrderByDescending(p => p.Value)
            .ToList();

        var totalValue = productValues.Sum(p => p.Value);
        var aCount = (int)(productValues.Count * 0.2); // Top 20%
        var bCount = (int)(productValues.Count * 0.3); // Next 30%

        analytics.ABCItems = new List<ABCAnalysis>
        {
            new ABCAnalysis
            {
                Category = "A",
                ItemCount = aCount,
                ValuePercentage = totalValue > 0 ? (productValues.Take(aCount).Sum(p => p.Value) / totalValue) * 100 : 0
            },
            new ABCAnalysis
            {
                Category = "B",
                ItemCount = bCount,
                ValuePercentage = totalValue > 0 ? (productValues.Skip(aCount).Take(bCount).Sum(p => p.Value) / totalValue) * 100 : 0
            },
            new ABCAnalysis
            {
                Category = "C",
                ItemCount = productValues.Count - aCount - bCount,
                ValuePercentage = totalValue > 0 ? (productValues.Skip(aCount + bCount).Sum(p => p.Value) / totalValue) * 100 : 0
            }
        };

        return analytics;
    }

    public async Task<List<SalesTrend>> GetSalesTrendAsync(int days = 30)
    {
        var from = DateTime.UtcNow.Date.AddDays(-days);

        return await _context.Orders
            .Where(o => o.Type == Domain.Entities.OrderType.Sales &&
                        o.Status == Domain.Entities.OrderStatus.Completed &&
                        o.OrderDate >= from)
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new SalesTrend
            {
                Date = g.Key,
                OrderCount = g.Count(),
                Revenue = g.Sum(o => o.TotalAmount)
            })
            .OrderBy(t => t.Date)
            .ToListAsync();
    }
}
