using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServerApp.Data.Repositories;
using ServerApp.Domain.Entities;
using Shared.DTOs;

namespace ServerApp.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/products")
            .WithTags("Products")
            .RequireAuthorization();

        group.MapGet("/", GetProductsAsync)
            .WithName("GetProducts")
            .WithOpenApi()
            .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)));

        group.MapGet("/{id:guid}", GetProductByIdAsync)
            .WithName("GetProductById")
            .WithOpenApi();

        group.MapPost("/", CreateProductAsync)
            .WithName("CreateProduct")
            .WithOpenApi();

        group.MapPut("/{id:guid}", UpdateProductAsync)
            .WithName("UpdateProduct")
            .WithOpenApi();

        group.MapDelete("/{id:guid}", DeleteProductAsync)
            .WithName("DeleteProduct")
            .WithOpenApi();

        group.MapGet("/search", SearchProductsAsync)
            .WithName("SearchProducts")
            .WithOpenApi();
    }

    private static async Task<IResult> GetProductsAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        IProductRepository productRepository)
    {
        if (pageSize > 100) pageSize = 100;

        var skip = (page - 1) * pageSize;
        var products = await productRepository.GetProductsWithDetailsAsync(skip, pageSize);
        var totalCount = await productRepository.CountAsync();

        var productDTOs = products.Select(p => new ProductDTO
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Stock = p.Stock,
            Available = p.Available,
            SKU = p.SKU,
            CreatedAt = p.CreatedAt,
            Category = p.Category != null ? new CategoryDTO
            {
                Id = p.Category.Id,
                Name = p.Category.Name,
                Description = p.Category.Description
            } : null,
            Supplier = p.Supplier != null ? new SupplierDTO
            {
                Id = p.Supplier.Id,
                Name = p.Supplier.Name,
                ContactName = p.Supplier.ContactName,
                Email = p.Supplier.Email,
                Phone = p.Supplier.Phone,
                Country = p.Supplier.Country
            } : null
        });

        var result = new PagedResult<ProductDTO>
        {
            Items = productDTOs,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Results.Ok(result);
    }

    private static async Task<IResult> GetProductByIdAsync(
        Guid id,
        IProductRepository productRepository)
    {
        var product = await productRepository.GetProductWithDetailsAsync(id);

        if (product == null)
        {
            return Results.NotFound(new { error = "Product not found" });
        }

        var productDTO = new ProductDTO
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            Available = product.Available,
            SKU = product.SKU,
            CreatedAt = product.CreatedAt,
            Category = product.Category != null ? new CategoryDTO
            {
                Id = product.Category.Id,
                Name = product.Category.Name,
                Description = product.Category.Description
            } : null,
            Supplier = product.Supplier != null ? new SupplierDTO
            {
                Id = product.Supplier.Id,
                Name = product.Supplier.Name,
                ContactName = product.Supplier.ContactName,
                Email = product.Supplier.Email,
                Phone = product.Supplier.Phone,
                Country = product.Supplier.Country
            } : null
        };

        return Results.Ok(productDTO);
    }

    private static async Task<IResult> CreateProductAsync(
        [FromBody] CreateProductRequest request,
        IProductRepository productRepository,
        HttpContext httpContext)
    {
        var tenantId = Guid.Parse(httpContext.User.FindFirst("TenantId")!.Value);

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Stock = request.Stock,
            Available = request.Available,
            CategoryId = request.CategoryId,
            SupplierId = request.SupplierId,
            SKU = request.SKU,
            CostPrice = request.CostPrice,
            TenantId = tenantId
        };

        var created = await productRepository.AddAsync(product);

        return Results.Created($"/api/v1/products/{created.Id}", new { id = created.Id });
    }

    private static async Task<IResult> UpdateProductAsync(
        Guid id,
        [FromBody] UpdateProductRequest request,
        IProductRepository productRepository)
    {
        var product = await productRepository.GetByIdAsync(id);

        if (product == null)
        {
            return Results.NotFound(new { error = "Product not found" });
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Stock = request.Stock;
        product.Available = request.Available;
        product.CategoryId = request.CategoryId;
        product.SupplierId = request.SupplierId;
        product.SKU = request.SKU;
        product.CostPrice = request.CostPrice;

        await productRepository.UpdateAsync(product);

        return Results.NoContent();
    }

    private static async Task<IResult> DeleteProductAsync(
        Guid id,
        IProductRepository productRepository)
    {
        var product = await productRepository.GetByIdAsync(id);

        if (product == null)
        {
            return Results.NotFound(new { error = "Product not found" });
        }

        await productRepository.DeleteAsync(id);

        return Results.NoContent();
    }

    private static async Task<IResult> SearchProductsAsync(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        IProductRepository productRepository)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Results.BadRequest(new { error = "Search query is required" });
        }

        if (pageSize > 100) pageSize = 100;

        var skip = (page - 1) * pageSize;
        var products = await productRepository.SearchProductsAsync(q, skip, pageSize);

        var productDTOs = products.Select(p => new ProductDTO
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Stock = p.Stock,
            Available = p.Available,
            SKU = p.SKU,
            CreatedAt = p.CreatedAt,
            Category = p.Category != null ? new CategoryDTO
            {
                Id = p.Category.Id,
                Name = p.Category.Name
            } : null,
            Supplier = p.Supplier != null ? new SupplierDTO
            {
                Id = p.Supplier.Id,
                Name = p.Supplier.Name
            } : null
        });

        return Results.Ok(productDTOs);
    }
}
