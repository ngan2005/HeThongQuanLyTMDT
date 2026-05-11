using System;
using System.Collections.Generic;

namespace TMDT.Models;

public partial class WithdrawRequest
{
    public int WithdrawId { get; set; }

    public int? ShopId { get; set; }

    public decimal? Amount { get; set; }

    public string? BankName { get; set; }

    public string? AccountNumber { get; set; }

    public string? Status { get; set; }

    public DateTime? RequestedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public virtual Shop? Shop { get; set; }
}
