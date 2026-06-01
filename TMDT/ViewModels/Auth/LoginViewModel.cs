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
                if (IsLoginFailed) IsLoginFailed = false;
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                SetProperty(ref _password, value);
                if (IsLoginFailed) IsLoginFailed = false;
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        private bool _isLoginSuccess;
        public bool IsLoginSuccess
        {
            get => _isLoginSuccess;
            set => SetProperty(ref _isLoginSuccess, value);
        }

        public bool IsLoginFailed
        {
            get => _isLoginFailed;
            set => SetProperty(ref _isLoginFailed, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand ShowRegisterCommand { get; }
        public ICommand ExitCommand { get; }

        public LoginViewModel()
        {
            _authService = new AuthService(new TmdtContext());

            LoginCommand = new RelayCommand(ExecuteLogin);
            ShowRegisterCommand = new RelayCommand(ExecuteShowRegister);
            ExitCommand = new RelayCommand(ExecuteExit);
        }

        private async void ExecuteLogin(object parameter)
        {
            if (IsLoading) return;

            IsLoginFailed = false;

            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                IsLoginFailed = true;
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;

            try
            {
                var user = await _authService.LoginAsync(Username.Trim(), Password);

                if (user != null)
                {
                    // Lưu session
                    SessionManager.CurrentUser = user;

                    Window targetWindow = null;
                    string redirectMsg = "";

                    switch (user.RoleName)
                    {
                        case "Admin":
                            targetWindow = new Views.Admin.AdminMainView();
                            redirectMsg = $"Chào Admin {user.FullName}!";
                            break;
                        case "Seller":
                            targetWindow = new Views.Seller.SellerMainView();
                            redirectMsg = $"Chào Seller {user.FullName}!";
                            break;
                        case "Buyer":
                            targetWindow = new Views.MainWindow();
                            redirectMsg = $"Chào {user.FullName}!";
                            break;
                        default:
                            MessageBox.Show("Tài khoản không có quyền truy cập hệ thống.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                            IsLoading = false;
                            return;
                    }

                    // Đóng Login
                    foreach (Window win in Application.Current.Windows)
                    {
                        if (win is Views.Auth.LoginView)
                        {
                            win.Close();
                            break;
                        }
                    }

                    // Mở trang phù hợp
                    if (targetWindow != null)
                    {
                        MessageBox.Show(redirectMsg, "Đăng nhập thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                        targetWindow.Show();
                    }
                }
                else
                {
                    IsLoginFailed = true;
                    MessageBox.Show("Email hoặc mật khẩu không đúng!", "Đăng nhập thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteShowRegister(object parameter)
        {
            // TODO: navigation sang RegisterView
        }

        private void ExecuteExit(object parameter)
        {
            Application.Current.Shutdown();
        }
    }
}
