using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using QRCoder;

namespace TMDT.Views.Components
{
    public partial class ZaloPayMockWindow : Window
    {
        public string TransactionCode { get; private set; } = string.Empty;

        private readonly decimal _amount;
        private readonly string? _orderCode;

        public ZaloPayMockWindow(decimal amount, string? orderCode = null)
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

            Loaded += (_, _) => GenerateQrCode();
        }

        public ZaloPayMockWindow(decimal amount) : this(amount, null) { }

        private void GenerateQrCode()
        {
            try
            {
                // ZaloPay QR content (mô phỏng)
                string note = string.IsNullOrWhiteSpace(_orderCode)
                    ? "Thanh toan don hang"
                    : $"ZLP {_orderCode}";
                string qrContent = $"zalopay://payment?amount={(long)_amount}&note={note}&appId=TMDT_POS_DEMO";

                var qrGenerator = new QRCodeGenerator();
                var qrData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.M);
                var qrCode = new PngByteQRCode(qrData);
                byte[] qrBytes = qrCode.GetGraphic(10);

                using var ms = new MemoryStream(qrBytes);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();

                imgQrCode.Source = bitmap;
                imgQrCode.Visibility = Visibility.Visible;
                txtQrPlaceholder.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ZaloPay QR generate error: " + ex.Message);
                txtQrPlaceholder.Text = "📱";
                txtQrPlaceholder.FontSize = 60;
                txtQrPlaceholder.Opacity = 0.3;
            }
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            TransactionCode = "ZALOPAY_" + DateTime.Now.Ticks.ToString();
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
