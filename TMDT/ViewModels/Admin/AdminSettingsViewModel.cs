using System;
using System.Windows;
using System.Windows.Input;
using TMDT.Models;
using TMDT.Utilities;

namespace TMDT.ViewModels.Admin
{
    public class AdminSettingsViewModel : ViewModelBase
    {
        private decimal _platformCommissionRate;
        private decimal _minWithdrawAmount;
        private bool _maintenanceMode;
        private bool _requireProductApproval;
        private string _supportEmail = "";

        public decimal PlatformCommissionRate
        {
            get => _platformCommissionRate;
            set { _platformCommissionRate = value; OnPropertyChanged(); }
        }

        public decimal MinWithdrawAmount
        {
            get => _minWithdrawAmount;
            set { _minWithdrawAmount = value; OnPropertyChanged(); }
        }

        public bool MaintenanceMode
        {
            get => _maintenanceMode;
            set { _maintenanceMode = value; OnPropertyChanged(); }
        }

        public bool RequireProductApproval
        {
            get => _requireProductApproval;
            set { _requireProductApproval = value; OnPropertyChanged(); }
        }

        public string SupportEmail
        {
            get => _supportEmail;
            set { _supportEmail = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand SaveSettingsCommand { get; } = null!;
        public ICommand ResetSettingsCommand { get; } = null!;

        public AdminSettingsViewModel()
        {
            SaveSettingsCommand = new RelayCommand(ExecuteSaveSettings);
            ResetSettingsCommand = new RelayCommand(ExecuteResetSettings);

            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            SystemSettingsHelper.LoadSettings();
            var settings = SystemSettingsHelper.Current;

            PlatformCommissionRate = settings.PlatformCommissionRate;
            MinWithdrawAmount = settings.MinWithdrawAmount;
            MaintenanceMode = settings.MaintenanceMode;
            RequireProductApproval = settings.RequireProductApproval;
            SupportEmail = settings.SupportEmail;
        }

        private void ExecuteSaveSettings(object? obj)
        {
            if (PlatformCommissionRate < 0 || PlatformCommissionRate > 100)
            {
                MessageBox.Show("Tỷ lệ hoa hồng chiết khấu phải nằm trong khoảng từ 0% đến 100%!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MinWithdrawAmount < 0)
            {
                MessageBox.Show("Số tiền rút tối thiểu không được là số âm!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(SupportEmail) || !SupportEmail.Contains("@"))
            {
                MessageBox.Show("Email hỗ trợ không hợp lệ!", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 🟢 Audit: snapshot rate cũ trước khi ghi đè
            var oldRate = SystemSettingsHelper.Current.PlatformCommissionRate;

            // 🟢 Nếu rate thay đổi → cảnh báo số đơn sắp tới bị ảnh hưởng (tháng này)
            if (oldRate != PlatformCommissionRate)
            {
                int affectedOrders;
                try
                {
                    using var ctx = new TmdtContext();
                    var since = DateTime.Now.AddDays(-30);
                    affectedOrders = ctx.Orders.Count(o => o.OrderDate >= since && o.OrderStatus != "Cancelled");
                }
                catch { affectedOrders = -1; }

                var delta = PlatformCommissionRate - oldRate;
                var sign  = delta > 0 ? "+" : "";
                var warn = affectedOrders >= 0
                    ? $"Số đơn 30 ngày qua: {affectedOrders:N0}\n→ Sẽ ảnh hưởng các đơn MỚI của shop không có rate riêng."
                    : "Không thể truy vấn số đơn.";

                var confirmRate = MessageBox.Show(
                    $"⚠️ THAY ĐỔI PHÍ SÀN TOÀN SÀN\n\n" +
                    $"Tỷ lệ hiện tại: {oldRate:N1}%\n" +
                    $"Tỷ lệ mới:     {PlatformCommissionRate:N1}%  ({sign}{delta:N1}%)\n\n" +
                    $"{warn}\n\n" +
                    $"Bạn có chắc muốn thay đổi?",
                    "Xác nhận thay đổi phí sàn", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirmRate != MessageBoxResult.Yes) return;
            }

            // Save to helper
            var settings = SystemSettingsHelper.Current;
            settings.PlatformCommissionRate = PlatformCommissionRate;
            settings.MinWithdrawAmount = MinWithdrawAmount;
            settings.MaintenanceMode = MaintenanceMode;
            settings.RequireProductApproval = RequireProductApproval;
            settings.SupportEmail = SupportEmail;

            try
            {
                SystemSettingsHelper.SaveSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lưu cấu hình: {ex.Message}", "Lỗi cơ sở dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 🟢 Ghi log thay đổi phí sàn global (ConfigChangeLog + AuditLog)
            var adminName = SessionManager.CurrentUser?.FullName ?? SessionManager.CurrentUser?.Email ?? "Admin";
            ConfigChangeLogger.LogGlobalRateChange(oldRate, PlatformCommissionRate, adminName,
                $"Settings save (Global rate)");
            AuditLogHelper.Log("UPDATE_COMMISSION_GLOBAL",
                $"Phí sàn toàn sàn: {oldRate:N1}% → {PlatformCommissionRate:N1}%",
                "SystemConfig", "Warning");

            string statusMsg = "Lưu cấu hình hệ thống thành công!";
            if (MaintenanceMode)
            {
                statusMsg += "\n⚠️ Chú ý: Hệ thống đang ở chế độ bảo trì sàn. Người dùng không thể mua bán!";
            }

            MessageBox.Show(statusMsg, "Cấu hình thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecuteResetSettings(object? obj)
        {
            var result = MessageBox.Show("Bạn có chắc chắn muốn đặt lại tất cả tham số hệ thống về mặc định ban đầu?",
                                         "Xác nhận đặt lại", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            // 🟢 Audit: snapshot rate cũ trước khi reset
            var oldRate = SystemSettingsHelper.Current.PlatformCommissionRate;

            PlatformCommissionRate = 5.0m;
            MinWithdrawAmount = 100000m;
            MaintenanceMode = false;
            RequireProductApproval = true;
            SupportEmail = "support@myshop.vn";

            // Save immediately
            var settings = SystemSettingsHelper.Current;
            settings.PlatformCommissionRate = PlatformCommissionRate;
            settings.MinWithdrawAmount = MinWithdrawAmount;
            settings.MaintenanceMode = MaintenanceMode;
            settings.RequireProductApproval = RequireProductApproval;
            settings.SupportEmail = SupportEmail;

            SystemSettingsHelper.SaveSettings();

            // 🟢 Ghi log reset
            var adminName = SessionManager.CurrentUser?.FullName ?? SessionManager.CurrentUser?.Email ?? "Admin";
            ConfigChangeLogger.LogGlobalRateChange(oldRate, PlatformCommissionRate, adminName, "Reset to default");
            AuditLogHelper.Log("RESET_COMMISSION_GLOBAL",
                $"Reset phí sàn: {oldRate:N1}% → {PlatformCommissionRate:N1}%",
                "SystemConfig", "Warning");

            MessageBox.Show("Đã khôi phục các tham số hệ thống về mặc định thành công!", "Đặt lại thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
