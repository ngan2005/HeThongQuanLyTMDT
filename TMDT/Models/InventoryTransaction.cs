using System;

namespace TMDT.Models;

/// <summary>
/// 🟢 Lịch sử biến động tồn kho — ghi log mỗi lần Product.StockQuantity / ProductVariant.Quantity thay đổi.
/// Type: Import (nhập), Export (xuất), Adjust (kiểm kê), Order (bán), Refund (hoàn), Cancel (hủy).
/// ReferenceType: Manual | CSV | Order.
/// </summary>
public class InventoryTransaction
{
    public int TransactionId { get; set; }

    public int? ProductId { get; set; }

    public int? VariantId { get; set; }

    public int ShopId { get; set; }

    /// <summary>"Import" | "Export" | "Adjust" | "Order" | "Refund" | "Cancel".</summary>
    public string Type { get; set; } = "";

    public int QuantityBefore { get; set; }

    /// <summary>Dương nếu nhập, âm nếu xuất, ký tùy ý nếu adjust.</summary>
    public int QuantityChange { get; set; }

    public int QuantityAfter { get; set; }

    public string Reason { get; set; } = "";

    public string? ReferenceOrderCode { get; set; }

    /// <summary>"Manual" | "CSV" | "Order".</summary>
    public string? ReferenceType { get; set; }

    public string? PerformedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public Product? Product { get; set; }

    public ProductVariant? Variant { get; set; }

    public Shop? Shop { get; set; }
}
