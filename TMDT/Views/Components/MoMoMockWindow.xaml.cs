using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using QRCoder;

namespace TMDT.Views.Components
{
    public partial class MoMoMockWindow : Window
    {
        public string TransactionCode { get; private set; } = string.Empty;

        /// <summary>True khi người dùng nhấn "Sửa đơn" — POS sẽ load đơn về tab để chỉnh sửa.</summary>
        public bool UserChoseToEdit { get; private set; }

        /// <summary>True khi cashier xác nhận offline (mạng lỗi) — POS sẽ lưu queue để sync sau.</summary>
        public bool UserChoseOffline { get; private set; }

        // 🟢 SĐT MoMo nhận tiền — truyền qua constructor (mỗi POS có thể khác nhau).
        // null = seller chưa cài đặt → hiển thị cảnh báo vàng trong QR.
        private readonly string? _phone;

        public MoMoMockWindow(decimal amount, string? orderCode = null) : this(amount, null, orderCode) { }

        public MoMoMockWindow(decimal amount, string? phone, string? orderCode = null)
        {
            InitializeComponent();
            txtAmount.Text = $"{amount:N0} đ";
            _phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

            if (string.IsNullOrWhiteSpace(orderCode))
            {
                txtOrderCode.Text = string.Empty;
                txtOrderCode.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtOrderCode.Text = $"Mã đơn: {orderCode}";
                txtOrderCode.Visibility = Visibility.Visible;
            }

            // Hiển thị SĐT nhận tiền + cảnh báo nếu chưa cấu hình
            if (!string.IsNullOrEmpty(_phone))
                txtPhoneInfo.Text = $"Nhận tiền: {_phone}";
            else
            {
                txtPhoneInfo.Text = "⚠ Chưa cài đặt SĐT MoMo nhận tiền (POS > Cài đặt)";
                txtPhoneInfo.Foreground = System.Windows.Media.Brushes.DarkOrange;
            }

            GenerateQRCode(amount);
        }

        private void GenerateQRCode(decimal amount)
        {
            try
            {
                var settings = TMDT.Services.PosSettingsHelper.Current;
                if (!string.IsNullOrEmpty(settings.MoMoQrImagePath) && File.Exists(settings.MoMoQrImagePath))
                {
                    // 🟢 Sử dụng ảnh QR tĩnh được cấu hình trong POS Settings
                    var staticBitmap = new BitmapImage();
                    staticBitmap.BeginInit();
                    staticBitmap.CacheOption = BitmapCacheOption.OnLoad;
                    staticBitmap.UriSource = new Uri(settings.MoMoQrImagePath, UriKind.Absolute);
                    staticBitmap.EndInit();
                    staticBitmap.Freeze();
                    imgQR.Source = staticBitmap;
                    return; // Bỏ qua việc tự sinh mã QR
                }

                // 🟢 Không có QR tĩnh → Sinh mã QR tự động từ SĐT
                string momoLink = !string.IsNullOrEmpty(_phone)
                    ? $"https://nhantien.momo.vn/{_phone}?amount={amount:0}&description=TMDT"
                    : "https://momo.vn";

                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(momoLink, QRCodeGenerator.ECCLevel.M);
                using var qrCode = new PngByteQRCode(qrCodeData);
                byte[] pngBytes = qrCode.GetGraphic(10, new byte[] { 174, 32, 112 }, new byte[] { 255, 255, 255 });

                using var ms = new MemoryStream(pngBytes);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();

                imgQR.Source = bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("QR generation failed: " + ex.Message);
            }
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            TransactionCode = "MOMO_" + DateTime.Now.Ticks.ToString();
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void BtnEditOrder_Click(object sender, RoutedEventArgs e)
        {
            UserChoseToEdit = true;
            this.DialogResult = false;
            this.Close();
        }

        private void BtnOfflineConfirm_Click(object sender, RoutedEventArgs e)
        {
            UserChoseOffline = true;
            this.DialogResult = false;
            this.Close();
        }
    }
}
