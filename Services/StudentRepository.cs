using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using CampusVault.Models.Student;

namespace CampusVault.Services
{
    public class StudentRepository
    {
        private readonly string _connectionString;

        public StudentRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["CampusVaultConnection"]?.ConnectionString;
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("Connection string 'CampusVaultConnection' was not found in Web.config.");
            }
        }

        public StudentDashboardViewModel GetStudentDashboardData(int studentUserId)
        {
            var vm = new StudentDashboardViewModel();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_GetStudentDashboardData", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@StudentUserId", SqlDbType.Int).Value = studentUserId;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    // 1. Scalar Stats & Student Info
                    if (reader.Read())
                    {
                        vm.UserId = (int)reader["UserId"];
                        vm.FirstName = reader["FirstName"].ToString();
                        vm.LastName = reader["LastName"].ToString();
                        vm.FullName = reader["FullName"].ToString();
                        vm.Email = reader["Email"].ToString();
                        vm.EnrollmentNumber = reader["EnrollmentNumber"] != DBNull.Value ? reader["EnrollmentNumber"].ToString() : null;
                        vm.Department = reader["Department"] != DBNull.Value ? reader["Department"].ToString() : null;
                        vm.Semester = reader["Semester"] != DBNull.Value ? Convert.ToInt32(reader["Semester"]) : 0;
                        vm.Division = reader["Division"] != DBNull.Value ? reader["Division"].ToString() : null;

                        vm.EnrolledClassroomsCount = Convert.ToInt32(reader["EnrolledClassrooms"]);
                        vm.PendingTasksCount = Convert.ToInt32(reader["PendingTasks"]);
                        vm.UnderReviewCount = Convert.ToInt32(reader["UnderReview"]);
                        vm.ApprovedDocsCount = Convert.ToInt32(reader["ApprovedDocs"]);
                    }

                    // 2. Due Soon Tasks
                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            vm.DueSoonTasks.Add(new StudentDueSoonTaskItem
                            {
                                TaskId = (int)reader["TaskId"],
                                ClassroomId = (int)reader["ClassroomId"],
                                Title = reader["Title"].ToString(),
                                TaskType = reader["TaskType"].ToString(),
                                MaxScore = Convert.ToInt32(reader["MaxScore"]),
                                DueDate = (DateTime)reader["DueDate"],
                                CourseCode = reader["CourseCode"].ToString(),
                                CourseName = reader["CourseName"].ToString(),
                                FacultyName = reader["FacultyName"] != DBNull.Value ? reader["FacultyName"].ToString() : null,
                                SubmissionStatus = reader["SubmissionStatus"] != DBNull.Value ? reader["SubmissionStatus"].ToString() : null,
                                SubmittedAt = reader["SubmittedAt"] != DBNull.Value ? (DateTime?)reader["SubmittedAt"] : null,
                                Score = reader["Score"] != DBNull.Value ? (int?)Convert.ToInt32(reader["Score"]) : null
                            });
                        }
                    }

                    // 3. Enrolled Classrooms
                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            vm.EnrolledClassrooms.Add(new StudentClassroomSummaryItem
                            {
                                ClassroomId = (int)reader["ClassroomId"],
                                CourseCode = reader["CourseCode"].ToString(),
                                CourseName = reader["CourseName"].ToString(),
                                Department = reader["Department"].ToString(),
                                Semester = (int)reader["Semester"],
                                AcademicYear = reader["AcademicYear"].ToString(),
                                JoinCode = reader["JoinCode"].ToString(),
                                ThemeGradient = reader["ThemeGradient"] != DBNull.Value ? reader["ThemeGradient"].ToString() : null,
                                FacultyName = reader["FacultyName"] != DBNull.Value ? reader["FacultyName"].ToString() : null,
                                FacultyDesignation = reader["FacultyDesignation"] != DBNull.Value ? reader["FacultyDesignation"].ToString() : null,
                                ActiveTaskCount = Convert.ToInt32(reader["ActiveTaskCount"]),
                                StudentCount = Convert.ToInt32(reader["StudentCount"])
                            });
                        }
                    }
                }
            }
            return vm;
        }

        public List<StudentTaskItem> GetStudentTasks(int studentUserId)
        {
            var list = new List<StudentTaskItem>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_GetStudentTasks", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@StudentUserId", SqlDbType.Int).Value = studentUserId;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new StudentTaskItem
                        {
                            TaskId = (int)reader["TaskId"],
                            ClassroomId = (int)reader["ClassroomId"],
                            Title = reader["Title"].ToString(),
                            Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : "",
                            TaskType = reader["TaskType"].ToString(),
                            MaxScore = Convert.ToInt32(reader["MaxScore"]),
                            DueDate = (DateTime)reader["DueDate"],
                            TaskStatus = reader["TaskStatus"].ToString(),
                            CourseCode = reader["CourseCode"].ToString(),
                            CourseName = reader["CourseName"].ToString(),
                            ThemeGradient = reader["ThemeGradient"] != DBNull.Value ? reader["ThemeGradient"].ToString() : null,
                            FacultyName = reader["FacultyName"] != DBNull.Value ? reader["FacultyName"].ToString() : null,
                            SubmissionStatus = reader["SubmissionStatus"].ToString(),
                            SubmissionId = reader["SubmissionId"] != DBNull.Value ? (int?)reader["SubmissionId"] : null,
                            FileName = reader["FileName"] != DBNull.Value ? reader["FileName"].ToString() : null,
                            FileSize = reader["FileSize"] != DBNull.Value ? reader["FileSize"].ToString() : null,
                            SubmittedAt = reader["SubmittedAt"] != DBNull.Value ? (DateTime?)reader["SubmittedAt"] : null,
                            Score = reader["Score"] != DBNull.Value ? (int?)Convert.ToInt32(reader["Score"]) : null,
                            Feedback = reader["Feedback"] != DBNull.Value ? reader["Feedback"].ToString() : null
                        });
                    }
                }
            }
            return list;
        }

        public List<StudentDocumentItem> GetStudentDocuments(int studentUserId)
        {
            var list = new List<StudentDocumentItem>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_GetStudentDocuments", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@StudentUserId", SqlDbType.Int).Value = studentUserId;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new StudentDocumentItem
                        {
                            SubmissionId = (int)reader["SubmissionId"],
                            TaskId = (int)reader["TaskId"],
                            TaskTitle = reader["TaskTitle"].ToString(),
                            CourseCode = reader["CourseCode"].ToString(),
                            CourseName = reader["CourseName"].ToString(),
                            FacultyName = reader["FacultyName"] != DBNull.Value ? reader["FacultyName"].ToString() : null,
                            FileName     = reader["FileName"]   != DBNull.Value ? reader["FileName"].ToString()   : null,
                            FileSize     = reader["FileSize"]   != DBNull.Value ? reader["FileSize"].ToString()   : null,
                            FilePath     = reader["FilePath"]   != DBNull.Value ? reader["FilePath"].ToString()   : null,
                            Status       = reader["Status"].ToString(),
                            Score = reader["Score"] != DBNull.Value ? (int?)Convert.ToInt32(reader["Score"]) : null,
                            MaxScore = Convert.ToInt32(reader["MaxScore"]),
                            Feedback = reader["Feedback"] != DBNull.Value ? reader["Feedback"].ToString() : null,
                            SubmittedAt = (DateTime)reader["SubmittedAt"],
                            ReviewedAt = reader["ReviewedAt"] != DBNull.Value ? (DateTime?)reader["ReviewedAt"] : null
                        });
                    }
                }
            }
            return list;
        }

        public (bool success, string message) SubmitTaskDocument(int taskId, int studentUserId, string fileName, string fileSize, string filePath)
        {
            return SubmitTaskDocument(taskId, studentUserId, fileName, fileSize, filePath, null, null, null);
        }

        public (bool success, string message) SubmitTaskDocument(int taskId, int studentUserId, string fileName, string fileSize, string filePath, string cloudUrl, string cloudPublicId, string fileHash = null)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_SubmitTaskDocument", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@TaskId", SqlDbType.Int).Value = taskId;
                cmd.Parameters.Add("@StudentUserId", SqlDbType.Int).Value = studentUserId;
                cmd.Parameters.Add("@FileName", SqlDbType.NVarChar, 255).Value = fileName;
                cmd.Parameters.Add("@FileSize", SqlDbType.NVarChar, 50).Value = fileSize;
                cmd.Parameters.Add("@FilePath", SqlDbType.NVarChar, 1000).Value = !string.IsNullOrEmpty(cloudUrl) ? cloudUrl : filePath;
                cmd.Parameters.Add("@CloudPublicId", SqlDbType.NVarChar, 500).Value = (object)cloudPublicId ?? DBNull.Value;
                cmd.Parameters.Add("@FileHash", SqlDbType.NVarChar, 256).Value = (object)fileHash ?? DBNull.Value;

                var pSuccess = cmd.Parameters.Add("@Success", SqlDbType.Bit);
                pSuccess.Direction = ParameterDirection.Output;

                var pMessage = cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 255);
                pMessage.Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();

                return ((bool)pSuccess.Value, pMessage.Value?.ToString());
            }
        }
    }
}
