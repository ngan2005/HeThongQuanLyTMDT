using System;
using System.Collections.Generic;

namespace TMDT.Models;

public partial class OrderStatusHistory
{
    public int HistoryId { get; set; }

    public int? OrderId { get; set; }

    public string? NewStatus { get; set; }

    public string? Note { get; set; }

    public DateTime? ChangedAt { get; set; }

    public virtual Order? Order { get; set; }
}
