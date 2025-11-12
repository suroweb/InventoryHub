using Shared.Models;

namespace ServerApp.Domain.Entities;

public class Report : BaseEntity
{
    public Guid TenantId { get; set; }

    public required string Name { get; set; }
    public string? Description { get; set; }

    public ReportType Type { get; set; }
    public string? QueryJson { get; set; } // Stored query parameters

    public string? CreatedByUserId { get; set; }
    public bool IsShared { get; set; }

    public int RunCount { get; set; }
    public DateTime? LastRunAt { get; set; }
}

public enum ReportType
{
    Inventory = 1,
    Sales = 2,
    Purchase = 3,
    Analytics = 4,
    Audit = 5,
    Custom = 6
}

public class ScheduledReport : BaseEntity
{
    public Guid TenantId { get; set; }

    public Guid ReportId { get; set; }
    public Report? Report { get; set; }

    public required string Schedule { get; set; } // Cron expression
    public List<string> Recipients { get; set; } = new(); // Email addresses

    public ReportFormat Format { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime? LastRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
}

public enum ReportFormat
{
    PDF = 1,
    Excel = 2,
    CSV = 3,
    JSON = 4
}

public class ProductBarcode : BaseEntity
{
    public Guid TenantId { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public required string BarcodeValue { get; set; }
    public BarcodeType Type { get; set; }

    public bool IsPrimary { get; set; }
}

public enum BarcodeType
{
    UPC = 1,
    EAN13 = 2,
    Code128 = 3,
    QRCode = 4,
    Custom = 5
}

public class StockAdjustment : BaseEntity
{
    public Guid TenantId { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }

    public int QuantityBefore { get; set; }
    public int QuantityChange { get; set; }
    public int QuantityAfter { get; set; }

    public AdjustmentReason Reason { get; set; }
    public string? Notes { get; set; }

    public string? AdjustedByUserId { get; set; }
    public DateTime AdjustedAt { get; set; } = DateTime.UtcNow;
}

public enum AdjustmentReason
{
    Received = 1,
    Sold = 2,
    Damaged = 3,
    Lost = 4,
    Found = 5,
    Returned = 6,
    Expired = 7,
    StockCount = 8,
    Other = 9
}

public class ForecastModel : BaseEntity
{
    public Guid TenantId { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public int PredictedDemand { get; set; }
    public int ActualDemand { get; set; }

    public decimal Accuracy { get; set; } // Percentage
    public string? ModelParameters { get; set; } // JSON

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
