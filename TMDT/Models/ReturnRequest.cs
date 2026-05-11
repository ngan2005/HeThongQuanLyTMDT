using System;
using System.Collections.Generic;

namespace TMDT.Models;

public partial class ReturnRequest
{
    public int ReturnId { get; set; }

    public int? OrderDetailId { get; set; }

    public int? BuyerId { get; set; }

    public string? Reason { get; set; }

    public string? EvidenceImage { get; set; }

    public string? Status { get; set; }

    public DateTime? RequestedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public virtual User? Buyer { get; set; }

    public virtual OrderDetail? OrderDetail { get; set; }
}
