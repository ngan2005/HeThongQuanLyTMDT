using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TMDT.ViewModels.Seller;

namespace TMDT.Views.Seller
{
    public partial class SellerProfileView : UserControl
    {
        public SellerProfileView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SellerProfileViewModel vm)
            {
                vm.OpenProfileRequest += ShowLightbox;
                vm.CloseProfileRequest += HideLightbox;
            }
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
