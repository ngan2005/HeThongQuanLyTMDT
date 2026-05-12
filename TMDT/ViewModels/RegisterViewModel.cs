using System.Windows;
using System.Windows.Input;
using TMDT.Utilities;
using TMDT.DTOs;
using TMDT.Services.Interfaces;
using TMDT.Models;
using TMDT.Services;

namespace TMDT.ViewModels
{
    public class RegisterViewModel : ViewModelBase
    {
        private string _fullName;
        private string _email;
        private string _username;
        private string _password;
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
                MessageBox.Show("Vui lòng điền đầy đủ thông tin!");
                return;
            }

            var request = new RegisterRequest
            {
                Email = Email,
                Password = Password,
                FullName = FullName,
                Phone = "" // Có thể thêm trường Phone vào UI sau
            };

            var success = await _authService.RegisterAsync(request);
            if (success)
            {
                MessageBox.Show("Đăng ký thành công!");
                ExecuteShowLogin(null);
            }
            else
            {
                MessageBox.Show("Đăng ký thất bại. Email có thể đã tồn tại.");
            }
        }

        private void ExecuteShowLogin(object parameter)
        {
            // Chuyển sang màn hình đăng nhập
        }

        private void ExecuteExit(object parameter)
        {
            Application.Current.Shutdown();
        }
    }
}
