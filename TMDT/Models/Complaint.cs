using System;
using System.Collections.Generic;

namespace TMDT.Models;

public partial class Complaint
{
    public int ComplaintId { get; set; }

    public int? OrderId { get; set; }

    public int? BuyerId { get; set; }

    public string? Content { get; set; }

    public string? Status { get; set; }

    public string? Resolution { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public virtual User? Buyer { get; set; }

    public virtual Order? Order { get; set; }
}
