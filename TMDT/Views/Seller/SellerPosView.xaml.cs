using System.Windows.Controls;
using System.Windows.Input;

namespace TMDT.Views.Seller
{
    public partial class SellerPosView : UserControl
    {
        public SellerPosView()
        {
            InitializeComponent();
        }
        
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            
            if (e.Key == Key.F1)
            {
                txtSearch.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.F2)
            {
                txtBarcode.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.F3)
            {
                txtPhone.Focus();
                e.Handled = true;
            }
        }

        private void OpenScanner_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var scanner = new ScannerWindow();
            if (scanner.ShowDialog() == true)
            {
                var barcode = scanner.ScannedBarcode;
                if (!string.IsNullOrEmpty(barcode))
                {
                    // Gán vào TextBox và mô phỏng việc quét
                    if (this.DataContext is TMDT.ViewModels.Seller.SellerPosViewModel vm)
                    {
                        vm.BarcodeInput = barcode;
                    }
                }
            }
        }

        private void ManualDiscount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"[\d]");
        }
    }
}
