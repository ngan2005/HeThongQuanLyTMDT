using System;
using System.Collections.Generic;

namespace TMDT.Models;

public partial class ReviewReply
{
    public int ReplyId { get; set; }

    public int? ReviewId { get; set; }

    public int? UserId { get; set; }

    public string? Content { get; set; }

    public DateTime? RepliedAt { get; set; }

    public virtual Review? Review { get; set; }

    public virtual User? User { get; set; }
}
