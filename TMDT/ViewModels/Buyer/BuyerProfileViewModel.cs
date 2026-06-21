using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Utilities;
using TMDT.Helpers;
using TMDT.Services;
using TMDT.Views;

namespace TMDT.ViewModels.Buyer
{
    public class BuyerProfileViewModel : ViewModelBase
    {
        private readonly BuyerMainViewModel _mainVm;

        private int _userId;
        private string _fullName = "";
        private string _email = "";
        private string _phone = "";
        private string _avatar = "";
        private DateTime _joinedDate = DateTime.Now;
        private decimal _walletBalance;
        private int _loyaltyPoints;
        private int _totalOrders;
        private int _pendingOrders;
        private int _completedOrders;
        private bool _isUploadingAvatar;
        private string _currentPassword = "";
        private string _newPassword = "";
        private string _confirmPassword = "";
        private Address? _selectedAddress;
        private bool _isAddingAddress;
        private string _newRecipientName = "";
        private string _newPhone = "";
        private string _newFullAddress = "";

        public string FullName
        {
            get => _fullName;
            set { SetProperty(ref _fullName, value); }
        }

        public string Email
        {
            get => _email;
            set { SetProperty(ref _email, value); }
        }

        public string Phone
        {
            get => _phone;
            set { SetProperty(ref _phone, value); }
        }

        public string Avatar
        {
            get => _avatar;
            set { SetProperty(ref _avatar, value); }
        }

        public DateTime JoinedDate
        {
            get => _joinedDate;
            set { SetProperty(ref _joinedDate, value); }
        }

        public string JoinedDateText => JoinedDate.ToString("dd/MM/yyyy");

        public decimal WalletBalance
        {
            get => _walletBalance;
            set { SetProperty(ref _walletBalance, value); }
        }

        public string WalletBalanceText => WalletBalance >= 1000000
            ? (WalletBalance / 1000000m).ToString("N1") + "M"
            : WalletBalance.ToString("N0") + "đ";

        public int LoyaltyPoints
        {
            get => _loyaltyPoints;
            set { SetProperty(ref _loyaltyPoints, value); }
        }

        public int TotalOrders
        {
            get => _totalOrders;
            set { SetProperty(ref _totalOrders, value); }
        }

        public int PendingOrders
        {
            get => _pendingOrders;
            set { SetProperty(ref _pendingOrders, value); }
        }

        public int CompletedOrders
        {
            get => _completedOrders;
            set { SetProperty(ref _completedOrders, value); }
        }

        public bool IsUploadingAvatar
        {
            get => _isUploadingAvatar;
            set { SetProperty(ref _isUploadingAvatar, value); }
        }

        public string CurrentPassword
        {
            get => _currentPassword;
            set { SetProperty(ref _currentPassword, value); }
        }

        public string NewPassword
        {
            get => _newPassword;
            set { SetProperty(ref _newPassword, value); }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set { SetProperty(ref _confirmPassword, value); }
        }

        public Address? SelectedAddress
        {
            get => _selectedAddress;
            set { SetProperty(ref _selectedAddress, value); }
        }

        public bool IsAddingAddress
        {
            get => _isAddingAddress;
            set { SetProperty(ref _isAddingAddress, value); }
        }

        public string NewRecipientName
        {
            get => _newRecipientName;
            set { SetProperty(ref _newRecipientName, value); }
        }

        public string NewPhone
        {
            get => _newPhone;
            set { SetProperty(ref _newPhone, value); }
        }

        public string NewFullAddress
        {
            get => _newFullAddress;
            set { SetProperty(ref _newFullAddress, value); }
        }

        public ObservableCollection<Address> Addresses { get; } = new();

        public bool IsEmpty => Addresses.Count == 0;

        public ICommand SaveProfileCommand { get; }
        public ICommand ChangePasswordCommand { get; }
        public ICommand ChangeAvatarCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand AddAddressCommand { get; }
        public ICommand DeleteAddressCommand { get; }
        public ICommand SetDefaultAddressCommand { get; }
        public ICommand ToggleAddAddressCommand { get; }
        public ICommand OpenAddressPickerCommand { get; }

        public BuyerProfileViewModel(BuyerMainViewModel mainVm)
        {
            _mainVm = mainVm;

            SaveProfileCommand = new RelayCommand(ExecuteSaveProfile);
            ChangePasswordCommand = new RelayCommand(ExecuteChangePassword);
            ChangeAvatarCommand = new RelayCommand(async _ => await ExecuteChangeAvatar());
            BackCommand = new RelayCommand(_ => _mainVm.NavigateHome());
            AddAddressCommand = new RelayCommand(_ => OpenAddressPickerForNew());
            OpenAddressPickerCommand = new RelayCommand(_ => OpenAddressPickerForNew());
            DeleteAddressCommand = new RelayCommand(a => ExecuteDeleteAddress(a as Address));
            SetDefaultAddressCommand = new RelayCommand(a => ExecuteSetDefaultAddress(a as Address));
            ToggleAddAddressCommand = new RelayCommand(_ => IsAddingAddress = !IsAddingAddress);

            LoadProfile();
            LoadAddresses();
            LoadStats();
        }

        private void LoadProfile()
        {
            var user = SessionManager.CurrentUser;
            if (user == null) return;

            try
            {
                using var ctx = new TmdtContext();
                var dbUser = ctx.Users
                    .Include(u => u.Role)
                    .FirstOrDefault(u => u.UserId == user.UserId);

                if (dbUser == null) return;

                _userId = dbUser.UserId;
                FullName = dbUser.FullName ?? "";
                Email = dbUser.Email ?? "";
                Phone = dbUser.Phone ?? "";
                Avatar = dbUser.Avatar ?? "";
                JoinedDate = dbUser.CreatedAt ?? DateTime.Now;
                WalletBalance = dbUser.WalletBalance ?? 0;
                LoyaltyPoints = dbUser.LoyaltyPoints ?? 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Load profile failed: " + ex.Message);
            }
        }

        private void LoadAddresses()
        {
            if (!SessionManager.IsLoggedIn) return;
            try
            {
                using var ctx = new TmdtContext();
                var list = ctx.Addresses
                    .Where(a => a.UserId == SessionManager.CurrentUser!.UserId)
                    .OrderByDescending(a => a.IsDefault)
                    .ThenBy(a => a.AddressId)
                    .ToList();

                Addresses.Clear();
                foreach (var addr in list)
                    Addresses.Add(addr);

                SelectedAddress = Addresses.FirstOrDefault(a => a.IsDefault == true) ?? Addresses.FirstOrDefault();
                OnPropertyChanged(nameof(IsEmpty));
            }
            catch { }
        }

        private void LoadStats()
        {
            if (!SessionManager.IsLoggedIn) return;
            try
            {
                using var ctx = new TmdtContext();
                var orders = ctx.Orders
                    .Where(o => o.BuyerId == SessionManager.CurrentUser!.UserId)
                    .ToList();

                TotalOrders = orders.Count;
                PendingOrders = orders.Count(o => o.OrderStatus == "Pending" || o.OrderStatus == "Shipping");
                CompletedOrders = orders.Count(o => o.OrderStatus == "Completed");
            }
            catch { }
        }

        private bool CanAddAddress()
        {
            return !string.IsNullOrWhiteSpace(NewRecipientName) &&
                   !string.IsNullOrWhiteSpace(NewPhone) &&
                   !string.IsNullOrWhiteSpace(NewFullAddress);
        }

        private void OpenAddressPickerForNew()
        {
            var dialog = new AddressPickerDialog();
            dialog.Owner = Application.Current.MainWindow;
            var result = dialog.ShowDialog();
            if (result == true && dialog.SavedAddress != null)
            {
                var addr = dialog.SavedAddress;
                addr.UserId = SessionManager.CurrentUser!.UserId;
                addr.IsDefault = Addresses.Count == 0 || dialog.IsDefault;

                if (addr.IsDefault == true)
                {
                    using var ctx = new TmdtContext();
                    var existing = ctx.Addresses.Where(a => a.UserId == SessionManager.CurrentUser!.UserId);
                    foreach (var a in existing)
                        a.IsDefault = false;
                    ctx.SaveChanges();
                }

                using var ctx2 = new TmdtContext();
                ctx2.Addresses.Add(addr);
                ctx2.SaveChanges();

                LoadAddresses();
                IsAddingAddress = false;
                MessageBox.Show("Địa chỉ đã được thêm.", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                IsAddingAddress = false;
            }
        }

        private void ExecuteAddAddress()
        {
            if (!SessionManager.IsLoggedIn) return;

            try
            {
                using var ctx = new TmdtContext();
                var addr = new Address
                {
                    UserId = SessionManager.CurrentUser!.UserId,
                    RecipientName = NewRecipientName.Trim(),
                    Phone = NewPhone.Trim(),
                    FullAddress = NewFullAddress.Trim(),
                    IsDefault = Addresses.Count == 0
                };

                ctx.Addresses.Add(addr);
                ctx.SaveChanges();

                LoadAddresses();

                NewRecipientName = "";
                NewPhone = "";
                NewFullAddress = "";
                IsAddingAddress = false;

                MessageBox.Show("Địa chỉ đã được thêm.", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteDeleteAddress(Address? addr)
        {
            if (addr == null) return;

            var result = MessageBox.Show(
                $"Xóa địa chỉ '{addr.FullAddress}'?",
                "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                using var ctx = new TmdtContext();
                var dbAddr = ctx.Addresses.Find(addr.AddressId);
                if (dbAddr != null)
                {
                    ctx.Addresses.Remove(dbAddr);
                    ctx.SaveChanges();
                }

                LoadAddresses();
                MessageBox.Show("Địa chỉ đã xóa.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { }
        }

        private void ExecuteSetDefaultAddress(Address? addr)
        {
            if (addr == null || !addr.IsDefault.GetValueOrDefault()) return;

            try
            {
                using var ctx = new TmdtContext();
                var userAddrs = ctx.Addresses.Where(a => a.UserId == SessionManager.CurrentUser!.UserId);
                foreach (var a in userAddrs)
                    a.IsDefault = a.AddressId == addr.AddressId;

                ctx.SaveChanges();
                LoadAddresses();
            }
            catch { }
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

                SessionManager.CurrentUser.FullName = user.FullName;

                MessageBox.Show("Cập nhật thông tin thành công!", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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
                string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ các trường mật khẩu.", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                MessageBox.Show("Mật khẩu mới và xác nhận không khớp.", "Lỗi",
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
                ConfirmPassword = "";

                MessageBox.Show("Đổi mật khẩu thành công! Vui lòng đăng nhập lại.", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                SessionManager.Clear();
                CartService.Instance.Clear();
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExecuteChangeAvatar()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Chọn ảnh đại diện",
                    Filter = "Image Files (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp"
                };

                if (dialog.ShowDialog() != true) return;

                var fileInfo = new FileInfo(dialog.FileName);
                if (fileInfo.Length > 5 * 1024 * 1024)
                {
                    MessageBox.Show("Ảnh không được lớn hơn 5MB.", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var ext = Path.GetExtension(dialog.FileName).ToLowerInvariant();
                if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".webp")
                {
                    MessageBox.Show("Định dạng không được hỗ trợ.", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                IsUploadingAvatar = true;

                string base64 = await Task.Run(() =>
                {
                    byte[] bytes = File.ReadAllBytes(dialog.FileName);
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
                SessionManager.CurrentUser.Avatar = base64;

                MessageBox.Show("Cập nhật ảnh đại diện thành công!", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);
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
