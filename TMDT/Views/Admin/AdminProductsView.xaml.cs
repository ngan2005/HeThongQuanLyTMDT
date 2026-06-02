using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TMDT.Models;
using TMDT.ViewModels.Admin;

namespace TMDT.Views.Admin
{
    public partial class AdminProductsView : UserControl
    {
        public AdminProductsView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminProductsViewModel vm)
            {
                vm.ShowDetailRequest += ShowLightbox;
                vm.HideDetailRequest += HideLightbox;
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem is Product product && DataContext is AdminProductsViewModel vm)
            {
                vm.SelectedProduct = product;
            }
            ShowLightbox();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem is Product product && DataContext is AdminProductsViewModel vm)
            {
                vm.SelectedProduct = product;
            }
        }

        private void ViewProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is Product product && DataContext is AdminProductsViewModel vm)
            {
                vm.SelectedProduct = product;
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

        private void FilterPill_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && DataContext is AdminProductsViewModel vm)
            {
                vm.StatusFilter = rb.Tag?.ToString() ?? "All";
            }
        }
    }
}
