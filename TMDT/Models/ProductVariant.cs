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

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual Product? Product { get; set; }
}
