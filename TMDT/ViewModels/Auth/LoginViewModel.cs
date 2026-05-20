using System.Windows;
using System.Windows.Input;
using TMDT.Utilities;
using TMDT.Services.Interfaces;
using TMDT.Models;
using TMDT.Services;

namespace TMDT.ViewModels.Auth
{
    public class LoginViewModel : ViewModelBase
    {
        private string _username;
        private string _password;
        private bool _isLoading;
        private bool _isLoginFailed;
        private readonly IAuthService _authService;

        public string Username
        {
            get => _username;
            set 
            {
                SetProperty(ref _username, value);
                if (IsLoginFailed) IsLoginFailed = false; // Reset error when typing
                if (IsLoginSuccess) IsLoginSuccess = false;
            }
        }

        public string Password
        {
            get => _password;
            set 
            {
                SetProperty(ref _password, value);
                if (IsLoginFailed) IsLoginFailed = false; // Reset error when typing
                if (IsLoginSuccess) IsLoginSuccess = false;
            }
        }

        private bool _isLoginSuccess;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsLoginFailed
        {
            get => _isLoginFailed;
            set => SetProperty(ref _isLoginFailed, value);
        }

        public bool IsLoginSuccess
        {
            get => _isLoginSuccess;
            set => SetProperty(ref _isLoginSuccess, value);
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
            if (IsLoading) return;

            IsLoginFailed = false;
            IsLoginSuccess = false;

            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                IsLoginFailed = true;
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin!");
                return;
            }

            IsLoading = true;

            // Giả lập thời gian phản hồi mạng khoảng 1s để chạy hoạt ảnh xoay loading mượt mà
            await Task.Delay(1000);

            TMDT.DTOs.UserDto user = null;

            // Failsafe Mock Admin Login giúp bạn test giao diện cực nhanh!
            if (Username.Trim().ToLower() == "admin" && Password == "admin")
            {
                user = new TMDT.DTOs.UserDto
                {
                    UserCode = "USR-ADMIN",
                    Email = "admin@myshop.com",
                    FullName = "Administrator Tối Cao",
                    RoleName = "Admin",
                    Avatar = ""
                };
            }
            else
            {
                user = await _authService.LoginAsync(Username, Password);
            }

            IsLoading = false;

            if (user != null)
            {
                IsLoginSuccess = true;
                // Chờ mascot chạy hoạt ảnh thành công (success reaction)
                await Task.Delay(1200);

                MessageBox.Show($"Chào mừng {user.FullName} ({user.RoleName})!");
                
                if (user.RoleName == "Admin")
                {
                    var adminView = new TMDT.Views.Admin.AdminMainView();
                    adminView.Show();
                }
                else
                {
                    var mainView = new TMDT.Views.MainWindow();
                    mainView.Show();
                }

                // Tìm và đóng cửa sổ LoginView hiện tại
                foreach (Window win in Application.Current.Windows)
                {
                    if (win is TMDT.Views.Auth.LoginView)
                    {
                        win.Close();
                        break;
                    }
                }
            }
            else
            {
                IsLoginFailed = true;
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
