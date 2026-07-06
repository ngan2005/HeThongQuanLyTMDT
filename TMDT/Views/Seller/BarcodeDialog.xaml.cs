using System.Windows;

namespace TMDT.Views.Seller
{
    public partial class BarcodeDialog : Window
    {
        public BarcodeDialog()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
