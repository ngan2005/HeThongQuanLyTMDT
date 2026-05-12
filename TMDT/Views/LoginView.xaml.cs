using System.Windows;
using System.Windows.Input;

namespace TMDT.Views
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
            if (e.LeftButton == MouseButtonState.Pressed)
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
