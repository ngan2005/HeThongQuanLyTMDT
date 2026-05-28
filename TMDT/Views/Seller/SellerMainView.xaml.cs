using System.Windows;
using System.Windows.Input;
using TMDT.ViewModels.Seller;
using TMDT.Views.Auth;

namespace TMDT.Views.Seller
{
    public partial class SellerMainView : Window
    {
        public SellerMainView()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
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
                var loginView = new LoginView();
                loginView.Show();
                this.Close();
            }
        }
    }
}
