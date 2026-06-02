using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TMDT.Models;
using TMDT.ViewModels.Admin;

namespace TMDT.Views.Admin
{
    public partial class AdminWithdrawsView : UserControl
    {
        public AdminWithdrawsView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminWithdrawsViewModel vm)
            {
                vm.ShowDetailRequest += ShowLightbox;
                vm.HideDetailRequest += HideLightbox;
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem is WithdrawRequest request && DataContext is AdminWithdrawsViewModel vm)
            {
                vm.SelectedRequest = request;
            }
            ShowLightbox();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem is WithdrawRequest request && DataContext is AdminWithdrawsViewModel vm)
            {
                vm.SelectedRequest = request;
            }
        }

        private void ViewWithdraw_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is WithdrawRequest request && DataContext is AdminWithdrawsViewModel vm)
            {
                vm.SelectedRequest = request;
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
            if (sender is RadioButton rb && DataContext is AdminWithdrawsViewModel vm)
            {
                vm.StatusFilter = rb.Tag?.ToString() ?? "All";
            }
        }
    }
}
