using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TMDT.Views.Components
{
    public partial class TopUpDialog : Window
    {
        public decimal Amount { get; private set; }

        public TopUpDialog()
        {
            InitializeComponent();
        }

        private void TxtAmount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void TxtAmount_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtAmount.Text)) return;
            
            // Auto format with commas
            string rawText = txtAmount.Text.Replace(",", "");
            if (decimal.TryParse(rawText, out decimal val))
            {
                txtAmount.TextChanged -= TxtAmount_TextChanged;
                txtAmount.Text = val.ToString("N0");
                txtAmount.CaretIndex = txtAmount.Text.Length;
                txtAmount.TextChanged += TxtAmount_TextChanged;
            }
        }

        private void BtnQuickAmount_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string content = btn.Content.ToString() ?? "";
                if (content == "100k") txtAmount.Text = "100000";
                else if (content == "200k") txtAmount.Text = "200000";
                else if (content == "500k") txtAmount.Text = "500000";
                else if (content == "1M") txtAmount.Text = "1000000";
            }
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            string rawText = txtAmount.Text.Replace(",", "");
            if (decimal.TryParse(rawText, out decimal amount) && amount > 0)
            {
                Amount = amount;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Vui lòng nhập số tiền hợp lệ lớn hơn 0.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
