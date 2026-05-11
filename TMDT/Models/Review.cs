using System;
using System.Collections.Generic;

namespace TMDT.Models;

public partial class Review
{
    public int ReviewId { get; set; }

    public int? OrderDetailId { get; set; }

    public int? ProductId { get; set; }

    public int? UserId { get; set; }

    public byte? StarRating { get; set; }

    public string? Content { get; set; }

    public string? ImageUrl { get; set; }

    public bool? IsHidden { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public virtual OrderDetail? OrderDetail { get; set; }

    public virtual Product? Product { get; set; }

    public virtual ICollection<ReviewReply> ReviewReplies { get; set; } = new List<ReviewReply>();

    public virtual User? User { get; set; }
}
