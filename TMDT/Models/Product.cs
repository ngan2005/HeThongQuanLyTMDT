using System;
using System.Collections.Generic;

namespace TMDT.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public int? ShopId { get; set; }

    public int? CategoryId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public decimal? OriginalPrice { get; set; }

    public int? StockQuantity { get; set; }

    public int? SoldCount { get; set; }

    public decimal? Rating { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

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
}
