using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TMDT.Views.Seller
{
    public partial class SellerVouchersView : UserControl
    {
        public SellerVouchersView()
        {
            InitializeComponent();
        }

        private void OpenCreateVoucher_Click(object sender, RoutedEventArgs e)
        {
            LightboxOverlay.Visibility = Visibility.Visible;
        }

        private void CloseLightbox_Click(object sender, RoutedEventArgs e)
        {
            LightboxOverlay.Visibility = Visibility.Collapsed;
        }

        private void LightboxOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender || (sender is Border b && e.OriginalSource == b))
            {
                LightboxOverlay.Visibility = Visibility.Collapsed;
            }
        }
    }
}
