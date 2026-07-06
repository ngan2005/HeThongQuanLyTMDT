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

        // Số điện thoại MoMo nhận tiền — thay bằng SĐT thật của bạn
        private const string MoMoPhoneNumber = "0909123456";

        public MoMoMockWindow(decimal amount)
        {
            InitializeComponent();
            txtAmount.Text = $"{amount:N0} đ";
            txtPhoneInfo.Text = $"Nhận tiền: {MoMoPhoneNumber}";

            GenerateQRCode(amount);
        }

        private void GenerateQRCode(decimal amount)
        {
            try
            {
                // Link MoMo chuyển tiền thật: https://nhantien.momo.vn/{phone}
                // Có thể thêm tham số: ?amount=xxx&description=xxx
                string momoLink = $"https://nhantien.momo.vn/{MoMoPhoneNumber}";

                // Tạo QR Code từ link
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(momoLink, QRCodeGenerator.ECCLevel.M);
                using var qrCode = new PngByteQRCode(qrCodeData);
                byte[] pngBytes = qrCode.GetGraphic(10, new byte[] { 174, 32, 112 }, new byte[] { 255, 255, 255 });

                // Chuyển sang BitmapImage để hiển thị trong WPF
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
    }
}
