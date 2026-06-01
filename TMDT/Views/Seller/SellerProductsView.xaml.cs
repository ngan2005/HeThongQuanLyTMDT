using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TMDT.ViewModels.Seller;

namespace TMDT.Views.Seller
{
    public partial class SellerProductsView : UserControl
    {
        public SellerProductsView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ShowLightbox();
        }

        private void OpenDetail_Click(object sender, RoutedEventArgs e)
        {
            ShowLightbox();
        }

        private void AddNewProduct_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SellerProductsViewModel vm)
            {
                vm.ResetFieldsCommand.Execute(null);
            }
            ShowLightbox();
        }

        private void ShowLightbox()
        {
            LightboxOverlay.Visibility = Visibility.Visible;
        }

        private void HideLightbox()
        {
            LightboxOverlay.Visibility = Visibility.Collapsed;
        }

        private void CloseLightbox(object sender, RoutedEventArgs e)
        {
            HideLightbox();
        }

        private void LightboxOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == LightboxOverlay)
                HideLightbox();
        }
    }
}
