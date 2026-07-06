using System;
using System.Windows;

namespace TMDT.Views.Components
{
    public partial class ZaloPayMockWindow : Window
    {
        public string TransactionCode { get; private set; } = string.Empty;

        public ZaloPayMockWindow(decimal amount)
        {
            InitializeComponent();
            txtAmount.Text = $"{amount:N0} đ";
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            // Giả lập giao dịch thành công và sinh mã ngẫu nhiên
            TransactionCode = "ZALOPAY_" + DateTime.Now.Ticks.ToString();
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
