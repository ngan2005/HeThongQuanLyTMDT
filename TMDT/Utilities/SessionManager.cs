using System;
using TMDT.DTOs;

namespace TMDT.Utilities
{
    public static class SessionManager
    {
        // Role name constants — dùng thay cho string literal
        public const string RoleAdmin = "Admin";
        public const string RoleSeller = "Seller";
        public const string RoleBuyer = "Buyer";
        public const string RoleStaff = "Staff";

        private static UserDto _currentUser;

        public static UserDto CurrentUser
        {
            get => _currentUser;
            set => _currentUser = value;
        }

        public static bool IsLoggedIn => _currentUser != null;

        public static bool IsAdmin => _currentUser?.RoleName == RoleAdmin;

        public static bool IsSeller => _currentUser?.RoleName == RoleSeller;

        public static bool IsBuyer => _currentUser?.RoleName == RoleBuyer;

        public static bool IsStaff => _currentUser?.RoleName == RoleStaff;

        public static void Clear()
        {
            _currentUser = null;
        }
    }
}
