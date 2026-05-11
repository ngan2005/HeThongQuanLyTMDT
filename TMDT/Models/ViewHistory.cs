using System;
using System.Collections.Generic;

namespace TMDT.Models;

public partial class ViewHistory
{
    public int ViewHistoryId { get; set; }

    public int? UserId { get; set; }

    public int? ProductId { get; set; }

    public DateTime? ViewedAt { get; set; }

    public virtual Product? Product { get; set; }

    public virtual User? User { get; set; }
}
