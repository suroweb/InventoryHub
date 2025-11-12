using ClosedXML.Excel;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using ServerApp.Data.Contexts;
using System.Globalization;
using System.Text;

namespace ServerApp.Services;

public interface IExportService
{
    Task<byte[]> ExportProductsToCsvAsync();
    Task<byte[]> ExportProductsToExcelAsync();
    Task<byte[]> ExportOrdersToCsvAsync(DateTime? from, DateTime? to);
    Task<byte[]> ExportOrdersToExcelAsync(DateTime? from, DateTime? to);
    Task<byte[]> ExportInventoryReportToExcelAsync();
    Task<Stream> GenerateInventoryPdfAsync();
}

public class ExportService : IExportService
{
    private readonly TenantDbContext _context;
    private readonly ITenantService _tenantService;

    public ExportService(TenantDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<byte[]> ExportProductsToCsvAsync()
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .ToListAsync();

        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        var records = products.Select(p => new
        {
            p.Id,
            p.Name,
            p.SKU,
            p.Description,
            p.Price,
            p.CostPrice,
            p.Stock,
            p.Available,
            Category = p.Category?.Name,
            Supplier = p.Supplier?.Name,
            p.CreatedAt
        });

        await csv.WriteRecordsAsync(records);
        await writer.FlushAsync();

        return memoryStream.ToArray();
    }

    public async Task<byte[]> ExportProductsToExcelAsync()
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = worksheet.AddWorksheet("Products");

        // Headers
        worksheet.Cell(1, 1).Value = "SKU";
        worksheet.Cell(1, 2).Value = "Name";
        worksheet.Cell(1, 3).Value = "Description";
        worksheet.Cell(1, 4).Value = "Category";
        worksheet.Cell(1, 5).Value = "Supplier";
        worksheet.Cell(1, 6).Value = "Price";
        worksheet.Cell(1, 7).Value = "Cost";
        worksheet.Cell(1, 8).Value = "Stock";
        worksheet.Cell(1, 9).Value = "Available";
        worksheet.Cell(1, 10).Value = "Margin %";
        worksheet.Cell(1, 11).Value = "Value";
        worksheet.Cell(1, 12).Value = "Created";

        // Style headers
        var headerRange = worksheet.Range(1, 1, 1, 12);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;

        // Data
        int row = 2;
        foreach (var product in products)
        {
            worksheet.Cell(row, 1).Value = product.SKU;
            worksheet.Cell(row, 2).Value = product.Name;
            worksheet.Cell(row, 3).Value = product.Description;
            worksheet.Cell(row, 4).Value = product.Category?.Name;
            worksheet.Cell(row, 5).Value = product.Supplier?.Name;
            worksheet.Cell(row, 6).Value = product.Price;
            worksheet.Cell(row, 7).Value = product.CostPrice;
            worksheet.Cell(row, 8).Value = product.Stock;
            worksheet.Cell(row, 9).Value = product.Available ? "Yes" : "No";

            // Calculate margin
            if (product.Price > 0 && product.CostPrice.HasValue)
            {
                var margin = ((product.Price - product.CostPrice.Value) / product.Price) * 100;
                worksheet.Cell(row, 10).Value = $"{margin:F2}%";
            }

            // Calculate inventory value
            var value = product.Price * product.Stock;
            worksheet.Cell(row, 11).Value = value;
            worksheet.Cell(row, 11).Style.NumberFormat.Format = "$#,##0.00";

            worksheet.Cell(row, 12).Value = product.CreatedAt.ToString("yyyy-MM-dd");

            row++;
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();

        // Add totals row
        worksheet.Cell(row, 10).Value = "TOTAL:";
        worksheet.Cell(row, 10).Style.Font.Bold = true;
        worksheet.Cell(row, 11).FormulaA1 = $"=SUM(K2:K{row - 1})";
        worksheet.Cell(row, 11).Style.Font.Bold = true;
        worksheet.Cell(row, 11).Style.NumberFormat.Format = "$#,##0.00";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportOrdersToCsvAsync(DateTime? from, DateTime? to)
    {
        var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
        var toDate = to ?? DateTime.UtcNow;

        var orders = await _context.Orders
            .Include(o => o.Customer)
            .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate)
            .ToListAsync();

        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        var records = orders.Select(o => new
        {
            o.OrderNumber,
            o.OrderDate,
            Type = o.Type.ToString(),
            Status = o.Status.ToString(),
            Customer = o.Customer?.Name,
            o.SubTotal,
            o.TaxAmount,
            o.ShippingCost,
            o.TotalAmount,
            o.ExpectedDate,
            o.FulfilledDate
        });

        await csv.WriteRecordsAsync(records);
        await writer.FlushAsync();

        return memoryStream.ToArray();
    }

    public async Task<byte[]> ExportOrdersToExcelAsync(DateTime? from, DateTime? to)
    {
        var fromDate = from ?? DateTime.UtcNow.AddMonths(-1);
        var toDate = to ?? DateTime.UtcNow;

        var orders = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate)
            .ToListAsync();

        using var workbook = new XLWorkbook();

        // Orders Summary Sheet
        var summarySheet = workbook.AddWorksheet("Orders Summary");
        summarySheet.Cell(1, 1).Value = "Order #";
        summarySheet.Cell(1, 2).Value = "Date";
        summarySheet.Cell(1, 3).Value = "Customer";
        summarySheet.Cell(1, 4).Value = "Type";
        summarySheet.Cell(1, 5).Value = "Status";
        summarySheet.Cell(1, 6).Value = "Subtotal";
        summarySheet.Cell(1, 7).Value = "Tax";
        summarySheet.Cell(1, 8).Value = "Shipping";
        summarySheet.Cell(1, 9).Value = "Total";

        summarySheet.Range(1, 1, 1, 9).Style.Font.Bold = true;
        summarySheet.Range(1, 1, 1, 9).Style.Fill.BackgroundColor = XLColor.LightBlue;

        int row = 2;
        foreach (var order in orders)
        {
            summarySheet.Cell(row, 1).Value = order.OrderNumber;
            summarySheet.Cell(row, 2).Value = order.OrderDate.ToString("yyyy-MM-dd HH:mm");
            summarySheet.Cell(row, 3).Value = order.Customer?.Name ?? "N/A";
            summarySheet.Cell(row, 4).Value = order.Type.ToString();
            summarySheet.Cell(row, 5).Value = order.Status.ToString();
            summarySheet.Cell(row, 6).Value = order.SubTotal;
            summarySheet.Cell(row, 7).Value = order.TaxAmount;
            summarySheet.Cell(row, 8).Value = order.ShippingCost;
            summarySheet.Cell(row, 9).Value = order.TotalAmount;
            row++;
        }

        summarySheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportInventoryReportToExcelAsync()
    {
        var stockLevels = await _context.StockLevels
            .Include(s => s.Product)
                .ThenInclude(p => p!.Category)
            .Include(s => s.Location)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Inventory Report");

        // Headers
        worksheet.Cell(1, 1).Value = "Location";
        worksheet.Cell(1, 2).Value = "SKU";
        worksheet.Cell(1, 3).Value = "Product";
        worksheet.Cell(1, 4).Value = "Category";
        worksheet.Cell(1, 5).Value = "Quantity";
        worksheet.Cell(1, 6).Value = "Reorder Point";
        worksheet.Cell(1, 7).Value = "Status";
        worksheet.Cell(1, 8).Value = "Unit Price";
        worksheet.Cell(1, 9).Value = "Total Value";
        worksheet.Cell(1, 10).Value = "Last Counted";

        worksheet.Range(1, 1, 1, 10).Style.Font.Bold = true;
        worksheet.Range(1, 1, 1, 10).Style.Fill.BackgroundColor = XLColor.LightBlue;

        int row = 2;
        foreach (var stock in stockLevels)
        {
            worksheet.Cell(row, 1).Value = stock.Location?.Name;
            worksheet.Cell(row, 2).Value = stock.Product?.SKU;
            worksheet.Cell(row, 3).Value = stock.Product?.Name;
            worksheet.Cell(row, 4).Value = stock.Product?.Category?.Name;
            worksheet.Cell(row, 5).Value = stock.Quantity;
            worksheet.Cell(row, 6).Value = stock.ReorderPoint;

            // Status with conditional formatting
            string status;
            XLColor statusColor;

            if (stock.Quantity == 0)
            {
                status = "OUT OF STOCK";
                statusColor = XLColor.Red;
            }
            else if (stock.Quantity <= stock.ReorderPoint)
            {
                status = "LOW STOCK";
                statusColor = XLColor.Orange;
            }
            else
            {
                status = "IN STOCK";
                statusColor = XLColor.Green;
            }

            worksheet.Cell(row, 7).Value = status;
            worksheet.Cell(row, 7).Style.Fill.BackgroundColor = statusColor;
            worksheet.Cell(row, 7).Style.Font.FontColor = XLColor.White;

            worksheet.Cell(row, 8).Value = stock.Product?.Price ?? 0;
            worksheet.Cell(row, 9).Value = (stock.Product?.Price ?? 0) * stock.Quantity;
            worksheet.Cell(row, 10).Value = stock.LastCountedAt?.ToString("yyyy-MM-dd") ?? "Never";

            row++;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<Stream> GenerateInventoryPdfAsync()
    {
        // TODO: Implement PDF generation with iTextSharp
        // This is a placeholder - full implementation would use iTextSharp.LGPLv2.Core
        throw new NotImplementedException("PDF generation will be implemented in next phase");
    }
}
