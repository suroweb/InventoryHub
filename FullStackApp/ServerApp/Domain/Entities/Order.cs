using Shared.Models;

namespace ServerApp.Domain.Entities;

public class Order : BaseEntity
{
    public Guid TenantId { get; set; }

    public required string OrderNumber { get; set; }
    public OrderType Type { get; set; }
    public OrderStatus Status { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpectedDate { get; set; }
    public DateTime? FulfilledDate { get; set; }

    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }
    public string? TrackingNumber { get; set; }

    // Navigation
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public enum OrderType
{
    Sales = 1,
    Purchase = 2,
    Return = 3,
    Transfer = 4
}

public enum OrderStatus
{
    Draft = 1,
    Pending = 2,
    Confirmed = 3,
    InProgress = 4,
    Shipped = 5,
    Delivered = 6,
    Completed = 7,
    Cancelled = 8
}

public class OrderItem : BaseEntity
{
    public Guid TenantId { get; set; }

    public Guid OrderId { get; set; }
    public Order? Order { get; set; }

    public Guid ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
    public decimal LineTotal { get; set; }

    public string? Notes { get; set; }
}

public class Customer : BaseEntity
{
    public Guid TenantId { get; set; }

    public required string Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public string? CompanyName { get; set; }
    public string? TaxId { get; set; }

    public string? BillingAddress { get; set; }
    public string? ShippingAddress { get; set; }

    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }

    public bool IsActive { get; set; } = true;
    public decimal CreditLimit { get; set; }

    // Navigation
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
