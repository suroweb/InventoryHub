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

    public TenantDbContext(DbContextOptions<TenantDbContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        // Get tenant ID from HTTP context
        var tenantIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
        _tenantId = tenantIdClaim != null ? Guid.Parse(tenantIdClaim) : Guid.Empty;
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Supplier> Suppliers { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Product entity
        builder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.TenantId);
            entity.HasIndex(p => p.SKU);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Price).HasPrecision(18, 2);
            entity.Property(p => p.CostPrice).HasPrecision(18, 2);

            // Relationships
            entity.HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // Tenant isolation filter
            entity.HasQueryFilter(p => p.TenantId == _tenantId && !p.IsDeleted);
        });

        // Configure Category entity
        builder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.TenantId);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(100);

            // Tenant isolation filter
            entity.HasQueryFilter(c => c.TenantId == _tenantId && !c.IsDeleted);
        });

        // Configure Supplier entity
        builder.Entity<Supplier>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.TenantId);
            entity.Property(s => s.Name).IsRequired().HasMaxLength(200);

            // Tenant isolation filter
            entity.HasQueryFilter(s => s.TenantId == _tenantId && !s.IsDeleted);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Automatically set TenantId for new entities
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
        {
            if (entry.Entity is Product product && product.TenantId == Guid.Empty)
            {
                product.TenantId = _tenantId;
            }
            else if (entry.Entity is Category category && category.TenantId == Guid.Empty)
            {
                category.TenantId = _tenantId;
            }
            else if (entry.Entity is Supplier supplier && supplier.TenantId == Guid.Empty)
            {
                supplier.TenantId = _tenantId;
            }
        }

        // Set audit fields
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Shared.Models.BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (Shared.Models.BaseEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
