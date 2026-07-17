using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Services.Interfaces;

namespace TMDT.Services;

/// <summary>
/// 🟢 Service quản lý kho cho Seller — singleton pattern giống OrderService.Instance.
/// </summary>
public class InventoryService : IInventoryService
{
    private static InventoryService? _instance;
    public static InventoryService Instance => _instance ??= new InventoryService();

    private InventoryService() { }

    public async Task<List<InventoryRow>> GetInventoryAsync(int shopId, string? keyword = null, string? stockLevel = null)
    {
        using var ctx = new TmdtContext();

        var query = ctx.Products
            .AsNoTracking()
            .Include(p => p.ProductImages)
            .Include(p => p.ProductVariants)
            .Where(p => p.ShopId == shopId && p.Status != "Deleted");

        var list = new List<InventoryRow>();

        var products = await query.ToListAsync();
        foreach (var p in products)
        {
            // Nếu SP có variant → 1 row / variant
            if (p.ProductVariants != null && p.ProductVariants.Count > 0)
            {
                foreach (var v in p.ProductVariants)
                {
                    var row = new InventoryRow
                    {
                        ProductId = p.ProductId,
                        VariantId = v.VariantId,
                        ProductCode = p.ProductCode ?? "",
                        ProductName = p.ProductName ?? "",
                        VariantName = v.VariantName,
                        MainImageUrl = p.MainImageUrl ?? "",
                        StockQuantity = v.Quantity ?? 0,
                        LowStockThreshold = p.LowStockThreshold,
                        UnitPrice = (p.Price) + (v.ExtraPrice ?? 0m)
                    };
                    if (MatchFilter(row, keyword, stockLevel)) list.Add(row);
                }
            }
            else
            {
                var row = new InventoryRow
                {
                    ProductId = p.ProductId,
                    VariantId = null,
                    ProductCode = p.ProductCode ?? "",
                    ProductName = p.ProductName ?? "",
                    VariantName = null,
                    MainImageUrl = p.MainImageUrl ?? "",
                    StockQuantity = p.StockQuantity ?? 0,
                    LowStockThreshold = p.LowStockThreshold,
                    UnitPrice = p.Price
                };
                if (MatchFilter(row, keyword, stockLevel)) list.Add(row);
            }
        }

        return list.OrderBy(r => r.ProductName).ThenBy(r => r.VariantName).ToList();
    }

    private static bool MatchFilter(InventoryRow row, string? keyword, string? stockLevel)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            string kw = keyword.Trim();
            if (!((row.ProductName?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false)
                  || (row.ProductCode?.Contains(kw, StringComparison.OrdinalIgnoreCase) ?? false)))
                return false;
        }

        if (!string.IsNullOrWhiteSpace(stockLevel) && stockLevel != "All")
        {
            if (stockLevel == "Low" && !row.IsLowStock) return false;
            if (stockLevel == "Out" && !row.IsOutOfStock) return false;
        }

        return true;
    }

    public async Task<bool> ImportStockAsync(int productId, int? variantId, int qty, string reason, string? performedBy)
    {
        if (qty <= 0) throw new ArgumentException("Số lượng nhập phải lớn hơn 0.");

        using var ctx = new TmdtContext();
        await using var tx = await ctx.Database.BeginTransactionAsync();
        try
        {
            int before, after;
            int shopId;
            int? resolvedProductId;

            if (variantId.HasValue)
            {
                var variant = await ctx.ProductVariants.FindAsync(variantId.Value);
                if (variant == null) throw new InvalidOperationException("Không tìm thấy biến thể sản phẩm.");
                var product = await ctx.Products.FindAsync(variant.ProductId);
                if (product == null || product.ShopId == null) throw new InvalidOperationException("Không tìm thấy sản phẩm.");
                shopId = product.ShopId.Value;
                resolvedProductId = product.ProductId;

                before = variant.Quantity ?? 0;
                after = before + qty;
                variant.Quantity = after;
            }
            else
            {
                var product = await ctx.Products.FindAsync(productId);
                if (product == null || product.ShopId == null) throw new InvalidOperationException("Không tìm thấy sản phẩm.");
                shopId = product.ShopId.Value;
                resolvedProductId = productId;

                before = product.StockQuantity ?? 0;
                after = before + qty;
                product.StockQuantity = after;
            }

            ctx.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = resolvedProductId,
                VariantId = variantId,
                ShopId = shopId,
                Type = "Import",
                QuantityBefore = before,
                QuantityChange = qty,
                QuantityAfter = after,
                Reason = reason ?? "Nhập từ NCC",
                ReferenceType = "Manual",
                PerformedBy = performedBy,
                CreatedAt = DateTime.Now
            });

            await ctx.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> ExportStockAsync(int productId, int? variantId, int qty, string reason, string? performedBy)
    {
        if (qty <= 0) throw new ArgumentException("Số lượng xuất phải lớn hơn 0.");

        using var ctx = new TmdtContext();
        await using var tx = await ctx.Database.BeginTransactionAsync();
        try
        {
            int before, after;
            int shopId;
            int? resolvedProductId;

            if (variantId.HasValue)
            {
                var variant = await ctx.ProductVariants.FindAsync(variantId.Value);
                if (variant == null) throw new InvalidOperationException("Không tìm thấy biến thể sản phẩm.");
                var product = await ctx.Products.FindAsync(variant.ProductId);
                if (product == null || product.ShopId == null) throw new InvalidOperationException("Không tìm thấy sản phẩm.");
                shopId = product.ShopId.Value;
                resolvedProductId = product.ProductId;

                before = variant.Quantity ?? 0;
                if (before < qty) throw new InvalidOperationException($"Tồn kho không đủ (hiện có {before}, yêu cầu xuất {qty}).");
                after = before - qty;
                variant.Quantity = after;
            }
            else
            {
                var product = await ctx.Products.FindAsync(productId);
                if (product == null || product.ShopId == null) throw new InvalidOperationException("Không tìm thấy sản phẩm.");
                shopId = product.ShopId.Value;
                resolvedProductId = productId;

                before = product.StockQuantity ?? 0;
                if (before < qty) throw new InvalidOperationException($"Tồn kho không đủ (hiện có {before}, yêu cầu xuất {qty}).");
                after = before - qty;
                product.StockQuantity = after;
            }

            ctx.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = resolvedProductId,
                VariantId = variantId,
                ShopId = shopId,
                Type = "Export",
                QuantityBefore = before,
                QuantityChange = -qty,
                QuantityAfter = after,
                Reason = reason ?? "Xuất kho",
                ReferenceType = "Manual",
                PerformedBy = performedBy,
                CreatedAt = DateTime.Now
            });

            await ctx.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> AdjustStockAsync(int productId, int? variantId, int newQty, string reason, string? performedBy)
    {
        if (newQty < 0) throw new ArgumentException("Tồn kho sau kiểm kê không được âm.");

        using var ctx = new TmdtContext();
        await using var tx = await ctx.Database.BeginTransactionAsync();
        try
        {
            int before, after;
            int shopId;
            int? resolvedProductId;

            if (variantId.HasValue)
            {
                var variant = await ctx.ProductVariants.FindAsync(variantId.Value);
                if (variant == null) throw new InvalidOperationException("Không tìm thấy biến thể sản phẩm.");
                var product = await ctx.Products.FindAsync(variant.ProductId);
                if (product == null || product.ShopId == null) throw new InvalidOperationException("Không tìm thấy sản phẩm.");
                shopId = product.ShopId.Value;
                resolvedProductId = product.ProductId;

                before = variant.Quantity ?? 0;
                after = newQty;
                variant.Quantity = after;
            }
            else
            {
                var product = await ctx.Products.FindAsync(productId);
                if (product == null || product.ShopId == null) throw new InvalidOperationException("Không tìm thấy sản phẩm.");
                shopId = product.ShopId.Value;
                resolvedProductId = productId;

                before = product.StockQuantity ?? 0;
                after = newQty;
                product.StockQuantity = after;
            }

            int change = after - before;
            ctx.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = resolvedProductId,
                VariantId = variantId,
                ShopId = shopId,
                Type = "Adjust",
                QuantityBefore = before,
                QuantityChange = change,
                QuantityAfter = after,
                Reason = reason ?? "Kiểm kê",
                ReferenceType = "Manual",
                PerformedBy = performedBy,
                CreatedAt = DateTime.Now
            });

            await ctx.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<List<InventoryTransaction>> GetTransactionsAsync(int shopId, DateTime? from = null, DateTime? to = null)
    {
        using var ctx = new TmdtContext();
        var query = ctx.InventoryTransactions
            .AsNoTracking()
            .Include(t => t.Product)
            .Include(t => t.Variant)
            .Where(t => t.ShopId == shopId);

        if (from.HasValue) query = query.Where(t => t.CreatedAt >= from.Value);
        if (to.HasValue)
        {
            var toEnd = to.Value.Date.AddDays(1);
            query = query.Where(t => t.CreatedAt < toEnd);
        }

        return await query.OrderByDescending(t => t.CreatedAt).Take(500).ToListAsync();
    }

    public async Task<int> GetLowStockCountAsync(int shopId)
    {
        using var ctx = new TmdtContext();

        // Đếm product thường sắp hết
        int productLow = await ctx.Products
            .AsNoTracking()
            .Where(p => p.ShopId == shopId
                        && p.Status != "Deleted"
                        && (p.ProductVariants == null || !p.ProductVariants.Any())
                        && p.StockQuantity <= p.LowStockThreshold)
            .CountAsync();

        // Đếm variant sắp hết (của product có variant)
        int variantLow = await ctx.ProductVariants
            .AsNoTracking()
            .Where(v => v.Product != null
                        && v.Product.ShopId == shopId
                        && v.Product.Status != "Deleted"
                        && v.Quantity <= v.Product.LowStockThreshold)
            .CountAsync();

        return productLow + variantLow;
    }

    public async Task<InventoryReportRow> GetReportAsync(int shopId, DateTime from, DateTime to)
    {
        using var ctx = new TmdtContext();
        var toEnd = to.Date.AddDays(1);

        var txs = await ctx.InventoryTransactions
            .AsNoTracking()
            .Where(t => t.ShopId == shopId && t.CreatedAt >= from && t.CreatedAt < toEnd)
            .ToListAsync();

        var report = new InventoryReportRow
        {
            From = from,
            To = to
        };

        // Tính value theo giá sản phẩm
        var products = await ctx.Products.AsNoTracking().Where(p => p.ShopId == shopId && p.Status != "Deleted").ToListAsync();
        var variants = await ctx.ProductVariants.AsNoTracking().Where(v => v.Product != null && v.Product.ShopId == shopId).ToListAsync();
        decimal GetPrice(int? pid, int? vid)
        {
            if (vid.HasValue)
            {
                var v = variants.FirstOrDefault(x => x.VariantId == vid.Value);
                var p = v != null ? products.FirstOrDefault(x => x.ProductId == v.ProductId) : null;
                if (p == null) return 0m;
                return p.Price + (v.ExtraPrice ?? 0m);
            }
            if (pid.HasValue)
            {
                var p = products.FirstOrDefault(x => x.ProductId == pid.Value);
                return p?.Price ?? 0m;
            }
            return 0m;
        }

        foreach (var t in txs)
        {
            decimal price = GetPrice(t.ProductId, t.VariantId);
            int absChange = Math.Abs(t.QuantityChange);

            switch (t.Type)
            {
                case "Import":
                    report.TotalImportQty += absChange;
                    report.TotalImportValue += absChange * price;
                    break;
                case "Export":
                    report.TotalExportQty += absChange;
                    report.TotalExportValue += absChange * price;
                    break;
                case "Order":
                    report.TotalOrderQty += absChange;
                    break;
                case "Refund":
                    report.TotalRefundQty += absChange;
                    break;
                case "Cancel":
                    report.TotalCancelQty += absChange;
                    break;
                case "Adjust":
                    report.TotalAdjustQty += absChange;
                    break;
            }
        }

        // Current inventory value
        foreach (var p in products)
        {
            if (p.ProductVariants != null && p.ProductVariants.Count > 0)
            {
                foreach (var v in p.ProductVariants)
                {
                    report.CurrentInventoryValue += (v.Quantity ?? 0) * (p.Price + (v.ExtraPrice ?? 0m));
                }
            }
            else
            {
                report.CurrentInventoryValue += (p.StockQuantity ?? 0) * p.Price;
            }
        }

        // Top moved
        report.TopMovedProducts = txs
            .Where(t => t.ProductId.HasValue)
            .GroupBy(t => t.ProductId!.Value)
            .Select(g => new TopProductMovementRow
            {
                ProductId = g.Key,
                ProductName = g.First().Product?.ProductName ?? "(đã xoá)",
                ProductCode = g.First().Product?.ProductCode ?? "",
                TotalChange = g.Sum(x => Math.Abs(x.QuantityChange)),
                NetChange = g.Sum(x => x.QuantityChange)
            })
            .OrderByDescending(x => x.TotalChange)
            .Take(5)
            .ToList();

        return report;
    }

    public async Task<(int Success, int Failed, List<string> Errors)> ImportCsvAsync(int shopId, string csvText, string? performedBy)
    {
        var errors = new List<string>();
        int success = 0;
        int failed = 0;

        if (string.IsNullOrWhiteSpace(csvText))
        {
            errors.Add("File CSV rỗng.");
            return (0, 1, errors);
        }

        using var ctx = new TmdtContext();

        // Parse CSV lines (simple — comma-separated, có thể có quoted values)
        var lines = SplitCsvLines(csvText);
        if (lines.Count < 2)
        {
            errors.Add("File CSV phải có header + ít nhất 1 dòng dữ liệu.");
            return (0, 1, errors);
        }

        var header = SplitCsvRow(lines[0]).Select(h => h.Trim().ToLowerInvariant()).ToList();
        int codeIdx = header.IndexOf("productcode");
        int skuIdx = header.IndexOf("variantsku");
        int qtyIdx = header.IndexOf("quantitychange");
        int typeIdx = header.IndexOf("type");
        int reasonIdx = header.IndexOf("reason");

        if (codeIdx < 0 || qtyIdx < 0 || typeIdx < 0)
        {
            errors.Add("Header CSV phải có: ProductCode,QuantityChange,Type. Có thể thêm VariantSku,Reason.");
            return (0, 1, errors);
        }

        await using var tx = await ctx.Database.BeginTransactionAsync();
        try
        {
            // Cache products/variants của shop để giảm query
            var products = await ctx.Products.Where(p => p.ShopId == shopId && p.Status != "Deleted").ToListAsync();
            var variants = await ctx.ProductVariants
                .Where(v => v.Product != null && v.Product.ShopId == shopId)
                .ToListAsync();

            for (int i = 1; i < lines.Count; i++)
            {
                var rawLine = lines[i];
                if (string.IsNullOrWhiteSpace(rawLine)) continue;

                var cols = SplitCsvRow(rawLine);
                string lineLabel = $"Dòng {i + 1}";

                try
                {
                    string code = cols.ElementAtOrDefault(codeIdx)?.Trim() ?? "";
                    string? sku = skuIdx >= 0 ? cols.ElementAtOrDefault(skuIdx)?.Trim() : null;
                    if (string.IsNullOrEmpty(sku)) sku = null;
                    string qtyStr = cols.ElementAtOrDefault(qtyIdx)?.Trim() ?? "";
                    string type = cols.ElementAtOrDefault(typeIdx)?.Trim() ?? "Import";
                    string reason = reasonIdx >= 0 ? (cols.ElementAtOrDefault(reasonIdx)?.Trim() ?? "Import CSV") : "Import CSV";

                    if (!int.TryParse(qtyStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int qty))
                    {
                        errors.Add($"{lineLabel}: QuantityChange '{qtyStr}' không hợp lệ.");
                        failed++;
                        continue;
                    }

                    Product? product = products.FirstOrDefault(p =>
                        string.Equals(p.ProductCode ?? "", code, StringComparison.OrdinalIgnoreCase));
                    if (product == null)
                    {
                        errors.Add($"{lineLabel}: Không tìm thấy sản phẩm có mã '{code}'.");
                        failed++;
                        continue;
                    }

                    ProductVariant? variant = null;
                    if (sku != null)
                    {
                        variant = variants.FirstOrDefault(v =>
                            v.ProductId == product.ProductId &&
                            string.Equals(v.Sku ?? "", sku, StringComparison.OrdinalIgnoreCase));
                        if (variant == null)
                        {
                            errors.Add($"{lineLabel}: Không tìm thấy biến thể có SKU '{sku}' của '{code}'.");
                            failed++;
                            continue;
                        }
                    }

                    int before = variant != null
                        ? (variant.Quantity ?? 0)
                        : (product.StockQuantity ?? 0);
                    int after;
                    int change;

                    switch (type.ToLowerInvariant())
                    {
                        case "import":
                            change = Math.Abs(qty);
                            after = before + change;
                            if (variant != null) variant.Quantity = after;
                            else product.StockQuantity = after;
                            break;
                        case "export":
                            change = -Math.Abs(qty);
                            if (before < Math.Abs(qty))
                                throw new InvalidOperationException($"Tồn kho không đủ (hiện {before}, yêu cầu xuất {Math.Abs(qty)}).");
                            after = before + change;
                            if (variant != null) variant.Quantity = after;
                            else product.StockQuantity = after;
                            break;
                        case "adjust":
                            if (qty < 0) throw new InvalidOperationException("Adjust không cho phép tồn kho âm.");
                            change = qty - before;
                            after = qty;
                            if (variant != null) variant.Quantity = after;
                            else product.StockQuantity = after;
                            break;
                        default:
                            throw new InvalidOperationException($"Type '{type}' không hợp lệ (chỉ Import/Export/Adjust).");
                    }

                    ctx.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = product.ProductId,
                        VariantId = variant?.VariantId,
                        ShopId = shopId,
                        Type = char.ToUpper(type[0]) + type.Substring(1).ToLowerInvariant(),
                        QuantityBefore = before,
                        QuantityChange = change,
                        QuantityAfter = after,
                        Reason = reason,
                        ReferenceType = "CSV",
                        PerformedBy = performedBy,
                        CreatedAt = DateTime.Now
                    });

                    success++;
                }
                catch (Exception ex)
                {
                    errors.Add($"{lineLabel}: {ex.Message}");
                    failed++;
                }
            }

            await ctx.SaveChangesAsync();
            await tx.CommitAsync();
            return (success, failed, errors);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            errors.Add($"Lỗi hệ thống: {ex.Message}");
            return (0, lines.Count - 1, errors);
        }
    }

    public string ExportReportCsv(int shopId, DateTime from, DateTime to)
    {
        using var ctx = new TmdtContext();
        var toEnd = to.Date.AddDays(1);

        var txs = ctx.InventoryTransactions
            .AsNoTracking()
            .Include(t => t.Product)
            .Include(t => t.Variant)
            .Where(t => t.ShopId == shopId && t.CreatedAt >= from && t.CreatedAt < toEnd)
            .OrderByDescending(t => t.CreatedAt)
            .ToList();

        var sb = new StringBuilder();
        // UTF-8 BOM để Excel mở đúng tiếng Việt
        sb.AppendLine("Thời gian,Loại,Mã SP,Tên SP,Biến thể,Tồn trước,Thay đổi,Tồn sau,Lý do,Mã đơn,Nguồn,Người thực hiện");
        foreach (var t in txs)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                t.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                t.Type ?? "",
                CsvEscape(t.Product?.ProductCode ?? ""),
                CsvEscape(t.Product?.ProductName ?? "(đã xoá)"),
                CsvEscape(t.Variant?.VariantName ?? ""),
                t.QuantityBefore.ToString(),
                t.QuantityChange.ToString(),
                t.QuantityAfter.ToString(),
                CsvEscape(t.Reason ?? ""),
                CsvEscape(t.ReferenceOrderCode ?? ""),
                CsvEscape(t.ReferenceType ?? ""),
                CsvEscape(t.PerformedBy ?? "")
            }));
        }

        return sb.ToString();
    }

    /// <summary>Helper: parse CSV line đơn giản (cho phép quoted với dấu ").</summary>
    private static List<string> SplitCsvLines(string text)
    {
        return text.Replace("\r\n", "\n").Split('\n')
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();
    }

    private static List<string> SplitCsvRow(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else sb.Append(c);
            }
            else
            {
                if (c == ',')
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else sb.Append(c);
            }
        }
        result.Add(sb.ToString());
        return result;
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
