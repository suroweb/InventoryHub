using Shared.Models;

namespace ServerApp.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid TenantId { get; set; }

    public required string UserId { get; set; }
    public string? UserEmail { get; set; }

    public required string Action { get; set; } // Created, Updated, Deleted, Login, Logout, etc.
    public required string EntityType { get; set; } // Product, Order, User, etc.
    public Guid? EntityId { get; set; }

    public string? OldValues { get; set; } // JSON
    public string? NewValues { get; set; } // JSON

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class Alert : BaseEntity
{
    public Guid TenantId { get; set; }

    public AlertType Type { get; set; }
    public AlertPriority Priority { get; set; }

    public required string Title { get; set; }
    public required string Message { get; set; }

    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? ReadBy { get; set; }

    public bool IsDismissed { get; set; }
    public DateTime? DismissedAt { get; set; }

    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
}

public enum AlertType
{
    LowStock = 1,
    OutOfStock = 2,
    ExpiryWarning = 3,
    OrderPending = 4,
    StockTransfer = 5,
    Security = 6,
    System = 7,
    Custom = 8
}

public enum AlertPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public class Webhook : BaseEntity
{
    public Guid TenantId { get; set; }

    public required string Name { get; set; }
    public required string Url { get; set; }

    public List<string> Events { get; set; } = new(); // product.created, order.completed, etc.

    public string? Secret { get; set; } // For HMAC signature

    public bool IsActive { get; set; } = true;
    public int RetryCount { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 30;

    public DateTime? LastTriggeredAt { get; set; }
    public string? LastStatus { get; set; }
}

public class WebhookLog : BaseEntity
{
    public Guid WebhookId { get; set; }
    public Webhook? Webhook { get; set; }

    public required string Event { get; set; }
    public required string Payload { get; set; }

    public int StatusCode { get; set; }
    public string? Response { get; set; }

    public bool WasSuccessful { get; set; }
    public int AttemptNumber { get; set; }

    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public int DurationMs { get; set; }
}
