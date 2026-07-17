using System;
using System.IO;
using System.Text.Json;

namespace TMDT.Services
{
    /// <summary>
    /// 🟢 Cài đặt cục bộ của máy POS (AppData JSON, không phụ thuộc DB).
    /// - Mỗi máy/cửa hàng có 1 bộ cài đặt riêng (multi-shop multi-machine đều OK).
    /// - Bao gồm SĐT MoMo nhận tiền, mặc định đơn hàng treo, ngưỡng cảnh báo tồn kho, ...
    /// - Persist tại %LocalAppData%/TMDT_POS/pos_settings.json.
    /// </summary>
    public class PosSettings
    {
        public string? MoMoPhone { get; set; }
        public string? MoMoQrImagePath { get; set; }
        public string? VnpayBankAccount { get; set; }
        public string? VnpayBankName { get; set; }
        public string? VnpayQrImagePath { get; set; }  // 🟢 Ảnh QR chuyển khoản VNPay của seller
        public bool AutoReprintReceipt { get; set; }
        public bool PrintAfterSync { get; set; }
    }

    public static class PosSettingsHelper
    {
        private static readonly string _folder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TMDT_POS");
        private static readonly string _filePath = Path.Combine(_folder, "pos_settings.json");
        private static readonly object _lock = new();

        private static PosSettings _cache = Load();

        public static PosSettings Current
        {
            get { lock (_lock) return _cache; }
        }

        /// <summary>🟢 Lấy SĐT MoMo nhận tiền. Trả về null nếu seller chưa cài đặt.</summary>
        public static string? GetMoMoPhone()
        {
            return Current.MoMoPhone;
        }

        /// <summary>🟢 Lưu cài đặt POS xuống file JSON. Thread-safe.</summary>
        public static void Save(PosSettings settings)
        {
            lock (_lock)
            {
                _cache = settings;
                Persist();
            }
        }

        /// <summary>🟢 Chỉ cập nhật 1 field và lưu (tiện cho form binding).</summary>
        public static void UpdateMoMoPhone(string? phone)
        {
            lock (_lock)
            {
                _cache.MoMoPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
                Persist();
            }
        }

        private static PosSettings Load()
        {
            try
            {
                Directory.CreateDirectory(_folder);
                if (!File.Exists(_filePath)) return new PosSettings();
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<PosSettings>(json) ?? new PosSettings();
            }
            catch
            {
                // File lỗi → trả về default, không crash POS
                return new PosSettings();
            }
        }

        private static void Persist()
        {
            try
            {
                Directory.CreateDirectory(_folder);
                var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PosSettings] Persist failed: {ex.Message}");
            }
        }
    }
}
