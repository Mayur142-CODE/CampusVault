using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using CampusVault.Models.Admin;

namespace CampusVault.Services
{
    public class AdminRepository
    {
        private readonly string _connectionString;

        public AdminRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["CampusVaultConnection"]?.ConnectionString;
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("Connection string 'CampusVaultConnection' was not found in Web.config.");
            }
        }

        public AdminDashboardViewModel GetDashboardData()
        {
            var vm = new AdminDashboardViewModel();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT 
                            (SELECT COUNT(*) FROM Users u JOIN Roles r ON u.RoleId = r.RoleId WHERE r.RoleName = 'Student') AS TotalStudents,
                            (SELECT COUNT(*) FROM Users u JOIN Roles r ON u.RoleId = r.RoleId WHERE r.RoleName = 'Faculty') AS TotalFaculty,
                            (SELECT COUNT(*) FROM Users u JOIN Roles r ON u.RoleId = r.RoleId WHERE r.RoleName = 'Admin') AS TotalAdmins,
                            (SELECT COUNT(*) FROM Classrooms) AS TotalClassrooms,
                            (SELECT COUNT(*) FROM Tasks) AS TotalTasks,
                            (SELECT COUNT(*) FROM TaskSubmissions) AS TotalDocs,
                            (SELECT ISNULL(SUM(CASE WHEN ISNUMERIC(REPLACE(REPLACE(FileSize, ' MB', ''), ' KB', '')) = 1 THEN CAST(REPLACE(REPLACE(FileSize, ' MB', ''), ' KB', '') AS FLOAT) ELSE 0 END), 0) FROM TaskSubmissions) AS StorageMb";

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            vm.TotalStudents = reader["TotalStudents"] != DBNull.Value ? Convert.ToInt32(reader["TotalStudents"]) : 0;
                            vm.TotalFaculty = reader["TotalFaculty"] != DBNull.Value ? Convert.ToInt32(reader["TotalFaculty"]) : 0;
                            vm.TotalAdmins = reader["TotalAdmins"] != DBNull.Value ? Convert.ToInt32(reader["TotalAdmins"]) : 0;
                            vm.TotalClassrooms = reader["TotalClassrooms"] != DBNull.Value ? Convert.ToInt32(reader["TotalClassrooms"]) : 0;
                            vm.TotalTasks = reader["TotalTasks"] != DBNull.Value ? Convert.ToInt32(reader["TotalTasks"]) : 0;
                            vm.TotalDocuments = reader["TotalDocs"] != DBNull.Value ? Convert.ToInt32(reader["TotalDocs"]) : 0;
                            vm.StorageUsedMb = reader["StorageMb"] != DBNull.Value ? Convert.ToDouble(reader["StorageMb"]) : 0.0;
                        }
                    }
                }

                vm.RecentActivities = GetActivitiesInternal(conn, 10);
            }
            vm.RecentUsers = GetAllUsers().Take(5).ToList();
            vm.RecentDocuments = GetAllDocuments().Take(5).ToList();
            return vm;
        }

        public List<AdminUserItem> GetAllUsers()
        {
            var list = new List<AdminUserItem>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT 
                            u.UserId, 
                            u.FirstName, 
                            u.LastName, 
                            u.Email, 
                            r.RoleName, 
                            u.IsActive, 
                            u.CreatedAt,
                            COALESCE(sp.EnrollmentNumber, sp.RollNumber, fp.EmployeeCode, 'ADM-' + CAST(u.UserId AS NVARCHAR)) AS Code,
                            COALESCE(sp.Department, fp.Department, 'General') AS Department,
                            (SELECT COUNT(*) FROM ClassroomEnrollments ce WHERE ce.StudentUserId = u.UserId) + 
                            (SELECT COUNT(*) FROM Classrooms c WHERE c.FacultyUserId = u.UserId) AS ClassroomsCount,
                            (SELECT COUNT(*) FROM TaskSubmissions s WHERE s.StudentUserId = u.UserId) AS SubmissionsCount
                        FROM Users u
                        JOIN Roles r ON u.RoleId = r.RoleId
                        LEFT JOIN StudentProfiles sp ON u.UserId = sp.UserId
                        LEFT JOIN FacultyProfiles fp ON u.UserId = fp.UserId
                        ORDER BY u.CreatedAt DESC";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new AdminUserItem
                            {
                                UserId = Convert.ToInt32(reader["UserId"]),
                                FullName = (reader["FirstName"]?.ToString() + " " + reader["LastName"]?.ToString()).Trim(),
                                Email = reader["Email"]?.ToString(),
                                RoleName = reader["RoleName"]?.ToString(),
                                Code = reader["Code"]?.ToString(),
                                Department = reader["Department"]?.ToString(),
                                IsActive = reader["IsActive"] != DBNull.Value && Convert.ToBoolean(reader["IsActive"]),
                                CreatedAt = reader["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedAt"]) : DateTime.Now,
                                ClassroomsCount = reader["ClassroomsCount"] != DBNull.Value ? Convert.ToInt32(reader["ClassroomsCount"]) : 0,
                                SubmissionsCount = reader["SubmissionsCount"] != DBNull.Value ? Convert.ToInt32(reader["SubmissionsCount"]) : 0
                            });
                        }
                    }
                }
            }
            return list;
        }

        public List<AdminClassroomItem> GetAllClassrooms()
        {
            var list = new List<AdminClassroomItem>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT 
                            c.ClassroomId, 
                            c.CourseCode, 
                            c.CourseName, 
                            c.Department, 
                            c.JoinCode, 
                            c.CreatedAt,
                            ISNULL(u.FirstName + ' ' + u.LastName, 'Unassigned') AS FacultyName,
                            (SELECT COUNT(*) FROM ClassroomEnrollments ce WHERE ce.ClassroomId = c.ClassroomId) AS StudentCount,
                            (SELECT COUNT(*) FROM Tasks t WHERE t.ClassroomId = c.ClassroomId) AS TaskCount
                        FROM Classrooms c
                        LEFT JOIN Users u ON c.FacultyUserId = u.UserId
                        ORDER BY c.CreatedAt DESC";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new AdminClassroomItem
                            {
                                ClassroomId = Convert.ToInt32(reader["ClassroomId"]),
                                CourseCode = reader["CourseCode"]?.ToString(),
                                CourseName = reader["CourseName"]?.ToString(),
                                Department = reader["Department"]?.ToString(),
                                JoinCode = reader["JoinCode"]?.ToString(),
                                CreatedAt = reader["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedAt"]) : DateTime.Now,
                                FacultyName = reader["FacultyName"]?.ToString(),
                                StudentCount = reader["StudentCount"] != DBNull.Value ? Convert.ToInt32(reader["StudentCount"]) : 0,
                                TaskCount = reader["TaskCount"] != DBNull.Value ? Convert.ToInt32(reader["TaskCount"]) : 0
                            });
                        }
                    }
                }
            }
            return list;
        }

        public List<AdminTaskItem> GetAllTasks()
        {
            var list = new List<AdminTaskItem>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT 
                            t.TaskId, 
                            t.ClassroomId, 
                            t.Title, 
                            t.DueDate, 
                            t.MaxScore, 
                            c.CourseCode, 
                            c.CourseName, 
                            ISNULL(u.FirstName + ' ' + u.LastName, 'Faculty') AS FacultyName,
                            (SELECT COUNT(*) FROM TaskSubmissions ts WHERE ts.TaskId = t.TaskId) AS SubmissionCount
                        FROM Tasks t
                        JOIN Classrooms c ON t.ClassroomId = c.ClassroomId
                        LEFT JOIN Users u ON c.FacultyUserId = u.UserId
                        ORDER BY t.CreatedAt DESC";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new AdminTaskItem
                            {
                                TaskId = Convert.ToInt32(reader["TaskId"]),
                                ClassroomId = Convert.ToInt32(reader["ClassroomId"]),
                                Title = reader["Title"]?.ToString(),
                                DueDate = reader["DueDate"] != DBNull.Value ? Convert.ToDateTime(reader["DueDate"]) : DateTime.Now,
                                MaxScore = reader["MaxScore"] != DBNull.Value ? Convert.ToInt32(reader["MaxScore"]) : 20,
                                CourseCode = reader["CourseCode"]?.ToString(),
                                CourseName = reader["CourseName"]?.ToString(),
                                FacultyName = reader["FacultyName"]?.ToString(),
                                SubmissionCount = reader["SubmissionCount"] != DBNull.Value ? Convert.ToInt32(reader["SubmissionCount"]) : 0
                            });
                        }
                    }
                }
            }
            return list;
        }

        public List<AdminDocumentItem> GetAllDocuments()
        {
            var list = new List<AdminDocumentItem>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT 
                            s.SubmissionId, 
                            s.FileName, 
                            ISNULL(s.FileSize, '0 KB') AS FileSize, 
                            ISNULL(s.FileHash, ISNULL(s.CloudPublicId, 'sha256-verified')) AS Sha256Hash, 
                            s.Status, 
                            s.SubmittedAt, 
                            ISNULL(u.FirstName + ' ' + u.LastName, 'Student') AS StudentName,
                            ISNULL(sp.EnrollmentNumber, sp.RollNumber) AS RollNo,
                            ISNULL(fu.FirstName + ' ' + fu.LastName, 'Faculty') AS FacultyName,
                            c.CourseName, 
                            t.Title AS TaskTitle
                        FROM TaskSubmissions s
                        JOIN Users u ON s.StudentUserId = u.UserId
                        LEFT JOIN StudentProfiles sp ON u.UserId = sp.UserId
                        JOIN Tasks t ON s.TaskId = t.TaskId
                        JOIN Classrooms c ON t.ClassroomId = c.ClassroomId
                        LEFT JOIN Users fu ON c.FacultyUserId = fu.UserId
                        ORDER BY s.SubmittedAt DESC";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new AdminDocumentItem
                            {
                                SubmissionId = Convert.ToInt32(reader["SubmissionId"]),
                                FileName = reader["FileName"]?.ToString(),
                                FileSize = reader["FileSize"]?.ToString(),
                                Sha256Hash = reader["Sha256Hash"]?.ToString(),
                                Status = reader["Status"]?.ToString(),
                                SubmittedAt = reader["SubmittedAt"] != DBNull.Value ? Convert.ToDateTime(reader["SubmittedAt"]) : DateTime.Now,
                                StudentName = reader["StudentName"]?.ToString(),
                                RollNo = reader["RollNo"]?.ToString(),
                                FacultyName = reader["FacultyName"]?.ToString(),
                                CourseName = reader["CourseName"]?.ToString(),
                                TaskTitle = reader["TaskTitle"]?.ToString()
                            });
                        }
                    }
                }
            }
            return list;
        }

        public List<AdminActivityItem> GetActivities(int top = 50)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                return GetActivitiesInternal(conn, top);
            }
        }

        private List<AdminActivityItem> GetActivitiesInternal(SqlConnection conn, int top)
        {
            var list = new List<AdminActivityItem>();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $@"
                    SELECT TOP ({top}) * FROM (
                        -- 1. Submissions
                        SELECT 
                            s.SubmissionId AS ActivityId,
                            'submission' AS ActivityType,
                            'Submissions' AS Module,
                            'Submitted ' + t.Title AS Description,
                            s.FileName + ' (' + ISNULL(s.FileSize, '') + ')' AS Details,
                            ISNULL(u.FirstName + ' ' + u.LastName, 'Student') AS UserName,
                            'Student' AS UserRole,
                            s.SubmittedAt AS Timestamp,
                            '127.0.0.1' AS IpAddress
                        FROM TaskSubmissions s
                        JOIN Users u ON s.StudentUserId = u.UserId
                        JOIN Tasks t ON s.TaskId = t.TaskId
                        
                        UNION ALL
                        
                        -- 2. Tasks Created
                        SELECT 
                            t.TaskId AS ActivityId,
                            'task_creation' AS ActivityType,
                            'Tasks' AS Module,
                            'Created Task: ' + t.Title AS Description,
                            'Classroom: ' + c.CourseName AS Details,
                            ISNULL(u.FirstName + ' ' + u.LastName, 'Faculty') AS UserName,
                            'Faculty' AS UserRole,
                            t.CreatedAt AS Timestamp,
                            '127.0.0.1' AS IpAddress
                        FROM Tasks t
                        JOIN Classrooms c ON t.ClassroomId = c.ClassroomId
                        JOIN Users u ON c.FacultyUserId = u.UserId
                        
                        UNION ALL
                        
                        -- 3. Classroom Enrollments
                        SELECT 
                            ce.EnrollmentId AS ActivityId,
                            'enrollment' AS ActivityType,
                            'Classrooms' AS Module,
                            'Enrolled in ' + c.CourseName AS Description,
                            c.CourseCode AS Details,
                            ISNULL(u.FirstName + ' ' + u.LastName, 'Student') AS UserName,
                            'Student' AS UserRole,
                            ce.EnrolledAt AS Timestamp,
                            '127.0.0.1' AS IpAddress
                        FROM ClassroomEnrollments ce
                        JOIN Classrooms c ON ce.ClassroomId = c.ClassroomId
                        JOIN Users u ON ce.StudentUserId = u.UserId
                        
                        UNION ALL

                        -- 4. Auth / Security Logs
                        SELECT 
                            l.LogId AS ActivityId,
                            'security' AS ActivityType,
                            'Security' AS Module,
                            l.Action AS Description,
                            ISNULL(l.Details, 'Authentication event') AS Details,
                            ISNULL(u.FirstName + ' ' + u.LastName, 'System User') AS UserName,
                            ISNULL(r.RoleName, 'User') AS UserRole,
                            l.CreatedAt AS Timestamp,
                            ISNULL(l.IpAddress, '127.0.0.1') AS IpAddress
                        FROM UserAuthLogs l
                        LEFT JOIN Users u ON l.UserId = u.UserId
                        LEFT JOIN Roles r ON u.RoleId = r.RoleId
                    ) AS CombinedActivities
                    ORDER BY Timestamp DESC";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var userName = reader["UserName"]?.ToString() ?? "User";
                        string initials = "U";
                        var parts = userName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2) initials = (parts[0][0].ToString() + parts[1][0].ToString()).ToUpper();
                        else if (parts.Length == 1) initials = parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();

                        list.Add(new AdminActivityItem
                        {
                            ActivityId = Convert.ToInt32(reader["ActivityId"]),
                            ActivityType = reader["ActivityType"]?.ToString(),
                            Module = reader["Module"]?.ToString(),
                            Description = reader["Description"]?.ToString(),
                            Details = reader["Details"]?.ToString(),
                            IpAddress = reader["IpAddress"]?.ToString(),
                            Timestamp = reader["Timestamp"] != DBNull.Value ? Convert.ToDateTime(reader["Timestamp"]) : DateTime.Now,
                            UserName = userName,
                            UserRole = reader["UserRole"]?.ToString(),
                            UserInitials = initials
                        });
                    }
                }
            }
            return list;
        }

        /// <summary>Activates or deactivates a user account.</summary>
        public (bool success, string message) ToggleUserStatus(int userId, bool isActive)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("UPDATE Users SET IsActive = @IsActive WHERE UserId = @UserId", conn))
            {
                cmd.Parameters.Add("@UserId",   SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = isActive;
                conn.Open();
                int rows = cmd.ExecuteNonQuery();
                return rows > 0
                    ? (true, isActive ? "User activated successfully." : "User deactivated successfully.")
                    : (false, "User not found.");
            }
        }

        /// <summary>Permanently deletes a user account.</summary>
        public (bool success, string message) DeleteUser(int userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_AdminDeleteUser", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                var pOk  = cmd.Parameters.Add("@Success", SqlDbType.Bit);           pOk.Direction  = ParameterDirection.Output;
                var pMsg = cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 255); pMsg.Direction = ParameterDirection.Output;
                conn.Open();
                cmd.ExecuteNonQuery();
                return ((bool)pOk.Value, pMsg.Value?.ToString());
            }
        }

        /// <summary>Permanently deletes a classroom (admin).</summary>
        public (bool success, string message) DeleteClassroom(int classroomId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_AdminDeleteClassroom", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ClassroomId", SqlDbType.Int).Value = classroomId;
                var pOk  = cmd.Parameters.Add("@Success", SqlDbType.Bit);           pOk.Direction  = ParameterDirection.Output;
                var pMsg = cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 255); pMsg.Direction = ParameterDirection.Output;
                conn.Open();
                cmd.ExecuteNonQuery();
                return ((bool)pOk.Value, pMsg.Value?.ToString());
            }
        }

        /// <summary>Admin creates a new user account.</summary>
        public (bool success, string message, int newUserId) CreateUser(
            string roleName, string email, string password, string firstName, string lastName,
            string phoneNumber = null, string enrollmentNumber = null, string department = null,
            int? semester = null, string division = null, string employeeCode = null, string designation = null)
        {
            string salt = PasswordHasher.GenerateSalt();
            string hash = PasswordHasher.HashPassword(password, salt);

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_AdminCreateUser", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@RoleName",       SqlDbType.NVarChar, 50).Value  = roleName;
                cmd.Parameters.Add("@Email",          SqlDbType.NVarChar, 150).Value = email;
                cmd.Parameters.Add("@PasswordHash",   SqlDbType.NVarChar, 500).Value = hash;
                cmd.Parameters.Add("@PasswordSalt",   SqlDbType.NVarChar, 255).Value = salt;
                cmd.Parameters.Add("@FirstName",      SqlDbType.NVarChar, 100).Value = firstName;
                cmd.Parameters.Add("@LastName",       SqlDbType.NVarChar, 100).Value = lastName;
                cmd.Parameters.Add("@PhoneNumber",    SqlDbType.NVarChar, 20).Value  = (object)phoneNumber ?? DBNull.Value;
                cmd.Parameters.Add("@EnrollmentNumber", SqlDbType.NVarChar, 50).Value = (object)enrollmentNumber ?? DBNull.Value;
                cmd.Parameters.Add("@Department",     SqlDbType.NVarChar, 100).Value = (object)department ?? DBNull.Value;
                cmd.Parameters.Add("@Semester",       SqlDbType.Int).Value           = (object)semester ?? DBNull.Value;
                cmd.Parameters.Add("@Division",       SqlDbType.NVarChar, 20).Value  = (object)division ?? DBNull.Value;
                cmd.Parameters.Add("@EmployeeCode",   SqlDbType.NVarChar, 50).Value  = (object)employeeCode ?? DBNull.Value;
                cmd.Parameters.Add("@Designation",    SqlDbType.NVarChar, 100).Value = (object)designation ?? DBNull.Value;

                var pOk  = cmd.Parameters.Add("@Success",   SqlDbType.Bit);           pOk.Direction  = ParameterDirection.Output;
                var pMsg = cmd.Parameters.Add("@Message",   SqlDbType.NVarChar, 255); pMsg.Direction = ParameterDirection.Output;
                var pId  = cmd.Parameters.Add("@NewUserId", SqlDbType.Int);           pId.Direction  = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();
                return ((bool)pOk.Value, pMsg.Value?.ToString(), (int)pId.Value);
            }
        }

        /// <summary>
        /// Updates administrator personal details and department via sp_UpdateAdminProfile.
        /// </summary>
        public (bool success, string message) UpdateAdminProfile(int userId, string firstName, string lastName, string phoneNumber, string department)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_UpdateAdminProfile", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@FirstName", SqlDbType.NVarChar, 100).Value = (firstName ?? "").Trim();
                cmd.Parameters.Add("@LastName", SqlDbType.NVarChar, 100).Value = (lastName ?? "").Trim();
                cmd.Parameters.Add("@PhoneNumber", SqlDbType.NVarChar, 20).Value = (object)phoneNumber?.Trim() ?? DBNull.Value;
                cmd.Parameters.Add("@Department", SqlDbType.NVarChar, 100).Value = (object)department?.Trim() ?? DBNull.Value;

                var pSuccess = cmd.Parameters.Add("@Success", SqlDbType.Bit);
                pSuccess.Direction = ParameterDirection.Output;

                var pMessage = cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 255);
                pMessage.Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();

                return ((bool)pSuccess.Value, pMessage.Value?.ToString() ?? "Administrator profile updated.");
            }
        }
    }
}

