using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMDT.Models;

namespace TMDT.Services.Interfaces;

/// <summary>
/// 🟢 Service quản lý kho cho Seller: xem tồn kho, nhập/xuất/kiểm kê, lịch sử, báo cáo, import CSV.
/// </summary>
public interface IInventoryService
{
    /// <summary>Lấy danh sách tồn kho (mỗi row = 1 SKU: product hoặc variant) theo shop, có filter keyword + stockLevel.</summary>
    Task<List<InventoryRow>> GetInventoryAsync(int shopId, string? keyword = null, string? stockLevel = null);

    /// <summary>Nhập kho: tăng StockQuantity/Quantity lên qty, ghi transaction.</summary>
    Task<bool> ImportStockAsync(int productId, int? variantId, int qty, string reason, string? performedBy);

    /// <summary>Xuất kho: giảm StockQuantity/Quantity đi qty (validate qty &lt;= current), ghi transaction.</summary>
    Task<bool> ExportStockAsync(int productId, int? variantId, int qty, string reason, string? performedBy);

    /// <summary>Kiểm kê: set trực tiếp tồn kho tới newQty, ghi transaction với change = newQty - before.</summary>
    Task<bool> AdjustStockAsync(int productId, int? variantId, int newQty, string reason, string? performedBy);

    /// <summary>Lấy lịch sử biến động tồn kho theo shop, filter theo khoảng thời gian.</summary>
    Task<List<InventoryTransaction>> GetTransactionsAsync(int shopId, DateTime? from = null, DateTime? to = null);

    /// <summary>Đếm số SKU sắp hết hàng (stock &lt;= threshold) của shop.</summary>
    Task<int> GetLowStockCountAsync(int shopId);

    /// <summary>Tổng hợp báo cáo tồn kho trong khoảng thời gian.</summary>
    Task<InventoryReportRow> GetReportAsync(int shopId, DateTime from, DateTime to);

    /// <summary>Import CSV — format: ProductCode,VariantSku,QuantityChange,Type,Reason. Trả về (success, failed, errors).</summary>
    Task<(int Success, int Failed, List<string> Errors)> ImportCsvAsync(int shopId, string csvText, string? performedBy);

    /// <summary>Xuất báo cáo ra CSV (lịch sử biến động).</summary>
    string ExportReportCsv(int shopId, DateTime from, DateTime to);
}

/// <summary>Row hiển thị trong bảng tồn kho — mỗi row = 1 SKU (product hoặc variant).</summary>
public class InventoryRow
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string? VariantName { get; set; }
    public string MainImageUrl { get; set; } = "";
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; }
    public decimal UnitPrice { get; set; }

    public bool IsLowStock => StockQuantity <= LowStockThreshold && StockQuantity > 0;
    public bool IsOutOfStock => StockQuantity <= 0;
    public decimal InventoryValue => StockQuantity * UnitPrice;

    /// <summary>OK | Low | OutOfStock.</summary>
    public string StockStatus => IsOutOfStock ? "OutOfStock" : IsLowStock ? "Low" : "OK";
}

/// <summary>Tổng hợp báo cáo.</summary>
public class InventoryReportRow
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    // Tổng biến động theo loại
    public int TotalImportQty { get; set; }
    public decimal TotalImportValue { get; set; }
    public int TotalExportQty { get; set; }
    public decimal TotalExportValue { get; set; }
    public int TotalOrderQty { get; set; }
    public int TotalRefundQty { get; set; }
    public int TotalCancelQty { get; set; }
    public int TotalAdjustQty { get; set; }

    // Tổng giá trị tồn kho hiện tại
    public decimal CurrentInventoryValue { get; set; }

    // Top 5 SP biến động nhiều nhất (tính theo |change|)
    public List<TopProductMovementRow> TopMovedProducts { get; set; } = new();
}

public class TopProductMovementRow
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string ProductCode { get; set; } = "";
    public int TotalChange { get; set; }  // tổng |change|
    public int NetChange { get; set; }    // change dương/âm
}
