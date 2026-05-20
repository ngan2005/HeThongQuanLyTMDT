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

            return new UserDto
            {
                UserCode = user.UserCode ?? $"USR-{user.UserId}",
                Email = user.Email,
                FullName = user.FullName,
                RoleName = user.Role?.RoleName,
                Avatar = user.Avatar
            };
        }

        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            // Kiểm tra email tồn tại
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return false;

            // Lấy RoleId mặc định (ví dụ 2 là Buyer)
            // Trong thực tế nên query theo tên Role
            var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Buyer") 
                             ?? await _context.Roles.FirstOrDefaultAsync();

            var user = new User
            {
                UserCode = "USR-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                FullName = request.FullName,
                Email = request.Email,
                Password = PasswordHelper.HashPassword(request.Password), // Đã mã hóa
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
            // Xử lý logout nếu có session/token
            return Task.FromResult(true);
        }
    }
}
