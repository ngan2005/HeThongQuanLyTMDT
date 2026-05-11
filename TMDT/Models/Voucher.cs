using System;
using System.Collections.Generic;

namespace TMDT.Models;

public partial class Voucher
{
    public int VoucherId { get; set; }

    public int? ShopId { get; set; }

    public string VoucherCode { get; set; } = null!;

    public string? VoucherName { get; set; }

    public string? DiscountType { get; set; }

    public decimal? DiscountValue { get; set; }

    public decimal? MaxDiscount { get; set; }

    public decimal? MinOrderValue { get; set; }

    public int? TotalQuantity { get; set; }

    public int? UsedCount { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual Shop? Shop { get; set; }
}
