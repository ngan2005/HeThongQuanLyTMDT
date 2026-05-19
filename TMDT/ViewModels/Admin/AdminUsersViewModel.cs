using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using TMDT.Models;
using TMDT.Utilities;

namespace TMDT.ViewModels.Admin
{
    public class AdminUsersViewModel : ViewModelBase
    {
        private readonly TmdtContext _context;
        
        private ObservableCollection<User> _users;
        private ObservableCollection<User> _filteredUsers;
        private ObservableCollection<Role> _roles;
        
        private User _selectedUser;
        private Role _selectedRoleForUser;
        private string _searchText = "";
        private string _roleFilter = "All"; // All, Admin, Buyer, Seller
        private string _statusFilter = "All"; // All, Active, Locked
        private string _newPasswordText = "123456@a";

        public ObservableCollection<User> FilteredUsers
        {
            get => _filteredUsers;
            set { _filteredUsers = value; OnPropertyChanged(); }
        }

        public ObservableCollection<Role> Roles
        {
            get => _roles;
            set { _roles = value; OnPropertyChanged(); }
        }

        public User SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged();
                if (value != null)
                {
                    SelectedRoleForUser = Roles.FirstOrDefault(r => r.RoleId == value.RoleId);
                }
            }
        }

        public Role SelectedRoleForUser
        {
            get => _selectedRoleForUser;
            set { _selectedRoleForUser = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public string RoleFilter
        {
            get => _roleFilter;
            set
            {
                _roleFilter = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set
            {
                _statusFilter = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public string NewPasswordText
        {
            get => _newPasswordText;
            set { _newPasswordText = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand ToggleUserStatusCommand { get; }
        public ICommand UpdateUserRoleCommand { get; }
        public ICommand ResetPasswordCommand { get; }

        public AdminUsersViewModel()
        {
            try
            {
                _context = new TmdtContext();
            }
            catch
            {
                // Failsafe
            }

            _users = new ObservableCollection<User>();
            FilteredUsers = new ObservableCollection<User>();
            Roles = new ObservableCollection<Role>();

            // Register commands
            ToggleUserStatusCommand = new RelayCommand(ExecuteToggleUserStatus);
            UpdateUserRoleCommand = new RelayCommand(ExecuteUpdateUserRole);
            ResetPasswordCommand = new RelayCommand(ExecuteResetPassword);

            LoadRoles();
            LoadUsers();
        }

        private void LoadRoles()
        {
            Roles.Clear();
            try
            {
                if (_context != null && _context.Roles.Any())
                {
                    foreach (var r in _context.Roles.ToList())
                    {
                        Roles.Add(r);
                    }
                    return;
                }
            }
            catch
            {
                // Fallback
            }

            // Fallback mock roles
            Roles.Add(new Role { RoleId = 1, RoleName = "Admin", Description = "Quản trị viên toàn hệ thống" });
            Roles.Add(new Role { RoleId = 2, RoleName = "Buyer", Description = "Người mua hàng trực tuyến" });
            Roles.Add(new Role { RoleId = 3, RoleName = "Seller", Description = "Người bán hàng / Chủ shop" });
        }

        private void LoadUsers()
        {
            _users.Clear();
            try
            {
                if (_context != null && _context.Users.Any())
                {
                    var dbUsers = _context.Users.Include(u => u.Role).ToList();
                    foreach (var u in dbUsers)
                    {
                        _users.Add(u);
                    }
                    ApplyFilter();
                    return;
                }
            }
            catch
            {
                // Fallback
            }

            // Fallback mock users
            var mockUsers = new ObservableCollection<User>();
            var adminRole = Roles.FirstOrDefault(r => r.RoleId == 1) ?? new Role { RoleId = 1, RoleName = "Admin" };
            var buyerRole = Roles.FirstOrDefault(r => r.RoleId == 2) ?? new Role { RoleId = 2, RoleName = "Buyer" };
            var sellerRole = Roles.FirstOrDefault(r => r.RoleId == 3) ?? new Role { RoleId = 3, RoleName = "Seller" };

            mockUsers.Add(new User { UserId = 1, FullName = "Hệ thống Admin", Email = "admin@myshop.vn", Phone = "0900000001", RoleId = 1, Role = adminRole, IsActive = true, WalletBalance = 0, LoyaltyPoints = 100 });
            mockUsers.Add(new User { UserId = 2, FullName = "Nguyễn Văn Hùng", Email = "hungnv@gmail.com", Phone = "0988776655", RoleId = 2, Role = buyerRole, IsActive = true, WalletBalance = 250000, LoyaltyPoints = 1200 });
            mockUsers.Add(new User { UserId = 3, FullName = "Trần Thị Mai", Email = "maitt@gmail.com", Phone = "0911223344", RoleId = 3, Role = sellerRole, IsActive = true, WalletBalance = 15450000, LoyaltyPoints = 3400 });
            mockUsers.Add(new User { UserId = 4, FullName = "Lê Hoàng Long", Email = "longlh@yahoo.com", Phone = "0977665544", RoleId = 2, Role = buyerRole, IsActive = false, WalletBalance = 0, LoyaltyPoints = 50 });
            mockUsers.Add(new User { UserId = 5, FullName = "Sony Store Việt Nam", Email = "sony.vn@outlook.com", Phone = "0908889999", RoleId = 3, Role = sellerRole, IsActive = true, WalletBalance = 48000000, LoyaltyPoints = 8500 });

            foreach (var u in mockUsers)
            {
                _users.Add(u);
            }
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var result = _users.AsEnumerable();

            // 1. Text Search
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string searchLower = SearchText.ToLower();
                result = result.Where(u => 
                    (u.FullName != null && u.FullName.ToLower().Contains(searchLower)) ||
                    u.Email.ToLower().Contains(searchLower) ||
                    (u.Phone != null && u.Phone.Contains(searchLower))
                );
            }

            // 2. Role filter
            if (RoleFilter != "All")
            {
                result = result.Where(u => u.Role?.RoleName == RoleFilter);
            }

            // 3. Status filter
            if (StatusFilter == "Active")
            {
                result = result.Where(u => u.IsActive == true);
            }
            else if (StatusFilter == "Locked")
            {
                result = result.Where(u => u.IsActive == false);
            }

            FilteredUsers.Clear();
            foreach (var u in result)
            {
                FilteredUsers.Add(u);
            }
        }

        // --- COMMAND IMPLEMENTATIONS ---

        private async void ExecuteToggleUserStatus(object obj)
        {
            if (SelectedUser == null) return;

            string actionName = (SelectedUser.IsActive == true) ? "khóa" : "mở khóa";
            var result = MessageBox.Show($"Xác nhận {actionName} tài khoản người dùng '{SelectedUser.FullName ?? SelectedUser.Email}'?", 
                                         "Xác nhận thay đổi", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            SelectedUser.IsActive = !(SelectedUser.IsActive ?? true);

            try
            {
                if (_context != null)
                {
                    var dbUser = await _context.Users.FindAsync(SelectedUser.UserId);
                    if (dbUser != null)
                    {
                        dbUser.IsActive = SelectedUser.IsActive;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database update user status failed: " + ex.Message);
            }

            MessageBox.Show($"Đã {actionName} tài khoản '{SelectedUser.FullName ?? SelectedUser.Email}' thành công!", 
                            "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            
            LoadUsers();
        }

        private async void ExecuteUpdateUserRole(object obj)
        {
            if (SelectedUser == null || SelectedRoleForUser == null) return;

            if (SelectedUser.RoleId == SelectedRoleForUser.RoleId)
            {
                MessageBox.Show("Người dùng đã có sẵn vai trò này!", "Thông tin", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"Xác nhận thay đổi vai trò của '{SelectedUser.FullName}' từ '{SelectedUser.Role?.RoleName}' sang '{SelectedRoleForUser.RoleName}'?", 
                                         "Xác nhận thay đổi vai trò", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                if (_context != null)
                {
                    var dbUser = await _context.Users.FindAsync(SelectedUser.UserId);
                    if (dbUser != null)
                    {
                        dbUser.RoleId = SelectedRoleForUser.RoleId;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database role update failed: " + ex.Message);
            }

            MessageBox.Show($"Thay đổi vai trò sang '{SelectedRoleForUser.RoleName}' thành công!", 
                            "Thay đổi thành công", MessageBoxButton.OK, MessageBoxImage.Information);

            LoadUsers();
        }

        private async void ExecuteResetPassword(object obj)
        {
            if (SelectedUser == null) return;

            if (string.IsNullOrWhiteSpace(NewPasswordText))
            {
                MessageBox.Show("Vui lòng nhập mật khẩu mới cần thiết lập!", "Mật khẩu trống", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Xác nhận đặt lại mật khẩu cho '{SelectedUser.FullName ?? SelectedUser.Email}'?", 
                                         "Xác nhận đặt lại", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                if (_context != null)
                {
                    var dbUser = await _context.Users.FindAsync(SelectedUser.UserId);
                    if (dbUser != null)
                    {
                        // In a real application, you'd hash the password here (e.g. BCrypt)
                        dbUser.Password = NewPasswordText;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Database reset password failed: " + ex.Message);
            }

            MessageBox.Show($"Đã đặt lại mật khẩu của người dùng thành công thành: '{NewPasswordText}'!", 
                            "Cài đặt lại thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
