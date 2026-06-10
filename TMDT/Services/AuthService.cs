using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TMDT.DTOs;
using TMDT.Models;
using TMDT.Services.Interfaces;
using TMDT.Helpers;
using TMDT.Utilities;

namespace TMDT.Services
{
    public class AuthService : IAuthService
    {
        private readonly TmdtContext _context;

        public AuthService(TmdtContext context)
        {
            _context = context;
        }

        public async Task<UserDto> LoginAsync(string email, string password)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || !PasswordHelper.VerifyPassword(password, user.Password))
                return null;

            // Lấy ShopId + ShopName nếu là Seller
            int? shopId = null;
            string? shopName = null;
            if (user.Role?.RoleName == SessionManager.RoleSeller)
            {
                var shop = await _context.Shops.FirstOrDefaultAsync(s => s.UserId == user.UserId);
                if (shop != null)
                {
                    shopId = shop.ShopId;
                    shopName = shop.ShopName;
                }
            }

            return new UserDto
            {
                UserId = user.UserId,
                UserCode = user.UserCode ?? $"USR-{user.UserId}",
                Email = user.Email,
                FullName = user.FullName ?? user.Email.Split('@')[0],
                RoleName = user.Role?.RoleName,
                Avatar = user.Avatar,
                ShopId = shopId,
                ShopName = shopName
            };
        }

        public async Task<(bool Success, string? ErrorMessage)> RegisterAsync(RegisterRequest request)
        {
            // Kiểm tra email trùng
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return (false, "Email này đã được đăng ký. Vui lòng sử dụng email khác.");

            // Kiểm tra email hợp lệ
            if (string.IsNullOrWhiteSpace(request.Email) ||
                !request.Email.Contains('@') ||
                !request.Email.Contains('.'))
                return (false, "Email không hợp lệ. Vui lòng nhập đúng định dạng email.");

            // Kiểm tra độ dài mật khẩu
            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
                return (false, "Mật khẩu phải có ít nhất 6 ký tự.");

            var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == SessionManager.RoleBuyer)
                             ?? await _context.Roles.FirstOrDefaultAsync();

            var user = new User
            {
                UserCode = "USR-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                FullName = request.FullName,
                Email = request.Email,
                Password = PasswordHelper.HashPassword(request.Password),
                Phone = request.Phone,
                RoleId = defaultRole?.RoleId ?? 1,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            _context.Users.Add(user);
            return await _context.SaveChangesAsync() > 0
                ? (true, null)
                : (false, "Đã xảy ra lỗi khi lưu. Vui lòng thử lại.");
        }

        public Task<bool> LogoutAsync()
        {
            return Task.FromResult(true);
        }
    }
}
