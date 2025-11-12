using Microsoft.EntityFrameworkCore;
using ServerApp.Data.Contexts;
using ServerApp.Domain.Entities;

namespace ServerApp.Services;

public interface IAlertService
{
    Task CheckLowStockAlertsAsync();
    Task CreateAlertAsync(AlertType type, AlertPriority priority, string title, string message, Guid? relatedEntityId = null, string? relatedEntityType = null);
    Task<List<Alert>> GetUnreadAlertsAsync();
    Task<List<Alert>> GetAlertsAsync(int skip = 0, int take = 20);
    Task MarkAsReadAsync(Guid alertId, string userId);
    Task DismissAsync(Guid alertId);
}

public class AlertService : IAlertService
{
    private readonly TenantDbContext _context;
    private readonly ITenantService _tenantService;

    public AlertService(TenantDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task CheckLowStockAlertsAsync()
    {
        var tenantId = _tenantService.GetTenantId();

        // Check for low stock
        var lowStockItems = await _context.StockLevels
            .Include(s => s.Product)
            .Include(s => s.Location)
            .Where(s => s.Quantity <= s.ReorderPoint && s.Quantity > 0)
            .ToListAsync();

        foreach (var item in lowStockItems)
        {
            // Check if alert already exists for this product/location
            var existingAlert = await _context.Alerts
                .Where(a => a.Type == AlertType.LowStock &&
                            a.RelatedEntityId == item.ProductId &&
                            !a.IsDismissed &&
                            a.TriggeredAt > DateTime.UtcNow.AddDays(-1))
                .AnyAsync();

            if (!existingAlert)
            {
                await CreateAlertAsync(
                    AlertType.LowStock,
                    AlertPriority.Medium,
                    $"Low Stock: {item.Product?.Name}",
                    $"Product '{item.Product?.Name}' at location '{item.Location?.Name}' is running low. Current stock: {item.Quantity}, Reorder point: {item.ReorderPoint}",
                    item.ProductId,
                    "Product"
                );
            }
        }

        // Check for out of stock
        var outOfStockItems = await _context.StockLevels
            .Include(s => s.Product)
            .Include(s => s.Location)
            .Where(s => s.Quantity == 0)
            .ToListAsync();

        foreach (var item in outOfStockItems)
        {
            var existingAlert = await _context.Alerts
                .Where(a => a.Type == AlertType.OutOfStock &&
                            a.RelatedEntityId == item.ProductId &&
                            !a.IsDismissed &&
                            a.TriggeredAt > DateTime.UtcNow.AddDays(-1))
                .AnyAsync();

            if (!existingAlert)
            {
                await CreateAlertAsync(
                    AlertType.OutOfStock,
                    AlertPriority.High,
                    $"Out of Stock: {item.Product?.Name}",
                    $"Product '{item.Product?.Name}' at location '{item.Location?.Name}' is completely out of stock.",
                    item.ProductId,
                    "Product"
                );
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task CreateAlertAsync(AlertType type, AlertPriority priority, string title, string message, Guid? relatedEntityId = null, string? relatedEntityType = null)
    {
        var alert = new Alert
        {
            TenantId = _tenantService.GetTenantId(),
            Type = type,
            Priority = priority,
            Title = title,
            Message = message,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            IsRead = false,
            IsDismissed = false,
            TriggeredAt = DateTime.UtcNow
        };

        _context.Alerts.Add(alert);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Alert>> GetUnreadAlertsAsync()
    {
        return await _context.Alerts
            .Where(a => !a.IsRead && !a.IsDismissed)
            .OrderByDescending(a => a.Priority)
            .ThenByDescending(a => a.TriggeredAt)
            .Take(50)
            .ToListAsync();
    }

    public async Task<List<Alert>> GetAlertsAsync(int skip = 0, int take = 20)
    {
        return await _context.Alerts
            .Where(a => !a.IsDismissed)
            .OrderBy(a => a.IsRead)
            .ThenByDescending(a => a.Priority)
            .ThenByDescending(a => a.TriggeredAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task MarkAsReadAsync(Guid alertId, string userId)
    {
        var alert = await _context.Alerts.FindAsync(alertId);
        if (alert != null)
        {
            alert.IsRead = true;
            alert.ReadAt = DateTime.UtcNow;
            alert.ReadBy = userId;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DismissAsync(Guid alertId)
    {
        var alert = await _context.Alerts.FindAsync(alertId);
        if (alert != null)
        {
            alert.IsDismissed = true;
            alert.DismissedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
