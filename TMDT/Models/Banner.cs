using System;
using System.Collections.Generic;

namespace TMDT.Models;

public partial class Banner
{
    public int BannerId { get; set; }

    public string? Title { get; set; }

    public string? ImageUrl { get; set; }

    public string? LinkUrl { get; set; }

    public int? SortOrder { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool? IsActive { get; set; }
}
