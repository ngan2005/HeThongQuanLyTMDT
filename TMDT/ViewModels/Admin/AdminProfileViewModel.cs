using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Helpers;
using TMDT.Utilities;

namespace TMDT.ViewModels.Admin
{
    public class AdminProfileViewModel : ViewModelBase
    {
        private int _userId;

        private string _fullName = "";
        public string FullName
        {
            get => _fullName;
            set { _fullName = value; OnPropertyChanged(); }
        }

        private string _email = "";
        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        private string _phone = "";
        public string Phone
        {
            get => _phone;
            set { _phone = value; OnPropertyChanged(); }
        }

        private string _role = "";
        public string Role
        {
            get => _role;
            set { _role = value; OnPropertyChanged(); }
        }

        private DateTime _joinedDate = DateTime.Now;
        public DateTime JoinedDate
        {
            get => _joinedDate;
            set { _joinedDate = value; OnPropertyChanged(); }
        }

        public string JoinedDateText => JoinedDate.ToString("dd/MM/yyyy");

        private string _avatar = "";
        public string Avatar
        {
            get => _avatar;
            set { _avatar = value; OnPropertyChanged(); }
        }

        private bool _isUploadingAvatar;
        public bool IsUploadingAvatar
        {
            get => _isUploadingAvatar;
            set { _isUploadingAvatar = value; OnPropertyChanged(); }
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
        public ICommand ChangeAvatarCommand { get; }

        public AdminProfileViewModel()
        {
            SaveProfileCommand = new RelayCommand(ExecuteSaveProfile, CanSaveProfile);
            ChangePasswordCommand = new RelayCommand(ExecuteChangePassword);
            ChangeAvatarCommand = new RelayCommand(async _ => await ExecuteChangeAvatar());

            LoadProfile();
        }

        private void LoadProfile()
        {
            var user = SessionManager.CurrentUser;
            if (user == null) return;

            using var ctx = new TmdtContext();
            var dbUser = ctx.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.UserId == user.UserId);

            if (dbUser == null) return;

            _userId = dbUser.UserId;
            FullName = dbUser.FullName ?? "";
            Email = dbUser.Email ?? "";
            Phone = dbUser.Phone ?? "";
            Role = dbUser.Role?.RoleName ?? "";
            JoinedDate = dbUser.CreatedAt ?? DateTime.Now;
            Avatar = dbUser.Avatar ?? "";
        }

        private bool CanSaveProfile(object _)
        {
            return !string.IsNullOrWhiteSpace(FullName) && !string.IsNullOrWhiteSpace(Phone);
        }

        private void ExecuteSaveProfile(object _)
        {
            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Phone))
            {
                MessageBox.Show("Họ tên và số điện thoại không được để trống.", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var ctx = new TmdtContext();
                var user = ctx.Users.FirstOrDefault(u => u.UserId == _userId);
                if (user == null) return;

                user.FullName = FullName.Trim();
                user.Phone = Phone.Trim();
                ctx.SaveChanges();

                // Cập nhật session
                SessionManager.CurrentUser.FullName = user.FullName;

                MessageBox.Show("Cập nhật thông tin thành công!", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                AuditLogHelper.Log("UPDATE_PROFILE", $"Admin cập nhật FullName={user.FullName}, Phone={user.Phone}", "Hồ sơ", "Normal");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteChangePassword(object _)
        {
            if (string.IsNullOrWhiteSpace(CurrentPassword) ||
                string.IsNullOrWhiteSpace(NewPassword) ||
                string.IsNullOrWhiteSpace(ConfirmNewPassword))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ tất cả các trường mật khẩu.", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NewPassword != ConfirmNewPassword)
            {
                MessageBox.Show("Mật khẩu mới và xác nhận mật khẩu không khớp.", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NewPassword.Length < 8)
            {
                MessageBox.Show("Mật khẩu mới phải có ít nhất 8 ký tự.", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var ctx = new TmdtContext();
                var user = ctx.Users.FirstOrDefault(u => u.UserId == _userId);
                if (user == null) return;

                // Verify current password
                if (!PasswordHelper.VerifyPassword(CurrentPassword, user.Password))
                {
                    MessageBox.Show("Mật khẩu hiện tại không đúng.", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                user.Password = PasswordHelper.HashPassword(NewPassword);
                ctx.SaveChanges();

                CurrentPassword = "";
                NewPassword = "";
                ConfirmNewPassword = "";

                MessageBox.Show("Đổi mật khẩu thành công! Vui lòng đăng nhập lại.", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                AuditLogHelper.Log("CHANGE_PASSWORD", "Admin thay đổi mật khẩu", "Hồ sơ", "Warning");

                // Logout
                SessionManager.Clear();
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async System.Threading.Tasks.Task ExecuteChangeAvatar()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Chọn ảnh đại diện",
                    Filter = "Image Files (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp"
                };

                if (openFileDialog.ShowDialog() != true) return;

                var filePath = openFileDialog.FileName;
                var fileInfo = new FileInfo(filePath);

                if (fileInfo.Length > 5 * 1024 * 1024)
                {
                    MessageBox.Show("Ảnh không được lớn hơn 5MB.", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var ext = System.IO.Path.GetExtension(filePath).ToLowerInvariant();
                if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".webp")
                {
                    MessageBox.Show("Định dạng không được hỗ trợ. Chỉ .jpg, .png, .webp.", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                IsUploadingAvatar = true;

                string base64 = await System.Threading.Tasks.Task.Run(() =>
                {
                    byte[] bytes = File.ReadAllBytes(filePath);
                    return Convert.ToBase64String(bytes);
                });

                using var ctx = new TmdtContext();
                var dbUser = await ctx.Users.FindAsync(_userId);
                if (dbUser != null)
                {
                    dbUser.Avatar = base64;
                    await ctx.SaveChangesAsync();
                }

                Avatar = base64;

                // Cập nhật session
                SessionManager.CurrentUser.Avatar = base64;

                MessageBox.Show("Cập nhật ảnh đại diện thành công!", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                AuditLogHelper.Log("UPDATE_AVATAR", "Admin đổi ảnh đại diện", "Hồ sơ", "Normal");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsUploadingAvatar = false;
            }
        }
    }
}
