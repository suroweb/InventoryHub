using Shared.Models;

namespace ServerApp.Domain.Entities;

public class Location : BaseEntity
{
    public Guid TenantId { get; set; }

    public required string Name { get; set; }
    public string? Code { get; set; }
    public LocationType Type { get; set; }

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }

    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }

    // Navigation
    public ICollection<StockLevel> StockLevels { get; set; } = new List<StockLevel>();
    public ICollection<StockTransfer> TransfersFrom { get; set; } = new List<StockTransfer>();
    public ICollection<StockTransfer> TransfersTo { get; set; } = new List<StockTransfer>();
}

public enum LocationType
{
    Warehouse = 1,
    Store = 2,
    Distribution = 3,
    Virtual = 4
}

public class StockLevel : BaseEntity
{
    public Guid TenantId { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public Guid LocationId { get; set; }
    public Location? Location { get; set; }

    public int Quantity { get; set; }
    public int ReorderPoint { get; set; }
    public int MaxQuantity { get; set; }

    public DateTime? LastCountedAt { get; set; }
    public int? VarianceCount { get; set; }
}

public class StockTransfer : BaseEntity
{
    public Guid TenantId { get; set; }

    public required string TransferNumber { get; set; }

    public Guid FromLocationId { get; set; }
    public Location? FromLocation { get; set; }

    public Guid ToLocationId { get; set; }
    public Location? ToLocation { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }
    public StockTransferStatus Status { get; set; }

    public DateTime? ShippedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }

    public string? Notes { get; set; }
    public string? TrackingNumber { get; set; }
}

public enum StockTransferStatus
{
    Pending = 1,
    InTransit = 2,
    Received = 3,
    Cancelled = 4
}
