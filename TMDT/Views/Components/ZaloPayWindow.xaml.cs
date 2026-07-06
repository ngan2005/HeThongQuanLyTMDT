using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace TMDT.Views.Components
{
    public partial class ZaloPayWindow : Window
    {
        public string TransactionCode { get; private set; } = string.Empty;
        private string _paymentUrl;

        public ZaloPayWindow(string paymentUrl)
        {
            InitializeComponent();
            _paymentUrl = paymentUrl;
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await webView.EnsureCoreWebView2Async(null);
            webView.CoreWebView2.Navigate(_paymentUrl);
            webView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
        }

        private void CoreWebView2_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            string url = e.Uri;
            
            // ZaloPay Sandbox redirects differently, usually to a merchant site or shows success on their own UI.
            // For this implementation, we simulate that returning back to our domain or detecting "success" means it's done.
            if (url.Contains("tmdt.local/zalopay-return") || url.Contains("appid="))
            {
                // Parse the URL parameters to check success and get transaction code
                try
                {
                    var uri = new Uri(url);
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    
                    if (query["status"] == "1" || query["status"] == "success" || url.Contains("success"))
                    {
                        TransactionCode = query["apptransid"] ?? Guid.NewGuid().ToString();
                        this.DialogResult = true;
                    }
                    else
                    {
                        this.DialogResult = false;
                    }
                }
                catch
                {
                    this.DialogResult = false;
                }
                this.Close();
            }
        }
    }
}
