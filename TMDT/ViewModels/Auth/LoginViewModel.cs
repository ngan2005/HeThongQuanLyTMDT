using System;
using System.Windows;
using System.Windows.Input;
using TMDT.Services;
using TMDT.Services.Interfaces;
using TMDT.Models;
using TMDT.Utilities;

namespace TMDT.ViewModels.Auth
{
    public class LoginViewModel : ViewModelBase
    {
        private string _email = "";
        private string _password = "";
        private bool _isLoading;
        private bool _isLoginFailed;

        public string Email
        {
            get => _email;
            set { SetProperty(ref _email, value); if (IsLoginFailed) IsLoginFailed = false; }
        }

        public string Password
        {
            get => _password;
            set { SetProperty(ref _password, value); if (IsLoginFailed) IsLoginFailed = false; }
        }

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

        public ICommand LoginCommand { get; }
        public ICommand LoginWithGoogleCommand { get; }
        public ICommand ShowRegisterCommand { get; }
        public ICommand ShowForgotPasswordCommand { get; }
        public ICommand ExitCommand { get; }

        private readonly IAuthService _authService;

        public LoginViewModel() : this(new AuthService(new TmdtContext())) { }

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;
            LoginCommand = new RelayCommand(ExecuteLogin);
            LoginWithGoogleCommand = new RelayCommand(ExecuteLoginWithGoogle);
            ShowRegisterCommand = new RelayCommand(ExecuteShowRegister);
            ShowForgotPasswordCommand = new RelayCommand(ExecuteShowForgotPassword);
            ExitCommand = new RelayCommand(ExecuteExit);
        }

        private async void ExecuteLogin(object parameter)
        {
            if (IsLoading) return;

            IsLoginFailed = false;

            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                IsLoginFailed = true;
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;

            try
            {
                var user = await _authService.LoginAsync(Email.Trim(), Password);

                if (user != null)
                {
                    if (SystemSettingsHelper.Current.MaintenanceMode && user.RoleName != SessionManager.RoleAdmin)
                    {
                        MessageBox.Show("Hệ thống đang được bảo trì để nâng cấp. Vui lòng quay lại sau!", "Bảo trì hệ thống", MessageBoxButton.OK, MessageBoxImage.Information);
                        IsLoading = false;
                        return;
                    }

                    SessionManager.CurrentUser = user;

                    if (user.RoleName == SessionManager.RoleBuyer)
                    {
                        CartService.Instance.LoadFromDatabase(user.UserId);
                    }

                    Window targetWindow = null;
                    string redirectMsg = "";

                    switch (user.RoleName)
                    {
                        case SessionManager.RoleAdmin:
                            targetWindow = new Views.Admin.AdminMainView();
                            redirectMsg = $"Chào Admin {user.FullName}!";
                            break;
                        case SessionManager.RoleSeller:
                        case SessionManager.RoleStaff:
                            targetWindow = new Views.Seller.SellerMainView();
                            redirectMsg = $"Chào {user.FullName}!";
                            break;
                        case SessionManager.RoleBuyer:
                            targetWindow = new Views.MainWindow();
                            redirectMsg = $"Chào {user.FullName}!";
                            break;
                        default:
                            MessageBox.Show("Tài khoản không có quyền truy cập hệ thống.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                            IsLoading = false;
                            return;
                    }

                    if (targetWindow != null) targetWindow.Show();

                    var windowsToClose = new System.Collections.Generic.List<Window>();
                    foreach (Window win in Application.Current.Windows)
                    {
                        if (win is Views.Auth.LoginView)
                        {
                            windowsToClose.Add(win);
                        }
                        else if (win is Views.MainWindow && win != targetWindow)
                        {
                            windowsToClose.Add(win);
                        }
                    }

                    foreach (var win in windowsToClose)
                    {
                        win.Close();
                    }

                    MessageBox.Show(redirectMsg, "Đăng nhập thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    IsLoginFailed = true;
                    MessageBox.Show("Email hoặc mật khẩu không đúng!", "Đăng nhập thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void ExecuteLoginWithGoogle(object parameter)
        {
            if (IsLoading) return;
            IsLoading = true;
            IsLoginFailed = false;

            try
            {
                var googleUser = await GoogleAuthService.LoginAsync();
                if (googleUser == null)
                {
                    IsLoading = false;
                    return; // Người dùng hủy hoặc lỗi
                }

                var user = await _authService.LoginWithGoogleAsync(googleUser.Email, googleUser.FullName, googleUser.AvatarUrl);

                if (user == null)
                {
                    MessageBox.Show("Tài khoản chưa được đăng ký hoặc đã bị khóa. Vui lòng đăng ký trước!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    IsLoading = false;
                    ExecuteShowRegister(null);
                    return;
                }

                // Thực hiện logic sau khi đăng nhập
                SessionManager.CurrentUser = user;

                if (user.RoleName == SessionManager.RoleBuyer)
                {
                    CartService.Instance.LoadFromDatabase(user.UserId);
                }

                Window targetWindow = null;
                string redirectMsg = "";

                switch (user.RoleName)
                {
                    case SessionManager.RoleAdmin:
                        targetWindow = new Views.Admin.AdminMainView();
                        redirectMsg = $"Chào Admin {user.FullName}!";
                        break;
                    case SessionManager.RoleSeller:
                    case SessionManager.RoleStaff:
                        targetWindow = new Views.Seller.SellerMainView();
                        redirectMsg = $"Chào {user.FullName}!";
                        break;
                    case SessionManager.RoleBuyer:
                        targetWindow = new Views.MainWindow();
                        redirectMsg = $"Chào {user.FullName}!";
                        break;
                    default:
                        MessageBox.Show("Tài khoản không có quyền truy cập hệ thống.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        IsLoading = false;
                        return;
                }

                if (targetWindow != null) targetWindow.Show();

                var windowsToClose = new System.Collections.Generic.List<Window>();
                foreach (Window win in Application.Current.Windows)
                {
                    if (win is Views.Auth.LoginView) windowsToClose.Add(win);
                    else if (win is Views.MainWindow && win != targetWindow) windowsToClose.Add(win);
                }
                foreach (var win in windowsToClose) win.Close();

                MessageBox.Show(redirectMsg, "Đăng nhập Google thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi Đăng nhập Google: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteShowRegister(object parameter)
        {
            var loginWindow = Application.Current.MainWindow;
            var register = new Views.Auth.RegisterView();
            register.Show();
            loginWindow?.Close();
        }

        private void ExecuteShowForgotPassword(object parameter)
        {
            var loginWindow = Application.Current.MainWindow;
            var forgotWindow = new Views.Auth.ForgotPasswordView();
            forgotWindow.Show();
            loginWindow?.Close();
        }

        private void ExecuteExit(object parameter)
        {
            Application.Current.Shutdown();
        }
    }
}
