using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using CampusVault.Models.Academic;

namespace CampusVault.Services
{
    public class ClassroomRepository
    {
        private readonly string _connectionString;

        public ClassroomRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["CampusVaultConnection"]?.ConnectionString;
            if (string.IsNullOrEmpty(_connectionString))
                throw new InvalidOperationException("Connection string 'CampusVaultConnection' was not found in Web.config.");
        }

        public List<Classroom> GetStudentClassrooms(int studentUserId)
        {
            var list = new List<Classroom>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_GetStudentClassrooms", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@StudentUserId", SqlDbType.Int).Value = studentUserId;
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Classroom
                        {
                            ClassroomId       = (int)reader["ClassroomId"],
                            CourseCode        = reader["CourseCode"].ToString(),
                            CourseName        = reader["CourseName"].ToString(),
                            Department        = reader["Department"].ToString(),
                            Semester          = (int)reader["Semester"],
                            AcademicYear      = reader["AcademicYear"].ToString(),
                            JoinCode          = reader["JoinCode"].ToString(),
                            ThemeGradient     = reader["ThemeGradient"].ToString(),
                            FacultyName       = reader["FacultyName"].ToString(),
                            FacultyDesignation = reader["FacultyDesignation"] != DBNull.Value ? reader["FacultyDesignation"].ToString() : null,
                            EnrolledAt        = (DateTime)reader["EnrolledAt"],
                            EnrollmentStatus  = reader["EnrollmentStatus"].ToString(),
                            ActiveTaskCount   = reader["ActiveTaskCount"] != DBNull.Value ? Convert.ToInt32(reader["ActiveTaskCount"]) : 0
                        });
                    }
                }
            }
            return list;
        }

        public (bool success, string message, int classroomId) JoinClassroom(int studentUserId, string joinCode)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_JoinClassroom", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@StudentUserId", SqlDbType.Int).Value      = studentUserId;
                cmd.Parameters.Add("@JoinCode",      SqlDbType.NVarChar, 20).Value = joinCode.Trim();
                var pSuccess     = cmd.Parameters.Add("@Success",     SqlDbType.Bit);           pSuccess.Direction     = ParameterDirection.Output;
                var pMessage     = cmd.Parameters.Add("@Message",     SqlDbType.NVarChar, 255); pMessage.Direction     = ParameterDirection.Output;
                var pClassroomId = cmd.Parameters.Add("@ClassroomId", SqlDbType.Int);           pClassroomId.Direction = ParameterDirection.Output;
                conn.Open();
                cmd.ExecuteNonQuery();
                return (
                    (bool)pSuccess.Value,
                    pMessage.Value?.ToString(),
                    pClassroomId.Value != DBNull.Value ? (int)pClassroomId.Value : 0
                );
            }
        }

        /// <summary>Leave (unenroll from) a classroom.</summary>
        public (bool success, string message) LeaveClassroom(int studentUserId, int classroomId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_LeaveClassroom", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@StudentUserId", SqlDbType.Int).Value = studentUserId;
                cmd.Parameters.Add("@ClassroomId",   SqlDbType.Int).Value = classroomId;
                var pOk  = cmd.Parameters.Add("@Success", SqlDbType.Bit);           pOk.Direction  = ParameterDirection.Output;
                var pMsg = cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 255); pMsg.Direction = ParameterDirection.Output;
                conn.Open();
                cmd.ExecuteNonQuery();
                return ((bool)pOk.Value, pMsg.Value?.ToString());
            }
        }

        public ClassroomDetailsViewModel GetClassroomDetails(int classroomId, int studentUserId)
        {
            var vm = new ClassroomDetailsViewModel();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_GetClassroomDetails", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ClassroomId",   SqlDbType.Int).Value = classroomId;
                cmd.Parameters.Add("@StudentUserId", SqlDbType.Int).Value = studentUserId;
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    // 1. Classroom info
                    if (reader.Read())
                    {
                        vm.Classroom = new Classroom
                        {
                            ClassroomId       = (int)reader["ClassroomId"],
                            CourseCode        = reader["CourseCode"].ToString(),
                            CourseName        = reader["CourseName"].ToString(),
                            Department        = reader["Department"].ToString(),
                            Semester          = (int)reader["Semester"],
                            AcademicYear      = reader["AcademicYear"].ToString(),
                            JoinCode          = reader["JoinCode"].ToString(),
                            ThemeGradient     = reader["ThemeGradient"].ToString(),
                            FacultyName       = reader["FacultyName"].ToString(),
                            FacultyEmail      = reader["FacultyEmail"].ToString(),
                            FacultyDesignation = reader["FacultyDesignation"] != DBNull.Value ? reader["FacultyDesignation"].ToString() : null,
                            FacultyOffice     = reader["FacultyOffice"] != DBNull.Value ? reader["FacultyOffice"].ToString() : null,
                            StudentCount      = reader["StudentCount"] != DBNull.Value ? Convert.ToInt32(reader["StudentCount"]) : 0,
                            MaterialCount     = reader["MaterialCount"] != DBNull.Value ? Convert.ToInt32(reader["MaterialCount"]) : 0,
                            AnnouncementCount = reader["AnnouncementCount"] != DBNull.Value ? Convert.ToInt32(reader["AnnouncementCount"]) : 0
                        };
                    }

                    // 2. Tasks list
                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            vm.Tasks.Add(new ClassroomTask
                            {
                                TaskId          = (int)reader["TaskId"],
                                ClassroomId     = (int)reader["ClassroomId"],
                                Title           = reader["Title"].ToString(),
                                Description     = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : null,
                                TaskType        = reader["TaskType"].ToString(),
                                MaxScore        = Convert.ToInt32(reader["MaxScore"]),
                                DueDate         = (DateTime)reader["DueDate"],
                                Status          = reader["Status"].ToString(),
                                SubmissionStatus = reader["SubmissionStatus"] != DBNull.Value ? reader["SubmissionStatus"].ToString() : null,
                                SubmissionId    = reader["SubmissionId"] != DBNull.Value ? (int?)reader["SubmissionId"] : null,
                                SubmittedAt     = reader["SubmittedAt"] != DBNull.Value ? (DateTime?)reader["SubmittedAt"] : null,
                                Score           = reader["Score"] != DBNull.Value ? (int?)reader["Score"] : null
                            });
                        }
                    }

                    // 3. Announcements
                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            vm.Announcements.Add(new ClassroomAnnouncementItem
                            {
                                AnnouncementId = (int)reader["AnnouncementId"],
                                Title          = reader["Title"].ToString(),
                                Message        = reader["Message"].ToString(),
                                Priority       = reader["Priority"].ToString(),
                                IsPinned       = (bool)reader["IsPinned"],
                                PublishedAt    = Convert.ToDateTime(reader["PublishedAt"]),
                                FacultyName    = reader["FacultyName"].ToString()
                            });
                        }
                    }

                    // 4. Materials
                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            vm.Materials.Add(new ClassroomMaterialItem
                            {
                                MaterialId  = (int)reader["MaterialId"],
                                Title       = reader["Title"].ToString(),
                                Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : null,
                                FileName    = reader["FileName"] != DBNull.Value ? reader["FileName"].ToString() : null,
                                FileSize    = reader["FileSize"] != DBNull.Value ? reader["FileSize"].ToString() : null,
                                FileType    = reader["FileType"] != DBNull.Value ? reader["FileType"].ToString() : "File",
                                Category    = reader["Category"] != DBNull.Value ? reader["Category"].ToString() : "Other",
                                FileUrl     = reader["FileUrl"] != DBNull.Value ? reader["FileUrl"].ToString() : null,
                                ExternalUrl = reader["ExternalUrl"] != DBNull.Value ? reader["ExternalUrl"].ToString() : null,
                                UploadedAt  = Convert.ToDateTime(reader["UploadedAt"]),
                                FacultyName = reader["FacultyName"] != DBNull.Value ? reader["FacultyName"].ToString() : null
                            });
                        }
                    }
                }
            }
            return vm;
        }

        public ClassroomTask GetTaskDetails(int taskId, int studentUserId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_GetTaskDetails", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@TaskId",        SqlDbType.Int).Value = taskId;
                cmd.Parameters.Add("@StudentUserId", SqlDbType.Int).Value = studentUserId;
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new ClassroomTask
                        {
                            TaskId      = (int)reader["TaskId"],
                            ClassroomId = (int)reader["ClassroomId"],
                            Title       = reader["Title"].ToString(),
                            Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : null,
                            TaskType    = reader["TaskType"].ToString(),
                            MaxScore    = Convert.ToInt32(reader["MaxScore"]),
                            DueDate     = (DateTime)reader["DueDate"],
                            Status      = reader["TaskStatus"].ToString(),
                            CourseCode  = reader["CourseCode"].ToString(),
                            CourseName  = reader["CourseName"].ToString(),
                            FacultyName = reader["FacultyName"].ToString(),
                            FacultyEmail = reader["FacultyEmail"].ToString()
                        };
                    }
                }
            }
            return null;
        }
    }
}
