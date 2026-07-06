using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using TMDT.Utilities;

namespace TMDT.Views.Seller
{
    public partial class PosShiftWindow : Window
    {
        public decimal OpeningFloat { get; private set; } = 0;

        public PosShiftWindow()
        {
            InitializeComponent();

            txtCashierName.Text = SessionManager.CurrentUser?.FullName ?? "Thu ngân";
            txtDateTime.Text = DateTime.Now.ToString("HH:mm - dddd, dd/MM/yyyy");
            txtOpeningFloat.Text = "0";

            Loaded += (s, e) => txtOpeningFloat.Focus();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(txtOpeningFloat.Text.Replace(",", ""), out decimal amount))
                OpeningFloat = amount;
            else
                OpeningFloat = 0;

            DialogResult = true;
            Close();
        }

        private void NumberOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"[\d]");
        }

        private void txtOpeningFloat_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) btnStart_Click(sender, e);
        }
    }
}
