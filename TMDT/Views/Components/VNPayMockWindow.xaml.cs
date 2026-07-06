using System.Windows;

namespace TMDT.Views.Components
{
    public partial class VNPayMockWindow : Window
    {
        public VNPayMockWindow(decimal amount)
        {
            InitializeComponent();
            txtAmount.Text = $"{amount:N0} đ";
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
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
