using System;
using System.Collections.Generic;

namespace TMDT.Models;

public partial class ProductVariant
{
    public int VariantId { get; set; }

    public int? ProductId { get; set; }

    public string? VariantName { get; set; }

    public decimal? ExtraPrice { get; set; }

    public int? Quantity { get; set; }

    public string? Sku { get; set; }

    public int? WeightGrams { get; set; }

    public int? LengthCm { get; set; }

    public int? WidthCm { get; set; }

    public int? HeightCm { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual Product? Product { get; set; }
}
