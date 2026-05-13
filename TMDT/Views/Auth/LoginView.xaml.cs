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

            // Chỉ kéo cửa sổ khi click vào vùng nền, không phải vào các control
            var source = e.OriginalSource as DependencyObject;
            while (source != null)
            {
                if (source is System.Windows.Controls.TextBox ||
                    source is System.Windows.Controls.PasswordBox ||
                    source is System.Windows.Controls.Button ||
                    source is System.Windows.Controls.ComboBox ||
                    source is System.Windows.Controls.CheckBox)
                    return;
                source = System.Windows.Media.VisualTreeHelper.GetParent(source);
            }
            DragMove();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void GoToRegister_Click(object sender, RoutedEventArgs e)
        {
            RegisterView register = new RegisterView();
            register.Show();
            this.Close();
        }
    }
}
