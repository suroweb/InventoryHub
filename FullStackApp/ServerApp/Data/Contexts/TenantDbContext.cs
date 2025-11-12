using Microsoft.EntityFrameworkCore;
using ServerApp.Domain.Entities;

namespace ServerApp.Data.Contexts;

/// <summary>
/// Tenant-specific database context
/// Each tenant has their own database with this schema
/// </summary>
public class TenantDbContext : DbContext
{
    private readonly Guid _tenantId;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantDbContext(DbContextOptions<TenantDbContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
        // Get tenant ID from HTTP context
        var tenantIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
        _tenantId = tenantIdClaim != null ? Guid.Parse(tenantIdClaim) : Guid.Empty;
    }

    // Core Entities
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Customer> Customers { get; set; }

    // Location & Stock Management
    public DbSet<Location> Locations { get; set; }
    public DbSet<StockLevel> StockLevels { get; set; }
    public DbSet<StockTransfer> StockTransfers { get; set; }
    public DbSet<StockAdjustment> StockAdjustments { get; set; }

    // Order Management
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    // Security & RBAC
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }

    // Audit & Compliance
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Alert> Alerts { get; set; }

    // Integrations
    public DbSet<Webhook> Webhooks { get; set; }
    public DbSet<WebhookLog> WebhookLogs { get; set; }

    // Advanced Features
    public DbSet<Report> Reports { get; set; }
    public DbSet<ScheduledReport> ScheduledReports { get; set; }
    public DbSet<ProductBarcode> ProductBarcodes { get; set; }
    public DbSet<ForecastModel> ForecastModels { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureProduct(builder);
        ConfigureCategory(builder);
        ConfigureSupplier(builder);
        ConfigureCustomer(builder);
        ConfigureLocation(builder);
        ConfigureStock(builder);
        ConfigureOrder(builder);
        ConfigureRoles(builder);
        ConfigureAudit(builder);
        ConfigureWebhooks(builder);
        ConfigureAdvanced(builder);
    }

    private void ConfigureProduct(ModelBuilder builder)
    {
        builder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.TenantId);
            entity.HasIndex(p => p.SKU);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Price).HasPrecision(18, 2);
            entity.Property(p => p.CostPrice).HasPrecision(18, 2);

            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(p => p.TenantId == _tenantId && !p.IsDeleted);
        });
    }

    private void ConfigureCategory(ModelBuilder builder)
    {
        builder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.TenantId);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
            entity.HasQueryFilter(c => c.TenantId == _tenantId && !c.IsDeleted);
        });
    }

    private void ConfigureSupplier(ModelBuilder builder)
    {
        builder.Entity<Supplier>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.TenantId);
            entity.Property(s => s.Name).IsRequired().HasMaxLength(200);
            entity.HasQueryFilter(s => s.TenantId == _tenantId && !s.IsDeleted);
        });
    }

    private void ConfigureCustomer(ModelBuilder builder)
    {
        builder.Entity<Customer>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.TenantId);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
            entity.Property(c => c.CreditLimit).HasPrecision(18, 2);
            entity.HasQueryFilter(c => c.TenantId == _tenantId && !c.IsDeleted);
        });
    }

    private void ConfigureLocation(ModelBuilder builder)
    {
        builder.Entity<Location>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.HasIndex(l => l.TenantId);
            entity.HasIndex(l => l.Code);
            entity.Property(l => l.Name).IsRequired().HasMaxLength(200);
            entity.HasQueryFilter(l => l.TenantId == _tenantId && !l.IsDeleted);
        });
    }

    private void ConfigureStock(ModelBuilder builder)
    {
        builder.Entity<StockLevel>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => new { s.TenantId, s.ProductId, s.LocationId });

            entity.HasOne(s => s.Product)
                .WithMany()
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Location)
                .WithMany(l => l.StockLevels)
                .HasForeignKey(s => s.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(s => s.TenantId == _tenantId && !s.IsDeleted);
        });

        builder.Entity<StockTransfer>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.TenantId);
            entity.HasIndex(s => s.TransferNumber).IsUnique();

            entity.HasOne(s => s.FromLocation)
                .WithMany(l => l.TransfersFrom)
                .HasForeignKey(s => s.FromLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.ToLocation)
                .WithMany(l => l.TransfersTo)
                .HasForeignKey(s => s.ToLocationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.Product)
                .WithMany()
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(s => s.TenantId == _tenantId && !s.IsDeleted);
        });

        builder.Entity<StockAdjustment>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.TenantId);

            entity.HasOne(s => s.Product)
                .WithMany()
                .HasForeignKey(s => s.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(s => s.TenantId == _tenantId && !s.IsDeleted);
        });
    }

    private void ConfigureOrder(ModelBuilder builder)
    {
        builder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.HasIndex(o => o.TenantId);
            entity.HasIndex(o => o.OrderNumber).IsUnique();

            entity.Property(o => o.SubTotal).HasPrecision(18, 2);
            entity.Property(o => o.TaxAmount).HasPrecision(18, 2);
            entity.Property(o => o.ShippingCost).HasPrecision(18, 2);
            entity.Property(o => o.TotalAmount).HasPrecision(18, 2);

            entity.HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Supplier)
                .WithMany()
                .HasForeignKey(o => o.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(o => o.TenantId == _tenantId && !o.IsDeleted);
        });

        builder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.HasIndex(o => o.TenantId);

            entity.Property(o => o.UnitPrice).HasPrecision(18, 2);
            entity.Property(o => o.LineTotal).HasPrecision(18, 2);

            entity.HasOne(o => o.Order)
                .WithMany(ord => ord.Items)
                .HasForeignKey(o => o.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(o => o.Product)
                .WithMany()
                .HasForeignKey(o => o.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(o => o.TenantId == _tenantId && !o.IsDeleted);
        });
    }

    private void ConfigureRoles(ModelBuilder builder)
    {
        builder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.TenantId);
            entity.Property(r => r.Name).IsRequired().HasMaxLength(100);
            entity.HasQueryFilter(r => r.TenantId == _tenantId && !r.IsDeleted);
        });

        builder.Entity<UserRole>(entity =>
        {
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });

            entity.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureAudit(ModelBuilder builder)
    {
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.TenantId);
            entity.HasIndex(a => a.Timestamp);
            entity.HasIndex(a => new { a.EntityType, a.EntityId });
            entity.HasQueryFilter(a => a.TenantId == _tenantId);
        });

        builder.Entity<Alert>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.TenantId);
            entity.HasIndex(a => a.IsRead);
            entity.HasQueryFilter(a => a.TenantId == _tenantId && !a.IsDeleted);
        });
    }

    private void ConfigureWebhooks(ModelBuilder builder)
    {
        builder.Entity<Webhook>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.HasIndex(w => w.TenantId);
            entity.HasQueryFilter(w => w.TenantId == _tenantId && !w.IsDeleted);
        });

        builder.Entity<WebhookLog>(entity =>
        {
            entity.HasKey(w => w.Id);

            entity.HasOne(w => w.Webhook)
                .WithMany()
                .HasForeignKey(w => w.WebhookId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureAdvanced(ModelBuilder builder)
    {
        builder.Entity<Report>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.TenantId);
            entity.HasQueryFilter(r => r.TenantId == _tenantId && !r.IsDeleted);
        });

        builder.Entity<ScheduledReport>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.TenantId);

            entity.HasOne(s => s.Report)
                .WithMany()
                .HasForeignKey(s => s.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(s => s.TenantId == _tenantId && !s.IsDeleted);
        });

        builder.Entity<ProductBarcode>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.TenantId);
            entity.HasIndex(p => p.BarcodeValue);

            entity.HasOne(p => p.Product)
                .WithMany()
                .HasForeignKey(p => p.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasQueryFilter(p => p.TenantId == _tenantId && !p.IsDeleted);
        });

        builder.Entity<ForecastModel>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.HasIndex(f => f.TenantId);

            entity.HasOne(f => f.Product)
                .WithMany()
                .HasForeignKey(f => f.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(f => f.Accuracy).HasPrecision(5, 2);

            entity.HasQueryFilter(f => f.TenantId == _tenantId && !f.IsDeleted);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        // Auto-set TenantId and Audit fields
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added && entry.Entity is Shared.Models.BaseEntity addedEntity)
            {
                // Set TenantId using reflection for all tenant entities
                var tenantIdProp = entry.Entity.GetType().GetProperty("TenantId");
                if (tenantIdProp != null)
                {
                    var currentTenantId = (Guid?)tenantIdProp.GetValue(entry.Entity);
                    if (currentTenantId == Guid.Empty || currentTenantId == null)
                    {
                        tenantIdProp.SetValue(entry.Entity, _tenantId);
                    }
                }

                addedEntity.CreatedAt = DateTime.UtcNow;
                addedEntity.CreatedBy = currentUserId;
            }
            else if (entry.State == EntityState.Modified && entry.Entity is Shared.Models.BaseEntity modifiedEntity)
            {
                modifiedEntity.UpdatedAt = DateTime.UtcNow;
                modifiedEntity.UpdatedBy = currentUserId;
            }
        }

        // Create audit logs for important entities
        await CreateAuditLogsAsync(cancellationToken);

        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task CreateAuditLogsAsync(CancellationToken cancellationToken)
    {
        var auditableEntities = new[] { "Product", "Order", "StockTransfer", "StockAdjustment", "Customer", "Supplier" };
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userEmail = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        var userAgent = _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString();

        foreach (var entry in ChangeTracker.Entries())
        {
            var entityType = entry.Entity.GetType().Name;

            if (!auditableEntities.Contains(entityType))
                continue;

            if (entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
            {
                var entityId = (Guid?)entry.Entity.GetType().GetProperty("Id")?.GetValue(entry.Entity);

                var auditLog = new AuditLog
                {
                    TenantId = _tenantId,
                    UserId = userId ?? "System",
                    UserEmail = userEmail,
                    Action = entry.State.ToString(),
                    EntityType = entityType,
                    EntityId = entityId,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    Timestamp = DateTime.UtcNow
                };

                if (entry.State == EntityState.Modified)
                {
                    var oldValues = new Dictionary<string, object?>();
                    var newValues = new Dictionary<string, object?>();

                    foreach (var prop in entry.Properties)
                    {
                        if (prop.IsModified && prop.Metadata.Name != "UpdatedAt" && prop.Metadata.Name != "UpdatedBy")
                        {
                            oldValues[prop.Metadata.Name] = prop.OriginalValue;
                            newValues[prop.Metadata.Name] = prop.CurrentValue;
                        }
                    }

                    auditLog.OldValues = System.Text.Json.JsonSerializer.Serialize(oldValues);
                    auditLog.NewValues = System.Text.Json.JsonSerializer.Serialize(newValues);
                }

                AuditLogs.Add(auditLog);
            }
        }
    }
}
