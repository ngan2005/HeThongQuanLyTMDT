using System;
using System.Collections.Generic;

namespace TMDT.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public string OrderCode { get; set; } = null!;

    public int? BuyerId { get; set; }

    public int? ShopId { get; set; }

    public int? AddressId { get; set; }

    public int? VoucherId { get; set; }

    public decimal? SubTotal { get; set; }

    public decimal? ShippingFee { get; set; }

    public decimal? Discount { get; set; }

    public decimal? TotalAmount { get; set; }

    public decimal? PlatformFee { get; set; }

    public string? PaymentMethod { get; set; }

    public string? OrderStatus { get; set; }

    public string? TrackingCode { get; set; }

    public string? Note { get; set; }

    public DateTime? OrderDate { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual Address? Address { get; set; }

    public virtual User? Buyer { get; set; }

    public virtual ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<PointHistory> PointHistories { get; set; } = new List<PointHistory>();

    public virtual Shop? Shop { get; set; }

    public virtual Voucher? Voucher { get; set; }
}
