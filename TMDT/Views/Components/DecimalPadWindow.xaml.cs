using System.Windows;
using System.Windows.Controls;

namespace TMDT.Views.Components
{
    public partial class DecimalPadWindow : Window
    {
        public decimal Result { get; private set; }

        public DecimalPadWindow(decimal initialValue = 0)
        {
            InitializeComponent();
            Result = initialValue;
            txtDisplay.Text = FormatNumber(initialValue);
        }

        private static string FormatNumber(decimal v) => v == 0 ? "0" : ((long)v).ToString("N0");

        private void Key_Click(object sender, RoutedEventArgs e)
        {
            var key = ((Button)sender).Content?.ToString() ?? "";
            var current = txtDisplay.Text.Replace(",", "").Replace(".", "").Replace(" ", "");
            if (current == "0") current = "";

            if (key == "000")
            {
                if (string.IsNullOrEmpty(current)) current = "0";
                else current += "000";
            }
            else
            {
                current += key;
            }

            if (long.TryParse(current, out var n))
            {
                txtDisplay.Text = FormatNumber(n);
            }
        }

        private void Backspace_Click(object sender, RoutedEventArgs e)
        {
            var current = txtDisplay.Text.Replace(",", "").Replace(".", "").Replace(" ", "");
            if (current.Length <= 1) current = "0";
            else current = current.Substring(0, current.Length - 1);
            if (long.TryParse(current, out var n))
                txtDisplay.Text = FormatNumber(n);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            txtDisplay.Text = "0";
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            var s = txtDisplay.Text.Replace(",", "").Replace(".", "").Replace(" ", "");
            if (decimal.TryParse(s, out var v))
            {
                Result = v;
                DialogResult = true;
            }
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
