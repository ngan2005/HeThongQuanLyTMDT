using System;
using System.Collections.Generic;
using System.Web;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using TMDT.Services;

namespace TMDT.Views.Components
{
    public partial class VNPayWindow : Window
    {
        public bool IsPaymentSuccess { get; private set; } = false;
        public int PaidOrderId { get; private set; } = 0;
        public string TransactionCode { get; private set; } = "";

        public VNPayWindow(string paymentUrl)
        {
            InitializeComponent();
            InitializeWebView(paymentUrl);
        }

        private async void InitializeWebView(string url)
        {
            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
            webView.CoreWebView2.Navigate(url);
        }

        private void CoreWebView2_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (e.Uri.StartsWith("https://tmdt.local/vnpay-return", StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true; // Prevent actual navigation
                
                var uri = new Uri(e.Uri);
                var queryParams = HttpUtility.ParseQueryString(uri.Query);
                var dict = new Dictionary<string, string>();
                foreach (string? key in queryParams.AllKeys)
                {
                    if (!string.IsNullOrEmpty(key))
                    {
                        dict[key] = queryParams[key] ?? "";
                    }
                }

                if (VNPayService.ValidateSignature(dict, out int orderId, out string txnRef))
                {
                    IsPaymentSuccess = true;
                    PaidOrderId = orderId;
                    TransactionCode = txnRef;
                    MessageBox.Show("Thanh toán thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    IsPaymentSuccess = false;
                    MessageBox.Show("Thanh toán thất bại hoặc đã bị hủy.", "Lỗi thanh toán", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                this.DialogResult = IsPaymentSuccess;
                this.Close();
            }
        }
    }
}
