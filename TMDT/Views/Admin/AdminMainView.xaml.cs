using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TMDT.ViewModels.Admin;
using TMDT.Utilities;

namespace TMDT.Views.Admin
{
    public partial class AdminMainView : Window
    {
        private const double ExpandedWidth = 240;
        private const double CollapsedWidth = 68;
        private DateTime _lastClickTime = DateTime.MinValue;
        private bool _isDarkMode = true; // Dark by default (matches App.xaml)
        private System.Windows.Controls.TextBlock _themeIcon;

        private const string DarkThemeUri  = "Resources/Themes/AdminDarkTheme.xaml";
        private const string LightThemeUri = "Resources/Themes/AdminLightTheme.xaml";

        public AdminMainView()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                // Lấy TextBlock icon từ bên trong ControlTemplate của ThemeToggleBtn
                _themeIcon = FindVisualChild<System.Windows.Controls.TextBlock>(ThemeToggleBtn);
            };
        }

        private void SidebarToggle_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as AdminMainViewModel;
            if (vm == null) return;

            vm.IsSidebarExpanded = !vm.IsSidebarExpanded;
            double targetWidth = vm.IsSidebarExpanded ? ExpandedWidth : CollapsedWidth;

            var widthAnim = new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(280),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            SidebarBorder.BeginAnimation(WidthProperty, widthAnim);

            ToggleArrow.Text = vm.IsSidebarExpanded ? "\uE76B" : "\uE76C";
            BrandTitle.Visibility = vm.IsSidebarExpanded ? Visibility.Visible : Visibility.Collapsed;
            BrandSubtitle.Visibility = vm.IsSidebarExpanded ? Visibility.Visible : Visibility.Collapsed;
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

        // Tự động cập nhật CornerRadius và icon khi WindowState thay đổi
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                MainBorder.CornerRadius = new CornerRadius(0);
                SidebarBorder.CornerRadius = new CornerRadius(0);
                MaximizeIcon.Text = "\uE923"; // Restore icon
                MaximizeButton.ToolTip = "Thu nhỏ cửa sổ";
            }
            else
            {
                MainBorder.CornerRadius = new CornerRadius(16);
                SidebarBorder.CornerRadius = new CornerRadius(15, 0, 0, 15);
                MaximizeIcon.Text = "\uE922"; // Maximize icon
                MaximizeButton.ToolTip = "Phóng to";
            }
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _isDarkMode = !_isDarkMode;

                // Use relative URI to match App.xaml
                var newThemeUri = _isDarkMode ? DarkThemeUri : LightThemeUri;
                var relativeUri = new Uri(newThemeUri, UriKind.Relative);

                var appDicts = Application.Current.Resources.MergedDictionaries;

                // Find and remove the existing Admin theme dict
                var existing = appDicts.FirstOrDefault(d =>
                    d.Source != null &&
                    (d.Source.OriginalString.Contains("AdminDarkTheme") ||
                     d.Source.OriginalString.Contains("AdminLightTheme")));

                if (existing != null)
                {
                    appDicts.Remove(existing);
                }

                // Load and add the new theme dict
                appDicts.Add(new System.Windows.ResourceDictionary { Source = relativeUri });

                // Update theme icon: moon = dark mode, sun = light mode
                if (_themeIcon != null)
                    _themeIcon.Text = _isDarkMode ? "\uE708" : "\uE706"; // Moon / Brightness (Segoe MDL2)

                // Update tooltip
                if (ThemeToggleBtn != null)
                    ThemeToggleBtn.ToolTip = _isDarkMode ? "Chuyển sang chế độ Sáng" : "Chuyển sang chế độ Tối";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi chuyển đổi Theme: " + ex.Message);
                // Revert state if failed
                _isDarkMode = !_isDarkMode;
            }
        }

        // Helper: tìm visual child theo type
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            SessionManager.Clear();
            var loginView = new Auth.LoginView();
            loginView.Show();
            this.Close();
        }
    }
}
