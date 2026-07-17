using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace TMDT.Views.Components
{
    public partial class VNPayMockWindow : Window
    {
        public string TransactionCode { get; private set; } = string.Empty;

        /// <summary>True khi người dùng nhấn "Sửa đơn" — POS sẽ load đơn về tab để chỉnh sửa.</summary>
        public bool UserChoseToEdit { get; private set; }

        /// <summary>True khi cashier xác nhận offline (mạng lỗi) — POS sẽ lưu queue để sync sau.</summary>
        public bool UserChoseOffline { get; private set; }

        private readonly decimal _amount;
        private readonly string? _orderCode;

        public VNPayMockWindow(decimal amount, string? orderCode = null)
        {
            InitializeComponent();
            _amount = amount;
            _orderCode = orderCode;

            txtAmount.Text = $"{amount:N0} đ";

            if (!string.IsNullOrWhiteSpace(orderCode))
            {
                txtOrderCode.Text = $"Mã đơn: {orderCode}";
                txtOrderCode.Visibility = Visibility.Visible;
            }

            // Generate QR code after UI is loaded
            Loaded += (_, _) => GenerateQrCode();
        }

        public VNPayMockWindow(decimal amount) : this(amount, null) { }

        /// <summary>
        /// Tạo QR code VietQR chuẩn để quét bằng app ngân hàng.
        /// Format: VNPAY|TenNganHang|SoTK|SoTien|GhiChu
        /// </summary>
        private void GenerateQrCode()
        {
            // Cập nhật thông tin ngân hàng từ cài đặt POS
            var settings = Services.PosSettingsHelper.Current;
            if (!string.IsNullOrEmpty(settings.VnpayBankName) || !string.IsNullOrEmpty(settings.VnpayBankAccount))
            {
                string bankInfo = $"{settings.VnpayBankName ?? "VNPay"} · {settings.VnpayBankAccount ?? ""}";
                txtBankInfo.Text = bankInfo.Trim(' ', '·');
            }

            // 🟢 Toggle nút Confirm tùy theo có QR hay không
            BtnConfirm.IsEnabled = false;

            // Ưu tiên dùng ảnh QR seller đã upload
            string? customQrPath = settings.VnpayQrImagePath;
            if (!string.IsNullOrEmpty(customQrPath) && System.IO.File.Exists(customQrPath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(customQrPath, UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze();

                    imgQrCode.Source = bitmap;
                    imgQrCode.Visibility = Visibility.Visible;
                    txtQrPlaceholder.Visibility = Visibility.Collapsed;
                    BtnConfirm.IsEnabled = true; // 🟢 Có QR thật → cho phép cashier confirm
                    return;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Load custom VNPay QR error: " + ex.Message);
                }
            }

            // 🟢 Không có QR upload → HIỆN CẢNH BÁO, KHÔNG tự sinh QR (tránh cashier hiểu nhầm QR fake là thật)
            txtQrPlaceholder.Text = "⚠";
            txtQrPlaceholder.FontSize = 70;
            txtQrPlaceholder.Foreground = System.Windows.Media.Brushes.DarkOrange;
            txtQrPlaceholder.Opacity = 1.0;
            BtnConfirm.IsEnabled = false; // 🟢 Khóa nút confirm
            txtWarningText.Text = "CHƯA CÀI ĐẶT QR NGÂN HÀNG.\n\nVào POS > Cài đặt > VNPay\nđể upload ảnh QR từ app ngân hàng.\n\nQR tự sinh đã bị tắt để tránh sai sót.";
            txtWarning.Visibility = Visibility.Visible;
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            TransactionCode = $"VNP_{DateTime.Now.Ticks}";
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnEditOrder_Click(object sender, RoutedEventArgs e)
        {
            UserChoseToEdit = true;
            DialogResult = false;
            Close();
        }

        private void BtnOfflineConfirm_Click(object sender, RoutedEventArgs e)
        {
            UserChoseOffline = true;
            DialogResult = false;
            Close();
        }
    }
}
