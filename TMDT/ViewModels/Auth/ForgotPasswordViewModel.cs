using System.Threading.Tasks;
using System.Windows.Input;
using TMDT.Services.Interfaces;
using TMDT.Utilities;
using System.Windows;
using TMDT.Views.Auth;
using System.Text.RegularExpressions;

namespace TMDT.ViewModels.Auth
{
    public class ForgotPasswordViewModel : ViewModelBase
    {
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;

        private string _email = "";
        private string _otp = "";
        private string _newPassword = "";
        private bool _isEmailStep = true;
        private bool _isOtpStep = false;
        private bool _isLoading;
        private string _errorMessage;
        private string _successMessage;

        public string Email
        {
            get => _email;
            set { SetProperty(ref _email, value); ClearMessages(); }
        }

        public string Otp
        {
            get => _otp;
            set { SetProperty(ref _otp, value); ClearMessages(); }
        }

        public string NewPassword
        {
            get => _newPassword;
            set { SetProperty(ref _newPassword, value); ClearMessages(); }
        }

        public bool IsEmailStep
        {
            get => _isEmailStep;
            set => SetProperty(ref _isEmailStep, value);
        }

        public bool IsOtpStep
        {
            get => _isOtpStep;
            set => SetProperty(ref _isOtpStep, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public string SuccessMessage
        {
            get => _successMessage;
            set => SetProperty(ref _successMessage, value);
        }

        public ICommand SendOtpCommand { get; }
        public ICommand VerifyOtpAndResetCommand { get; }
        public ICommand BackToLoginCommand { get; }

        public ForgotPasswordViewModel() : this(new TMDT.Services.AuthService(new TMDT.Models.TmdtContext()), new TMDT.Services.EmailService()) { }

        public ForgotPasswordViewModel(IAuthService authService, IEmailService emailService)
        {
            _authService = authService;
            _emailService = emailService;

            SendOtpCommand = new RelayCommand(ExecuteSendOtp);
            VerifyOtpAndResetCommand = new RelayCommand(ExecuteVerifyOtpAndReset);
            BackToLoginCommand = new RelayCommand(ExecuteBackToLogin);
        }

        private async void ExecuteSendOtp(object parameter)
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Vui lòng nhập Email.";
                return;
            }

            if (!Regex.IsMatch(Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ErrorMessage = "Định dạng Email không hợp lệ.";
                return;
            }

            IsLoading = true;
            ClearMessages();

            var result = await _authService.SendPasswordResetOtpAsync(Email, _emailService);

            IsLoading = false;

            if (result.Success)
            {
                SuccessMessage = "Mã OTP đã được gửi đến Email của bạn.";
                IsEmailStep = false;
                IsOtpStep = true;
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }

        private async void ExecuteVerifyOtpAndReset(object parameter)
        {
            if (string.IsNullOrWhiteSpace(Otp))
            {
                ErrorMessage = "Vui lòng nhập mã OTP.";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
            {
                ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                return;
            }

            IsLoading = true;
            ClearMessages();

            var result = await _authService.VerifyOtpAndResetPasswordAsync(Email, Otp, NewPassword);

            IsLoading = false;

            if (result.Success)
            {
                MessageBox.Show("Mật khẩu đã được khôi phục thành công! Vui lòng đăng nhập bằng mật khẩu mới.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                ExecuteBackToLogin(null);
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }

        private void ExecuteBackToLogin(object parameter)
        {
            // Mở lại form Login
            var loginWindow = new LoginView();
            loginWindow.Show();
            
            // Đóng cửa sổ hiện tại
            foreach (Window window in Application.Current.Windows)
            {
                if (window is ForgotPasswordView)
                {
                    window.Close();
                    break;
                }
            }
        }

        private void ClearMessages()
        {
            ErrorMessage = null;
            SuccessMessage = null;
        }
    }
}
