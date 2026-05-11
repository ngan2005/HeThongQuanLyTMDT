using System;
using System.Collections.Generic;

namespace TMDT.Models;

public partial class FlashSale
{
    public int FlashSaleId { get; set; }

    public int? ShopId { get; set; }

    public string? CampaignName { get; set; }

    public int? ProductId { get; set; }

    public decimal? FlashPrice { get; set; }

    public int? StockLimit { get; set; }

    public int? SoldCount { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public bool? IsActive { get; set; }

    public virtual Product? Product { get; set; }

    public virtual Shop? Shop { get; set; }
}
