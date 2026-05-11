using System;
using System.Collections.Generic;

namespace TMDT.Models;

public partial class Shop
{
    public int ShopId { get; set; }

    public int? UserId { get; set; }

    public string ShopName { get; set; } = null!;

    public string? Logo { get; set; }

    public string? WarehouseAddress { get; set; }

    public decimal? CommissionRate { get; set; }

    public decimal? WalletBalance { get; set; }

    public decimal? Rating { get; set; }

    public bool? VacationMode { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? OpenedAt { get; set; }

    public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();

    public virtual ICollection<FlashSale> FlashSales { get; set; } = new List<FlashSale>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual User? User { get; set; }

    public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();

    public virtual ICollection<WithdrawRequest> WithdrawRequests { get; set; } = new List<WithdrawRequest>();
}
