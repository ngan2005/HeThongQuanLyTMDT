using System;
using System.Windows.Input;
using TMDT.Utilities;

namespace TMDT.ViewModels.Admin
{
    public class AdminProfileViewModel : ViewModelBase
    {
        private string _fullName = "Administrator";
        public string FullName
        {
            get => _fullName;
            set { _fullName = value; OnPropertyChanged(); }
        }

        private string _email = "admin@myshop.com";
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        private string _phone = "0988 123 456";
        public string Phone
        {
            get => _phone;
            set { _phone = value; OnPropertyChanged(); }
        }

        private string _role = "Super Admin";
        public string Role
        {
            get => _role;
            set { _role = value; OnPropertyChanged(); }
        }

        private DateTime _joinedDate = new DateTime(2023, 1, 15);
        public DateTime JoinedDate
        {
            get => _joinedDate;
            set { _joinedDate = value; OnPropertyChanged(); }
        }

        private string _currentPassword = "";
        public string CurrentPassword
        {
            get => _currentPassword;
            set { _currentPassword = value; OnPropertyChanged(); }
        }

        private string _newPassword = "";
        public string NewPassword
        {
            get => _newPassword;
            set { _newPassword = value; OnPropertyChanged(); }
        }

        private string _confirmNewPassword = "";
        public string ConfirmNewPassword
        {
            get => _confirmNewPassword;
            set { _confirmNewPassword = value; OnPropertyChanged(); }
        }

        public ICommand SaveProfileCommand { get; }
        public ICommand ChangePasswordCommand { get; }

        public AdminProfileViewModel()
        {
            SaveProfileCommand = new RelayCommand(ExecuteSaveProfile);
            ChangePasswordCommand = new RelayCommand(ExecuteChangePassword);
        }

        private void ExecuteSaveProfile(object obj)
        {
            // Placeholder for save logic
            System.Windows.MessageBox.Show("Đã cập nhật thông tin cá nhân thành công!", "Thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            AuditLogHelper.Log("UPDATE_PROFILE", "Admin cập nhật thông tin cá nhân", "Cài đặt hệ thống", "Normal");
        }

        private void ExecuteChangePassword(object obj)
        {
            if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword))
            {
                System.Windows.MessageBox.Show("Vui lòng nhập đầy đủ thông tin mật khẩu.", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (NewPassword != ConfirmNewPassword)
            {
                System.Windows.MessageBox.Show("Mật khẩu xác nhận không khớp.", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Placeholder for password change logic
            System.Windows.MessageBox.Show("Đã đổi mật khẩu thành công!", "Thành công", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            AuditLogHelper.Log("CHANGE_PASSWORD", "Admin thay đổi mật khẩu đăng nhập", "Cài đặt hệ thống", "Warning");
            
            CurrentPassword = "";
            NewPassword = "";
            ConfirmNewPassword = "";
        }
    }
}
