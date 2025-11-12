using Microsoft.EntityFrameworkCore;
using ServerApp.Data.Contexts;
using ServerApp.Domain.Entities;

namespace ServerApp.Data.Repositories;

public interface IProductRepository : IRepository<Product>
{
    Task<IEnumerable<Product>> GetProductsWithDetailsAsync(int skip, int take);
    Task<Product?> GetProductWithDetailsAsync(Guid id);
    Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm, int skip, int take);
    Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold);
}

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(TenantDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Product>> GetProductsWithDetailsAsync(int skip, int take)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Product?> GetProductWithDetailsAsync(Guid id)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm, int skip, int take)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Where(p => p.Name.Contains(searchTerm) ||
                        (p.Description != null && p.Description.Contains(searchTerm)) ||
                        (p.SKU != null && p.SKU.Contains(searchTerm)))
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold)
    {
        return await _dbSet
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Where(p => p.Stock <= threshold)
            .ToListAsync();
    }
}
