using Microsoft.EntityFrameworkCore;
using QRCoder;
using ServerApp.Data.Contexts;
using ServerApp.Domain.Entities;

namespace ServerApp.Services;

public interface IBarcodeService
{
    Task<byte[]> GenerateQRCodeAsync(Guid productId, int pixelsPerModule = 10);
    Task<byte[]> GenerateQRCodeForTextAsync(string text, int pixelsPerModule = 10);
    Task<ProductBarcode> CreateBarcodeAsync(Guid productId, string barcodeValue, BarcodeType type, bool isPrimary = false);
    Task<ProductBarcode?> GetPrimaryBarcodeAsync(Guid productId);
    Task<Product?> FindProductByBarcodeAsync(string barcodeValue);
}

public class BarcodeService : IBarcodeService
{
    private readonly TenantDbContext _context;
    private readonly ITenantService _tenantService;

    public BarcodeService(TenantDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<byte[]> GenerateQRCodeAsync(Guid productId, int pixelsPerModule = 10)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            throw new InvalidOperationException("Product not found");

        // Create JSON data for QR code
        var qrData = System.Text.Json.JsonSerializer.Serialize(new
        {
            productId = product.Id,
            sku = product.SKU,
            name = product.Name,
            price = product.Price
        });

        return GenerateQRCodeForTextAsync(qrData, pixelsPerModule).Result;
    }

    public Task<byte[]> GenerateQRCodeForTextAsync(string text, int pixelsPerModule = 10)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);

        var qrCodeImage = qrCode.GetGraphic(pixelsPerModule);

        return Task.FromResult(qrCodeImage);
    }

    public async Task<ProductBarcode> CreateBarcodeAsync(Guid productId, string barcodeValue, BarcodeType type, bool isPrimary = false)
    {
        // Check if barcode already exists
        var existing = await _context.ProductBarcodes
            .FirstOrDefaultAsync(b => b.BarcodeValue == barcodeValue);

        if (existing != null)
            throw new InvalidOperationException("Barcode already exists for another product");

        // If setting as primary, remove primary flag from others
        if (isPrimary)
        {
            var existingPrimary = await _context.ProductBarcodes
                .Where(b => b.ProductId == productId && b.IsPrimary)
                .ToListAsync();

            foreach (var barcode in existingPrimary)
            {
                barcode.IsPrimary = false;
            }
        }

        var productBarcode = new ProductBarcode
        {
            ProductId = productId,
            BarcodeValue = barcodeValue,
            Type = type,
            IsPrimary = isPrimary,
            TenantId = _tenantService.GetTenantId()
        };

        _context.ProductBarcodes.Add(productBarcode);
        await _context.SaveChangesAsync();

        return productBarcode;
    }

    public async Task<ProductBarcode?> GetPrimaryBarcodeAsync(Guid productId)
    {
        return await _context.ProductBarcodes
            .FirstOrDefaultAsync(b => b.ProductId == productId && b.IsPrimary);
    }

    public async Task<Product?> FindProductByBarcodeAsync(string barcodeValue)
    {
        var barcode = await _context.ProductBarcodes
            .Include(b => b.Product)
            .FirstOrDefaultAsync(b => b.BarcodeValue == barcodeValue);

        return barcode?.Product;
    }
}
