using System.Windows;
using System.Windows.Input;

namespace TMDT.Views.Auth
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : Window
    {
        public LoginView()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;

            // Cho phép kéo thả cửa sổ từ bất kỳ vị trí trống nào (không click vào control)
            var source = e.OriginalSource as DependencyObject;
            while (source != null)
            {
                if (source is System.Windows.Controls.TextBox ||
                    source is System.Windows.Controls.PasswordBox ||
                    source is System.Windows.Controls.Button ||
                    source is System.Windows.Controls.CheckBox ||
                    source is System.Windows.Controls.ScrollViewer)
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

        private void GoToRegister_Click(object sender, MouseButtonEventArgs e)
        {
            RegisterView register = new RegisterView();
            register.Show();
            this.Close();
        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (txtPasswordPlaceholder != null && txtPassword != null)
            {
                txtPasswordPlaceholder.Visibility = string.IsNullOrEmpty(txtPassword.Password) 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
            }
        }

        private void TogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (txtPassword.Visibility == Visibility.Visible)
            {
                // Chuyển sang hiển thị mật khẩu bằng TextBox thông thường
                txtPassword.Visibility = Visibility.Collapsed;
                txtPasswordPlaceholder.Visibility = Visibility.Collapsed;
                txtPasswordPlain.Visibility = Visibility.Visible;
                
                // Đồng bộ giá trị từ PasswordBox sang TextBox
                txtPasswordPlain.Text = txtPassword.Password;
                txtPasswordPlain.Focus();
                
                // Thay đổi icon mắt thành icon ẩn mật khẩu (icon mắt có gạch chéo \uF270)
                tbEyeIcon.Text = "\uF270";
            }
            else
            {
                // Chuyển về ẩn mật khẩu bằng PasswordBox
                txtPasswordPlain.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
                
                // Đồng bộ giá trị từ TextBox sang PasswordBox
                txtPassword.Password = txtPasswordPlain.Text;
                txtPassword.Focus();
                
                // Điều khiển hiển thị placeholder của PasswordBox
                txtPasswordPlaceholder.Visibility = string.IsNullOrEmpty(txtPassword.Password) 
                    ? Visibility.Visible 
                    : Visibility.Collapsed;
                
                // Thay đổi icon mắt về bình thường (\uE7B3)
                tbEyeIcon.Text = "\uE7B3";
            }
        }
    }
}
