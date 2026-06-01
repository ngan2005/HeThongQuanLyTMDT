using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TMDT.Models;
using TMDT.ViewModels.Admin;

namespace TMDT.Views.Admin
{
    public partial class AdminOrdersView : UserControl
    {
        public AdminOrdersView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminOrdersViewModel vm)
            {
                vm.ShowDetailRequest += _ => ShowLightbox();
                vm.HideDetailRequest += HideLightbox;
            }
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem is Order order && DataContext is AdminOrdersViewModel vm)
            {
                vm.SelectedOrder = order;
            }
            ShowLightbox();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem is Order order && DataContext is AdminOrdersViewModel vm)
            {
                vm.SelectedOrder = order;
            }
        }

        private void ViewOrder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is Order order && DataContext is AdminOrdersViewModel vm)
            {
                vm.SelectedOrder = order;
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
            if (sender is RadioButton rb && DataContext is AdminOrdersViewModel vm)
            {
                vm.SelectedStatus = rb.Tag?.ToString() ?? "Tất cả";
            }
        }
    }

    public class LastItemConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values.Length > 0 && values[0] != null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
