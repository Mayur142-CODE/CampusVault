using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using CampusVault.Models.Student;

namespace CampusVault.Services
{
    public class NotificationRepository
    {
        private readonly string _connectionString;

        public NotificationRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["CampusVaultConnection"]?.ConnectionString;
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("Connection string 'CampusVaultConnection' was not found in Web.config.");
            }
        }

        public List<StudentNotificationItem> GetNotifications(int userId, int top = 50)
        {
            var list = new List<StudentNotificationItem>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_GetUserNotifications", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@Top", SqlDbType.Int).Value = top;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new StudentNotificationItem
                        {
                            Id = reader["NotificationId"].ToString(),
                            Category = reader["Category"].ToString(),
                            Title = reader["Title"].ToString(),
                            Message = reader["Message"].ToString(),
                            BadgeText = reader["BadgeText"] != DBNull.Value ? reader["BadgeText"].ToString() : null,
                            BadgeType = reader["BadgeType"] != DBNull.Value ? reader["BadgeType"].ToString() : "info",
                            ContextInfo = reader["ContextInfo"] != DBNull.Value ? reader["ContextInfo"].ToString() : null,
                            ActionUrl = reader["ActionUrl"] != DBNull.Value ? reader["ActionUrl"].ToString() : null,
                            ActionText = reader["ActionText"] != DBNull.Value ? reader["ActionText"].ToString() : null,
                            IsUnread = !(bool)reader["IsRead"],
                            Timestamp = (DateTime)reader["CreatedAt"]
                        });
                    }
                }
            }
            return list;
        }

        public bool MarkAsRead(int notificationId, int userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_MarkNotificationAsRead", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@NotificationId", SqlDbType.Int).Value = notificationId;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool MarkAllAsRead(int userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_MarkAllNotificationsAsRead", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public void CreateNotification(int userId, string category, string title, string message,
            string badgeText = null, string badgeType = "info", string contextInfo = null,
            string actionUrl = null, string actionText = null)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_CreateNotification", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                cmd.Parameters.Add("@Category", SqlDbType.NVarChar, 50).Value = category;
                cmd.Parameters.Add("@Title", SqlDbType.NVarChar, 255).Value = title;
                cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 1000).Value = message;
                cmd.Parameters.Add("@BadgeText", SqlDbType.NVarChar, 50).Value = (object)badgeText ?? DBNull.Value;
                cmd.Parameters.Add("@BadgeType", SqlDbType.NVarChar, 50).Value = badgeType ?? "info";
                cmd.Parameters.Add("@ContextInfo", SqlDbType.NVarChar, 255).Value = (object)contextInfo ?? DBNull.Value;
                cmd.Parameters.Add("@ActionUrl", SqlDbType.NVarChar, 500).Value = (object)actionUrl ?? DBNull.Value;
                cmd.Parameters.Add("@ActionText", SqlDbType.NVarChar, 100).Value = (object)actionText ?? DBNull.Value;

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void NotifyClassroomStudents(int classroomId, string category, string title, string message,
            string badgeText = null, string badgeType = "info", string contextInfo = null,
            string actionUrl = null, string actionText = null)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                var studentUserIds = new List<int>();
                using (var cmd = new SqlCommand("SELECT StudentUserId FROM ClassroomEnrollments WHERE ClassroomId = @ClassroomId", conn))
                {
                    cmd.Parameters.Add("@ClassroomId", SqlDbType.Int).Value = classroomId;
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            studentUserIds.Add((int)r["StudentUserId"]);
                        }
                    }
                }

                foreach (var sId in studentUserIds)
                {
                    using (var cmd = new SqlCommand("sp_CreateNotification", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = sId;
                        cmd.Parameters.Add("@Category", SqlDbType.NVarChar, 50).Value = category;
                        cmd.Parameters.Add("@Title", SqlDbType.NVarChar, 255).Value = title;
                        cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 1000).Value = message;
                        cmd.Parameters.Add("@BadgeText", SqlDbType.NVarChar, 50).Value = (object)badgeText ?? DBNull.Value;
                        cmd.Parameters.Add("@BadgeType", SqlDbType.NVarChar, 50).Value = badgeType ?? "info";
                        cmd.Parameters.Add("@ContextInfo", SqlDbType.NVarChar, 255).Value = (object)contextInfo ?? DBNull.Value;
                        cmd.Parameters.Add("@ActionUrl", SqlDbType.NVarChar, 500).Value = (object)actionUrl ?? DBNull.Value;
                        cmd.Parameters.Add("@ActionText", SqlDbType.NVarChar, 100).Value = (object)actionText ?? DBNull.Value;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
