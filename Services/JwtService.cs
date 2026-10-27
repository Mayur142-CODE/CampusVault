using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Web.Configuration;
using System.Web.Script.Serialization;
using CampusVault.Models.Auth;

namespace CampusVault.Services
{
    public class JwtService
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expiryMinutes;
        private readonly JavaScriptSerializer _serializer;

        public JwtService()
        {
            _secretKey = WebConfigurationManager.AppSettings["JwtSecretKey"] ?? "CampusVault_Default_Fallback_Secret_Key_2026_Secure_Key";
            _issuer = WebConfigurationManager.AppSettings["JwtIssuer"] ?? "CampusVaultServer";
            _audience = WebConfigurationManager.AppSettings["JwtAudience"] ?? "CampusVaultClients";
            
            if (!int.TryParse(WebConfigurationManager.AppSettings["JwtExpiryMinutes"], out _expiryMinutes))
            {
                _expiryMinutes = 120; // default 2 hours
            }

            _serializer = new JavaScriptSerializer();
        }

        /// <summary>
        /// Generates a signed RFC 7519 JSON Web Token (HS256) for the authenticated user.
        /// </summary>
        public string GenerateToken(User user, out string jwtId, out DateTime expiresAt)
        {
            jwtId = Guid.NewGuid().ToString("N");
            DateTime now = DateTime.UtcNow;
            expiresAt = now.AddMinutes(_expiryMinutes);

            long iat = ToUnixTimeSeconds(now);
            long exp = ToUnixTimeSeconds(expiresAt);

            // 1. Header
            var header = new Dictionary<string, object>
            {
                { "alg", "HS256" },
                { "typ", "JWT" }
            };

            // 2. Payload (Claims)
            var payload = new Dictionary<string, object>
            {
                { "iss", _issuer },
                { "aud", _audience },
                { "sub", user.UserId.ToString() },
                { "jti", jwtId },
                { "iat", iat },
                { "exp", exp },
                { "userId", user.UserId },
                { "email", user.Email },
                { "role", user.RoleName },
                { "name", user.FullName },
                { "roleId", user.RoleId }
            };

            // Profile specific claims
            if (!string.IsNullOrEmpty(user.EnrollmentNumber))
                payload["enrollment"] = user.EnrollmentNumber;
            if (!string.IsNullOrEmpty(user.EmployeeCode))
                payload["employeeCode"] = user.EmployeeCode;

            string headerJson = _serializer.Serialize(header);
            string payloadJson = _serializer.Serialize(payload);

            string encodedHeader = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
            string encodedPayload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));

            string dataToSign = $"{encodedHeader}.{encodedPayload}";
            string signature = ComputeHmacSha256(dataToSign, _secretKey);

            return $"{dataToSign}.{signature}";
        }

        /// <summary>
        /// Validates a JWT token and returns claims if valid, null if invalid or expired.
        /// </summary>
        public JwtClaims ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            try
            {
                string[] parts = token.Split('.');
                if (parts.Length != 3) return null;

                string encodedHeader = parts[0];
                string encodedPayload = parts[1];
                string signature = parts[2];

                // Verify signature
                string expectedSignature = ComputeHmacSha256($"{encodedHeader}.{encodedPayload}", _secretKey);
                if (!string.Equals(signature, expectedSignature, StringComparison.Ordinal))
                {
                    return null; // Invalid signature
                }

                // Decode payload
                string payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(encodedPayload));
                var payload = _serializer.Deserialize<Dictionary<string, object>>(payloadJson);

                if (payload == null) return null;

                // Check expiration
                if (payload.ContainsKey("exp"))
                {
                    long exp = Convert.ToInt64(payload["exp"]);
                    long currentUnix = ToUnixTimeSeconds(DateTime.UtcNow);
                    if (currentUnix > exp)
                    {
                        return null; // Expired
                    }
                }

                return new JwtClaims
                {
                    UserId = payload.ContainsKey("userId") ? Convert.ToInt32(payload["userId"]) : 0,
                    Email = payload.ContainsKey("email") ? payload["email"]?.ToString() : null,
                    Role = payload.ContainsKey("role") ? payload["role"]?.ToString() : null,
                    FullName = payload.ContainsKey("name") ? payload["name"]?.ToString() : null,
                    Jti = payload.ContainsKey("jti") ? payload["jti"]?.ToString() : null,
                    Exp = payload.ContainsKey("exp") ? Convert.ToInt64(payload["exp"]) : 0,
                    Iat = payload.ContainsKey("iat") ? Convert.ToInt64(payload["iat"]) : 0
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Generates a cryptographically strong refresh token string.
        /// </summary>
        public string GenerateRefreshToken()
        {
            byte[] randomBytes = new byte[64];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(randomBytes);
            }
            return Base64UrlEncode(randomBytes);
        }

        private static string ComputeHmacSha256(string data, string secret)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Base64UrlEncode(hash);
            }
        }

        private static string Base64UrlEncode(byte[] input)
        {
            string base64 = Convert.ToBase64String(input);
            return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static byte[] Base64UrlDecode(string input)
        {
            string base64 = input.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }

        private static long ToUnixTimeSeconds(DateTime dateTime)
        {
            return (long)(dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }
    }
}
