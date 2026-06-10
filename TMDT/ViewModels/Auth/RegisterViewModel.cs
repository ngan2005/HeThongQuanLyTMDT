using System.Windows;
using System.Windows.Input;
using TMDT.Utilities;
using TMDT.DTOs;
using TMDT.Services.Interfaces;
using TMDT.Services;
using TMDT.Models;

namespace TMDT.ViewModels.Auth
{
    public class RegisterViewModel : ViewModelBase
    {
        private string _fullName;
        private string _email;
        private string _password;
        private string _phone;
        private readonly IAuthService _authService;

        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        // Commands
        public ICommand RegisterCommand { get; }
        public ICommand ShowLoginCommand { get; }
        public ICommand ExitCommand { get; }

        public RegisterViewModel()
        {
            // Trong thực tế nên dùng DI Container
            _authService = new AuthService(new TmdtContext());

            RegisterCommand = new RelayCommand(ExecuteRegister);
            ShowLoginCommand = new RelayCommand(ExecuteShowLogin);
            ExitCommand = new RelayCommand(ExecuteExit);
        }

        private async void ExecuteRegister(object parameter)
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(FullName))
            {
                MessageBox.Show("Vui lòng điền đầy đủ thông tin!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var request = new RegisterRequest
            {
                Email = Email,
                Password = Password,
                FullName = FullName,
                Phone = Phone ?? ""
            };

            var (success, errorMessage) = await _authService.RegisterAsync(request);
            if (success)
            {
                MessageBox.Show("Đăng ký thành công! Vui lòng đăng nhập.", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                ExecuteShowLogin(null);
            }
            else
            {
                MessageBox.Show(errorMessage ?? "Đăng ký thất bại.", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteShowLogin(object parameter)
        {
            var login = new Views.Auth.LoginView();
            login.Show();

            foreach (System.Windows.Window w in Application.Current.Windows)
            {
                if (w is Views.Auth.RegisterView)
                {
                    w.Close();
                    break;
                }
            }
        }

        private void ExecuteExit(object parameter)
        {
            Application.Current.Shutdown();
        }
    }
}
