using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using TMDT.ViewModels.Admin;

namespace TMDT.Views.Admin
{
    public partial class AdminMainView : Window
    {
        private const double ExpandedWidth = 240;
        private const double CollapsedWidth = 68;

        public AdminMainView()
        {
            InitializeComponent();
        }

        private void SidebarToggle_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as AdminMainViewModel;
            if (vm == null) return;

            vm.IsSidebarExpanded = !vm.IsSidebarExpanded;
            double targetWidth = vm.IsSidebarExpanded ? ExpandedWidth : CollapsedWidth;

            // Animate sidebar border width
            var widthAnim = new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(280),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };
            SidebarBorder.BeginAnimation(WidthProperty, widthAnim);

            // Rotate toggle arrow: &#xE76B; = ChevronLeft, &#xE76C; = ChevronRight
            ToggleArrow.Text = vm.IsSidebarExpanded ? "\uE76B" : "\uE76C";

            // Hide/show brand text
            BrandTitle.Visibility = vm.IsSidebarExpanded ? Visibility.Visible : Visibility.Collapsed;
            BrandSubtitle.Visibility = vm.IsSidebarExpanded ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
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
                source = System.Windows.Media.VisualTreeHelper.GetParent(source);
            }
            DragMove();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var loginView = new Auth.LoginView();
            loginView.Show();
            this.Close();
        }
    }
}
