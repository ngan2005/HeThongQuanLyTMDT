using System;
using System.Security.Cryptography;
using System.Text;

namespace TMDT.Helpers
{
    public static class PasswordHelper
    {
        // Hàm mã hóa mật khẩu đơn giản bằng SHA256
        // Trong thực tế nên dùng BCrypt hoặc Argon2
        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            string hashOfInput = HashPassword(password);
            return string.Compare(hashOfInput, hashedPassword, StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}
