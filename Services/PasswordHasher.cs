using System;
using System.Security.Cryptography;
using System.Text;

namespace CampusVault.Services
{
    public static class PasswordHasher
    {
        /// <summary>
        /// Generates a cryptographically secure random salt string.
        /// </summary>
        public static string GenerateSalt(int size = 32)
        {
            byte[] saltBytes = new byte[size];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        /// <summary>
        /// Hashes password with provided salt using SHA-256.
        /// Returns hex uppercase string (matching SQL Server HASHBYTES).
        /// </summary>
        public static string HashPassword(string password, string salt, bool useUnicode = true)
        {
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));
            if (string.IsNullOrEmpty(salt)) throw new ArgumentNullException(nameof(salt));

            string combined = password + salt;
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = useUnicode 
                    ? sha256.ComputeHash(Encoding.Unicode.GetBytes(combined))
                    : sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));

                var sb = new StringBuilder();
                foreach (byte b in bytes)
                {
                    sb.Append(b.ToString("X2"));
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Verifies plain text password against stored hash and salt.
        /// Checks both Unicode (SQL Server N'...' standard) and UTF-8.
        /// </summary>
        public static bool VerifyPassword(string enteredPassword, string storedHash, string storedSalt)
        {
            if (string.IsNullOrEmpty(enteredPassword) || string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
                return false;

            // Check Unicode hash (matches SQL Server HASHBYTES on NVARCHAR strings)
            string unicodeHash = HashPassword(enteredPassword, storedSalt, true);
            if (string.Equals(unicodeHash, storedHash, StringComparison.OrdinalIgnoreCase))
                return true;

            // Check UTF-8 hash fallback
            string utf8Hash = HashPassword(enteredPassword, storedSalt, false);
            return string.Equals(utf8Hash, storedHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
