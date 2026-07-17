using System;
using System.Collections.Generic;
using System.Linq;

namespace TMDT.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public int? ShopId { get; set; }

    public int? CategoryId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? ProductCode { get; set; }

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public decimal? OriginalPrice { get; set; }

    public int? StockQuantity { get; set; }

    /// <summary>Ngưỡng cảnh báo sắp hết hàng (per-product). Mặc định 10.</summary>
    public int LowStockThreshold { get; set; } = 10;

    public int? SoldCount { get; set; }

    public decimal? Rating { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual Category? Category { get; set; }

    public virtual ICollection<FlashSale> FlashSales { get; set; } = new List<FlashSale>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<ProductComparison> ProductComparisons { get; set; } = new List<ProductComparison>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<ProductVariant> ProductVariants { get; set; } = new List<ProductVariant>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual Shop? Shop { get; set; }

    public virtual ICollection<ViewHistory> ViewHistories { get; set; } = new List<ViewHistory>();

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public string? MainImageUrl => ProductImages?.FirstOrDefault(i => i.IsMain == true)?.ImageUrl ?? ProductImages?.FirstOrDefault()?.ImageUrl;
}
