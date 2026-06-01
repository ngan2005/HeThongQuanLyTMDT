using System;
using TMDT.DTOs;

namespace TMDT.Utilities
{
    public static class SessionManager
    {
        private static UserDto _currentUser;

        public static UserDto CurrentUser
        {
            get => _currentUser;
            set => _currentUser = value;
        }

        public static bool IsLoggedIn => _currentUser != null;

        public static bool IsAdmin => _currentUser?.RoleName == "Admin";

        public static bool IsSeller => _currentUser?.RoleName == "Seller";

        public static bool IsBuyer => _currentUser?.RoleName == "Buyer";

        public static void Clear()
        {
            _currentUser = null;
        }
    }
}
