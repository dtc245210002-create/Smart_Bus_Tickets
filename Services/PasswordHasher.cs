using System;
using System.Security.Cryptography;
using System.Text;

namespace WebApplication1.Services
{
    public static class PasswordHasher
    {
        private const string Salt = "SmartBusGo_Secret_Salt_2026";

        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;

            using var sha256 = SHA256.Create();
            var saltedPassword = $"{password}_{Salt}";
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(saltedPassword));
            return Convert.ToBase64String(bytes);
        }

        public static bool VerifyPassword(string inputPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(inputPassword))
                return false;

            // 1. Kiểm tra đối chiếu với mật khẩu đã hash chuẩn SHA256 + Salt
            var computedHash = HashPassword(inputPassword);
            if (string.Equals(computedHash, storedHash, StringComparison.Ordinal))
                return true;

            // 2. Tương thích ngược: đối chiếu trực tiếp chuỗi (nếu mật khẩu ban đầu chưa hash)
            if (string.Equals(inputPassword, storedHash, StringComparison.Ordinal))
                return true;

            // 3. Hỗ trợ tài khoản mẫu có sẵn trong database (demo_hash, PASS_HASH, hashed_pass_*) với mật khẩu mặc định 123456
            if (inputPassword == "123456" && (storedHash == "demo_hash" || storedHash == "PASS_HASH" || storedHash.StartsWith("hashed_pass")))
                return true;

            return false;
        }
    }
}
