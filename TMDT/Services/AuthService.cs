using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TMDT.DTOs;
using TMDT.Models;
using TMDT.Services.Interfaces;
using TMDT.Helpers;

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
            if (user.Role?.RoleName == "Seller")
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

        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return false;

            var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Buyer")
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
            return await _context.SaveChangesAsync() > 0;
        }

        public Task<bool> LogoutAsync()
        {
            return Task.FromResult(true);
        }
    }
}
