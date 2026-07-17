using System;
using TMDT.Models;
using TMDT.Utilities;

namespace TMDT.Utilities;

/// <summary>
/// 🟢 Helper ghi log + audit thay đổi phí sàn vào DB ConfigChangeLog.
/// Dùng để truy vết "admin đổi rate từ X% → Y% lúc nào, ai đổi".
/// </summary>
public static class ConfigChangeLogger
{
    /// <summary>Log thay đổi global rate (PlatformCommissionRate).</summary>
    public static void LogGlobalRateChange(decimal oldRate, decimal newRate, string changedBy, string? note = null)
    {
        if (oldRate == newRate) return;
        Write("Global", null, "PlatformCommissionRate", oldRate, newRate, changedBy, note);
    }

    /// <summary>Log thay đổi rate riêng của shop.</summary>
    public static void LogShopRateChange(int shopId, decimal? oldRate, decimal? newRate, string changedBy, string? note = null)
    {
        if (oldRate == newRate) return;
        Write("Shop", shopId, "ShopCommissionRate", oldRate, newRate, changedBy, note);
    }

    /// <summary>Lấy lịch sử thay đổi rate (gộp cả Global + Shop) — dùng cho Admin UI.</summary>
    public static System.Collections.Generic.List<ConfigChangeLog> GetRateHistory(int? shopId = null, int limit = 100)
    {
        try
        {
            using var ctx = new TmdtContext();
            var q = ctx.ConfigChangeLogs
                .Where(l => l.ConfigKey == "PlatformCommissionRate" || l.ConfigKey == "ShopCommissionRate");
            if (shopId.HasValue)
                q = q.Where(l => l.TargetId == shopId.Value);
            return q.OrderByDescending(l => l.ChangedAt)
                    .Take(limit)
                    .ToList();
        }
        catch
        {
            return new System.Collections.Generic.List<ConfigChangeLog>();
        }
    }

    private static void Write(string configType, int? targetId, string configKey, decimal? oldVal, decimal? newVal, string changedBy, string? note)
    {
        try
        {
            using var ctx = new TmdtContext();
            ctx.ConfigChangeLogs.Add(new ConfigChangeLog
            {
                ConfigType = configType,
                TargetId = targetId,
                ConfigKey = configKey,
                OldValue = oldVal,
                NewValue = newVal,
                ChangedBy = changedBy,
                Note = note,
                ChangedAt = DateTime.Now
            });
            ctx.SaveChanges();
        }
        catch (Exception ex)
        {
            // Không chặn luồng chính nếu log lỗi — chỉ in Debug
            System.Diagnostics.Debug.WriteLine($"ConfigChangeLogger.Write failed: {ex.Message}");
        }
    }
}
