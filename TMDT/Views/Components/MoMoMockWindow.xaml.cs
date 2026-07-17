using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

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

            // 🟢 Hiển thị SĐT nhận tiền + cảnh báo nếu chưa cấu hình
            if (!string.IsNullOrEmpty(_phone))
                txtPhoneInfo.Text = $"Nhận tiền: {_phone}";
            else
            {
                txtPhoneInfo.Text = "⚠ Chưa cài đặt SĐT MoMo nhận tiền (POS > Cài đặt)";
                txtPhoneInfo.Foreground = System.Windows.Media.Brushes.DarkOrange;
            }

            // 🟢 Nếu có QR upload ảnh → ưu tiên, KHÔNG cần SĐT
            var settings = TMDT.Services.PosSettingsHelper.Current;
            bool hasUploadedQr = !string.IsNullOrEmpty(settings.MoMoQrImagePath)
                                  && File.Exists(settings.MoMoQrImagePath);
            bool hasPhone = !string.IsNullOrEmpty(_phone);

            if (hasUploadedQr)
            {
                GenerateQRCode(amount); // dùng upload
            }
            else if (hasPhone)
            {
                GenerateQRCode(amount); // fallback QR từ SĐT (vẫn cho phép theo yêu cầu)
            }
            else
            {
                GenerateQRCode(amount); // không có gì → show cảnh báo, khóa confirm
            }
        }

        private void GenerateQRCode(decimal amount)
        {
            try
            {
                var settings = TMDT.Services.PosSettingsHelper.Current;

                // 🟢 Nút Confirm mặc định = disabled (chỉ bật khi có QR upload)
                BtnConfirm.IsEnabled = false;

                if (!string.IsNullOrEmpty(settings.MoMoQrImagePath) && File.Exists(settings.MoMoQrImagePath))
                {
                    // ✅ TRƯỜNG HỢP 1: Có ảnh QR upload — AN TOÀN nhất, dùng luôn
                    var staticBitmap = new BitmapImage();
                    staticBitmap.BeginInit();
                    staticBitmap.CacheOption = BitmapCacheOption.OnLoad;
                    staticBitmap.UriSource = new Uri(settings.MoMoQrImagePath, UriKind.Absolute);
                    staticBitmap.EndInit();
                    staticBitmap.Freeze();
                    imgQR.Source = staticBitmap;
                    txtNoQrPlaceholder.Visibility = Visibility.Collapsed;
                    BtnConfirm.IsEnabled = true; // 🟢 QR thật từ app MoMo seller → confirm OK
                    return;
                }

                if (!string.IsNullOrEmpty(_phone))
                {
                    // ⚠️ TRƯỜNG HỢP 2: Fallback QR từ SĐT — KHÔNG KHUYẾN NGHỊ
                    string momoLink = $"https://nhantien.momo.vn/{_phone}?amount={amount:0}&description=TMDT";

                    // 🟢 Generate QR (giữ lại QRCoder tạm thời vì user chọn keep-both)
                    using var qrGenerator = new QRCoder.QRCodeGenerator();
                    using var qrCodeData = qrGenerator.CreateQrCode(momoLink, QRCoder.QRCodeGenerator.ECCLevel.M);
                    using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
                    byte[] pngBytes = qrCode.GetGraphic(10, new byte[] { 174, 32, 112 }, new byte[] { 255, 255, 255 });

                    using var ms = new MemoryStream(pngBytes);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    imgQR.Source = bitmap;
                    txtNoQrPlaceholder.Visibility = Visibility.Collapsed;

                    // 🟢 Cảnh báo rõ rủi ro QR tự sinh từ SĐT
                    txtWarningText.Text =
                        $"⚠ QR TỰ SINH TỪ SĐT {_phone}\n\n" +
                        "Rủi ro: QR này chỉ hoạt động nếu SĐT đúng và đã liên kết MoMo.\n" +
                        "Nếu khách quét mà app MoMo báo lỗi → bạn mất đơn.\n\n" +
                        "Khuyến nghị: Vào POS > Cài đặt > MoMo > upload ảnh QR cá nhân (lấy từ app MoMo của bạn) để an toàn 100%.";
                    txtWarning.Visibility = Visibility.Visible;

                    BtnConfirm.IsEnabled = true; // Vẫn cho cashier confirm, đã có cảnh báo
                    return;
                }

                // 🟢 TRƯỜNG HỢP 3: Không có QR upload, không có SĐT → khóa confirm
                txtNoQrPlaceholder.Visibility = Visibility.Visible;
                imgQR.Source = null;
                BtnConfirm.IsEnabled = false;
                txtWarningText.Text =
                    "CHƯA CÀI ĐẶT MOMO.\n\n" +
                    "Vào POS > Cài đặt > MoMo:\n" +
                    "  • Upload ảnh QR cá nhân (khuyến nghị), HOẶC\n" +
                    "  • Nhập SĐT MoMo đã liên kết ngân hàng";
                txtWarning.Visibility = Visibility.Visible;
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
