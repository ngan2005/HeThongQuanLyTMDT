using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TMDT.Views.Seller
{
    public partial class SellerOrdersView : UserControl
    {
        public SellerOrdersView()
        {
            InitializeComponent();
        }

        private void OpenDetail_Click(object sender, RoutedEventArgs e)
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

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Only trigger if double click happens on a row (not empty area or header)
            if (sender is DataGrid grid && grid.SelectedItem != null)
            {
                LightboxOverlay.Visibility = Visibility.Visible;
            }
        }
    }
}
