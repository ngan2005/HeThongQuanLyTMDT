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

        /// <summary>
        /// 🟢 Click "Lưu" trong card "TỶ LỆ CHIẾT KHẤU" — cập nhật CommissionRate riêng cho shop.
        /// </summary>
        private void BtnUpdateCommission_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is AdminShopsViewModel vm && vm.SelectedShop != null)
            {
                if (!decimal.TryParse(txtCommissionRate.Text, out var rate))
                {
                    MessageBox.Show("Vui lòng nhập số hợp lệ (0-100).", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtCommissionRate.Focus();
                    txtCommissionRate.SelectAll();
                    return;
                }
                vm.UpdateCommissionCommand.Execute(rate);
            }
        }

        /// <summary>Chỉ cho phép nhập số + dấu thập phân.</summary>
        private void CommissionRate_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Chấp nhận 0-9 và dấu chấm thập phân (cho phép 2.5%)
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c) && c != '.')
                {
                    e.Handled = true;
                    return;
                }
            }
        }
    }
}
