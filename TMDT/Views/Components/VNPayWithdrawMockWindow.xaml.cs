using System;
using System.Windows;

namespace TMDT.Views.Components
{
    public partial class VNPayWithdrawMockWindow : Window
    {
        public string TransactionCode { get; private set; } = string.Empty;

        public VNPayWithdrawMockWindow(decimal amount, string accountInfo = "VNPay Wallet")
        {
            InitializeComponent();
            txtAmount.Text = $"{amount:N0} đ";
            txtAccountName.Text = accountInfo;
            txtTxnRef.Text = "VNPAY_WD_" + DateTime.Now.Ticks.ToString()[^8..];
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            TransactionCode = "VNPAY_WD_" + DateTime.Now.Ticks.ToString();
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
