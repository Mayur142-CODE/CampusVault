using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using CampusVault.Models.Auth;

namespace CampusVault.Services
{
    public class AuthRepository
    {
        private readonly string _connectionString;

        public AuthRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["CampusVaultConnection"]?.ConnectionString;
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("Connection string 'CampusVaultConnection' was not found in Web.config.");
            }
        }

        /// <summary>
        /// Retrieves user details by email via sp_AuthenticateUser.
        /// </summary>
        public User GetUserByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_AuthenticateUser", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 150).Value = email.Trim().ToLowerInvariant();

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return MapUserFromReader(reader);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieves full user profile by ID via sp_GetUserProfileById.
        /// </summary>
        public User GetUserById(int userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_GetUserProfileById", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return MapUserFromReader(reader);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Updates student personal details and division via sp_UpdateStudentProfile.
        /// </summary>
        public (bool success, string message) UpdateStudentProfile(int userId, string firstName, string lastName, string phone, string division)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_UpdateStudentProfile", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@FirstName", SqlDbType.NVarChar, 100).Value = firstName.Trim();
                cmd.Parameters.Add("@LastName", SqlDbType.NVarChar, 100).Value = lastName.Trim();
                cmd.Parameters.Add("@PhoneNumber", SqlDbType.NVarChar, 20).Value = (object)phone ?? DBNull.Value;
                cmd.Parameters.Add("@Division", SqlDbType.NVarChar, 20).Value = (object)division ?? DBNull.Value;

                var pSuccess = cmd.Parameters.Add("@Success", SqlDbType.Bit);
                pSuccess.Direction = ParameterDirection.Output;

                var pMessage = cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 255);
                pMessage.Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();

                return ((bool)pSuccess.Value, pMessage.Value?.ToString());
            }
        }

        /// <summary>
        /// Updates user password via sp_ChangeUserPassword.
        /// </summary>
        public (bool success, string message) ChangePassword(int userId, string newHash, string newSalt)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_ChangeUserPassword", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@NewPasswordHash", SqlDbType.NVarChar, 500).Value = newHash;
                cmd.Parameters.Add("@NewPasswordSalt", SqlDbType.NVarChar, 255).Value = newSalt;

                var pSuccess = cmd.Parameters.Add("@Success", SqlDbType.Bit);
                pSuccess.Direction = ParameterDirection.Output;

                var pMessage = cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 255);
                pMessage.Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();

                return ((bool)pSuccess.Value, pMessage.Value?.ToString());
            }
        }

        /// <summary>
        /// Registers a new student via sp_RegisterStudent.
        /// </summary>
        public (int statusCode, string message, int newUserId) RegisterStudent(RegisterStudentRequest req, string passwordHash, string passwordSalt)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_RegisterStudent", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 150).Value = req.Email.Trim().ToLowerInvariant();
                cmd.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 500).Value = passwordHash;
                cmd.Parameters.Add("@PasswordSalt", SqlDbType.NVarChar, 255).Value = passwordSalt;
                cmd.Parameters.Add("@FirstName", SqlDbType.NVarChar, 100).Value = req.FirstName.Trim();
                cmd.Parameters.Add("@LastName", SqlDbType.NVarChar, 100).Value = req.LastName.Trim();
                cmd.Parameters.Add("@PhoneNumber", SqlDbType.NVarChar, 20).Value = (object)req.PhoneNumber ?? DBNull.Value;
                cmd.Parameters.Add("@EnrollmentNumber", SqlDbType.NVarChar, 50).Value = (object)req.EnrollmentNumber?.Trim() ?? DBNull.Value;
                cmd.Parameters.Add("@Department", SqlDbType.NVarChar, 100).Value = (object)req.Department?.Trim() ?? DBNull.Value;
                cmd.Parameters.Add("@Semester", SqlDbType.Int).Value = (object)req.Semester ?? DBNull.Value;
                cmd.Parameters.Add("@Division", SqlDbType.NVarChar, 20).Value = (object)req.Division ?? DBNull.Value;
                cmd.Parameters.Add("@RollNumber", SqlDbType.NVarChar, 30).Value = (object)req.RollNumber ?? DBNull.Value;
                cmd.Parameters.Add("@BatchYear", SqlDbType.Int).Value = (object)req.BatchYear ?? DBNull.Value;

                var pNewUserId = cmd.Parameters.Add("@NewUserId", SqlDbType.Int);
                pNewUserId.Direction = ParameterDirection.Output;

                var pStatusCode = cmd.Parameters.Add("@StatusCode", SqlDbType.Int);
                pStatusCode.Direction = ParameterDirection.Output;

                var pStatusMessage = cmd.Parameters.Add("@StatusMessage", SqlDbType.NVarChar, 255);
                pStatusMessage.Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();

                int statusCode = (int)pStatusCode.Value;
                string message = pStatusMessage.Value?.ToString() ?? "";
                int newUserId = pNewUserId.Value != DBNull.Value ? (int)pNewUserId.Value : 0;

                return (statusCode, message, newUserId);
            }
        }

        /// <summary>
        /// Registers a new faculty member via sp_RegisterFaculty.
        /// </summary>
        public (int statusCode, string message, int newUserId) RegisterFaculty(RegisterFacultyRequest req, string passwordHash, string passwordSalt)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_RegisterFaculty", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 150).Value = req.Email.Trim().ToLowerInvariant();
                cmd.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 500).Value = passwordHash;
                cmd.Parameters.Add("@PasswordSalt", SqlDbType.NVarChar, 255).Value = passwordSalt;
                cmd.Parameters.Add("@FirstName", SqlDbType.NVarChar, 100).Value = req.FirstName.Trim();
                cmd.Parameters.Add("@LastName", SqlDbType.NVarChar, 100).Value = req.LastName.Trim();
                cmd.Parameters.Add("@PhoneNumber", SqlDbType.NVarChar, 20).Value = (object)req.PhoneNumber ?? DBNull.Value;
                cmd.Parameters.Add("@EmployeeCode", SqlDbType.NVarChar, 50).Value = (object)req.EmployeeCode?.Trim() ?? DBNull.Value;
                cmd.Parameters.Add("@Department", SqlDbType.NVarChar, 100).Value = (object)req.Department?.Trim() ?? DBNull.Value;
                cmd.Parameters.Add("@Designation", SqlDbType.NVarChar, 100).Value = (object)req.Designation?.Trim() ?? DBNull.Value;
                cmd.Parameters.Add("@Qualification", SqlDbType.NVarChar, 100).Value = (object)req.Qualification ?? DBNull.Value;
                cmd.Parameters.Add("@OfficeLocation", SqlDbType.NVarChar, 100).Value = (object)req.OfficeLocation ?? DBNull.Value;

                var pNewUserId = cmd.Parameters.Add("@NewUserId", SqlDbType.Int);
                pNewUserId.Direction = ParameterDirection.Output;

                var pStatusCode = cmd.Parameters.Add("@StatusCode", SqlDbType.Int);
                pStatusCode.Direction = ParameterDirection.Output;

                var pStatusMessage = cmd.Parameters.Add("@StatusMessage", SqlDbType.NVarChar, 255);
                pStatusMessage.Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();

                int statusCode = (int)pStatusCode.Value;
                string message = pStatusMessage.Value?.ToString() ?? "";
                int newUserId = pNewUserId.Value != DBNull.Value ? (int)pNewUserId.Value : 0;

                return (statusCode, message, newUserId);
            }
        }

        /// <summary>
        /// Registers a new administrator via sp_RegisterAdmin.
        /// </summary>
        public (int statusCode, string message, int newUserId) RegisterAdmin(RegisterAdminRequest req, string passwordHash, string passwordSalt)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_RegisterAdmin", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 150).Value = req.Email.Trim().ToLowerInvariant();
                cmd.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, 500).Value = passwordHash;
                cmd.Parameters.Add("@PasswordSalt", SqlDbType.NVarChar, 255).Value = passwordSalt;
                cmd.Parameters.Add("@FirstName", SqlDbType.NVarChar, 100).Value = req.FirstName.Trim();
                cmd.Parameters.Add("@LastName", SqlDbType.NVarChar, 100).Value = req.LastName.Trim();
                cmd.Parameters.Add("@PhoneNumber", SqlDbType.NVarChar, 20).Value = (object)req.PhoneNumber ?? DBNull.Value;
                cmd.Parameters.Add("@Department", SqlDbType.NVarChar, 100).Value = (object)req.Department?.Trim() ?? "Administration";
                cmd.Parameters.Add("@AccessLevel", SqlDbType.NVarChar, 50).Value = (object)req.AccessLevel?.Trim() ?? "Tier 1 - Full Campus Access";

                var pNewUserId = cmd.Parameters.Add("@NewUserId", SqlDbType.Int);
                pNewUserId.Direction = ParameterDirection.Output;

                var pStatusCode = cmd.Parameters.Add("@StatusCode", SqlDbType.Int);
                pStatusCode.Direction = ParameterDirection.Output;

                var pStatusMessage = cmd.Parameters.Add("@StatusMessage", SqlDbType.NVarChar, 255);
                pStatusMessage.Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();

                int statusCode = (int)pStatusCode.Value;
                string message = pStatusMessage.Value?.ToString() ?? "";
                int newUserId = pNewUserId.Value != DBNull.Value ? (int)pNewUserId.Value : 0;

                return (statusCode, message, newUserId);
            }
        }

        /// <summary>
        /// Saves a generated JWT Refresh Token via sp_SaveRefreshToken.
        /// </summary>
        public void SaveRefreshToken(int userId, string token, string jwtId, DateTime expiresAt, string clientIp)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_SaveRefreshToken", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@Token", SqlDbType.NVarChar, 500).Value = token;
                cmd.Parameters.Add("@JwtId", SqlDbType.NVarChar, 100).Value = (object)jwtId ?? DBNull.Value;
                cmd.Parameters.Add("@ExpiresAt", SqlDbType.DateTime2).Value = expiresAt;
                cmd.Parameters.Add("@CreatedByIp", SqlDbType.NVarChar, 50).Value = (object)clientIp ?? DBNull.Value;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Logs user authentication action via sp_LogUserAuth.
        /// </summary>
        public void LogAuthAction(int? userId, string action, string ipAddress, string userAgent, string details)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("sp_LogUserAuth", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = (object)userId ?? DBNull.Value;
                    cmd.Parameters.Add("@Action", SqlDbType.NVarChar, 50).Value = action;
                    cmd.Parameters.Add("@IpAddress", SqlDbType.NVarChar, 50).Value = (object)ipAddress ?? DBNull.Value;
                    cmd.Parameters.Add("@UserAgent", SqlDbType.NVarChar, 500).Value = (object)userAgent ?? DBNull.Value;
                    cmd.Parameters.Add("@Details", SqlDbType.NVarChar, -1).Value = (object)details ?? DBNull.Value;

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
                // Logging failure should never break user authentication flow
            }
        }

        private static bool HasColumn(SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static string GetStringSafe(SqlDataReader reader, string columnName)
        {
            if (HasColumn(reader, columnName) && reader[columnName] != DBNull.Value)
                return reader[columnName].ToString();
            return null;
        }

        private static int? GetIntSafe(SqlDataReader reader, string columnName)
        {
            if (HasColumn(reader, columnName) && reader[columnName] != DBNull.Value)
                return Convert.ToInt32(reader[columnName]);
            return null;
        }

        private static bool GetBoolSafe(SqlDataReader reader, string columnName, bool defaultValue = false)
        {
            if (HasColumn(reader, columnName) && reader[columnName] != DBNull.Value)
                return Convert.ToBoolean(reader[columnName]);
            return defaultValue;
        }

        private static User MapUserFromReader(SqlDataReader reader)
        {
            return new User
            {
                UserId = HasColumn(reader, "UserId") ? (int)reader["UserId"] : 0,
                RoleId = HasColumn(reader, "RoleId") ? (int)reader["RoleId"] : 0,
                RoleName = GetStringSafe(reader, "RoleName") ?? "Student",
                Email = GetStringSafe(reader, "Email") ?? string.Empty,
                PasswordHash = GetStringSafe(reader, "PasswordHash") ?? string.Empty,
                PasswordSalt = GetStringSafe(reader, "PasswordSalt") ?? string.Empty,
                FirstName = GetStringSafe(reader, "FirstName") ?? string.Empty,
                LastName = GetStringSafe(reader, "LastName") ?? string.Empty,
                PhoneNumber = GetStringSafe(reader, "PhoneNumber"),
                ProfilePictureUrl = GetStringSafe(reader, "ProfilePictureUrl"),
                IsActive = GetBoolSafe(reader, "IsActive", true),
                IsEmailVerified = GetBoolSafe(reader, "IsEmailVerified", false),
                
                // Student info
                EnrollmentNumber = GetStringSafe(reader, "EnrollmentNumber"),
                StudentDepartment = GetStringSafe(reader, "StudentDepartment"),
                Semester = GetIntSafe(reader, "Semester"),
                Division = GetStringSafe(reader, "Division"),
                RollNumber = GetStringSafe(reader, "RollNumber"),

                // Faculty info
                EmployeeCode = GetStringSafe(reader, "EmployeeCode"),
                FacultyDepartment = GetStringSafe(reader, "FacultyDepartment"),
                FacultyDesignation = GetStringSafe(reader, "FacultyDesignation"),

                // Admin info
                AdminDepartment = GetStringSafe(reader, "AdminDepartment"),
                AdminAccessLevel = GetStringSafe(reader, "AdminAccessLevel")
            };
        }

        // ── FORGOT PASSWORD ──────────────────────────────────────────────────────
        public (bool success, string message, int userId) CreatePasswordResetToken(string email, string token)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_CreatePasswordResetToken", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Email",   SqlDbType.NVarChar, 150).Value = email.Trim().ToLowerInvariant();
                cmd.Parameters.Add("@Token",   SqlDbType.NVarChar, 128).Value = token;
                var pOk  = cmd.Parameters.Add("@Success", SqlDbType.Bit);           pOk.Direction  = ParameterDirection.Output;
                var pMsg = cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 255); pMsg.Direction = ParameterDirection.Output;
                var pUid = cmd.Parameters.Add("@UserId",  SqlDbType.Int);           pUid.Direction = ParameterDirection.Output;
                conn.Open();
                cmd.ExecuteNonQuery();
                return (
                    (bool)pOk.Value,
                    pMsg.Value?.ToString(),
                    pUid.Value != DBNull.Value ? (int)pUid.Value : 0
                );
            }
        }

        public (bool success, string message) ResetPasswordWithToken(string token, string newPassword)
        {
            string salt = PasswordHasher.GenerateSalt();
            string hash = PasswordHasher.HashPassword(newPassword, salt);
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_ResetPasswordWithToken", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@Token",   SqlDbType.NVarChar, 128).Value = token;
                cmd.Parameters.Add("@NewHash", SqlDbType.NVarChar, 500).Value = hash;
                cmd.Parameters.Add("@NewSalt", SqlDbType.NVarChar, 255).Value = salt;
                var pOk  = cmd.Parameters.Add("@Success", SqlDbType.Bit);           pOk.Direction  = ParameterDirection.Output;
                var pMsg = cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 255); pMsg.Direction = ParameterDirection.Output;
                conn.Open();
                cmd.ExecuteNonQuery();
                return ((bool)pOk.Value, pMsg.Value?.ToString());
            }
        }

        // ── PROFILE PICTURE ──────────────────────────────────────────────────────
        public (bool success, string message) UpdateProfilePicture(int userId, string pictureUrl)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_UpdateProfilePicture", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId",        SqlDbType.Int).Value           = userId;
                cmd.Parameters.Add("@ProfilePicUrl", SqlDbType.NVarChar, 500).Value = pictureUrl;
                var pOk  = cmd.Parameters.Add("@Success", SqlDbType.Bit);           pOk.Direction  = ParameterDirection.Output;
                var pMsg = cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 255); pMsg.Direction = ParameterDirection.Output;
                conn.Open();
                cmd.ExecuteNonQuery();
                return ((bool)pOk.Value, pMsg.Value?.ToString());
            }
        }
    }
}
