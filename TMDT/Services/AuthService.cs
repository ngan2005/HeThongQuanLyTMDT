using System;
using System.Linq;
using System.Collections.Concurrent;
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
        private static readonly ConcurrentDictionary<string, (string Otp, DateTime Expiry)> _otpCache = new ConcurrentDictionary<string, (string, DateTime)>();

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

            // Lấy ShopId + ShopName nếu có Shop
            int? shopId = null;
            string? shopName = null;
            
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.UserId == user.UserId);
            if (shop == null && user.Role?.RoleName == SessionManager.RoleStaff)
            {
                // Nếu là Staff, map với shop mặc định của hệ thống (vì demo)
                shop = await _context.Shops.FirstOrDefaultAsync();
            }

            if (shop != null)
            {
                shopId = shop.ShopId;
                shopName = shop.ShopName;
                
                // Self-healing Role
                if (shop.IsActive == true && user.Role?.RoleName == SessionManager.RoleBuyer)
                {
                    var sellerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == SessionManager.RoleSeller);
                    if (sellerRole != null)
                    {
                        user.RoleId = sellerRole.RoleId;
                        user.Role = sellerRole;
                        await _context.SaveChangesAsync();
                    }
                }
                else if (shop.IsActive == false && user.Role?.RoleName == SessionManager.RoleSeller)
                {
                    var buyerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == SessionManager.RoleBuyer);
                    if (buyerRole != null)
                    {
                        user.RoleId = buyerRole.RoleId;
                        user.Role = buyerRole;
                        await _context.SaveChangesAsync();
                    }
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

        public async Task<UserDto> LoginWithGoogleAsync(string email, string fullName, string avatarUrl)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return null; // Tài khoản không tồn tại, trả về null để LoginViewModel xử lý
            }
            else if (user.IsActive == false)
            {
                return null; // Tài khoản bị khóa
            }

            int? shopId = null;
            string? shopName = null;
            
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.UserId == user.UserId);
            if (shop == null && user.Role?.RoleName == SessionManager.RoleStaff)
            {
                // Nếu là Staff, map với shop mặc định của hệ thống (vì demo)
                shop = await _context.Shops.FirstOrDefaultAsync();
            }

            if (shop != null)
            {
                shopId = shop.ShopId;
                shopName = shop.ShopName;

                // Self-healing Role
                if (shop.IsActive == true && user.Role?.RoleName == SessionManager.RoleBuyer)
                {
                    var sellerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == SessionManager.RoleSeller);
                    if (sellerRole != null)
                    {
                        user.RoleId = sellerRole.RoleId;
                        user.Role = sellerRole;
                        await _context.SaveChangesAsync();
                    }
                }
                else if (shop.IsActive == false && user.Role?.RoleName == SessionManager.RoleSeller)
                {
                    var buyerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == SessionManager.RoleBuyer);
                    if (buyerRole != null)
                    {
                        user.RoleId = buyerRole.RoleId;
                        user.Role = buyerRole;
                        await _context.SaveChangesAsync();
                    }
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

        public async Task<(bool Success, string? ErrorMessage)> SendPasswordResetOtpAsync(string email, IEmailService emailService)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return (false, "Email không tồn tại trong hệ thống.");
            }

            // Generate 6-digit OTP
            Random rand = new Random();
            string otp = rand.Next(100000, 999999).ToString();

            // Store in cache with 5 minutes expiry
            _otpCache[email] = (otp, DateTime.Now.AddMinutes(5));

            string emailBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                    <h2 style='color: #6C63FF; text-align: center;'>Khôi Phục Mật Khẩu</h2>
                    <p>Xin chào <b>{user.FullName ?? "bạn"}</b>,</p>
                    <p>Hệ thống Volox AI nhận được yêu cầu khôi phục mật khẩu từ bạn. Vui lòng sử dụng mã OTP dưới đây để tiến hành thiết lập lại mật khẩu:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <span style='font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #1E293B; background: #F1F5F9; padding: 15px 30px; border-radius: 8px;'>{otp}</span>
                    </div>
                    <p style='color: #EF4444; font-size: 13px; text-align: center;'><i>Mã này sẽ hết hạn sau 5 phút. Vui lòng không chia sẻ mã này cho bất kỳ ai.</i></p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'/>
                    <p style='font-size: 12px; color: #94A3B8; text-align: center;'>Trợ lý vận hành thông minh Volox AI</p>
                </div>";

            var sendResult = await emailService.SendEmailAsync(email, "Volox AI - Mã xác nhận khôi phục mật khẩu", emailBody);
            
            if (!sendResult.Success)
            {
                return (false, sendResult.Message);
            }

            return (true, null);
        }

        public async Task<(bool Success, string? ErrorMessage)> VerifyOtpAndResetPasswordAsync(string email, string otp, string newPassword)
        {
            if (!_otpCache.TryGetValue(email, out var cacheEntry))
            {
                return (false, "Mã OTP không hợp lệ hoặc chưa được yêu cầu.");
            }

            if (DateTime.Now > cacheEntry.Expiry)
            {
                _otpCache.TryRemove(email, out _);
                return (false, "Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới.");
            }

            if (cacheEntry.Otp != otp)
            {
                return (false, "Mã OTP không chính xác.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return (false, "Không tìm thấy tài khoản tương ứng.");
            }

            user.Password = PasswordHelper.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            // Clear OTP after successful reset
            _otpCache.TryRemove(email, out _);

            return (true, null);
        }
    }
}
