using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TMDT.Views.Seller
{
    public partial class SellerPosView : UserControl
    {
        // DependencyProperty cho số cột sản phẩm (responsive)
        public static readonly DependencyProperty ProductColumnsProperty =
            DependencyProperty.Register(
                nameof(ProductColumns),
                typeof(int),
                typeof(SellerPosView),
                new PropertyMetadata(2));

        public int ProductColumns
        {
            get => (int)GetValue(ProductColumnsProperty);
            set => SetValue(ProductColumnsProperty, value);
        }

        public SellerPosView()
        {
            InitializeComponent();
            UpdateProductColumns();
        }

        /// <summary>
        /// Cập nhật số cột sản phẩm dựa trên ActualWidth thực tế.
        /// </summary>
        private void UpdateProductColumns()
        {
            if (ActualWidth <= 0) return;

            // Tính: UserControl.ActualWidth - right_panel(380) - splitter(6) - margin(16)
            double availableWidth = ActualWidth - 380 - 6 - 16;
            if (availableWidth < 280) availableWidth = ActualWidth * 0.55;

            // Card hiện tại nằm ngang, cần ít nhất 220px
            int cols = Math.Max(1, Math.Min(5, (int)(availableWidth / 220)));
            if (cols != ProductColumns) ProductColumns = cols;
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateProductColumns();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (e.Key == Key.F1)
            {
                txtSearch.Focus();
                txtSearch.SelectAll();
                e.Handled = true;
            }
            else if (e.Key == Key.F2)
            {
                txtBarcode.Focus();
                txtBarcode.SelectAll();
                e.Handled = true;
            }
            else if (e.Key == Key.F3)
            {
                txtPhone.Focus();
                txtPhone.SelectAll();
                e.Handled = true;
            }
            else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SearchCustomer_Click(this, new System.Windows.RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.F4)
            {
                AddCustomer_Click(this, new System.Windows.RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.F11)
            {
                if (DataContext is TMDT.ViewModels.Seller.SellerPosViewModel vm && vm.HoldOrderCommand.CanExecute(null))
                {
                    vm.HoldOrderCommand.Execute(null);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.F12)
            {
                if (DataContext is TMDT.ViewModels.Seller.SellerPosViewModel vm && vm.ShowHeldOrdersCommand.CanExecute(null))
                {
                    vm.ShowHeldOrdersCommand.Execute(null);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.F5 || e.Key == Key.F6 || e.Key == Key.F7 || e.Key == Key.F8)
            {
                if (DataContext is TMDT.ViewModels.Seller.SellerPosViewModel vm)
                {
                    System.Windows.Input.ICommand? cmd = e.Key switch
                    {
                        Key.F5 => vm.SelectPaymentCashCommand,
                        Key.F6 => vm.SelectPaymentVNPayCommand,
                        Key.F7 => vm.SelectPaymentMoMoCommand,
                        Key.F8 => vm.ReprintReceiptCommand,
                        _ => null
                    };
                    if (cmd?.CanExecute(null) == true)
                    {
                        cmd.Execute(null);
                        e.Handled = true;
                    }
                }
            }
        }

        private void txtPhone_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb) tb.SelectAll();
        }

        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb) tb.SelectAll();
        }

        private void txtBarcode_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb) tb.SelectAll();
        }

        private void OpenScanner_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var scanner = new ScannerWindow();
            if (scanner.ShowDialog() == true)
            {
                var barcode = scanner.ScannedBarcode;
                if (!string.IsNullOrEmpty(barcode))
                {
                    // Gán vào TextBox và mô phỏng việc quét
                    if (this.DataContext is TMDT.ViewModels.Seller.SellerPosViewModel vm)
                    {
                        vm.BarcodeInput = barcode;
                    }
                }
            }
        }

        private void ManualDiscount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"[\d]");
        }

        private void AddCustomer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var addWindow = new AddCustomerWindow();
            addWindow.Owner = System.Windows.Window.GetWindow(this);
            if (addWindow.ShowDialog() == true)
            {
                if (DataContext is TMDT.ViewModels.Seller.SellerPosViewModel vm && vm.SelectedTab != null)
                {
                    vm.SelectedTab.CustomerPhone = addWindow.RegisteredPhone;
                }
            }
        }

        /// <summary>
        /// Mở nhanh form tạo khách hàng với SĐT đã được điền sẵn từ ô tìm KH ở POS.
        /// Sau khi tạo xong sẽ trigger lại SearchCustomerAsync để gắn buyer + điểm tích lũy.
        /// </summary>
        private void QuickCreateCustomer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not TMDT.ViewModels.Seller.SellerPosViewModel vm || vm.SelectedTab == null)
                return;

            // Lấy SĐT hiện tại trong Tab — dùng làm prefill
            var phone = vm.SelectedTab.CustomerPhone?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(phone))
            {
                System.Windows.MessageBox.Show("Vui lòng nhập SĐT khách hàng trước khi tạo nhanh.",
                    "Thiếu SĐT", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                txtPhone.Focus();
                return;
            }

            var addWindow = new AddCustomerWindow(phone);
            addWindow.Owner = System.Windows.Window.GetWindow(this);
            if (addWindow.ShowDialog() == true && !string.IsNullOrWhiteSpace(addWindow.RegisteredPhone))
            {
                // Force refresh: SĐT thường không đổi so với prefill → SetProperty returns false.
                // Đặt "" trước rồi gán lại để đảm bảo setter chạy lại và trigger SearchCustomerAsync.
                vm.SelectedTab.CustomerPhone = "";
                vm.SelectedTab.CustomerPhone = addWindow.RegisteredPhone;
                txtPhone.Focus();
            }
        }

        private void SearchCustomer_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not TMDT.ViewModels.Seller.SellerPosViewModel vm || vm.SelectedTab == null)
                return;

            var searchWindow = new CustomerSearchWindow();
            searchWindow.Owner = System.Windows.Window.GetWindow(this);
            if (searchWindow.ShowDialog() == true && !string.IsNullOrWhiteSpace(searchWindow.SelectedPhone))
            {
                // Gán SĐT → ViewModel sẽ tự động gọi SearchCustomer() và áp dụng điểm tích lũy
                vm.SelectedTab.CustomerPhone = searchWindow.SelectedPhone;
                txtPhone.Focus();
            }
        }

        private void VoucherComboBox_DropDownOpened(object sender, System.EventArgs e)
        {
            if (DataContext is TMDT.ViewModels.Seller.SellerPosViewModel vm)
            {
                _ = vm.LoadActiveVouchersAsync();
            }
        }

        private void OfflineBadge_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is TMDT.ViewModels.Seller.SellerPosViewModel vm)
            {
                vm.ShowOfflineQueueCommand.Execute(null);
            }
        }

        private void OpenPosSettings_Click(object sender, RoutedEventArgs e)
        {
            var win = new TMDT.Views.Seller.PosSettingsWindow { Owner = Application.Current.MainWindow };
            win.ShowDialog();
        }

        private void OpenDecimalPad_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not TMDT.ViewModels.Seller.SellerPosViewModel vm || vm.SelectedTab == null)
                return;

            decimal current = 0;
            if (decimal.TryParse(vm.SelectedTab.CustomerGivenAmountInput, out var c))
                current = c;

            var pad = new TMDT.Views.Components.DecimalPadWindow(current);
            pad.Owner = System.Windows.Window.GetWindow(this);
            if (pad.ShowDialog() == true)
            {
                vm.SelectedTab.CustomerGivenAmountInput = ((long)pad.Result).ToString();
            }
        }

        private void QuickCash_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is decimal amt)
            {
                if (DataContext is TMDT.ViewModels.Seller.SellerPosViewModel vm && vm.SelectedTab != null)
                {
                    vm.SelectedTab.CustomerGivenAmountInput = ((long)amt).ToString();
                }
            }
        }

        private void RecentCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is TMDT.ViewModels.Seller.RecentCustomer rc)
            {
                if (DataContext is TMDT.ViewModels.Seller.SellerPosViewModel vm && vm.SelectedTab != null)
                {
                    vm.SelectedTab.CustomerPhone = rc.Phone;
                    txtPhone.Focus();
                }
            }
        }

        private void Numpad_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Content is string val)
            {
                if (DataContext is TMDT.ViewModels.Seller.SellerPosViewModel vm && vm.SelectedTab != null)
                {
                    var currentStr = vm.SelectedTab.CustomerGivenAmountInput ?? "";
                    // Nếu nhập 000 khi chuỗi rỗng thì bỏ qua
                    if (string.IsNullOrEmpty(currentStr) && val == "000") return;
                    
                    vm.SelectedTab.CustomerGivenAmountInput = currentStr + val;
                }
            }
        }

        private void NumpadClear_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is TMDT.ViewModels.Seller.SellerPosViewModel vm && vm.SelectedTab != null)
            {
                var currentStr = vm.SelectedTab.CustomerGivenAmountInput ?? "";
                if (currentStr.Length > 0)
                {
                    vm.SelectedTab.CustomerGivenAmountInput = currentStr.Substring(0, currentStr.Length - 1);
                }
            }
        }
    }
}
