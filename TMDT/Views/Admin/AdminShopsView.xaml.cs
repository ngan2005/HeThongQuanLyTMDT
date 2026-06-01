using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TMDT.ViewModels.Admin;

namespace TMDT.Views.Admin
{
    public partial class AdminShopsView : UserControl
    {
        public AdminShopsView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminShopsViewModel vm)
            {
                vm.ShowDetailRequest += ShowLightbox;
                vm.HideDetailRequest += HideLightbox;
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
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
