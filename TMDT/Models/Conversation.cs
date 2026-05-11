using System;
using System.Collections.Generic;

namespace TMDT.Models;

public partial class Conversation
{
    public int ConversationId { get; set; }

    public int? BuyerId { get; set; }

    public int? ShopId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastMessageAt { get; set; }

    public virtual User? Buyer { get; set; }

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual Shop? Shop { get; set; }
}
