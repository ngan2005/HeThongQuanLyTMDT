using System;

namespace TMDT.Models;

/// <summary>
/// 🟢 Lịch sử thay đổi phí sàn (và các config khác) — cho phép audit "ai đổi rate từ X → Y lúc nào".
/// ConfigType: "Global" (SystemSettings) hoặc "Shop" (Shop.CommissionRate).
/// </summary>
public partial class ConfigChangeLog
{
    public int LogId { get; set; }

    /// <summary>"Global" hoặc "Shop"</summary>
    public string ConfigType { get; set; } = null!;

    /// <summary>ID của Shop (nếu ConfigType = "Shop"), null nếu Global.</summary>
    public int? TargetId { get; set; }

    /// <summary>Tên config, vd: "PlatformCommissionRate"</summary>
    public string ConfigKey { get; set; } = null!;

    public decimal? OldValue { get; set; }
    public decimal? NewValue { get; set; }

    public string? ChangedBy { get; set; }
    public string? Note { get; set; }
    public DateTime? ChangedAt { get; set; }
}
