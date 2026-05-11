using System;
using System.Collections.Generic;

namespace TMDT.Models;

public partial class PointHistory
{
    public int PointHistoryId { get; set; }

    public int? UserId { get; set; }

    public int? Points { get; set; }

    public string? TransactionType { get; set; }

    public int? OrderId { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Order? Order { get; set; }

    public virtual User? User { get; set; }
}
