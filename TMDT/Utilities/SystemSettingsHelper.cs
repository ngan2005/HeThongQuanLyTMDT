using System;
using System.Linq;
using TMDT.Models;

namespace TMDT.Utilities
{
    /// <summary>
    /// Các key cố định dùng trong bảng SystemConfig.
    /// Dùng constant tránh typo khi truy vấn.
    /// </summary>
    public static class ConfigKeys
    {
        public const string PlatformCommissionRate = "PlatformCommissionRate";
        public const string MinWithdrawAmount      = "MinWithdrawAmount";
        public const string MaintenanceMode        = "MaintenanceMode";
        public const string RequireProductApproval = "RequireProductApproval";
        public const string SupportEmail           = "SupportEmail";
    }

    public class SystemSettings
    {
        public decimal PlatformCommissionRate { get; set; } = 5.0m;
        public decimal MinWithdrawAmount      { get; set; } = 100_000m;
        public bool    MaintenanceMode        { get; set; } = false;
        public bool    RequireProductApproval { get; set; } = true;
        public string  SupportEmail           { get; set; } = "support@myshop.vn";
    }

    /// <summary>
    /// Đọc/ghi cấu hình hệ thống từ bảng SystemConfig trong DB.
    /// Thay thế hoàn toàn systemsettings.json — đảm bảo đồng bộ
    /// giữa Admin App, Buyer App và Seller App.
    /// </summary>
    public static class SystemSettingsHelper
    {
        private static SystemSettings _cache;

        public static SystemSettings Current
        {
            get
            {
                if (_cache == null) LoadSettings();
                return _cache;
            }
        }

        /// <summary>Đọc toàn bộ cấu hình từ DB vào cache.</summary>
        public static void LoadSettings()
        {
            try
            {
                using var ctx = new TmdtContext();
                var rows = ctx.SystemConfigs.ToList();

                _cache = new SystemSettings();

                foreach (var row in rows)
                {
                    switch (row.ConfigKey)
                    {
                        case ConfigKeys.PlatformCommissionRate:
                            if (decimal.TryParse(row.ConfigValue, out var rate))
                                _cache.PlatformCommissionRate = rate;
                            break;

                        case ConfigKeys.MinWithdrawAmount:
                            if (decimal.TryParse(row.ConfigValue, out var minW))
                                _cache.MinWithdrawAmount = minW;
                            break;

                        case ConfigKeys.MaintenanceMode:
                            if (bool.TryParse(row.ConfigValue, out var maint))
                                _cache.MaintenanceMode = maint;
                            break;

                        case ConfigKeys.RequireProductApproval:
                            if (bool.TryParse(row.ConfigValue, out var approve))
                                _cache.RequireProductApproval = approve;
                            break;

                        case ConfigKeys.SupportEmail:
                            _cache.SupportEmail = row.ConfigValue ?? "support@myshop.vn";
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SystemSettings] LoadSettings failed: {ex.Message}");
                _cache = new SystemSettings(); // fallback về mặc định
            }
        }

        /// <summary>Ghi toàn bộ cấu hình từ cache xuống DB (Upsert).</summary>
        public static void SaveSettings()
        {
            if (_cache == null) return;

            try
            {
                using var ctx = new TmdtContext();

                UpsertConfig(ctx, ConfigKeys.PlatformCommissionRate,
                    _cache.PlatformCommissionRate.ToString(),
                    "Tỷ lệ hoa hồng nền tảng (%)");

                UpsertConfig(ctx, ConfigKeys.MinWithdrawAmount,
                    _cache.MinWithdrawAmount.ToString(),
                    "Số tiền rút tối thiểu (VNĐ)");

                UpsertConfig(ctx, ConfigKeys.MaintenanceMode,
                    _cache.MaintenanceMode.ToString(),
                    "Chế độ bảo trì — true: khoá toàn bộ giao dịch");

                UpsertConfig(ctx, ConfigKeys.RequireProductApproval,
                    _cache.RequireProductApproval.ToString(),
                    "Bắt buộc Admin duyệt sản phẩm trước khi hiển thị");

                UpsertConfig(ctx, ConfigKeys.SupportEmail,
                    _cache.SupportEmail,
                    "Email hỗ trợ khách hàng");

                ctx.SaveChanges();

                // Buộc load lại cache từ DB lần sau
                _cache = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SystemSettings] SaveSettings failed: {ex.Message}");
                throw; // re-throw để ViewModel hiển thị lỗi cho người dùng
            }
        }

        /// <summary>Insert nếu chưa có, Update nếu đã có (Upsert pattern).</summary>
        private static void UpsertConfig(TmdtContext ctx, string key, string value, string description)
        {
            var existing = ctx.SystemConfigs.FirstOrDefault(c => c.ConfigKey == key);
            if (existing == null)
            {
                ctx.SystemConfigs.Add(new SystemConfig
                {
                    ConfigKey   = key,
                    ConfigValue = value,
                    Description = description,
                    UpdatedAt   = DateTime.Now
                });
            }
            else
            {
                existing.ConfigValue = value;
                existing.UpdatedAt   = DateTime.Now;
            }
        }
    }
}
