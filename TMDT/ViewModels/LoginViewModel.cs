using System.Windows;
using System.Windows.Input;
using TMDT.Utilities;
using TMDT.Services.Interfaces;
using TMDT.Models;
using TMDT.Services;

namespace TMDT.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private string _username;
        private string _password;
        private readonly IAuthService _authService;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        // Commands
        public ICommand LoginCommand { get; }
        public ICommand ShowRegisterCommand { get; }
        public ICommand ExitCommand { get; }

        public LoginViewModel()
        {
            // Trong thực tế nên dùng DI Container
            _authService = new AuthService(new TmdtContext());
            
            LoginCommand = new RelayCommand(ExecuteLogin);
            ShowRegisterCommand = new RelayCommand(ExecuteShowRegister);
            ExitCommand = new RelayCommand(ExecuteExit);
        }

        private async void ExecuteLogin(object parameter)
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin!");
                return;
            }

            var user = await _authService.LoginAsync(Username, Password);
            if (user != null)
            {
                MessageBox.Show($"Chào mừng {user.FullName}!");
                // Chuyển sang màn hình chính ở đây
            }
            else
            {
                MessageBox.Show("Email hoặc mật khẩu không đúng!");
            }
        }

        private void ExecuteShowRegister(object parameter)
        {
            // Logic chuyển sang màn hình đăng ký sẽ được xử lý qua View hoặc NavigationService
        }

        private void ExecuteExit(object parameter)
        {
            Application.Current.Shutdown();
        }
    }
}
