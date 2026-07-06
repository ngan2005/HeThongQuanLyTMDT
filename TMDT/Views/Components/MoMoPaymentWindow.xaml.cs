using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace TMDT.Views.Components
{
    public partial class MoMoPaymentWindow : Window
    {
        public string TransactionCode { get; private set; } = string.Empty;
        private string _paymentUrl;

        public MoMoPaymentWindow(string paymentUrl)
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
            if (url.Contains("momo-return"))
            {
                // Parse the URL parameters to check success and get transaction code
                try
                {
                    var uri = new Uri(url);
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    
                    if (query["resultCode"] == "0")
                    {
                        TransactionCode = query["transId"] ?? Guid.NewGuid().ToString();
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
