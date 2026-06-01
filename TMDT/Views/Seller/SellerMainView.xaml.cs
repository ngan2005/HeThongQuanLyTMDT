using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TMDT.ViewModels.Seller;
using TMDT.Views.Auth;
using TMDT.Utilities;

namespace TMDT.Views.Seller
{
    public partial class SellerMainView : Window
    {
        private DateTime _lastClickTime = DateTime.MinValue;

        public SellerMainView()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            // Phát hiện double-click thủ công để Toggle Maximize
            var now = DateTime.Now;
            if ((now - _lastClickTime).TotalMilliseconds < 400)
            {
                ToggleMaximize();
                _lastClickTime = DateTime.MinValue;
                return;
            }
            _lastClickTime = now;

            // Không cho kéo khi đang Maximized
            if (this.WindowState == WindowState.Maximized) return;

            var source = e.OriginalSource as DependencyObject;
            while (source != null)
            {
                if (source is System.Windows.Controls.TextBox ||
                    source is System.Windows.Controls.PasswordBox ||
                    source is System.Windows.Controls.Button ||
                    source is System.Windows.Controls.CheckBox ||
                    source is System.Windows.Controls.ScrollViewer ||
                    source is System.Windows.Controls.DataGrid)
                    return;
                source = VisualTreeHelper.GetParent(source);
            }
            DragMove();
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void ToggleMaximize()
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;
        }

        // Cập nhật CornerRadius và icon khi WindowState thay đổi
        private void Window_StateChanged(object sender, EventArgs e)
        {
            // Tìm MainBorder (Border bao ngoài cùng)
            var mainBorder = this.Template?.FindName("MainBorder", this) as System.Windows.Controls.Border;

            if (this.WindowState == WindowState.Maximized)
            {
                // Khi phóng to: bỏ CornerRadius để lấp đầy màn hình
                if (SidebarBorder != null) SidebarBorder.CornerRadius = new CornerRadius(0);
                MaximizeIcon.Text = "\uE923"; // Restore icon
                MaximizeButton.ToolTip = "Thu nhỏ cửa sổ";
            }
            else
            {
                // Khi restore: khôi phục CornerRadius
                if (SidebarBorder != null) SidebarBorder.CornerRadius = new CornerRadius(15, 0, 0, 15);
                MaximizeIcon.Text = "\uE922"; // Maximize icon
                MaximizeButton.ToolTip = "Phóng to";
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void SidebarToggle_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as SellerMainViewModel;
            if (vm != null)
            {
                vm.IsSidebarExpanded = !vm.IsSidebarExpanded;

                if (vm.IsSidebarExpanded)
                {
                    SidebarColumn.Width = new GridLength(240);
                    SidebarBorder.Width = 240;
                    ToggleArrow.Text = "\uE76B"; // Arrow pointing Left
                }
                else
                {
                    SidebarColumn.Width = new GridLength(72);
                    SidebarBorder.Width = 72;
                    ToggleArrow.Text = "\uE76C"; // Arrow pointing Right
                }
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Bạn có muốn đăng xuất khỏi Kênh Người Bán?", "Đăng xuất", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                SessionManager.Clear();
                var loginView = new LoginView();
                loginView.Show();
                this.Close();
            }
        }
    }
}
