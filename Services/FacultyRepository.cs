using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using CampusVault.Models.Faculty;

namespace CampusVault.Services
{
    public class FacultyRepository
    {
        private readonly string _connectionString;

        public FacultyRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["CampusVaultConnection"]?.ConnectionString;
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("Connection string 'CampusVaultConnection' was not found in Web.config.");
            }
        }

        public FacultyDashboardViewModel GetDashboardData(int facultyUserId)
        {
            var vm = new FacultyDashboardViewModel();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_GetFacultyDashboardData", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@FacultyUserId", SqlDbType.Int).Value = facultyUserId;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    // 1. Stats
                    if (reader.Read())
                    {
                        vm.ActiveClassroomCount = Convert.ToInt32(reader["ActiveClassroomCount"]);
                        vm.TotalStudentCount = Convert.ToInt32(reader["TotalStudentCount"]);
                        vm.ActiveTaskCount = Convert.ToInt32(reader["ActiveTaskCount"]);
                        vm.PendingReviewCount = Convert.ToInt32(reader["PendingReviewCount"]);
                    }

                    // 2. Classrooms
                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            vm.Classrooms.Add(new FacultyClassroomItem
                            {
                                ClassroomId = (int)reader["ClassroomId"],
                                CourseCode = reader["CourseCode"].ToString(),
                                CourseName = reader["CourseName"].ToString(),
                                Department = reader["Department"].ToString(),
                                Semester = (int)reader["Semester"],
                                JoinCode = reader["JoinCode"].ToString(),
                                ThemeGradient = reader["ThemeGradient"].ToString(),
                                StudentCount = Convert.ToInt32(reader["StudentCount"]),
                                TaskCount = Convert.ToInt32(reader["TaskCount"]),
                                PendingReviewCount = Convert.ToInt32(reader["PendingReviews"])
                            });
                        }
                    }

                    // 3. Recent Tasks
                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            vm.RecentTasks.Add(new FacultyTaskItem
                            {
                                TaskId = (int)reader["TaskId"],
                                ClassroomId = (int)reader["ClassroomId"],
                                CourseCode = reader["CourseCode"].ToString(),
                                CourseName = reader["CourseName"].ToString(),
                                Title = reader["Title"].ToString(),
                                TaskType = reader["TaskType"].ToString(),
                                MaxScore = Convert.ToInt32(reader["MaxScore"]),
                                DueDate = (DateTime)reader["DueDate"],
                                Status = reader["Status"].ToString(),
                                SubmissionCount = Convert.ToInt32(reader["SubmissionCount"])
                            });
                        }
                    }

                    // 4. Pending Submissions
                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            vm.PendingSubmissions.Add(new FacultySubmissionItem
                            {
                                SubmissionId = (int)reader["SubmissionId"],
                                TaskId = (int)reader["TaskId"],
                                TaskTitle = reader["TaskTitle"].ToString(),
                                CourseCode = reader["CourseCode"].ToString(),
                                CourseName = reader["CourseName"].ToString(),
                                StudentName = reader["StudentName"].ToString(),
                                FileName = reader["FileName"].ToString(),
                                FileSize = reader["FileSize"].ToString(),
                                SubmittedAt = (DateTime)reader["SubmittedAt"],
                                Status = reader["Status"].ToString()
                            });
                        }
                    }

                    // 5. Submission overview counts (optional result set)
                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            vm.TotalSubmissions  = reader["TotalSubmissions"]  != DBNull.Value ? Convert.ToInt32(reader["TotalSubmissions"])  : 0;
                            vm.SubmittedCount    = reader["SubmittedCount"]    != DBNull.Value ? Convert.ToInt32(reader["SubmittedCount"])    : 0;
                            vm.UnderReviewCount  = reader["UnderReviewCount"]  != DBNull.Value ? Convert.ToInt32(reader["UnderReviewCount"])  : 0;
                            vm.RevisionCount     = reader["RevisionCount"]     != DBNull.Value ? Convert.ToInt32(reader["RevisionCount"])     : 0;
                            vm.ApprovedCount     = reader["ApprovedCount"]     != DBNull.Value ? Convert.ToInt32(reader["ApprovedCount"])     : 0;
                        }
                    }

                    // Fallback: derive totals from pending submissions if SP doesn't return result set 5
                    if (vm.TotalSubmissions == 0 && vm.PendingSubmissions.Count > 0)
                    {
                        vm.TotalSubmissions = vm.PendingSubmissions.Count;
                        vm.SubmittedCount   = vm.PendingSubmissions.Count(s =>
                            s.Status.Equals("Submitted", StringComparison.OrdinalIgnoreCase) ||
                            s.Status.Equals("PendingReview", StringComparison.OrdinalIgnoreCase));
                        vm.UnderReviewCount = vm.SubmittedCount;
                        vm.ApprovedCount    = vm.PendingSubmissions.Count(s =>
                            s.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                            s.Status.Equals("Reviewed", StringComparison.OrdinalIgnoreCase));
                        vm.RevisionCount    = vm.PendingSubmissions.Count(s =>
                            s.Status.IndexOf("Revision", StringComparison.OrdinalIgnoreCase) >= 0);
                    }
                }
            }
            return vm;
        }

        /// <summary>Returns single submission detail for the review page.</summary>
        public CampusVault.Models.Faculty.ReviewSubmissionViewModel GetSubmissionById(int submissionId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                SELECT
                    s.SubmissionId, s.TaskId, s.StudentUserId,
                    t.Title        AS TaskTitle,
                    t.MaxScore,    t.DueDate,
                    c.CourseCode,  c.CourseName,
                    (u.FirstName + ' ' + u.LastName)  AS StudentName,
                    (f.FirstName + ' ' + f.LastName)  AS FacultyName,
                    ISNULL(sp.EnrollmentNumber, sp.RollNumber) AS RollNo,
                    s.FileName,    s.FileSize,
                    ISNULL(s.CloudUrl, s.FilePath) AS CloudUrl,
                    s.Status,      s.Score,
                    s.Feedback,    s.SubmittedAt
                FROM TaskSubmissions s
                JOIN Tasks      t  ON s.TaskId        = t.TaskId
                JOIN Classrooms c  ON t.ClassroomId   = c.ClassroomId
                JOIN Users      u  ON s.StudentUserId = u.UserId
                JOIN Users      f  ON c.FacultyUserId = f.UserId
                LEFT JOIN StudentProfiles sp ON u.UserId = sp.UserId
                WHERE s.SubmissionId = @SubmissionId", conn))
            {
                cmd.Parameters.Add("@SubmissionId", SqlDbType.Int).Value = submissionId;
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        return new CampusVault.Models.Faculty.ReviewSubmissionViewModel
                        {
                            SubmissionId  = (int)r["SubmissionId"],
                            TaskId        = (int)r["TaskId"],
                            StudentUserId = (int)r["StudentUserId"],
                            TaskTitle    = r["TaskTitle"].ToString(),
                            MaxScore     = Convert.ToInt32(r["MaxScore"]),
                            DueDate      = Convert.ToDateTime(r["DueDate"]),
                            CourseCode   = r["CourseCode"].ToString(),
                            CourseName   = r["CourseName"].ToString(),
                            StudentName  = r["StudentName"].ToString(),
                            FacultyName  = r["FacultyName"].ToString(),
                            RollNo       = r["RollNo"] != DBNull.Value ? r["RollNo"].ToString() : null,
                            FileName     = r["FileName"].ToString(),
                            FileSize     = r["FileSize"] != DBNull.Value ? r["FileSize"].ToString() : null,
                            CloudUrl     = r["CloudUrl"] != DBNull.Value ? r["CloudUrl"].ToString() : null,
                            Status       = r["Status"].ToString(),
                            Score        = r["Score"] != DBNull.Value ? (int?)Convert.ToInt32(r["Score"]) : null,
                            Feedback     = r["Feedback"] != DBNull.Value ? r["Feedback"].ToString() : null,
                            SubmittedAt  = Convert.ToDateTime(r["SubmittedAt"])
                        };
                    }
                }
            }
            return null;
        }

        /// <summary>Approve or request revision on a submission.</summary>
        public (bool success, string message) ReviewSubmission(int submissionId, string action, int? score, string feedback, int reviewedBy)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_ReviewSubmission", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@SubmissionId", SqlDbType.Int).Value           = submissionId;
                cmd.Parameters.Add("@Action",       SqlDbType.NVarChar, 20).Value  = action;
                cmd.Parameters.Add("@Score",        SqlDbType.Int).Value           = (object)score ?? DBNull.Value;
                cmd.Parameters.Add("@Feedback",     SqlDbType.NVarChar, -1).Value  = (object)feedback ?? DBNull.Value;
                cmd.Parameters.Add("@ReviewedBy",   SqlDbType.Int).Value           = reviewedBy;
                var pSuccess = cmd.Parameters.Add("@Success", SqlDbType.Bit);
                pSuccess.Direction = ParameterDirection.Output;
                var pMessage = cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 255);
                pMessage.Direction = ParameterDirection.Output;
                conn.Open();
                cmd.ExecuteNonQuery();
                return ((bool)pSuccess.Value, pMessage.Value?.ToString());
            }
        }

        /// <summary>Returns ALL submissions for this faculty's classrooms (all statuses).</summary>
        public List<CampusVault.Models.Faculty.FacultySubmissionItem> GetAllSubmissions(int facultyUserId)
        {
            var list = new List<CampusVault.Models.Faculty.FacultySubmissionItem>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                SELECT
                    s.SubmissionId,
                    s.TaskId,
                    t.Title        AS TaskTitle,
                    c.CourseCode,
                    c.CourseName,
                    (u.FirstName + ' ' + u.LastName) AS StudentName,
                    ISNULL(sp.EnrollmentNumber, sp.RollNumber) AS RollNo,
                    s.FileName,
                    s.FileSize,
                    s.Status,
                    s.Feedback,
                    s.SubmittedAt,
                    s.CloudUrl,
                    s.FileHash
                FROM TaskSubmissions s
                JOIN Tasks t         ON s.TaskId        = t.TaskId
                JOIN Classrooms c    ON t.ClassroomId   = c.ClassroomId
                JOIN Users u         ON s.StudentUserId = u.UserId
                LEFT JOIN StudentProfiles sp ON u.UserId = sp.UserId
                WHERE c.FacultyUserId = @FacultyUserId
                ORDER BY s.SubmittedAt DESC", conn))
            {
                cmd.Parameters.Add("@FacultyUserId", SqlDbType.Int).Value = facultyUserId;
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new CampusVault.Models.Faculty.FacultySubmissionItem
                        {
                            SubmissionId = (int)r["SubmissionId"],
                            TaskId       = (int)r["TaskId"],
                            TaskTitle    = r["TaskTitle"].ToString(),
                            CourseCode   = r["CourseCode"].ToString(),
                            CourseName   = r["CourseName"].ToString(),
                            StudentName  = r["StudentName"].ToString(),
                            RollNo       = r["RollNo"] != DBNull.Value ? r["RollNo"].ToString() : null,
                            FileName     = r["FileName"] != DBNull.Value ? r["FileName"].ToString() : null,
                            FileSize     = r["FileSize"] != DBNull.Value ? r["FileSize"].ToString() : null,
                            Status       = r["Status"] != DBNull.Value ? r["Status"].ToString() : "Pending",
                            Feedback     = r["Feedback"] != DBNull.Value ? r["Feedback"].ToString() : null,
                            SubmittedAt  = r["SubmittedAt"] != DBNull.Value ? Convert.ToDateTime(r["SubmittedAt"]) : DateTime.Now,
                            CloudUrl     = r["CloudUrl"] != DBNull.Value ? r["CloudUrl"].ToString() : null,
                            FileHash     = r["FileHash"] != DBNull.Value ? r["FileHash"].ToString() : null
                        });
                    }
                }
            }
            return list;
        }

        public List<FacultyClassroomItem> GetFacultyClassrooms(int facultyUserId)
        {
            var list = new List<FacultyClassroomItem>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_GetFacultyClassrooms", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@FacultyUserId", SqlDbType.Int).Value = facultyUserId;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new FacultyClassroomItem
                        {
                            ClassroomId = (int)reader["ClassroomId"],
                            CourseCode = reader["CourseCode"].ToString(),
                            CourseName = reader["CourseName"].ToString(),
                            Department = reader["Department"].ToString(),
                            Semester = (int)reader["Semester"],
                            AcademicYear = reader["AcademicYear"].ToString(),
                            JoinCode = reader["JoinCode"].ToString(),
                            ThemeGradient = reader["ThemeGradient"].ToString(),
                            StudentCount = Convert.ToInt32(reader["StudentCount"]),
                            TaskCount = Convert.ToInt32(reader["TaskCount"]),
                            PendingReviewCount = Convert.ToInt32(reader["PendingReviewCount"]),
                            CreatedAt = (DateTime)reader["CreatedAt"]
                        });
                    }
                }
            }
            return list;
        }

        public (bool success, string message, int newClassroomId) CreateClassroom(int facultyUserId, CreateClassroomModel model)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_CreateClassroom", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@FacultyUserId", SqlDbType.Int).Value = facultyUserId;
                cmd.Parameters.Add("@CourseCode", SqlDbType.NVarChar, 20).Value = model.CourseCode ?? "";
                cmd.Parameters.Add("@CourseName", SqlDbType.NVarChar, 150).Value = model.CourseName ?? "";
                cmd.Parameters.Add("@Department", SqlDbType.NVarChar, 100).Value = model.Department ?? "Computer Engineering";
                cmd.Parameters.Add("@Semester", SqlDbType.Int).Value = model.Semester > 0 ? model.Semester : 5;
                cmd.Parameters.Add("@AcademicYear", SqlDbType.NVarChar, 20).Value = !string.IsNullOrEmpty(model.AcademicYear) ? model.AcademicYear : "2026-2027";
                cmd.Parameters.Add("@JoinCode", SqlDbType.NVarChar, 20).Value = model.JoinCode ?? "";
                cmd.Parameters.Add("@ThemeGradient", SqlDbType.NVarChar, 100).Value = model.ThemeGradient ?? "linear-gradient(135deg, #1e3a8a 0%, #3b82f6 100%)";

                var pSuccess = cmd.Parameters.Add("@Success", SqlDbType.Bit);
                pSuccess.Direction = ParameterDirection.Output;

                var pMessage = cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 255);
                pMessage.Direction = ParameterDirection.Output;

                var pNewClassroomId = cmd.Parameters.Add("@NewClassroomId", SqlDbType.Int);
                pNewClassroomId.Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();

                return (
                    (bool)pSuccess.Value,
                    pMessage.Value?.ToString(),
                    pNewClassroomId.Value != DBNull.Value ? (int)pNewClassroomId.Value : 0
                );
            }
        }

        public List<FacultyTaskItem> GetFacultyTasks(int facultyUserId)
        {
            var list = new List<FacultyTaskItem>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_GetFacultyTasks", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@FacultyUserId", SqlDbType.Int).Value = facultyUserId;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new FacultyTaskItem
                        {
                            TaskId = (int)reader["TaskId"],
                            ClassroomId = (int)reader["ClassroomId"],
                            CourseCode = reader["CourseCode"].ToString(),
                            CourseName = reader["CourseName"].ToString(),
                            Title = reader["Title"].ToString(),
                            Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : null,
                            TaskType = reader["TaskType"].ToString(),
                            MaxScore = Convert.ToInt32(reader["MaxScore"]),
                            DueDate = (DateTime)reader["DueDate"],
                            Status = reader["Status"].ToString(),
                            SubmissionCount = Convert.ToInt32(reader["SubmissionCount"]),
                            PendingReviewCount = Convert.ToInt32(reader["PendingReviewCount"])
                        });
                    }
                }
            }
            return list;
        }

        public (bool success, string message, int newTaskId) CreateTask(CreateTaskModel model)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_CreateTask", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ClassroomId", SqlDbType.Int).Value = model.ClassroomId;
                cmd.Parameters.Add("@Title", SqlDbType.NVarChar, 200).Value = model.Title ?? "";
                cmd.Parameters.Add("@Description", SqlDbType.NVarChar, -1).Value = model.Description ?? "";
                cmd.Parameters.Add("@TaskType", SqlDbType.NVarChar, 50).Value = !string.IsNullOrEmpty(model.TaskType) ? model.TaskType : "Assignment";
                cmd.Parameters.Add("@MaxScore", SqlDbType.Int).Value = model.MaxScore > 0 ? model.MaxScore : 20;
                cmd.Parameters.Add("@DueDate", SqlDbType.DateTime2).Value = model.DueDate != default(DateTime) ? model.DueDate : DateTime.Now.AddDays(7);

                var pSuccess = cmd.Parameters.Add("@Success", SqlDbType.Bit);
                pSuccess.Direction = ParameterDirection.Output;

                var pMessage = cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 255);
                pMessage.Direction = ParameterDirection.Output;

                var pNewTaskId = cmd.Parameters.Add("@NewTaskId", SqlDbType.Int);
                pNewTaskId.Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();

                return (
                    (bool)pSuccess.Value,
                    pMessage.Value?.ToString(),
                    pNewTaskId.Value != DBNull.Value ? (int)pNewTaskId.Value : 0
                );
            }
        }

        public FacultyProfileViewModel GetFacultyProfile(int userId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_GetFacultyProfile", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new FacultyProfileViewModel
                        {
                            UserId = (int)reader["UserId"],
                            FirstName = reader["FirstName"].ToString(),
                            LastName = reader["LastName"].ToString(),
                            FullName = reader["FullName"].ToString(),
                            Email = reader["Email"].ToString(),
                            PhoneNumber = reader["PhoneNumber"] != DBNull.Value ? reader["PhoneNumber"].ToString() : "",
                            EmployeeCode = reader["EmployeeCode"] != DBNull.Value ? reader["EmployeeCode"].ToString() : "N/A",
                            Department = reader["Department"] != DBNull.Value ? reader["Department"].ToString() : "Computer Engineering",
                            Designation = reader["Designation"] != DBNull.Value ? reader["Designation"].ToString() : "Faculty Member",
                            Qualification = reader["Qualification"] != DBNull.Value ? reader["Qualification"].ToString() : "Master of Technology",
                            OfficeLocation = reader["OfficeLocation"] != DBNull.Value ? reader["OfficeLocation"].ToString() : "Faculty Cabin",
                            ProfilePictureUrl = reader["ProfilePictureUrl"] != DBNull.Value ? reader["ProfilePictureUrl"].ToString() : null
                        };
                    }
                }
            }
            return null;
        }

        public (bool success, string message) UpdateFacultyProfile(FacultyProfileViewModel model)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_UpdateFacultyProfile", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = model.UserId;
                cmd.Parameters.Add("@FirstName", SqlDbType.NVarChar, 100).Value = model.FirstName ?? "";
                cmd.Parameters.Add("@LastName", SqlDbType.NVarChar, 100).Value = model.LastName ?? "";
                cmd.Parameters.Add("@PhoneNumber", SqlDbType.NVarChar, 20).Value = model.PhoneNumber ?? "";
                cmd.Parameters.Add("@Department", SqlDbType.NVarChar, 100).Value = model.Department ?? "";
                cmd.Parameters.Add("@Designation", SqlDbType.NVarChar, 100).Value = model.Designation ?? "";
                cmd.Parameters.Add("@Qualification", SqlDbType.NVarChar, 150).Value = model.Qualification ?? "";
                cmd.Parameters.Add("@OfficeLocation", SqlDbType.NVarChar, 100).Value = model.OfficeLocation ?? "";

                var pSuccess = cmd.Parameters.Add("@Success", SqlDbType.Bit);
                pSuccess.Direction = ParameterDirection.Output;

                var pMessage = cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 255);
                pMessage.Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();

                return ((bool)pSuccess.Value, pMessage.Value?.ToString());
            }
        }

        public List<CourseMaterialItem> GetFacultyMaterials(int facultyUserId)
        {
            var list = new List<CourseMaterialItem>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_GetFacultyMaterials", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@FacultyUserId", SqlDbType.Int).Value = facultyUserId;
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new CourseMaterialItem
                        {
                            MaterialId   = (int)r["MaterialId"],
                            ClassroomId  = (int)r["ClassroomId"],
                            CourseCode   = r["CourseCode"].ToString(),
                            CourseName   = r["CourseName"].ToString(),
                            Title        = r["Title"].ToString(),
                            Description  = r["Description"] != DBNull.Value ? r["Description"].ToString() : null,
                            FileName     = r["FileName"] != DBNull.Value ? r["FileName"].ToString() : null,
                            FileSize     = r["FileSize"] != DBNull.Value ? r["FileSize"].ToString()  : null,
                            FileType     = r["FileType"] != DBNull.Value ? r["FileType"].ToString() : "File",
                            Category     = r["Category"] != DBNull.Value ? r["Category"].ToString() : "Other",
                            FileUrl      = r["FileUrl"]  != DBNull.Value ? r["FileUrl"].ToString()   : null,
                            ExternalUrl  = r["ExternalUrl"] != DBNull.Value ? r["ExternalUrl"].ToString() : null,
                            UploadedAt   = Convert.ToDateTime(r["UploadedAt"]),
                            UpdatedAt    = r["UpdatedAt"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(r["UpdatedAt"]) : null,
                            UploaderName = r["UploaderName"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        public CourseMaterialItem GetMaterialById(int materialId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                SELECT m.MaterialId, m.ClassroomId, c.CourseCode, c.CourseName, m.FacultyUserId,
                       m.Title, m.Description, m.FileName, m.FileSize, m.FileType, m.Category,
                       ISNULL(m.CloudUrl, m.FilePath) AS FileUrl, m.CloudPublicId, m.ExternalUrl,
                       m.UploadedAt, m.UpdatedAt, (u.FirstName + ' ' + u.LastName) AS UploaderName
                FROM CourseMaterials m
                JOIN Classrooms c ON m.ClassroomId = c.ClassroomId
                JOIN Users u ON m.FacultyUserId = u.UserId
                WHERE m.MaterialId = @MaterialId AND m.IsActive = 1", conn))
            {
                cmd.Parameters.Add("@MaterialId", SqlDbType.Int).Value = materialId;
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        return new CourseMaterialItem
                        {
                            MaterialId   = (int)r["MaterialId"],
                            ClassroomId  = (int)r["ClassroomId"],
                            FacultyUserId = (int)r["FacultyUserId"],
                            CourseCode   = r["CourseCode"].ToString(),
                            CourseName   = r["CourseName"].ToString(),
                            Title        = r["Title"].ToString(),
                            Description  = r["Description"] != DBNull.Value ? r["Description"].ToString() : null,
                            FileName     = r["FileName"] != DBNull.Value ? r["FileName"].ToString() : null,
                            FileSize     = r["FileSize"] != DBNull.Value ? r["FileSize"].ToString() : null,
                            FileType     = r["FileType"] != DBNull.Value ? r["FileType"].ToString() : null,
                            Category     = r["Category"] != DBNull.Value ? r["Category"].ToString() : "Other",
                            FileUrl      = r["FileUrl"] != DBNull.Value ? r["FileUrl"].ToString() : null,
                            CloudPublicId = r["CloudPublicId"] != DBNull.Value ? r["CloudPublicId"].ToString() : null,
                            ExternalUrl  = r["ExternalUrl"] != DBNull.Value ? r["ExternalUrl"].ToString() : null,
                            UploadedAt   = Convert.ToDateTime(r["UploadedAt"]),
                            UpdatedAt    = r["UpdatedAt"] != DBNull.Value ? (DateTime?)Convert.ToDateTime(r["UpdatedAt"]) : null,
                            UploaderName = r["UploaderName"].ToString()
                        };
                    }
                }
            }
            return null;
        }

        public (bool success, int materialId, string message) UploadMaterial(
            int classroomId, int facultyUserId,
            string title, string description,
            string fileName, string fileSize,
            string fileType, string category,
            string cloudUrl, string cloudPublicId,
            string externalUrl = null)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_UploadCourseMaterial", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ClassroomId",   SqlDbType.Int).Value           = classroomId;
                cmd.Parameters.Add("@FacultyUserId", SqlDbType.Int).Value           = facultyUserId;
                cmd.Parameters.Add("@Title",         SqlDbType.NVarChar, 255).Value = title;
                cmd.Parameters.Add("@Description",   SqlDbType.NVarChar, 500).Value = (object)description ?? DBNull.Value;
                cmd.Parameters.Add("@FileName",      SqlDbType.NVarChar, 255).Value = (object)fileName ?? DBNull.Value;
                cmd.Parameters.Add("@FileSize",      SqlDbType.NVarChar, 50).Value  = (object)fileSize ?? DBNull.Value;
                cmd.Parameters.Add("@FileType",      SqlDbType.NVarChar, 50).Value  = (object)fileType ?? DBNull.Value;
                cmd.Parameters.Add("@Category",      SqlDbType.NVarChar, 50).Value  = category;
                cmd.Parameters.Add("@CloudUrl",      SqlDbType.NVarChar, 1000).Value = (object)cloudUrl ?? DBNull.Value;
                cmd.Parameters.Add("@CloudPublicId", SqlDbType.NVarChar, 500).Value  = (object)cloudPublicId ?? DBNull.Value;
                cmd.Parameters.Add("@ExternalUrl",   SqlDbType.NVarChar, 1000).Value = (object)externalUrl ?? DBNull.Value;

                var pId  = cmd.Parameters.Add("@MaterialId", SqlDbType.Int);   pId.Direction  = ParameterDirection.Output;
                var pOk  = cmd.Parameters.Add("@Success",    SqlDbType.Bit);   pOk.Direction  = ParameterDirection.Output;
                var pMsg = cmd.Parameters.Add("@Message",    SqlDbType.NVarChar, 255); pMsg.Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();
                return ((bool)pOk.Value, (int)pId.Value, pMsg.Value?.ToString());
            }
        }

        public (bool success, string message) UpdateMaterial(
            int materialId, int facultyUserId,
            string title, string description, string category,
            string externalUrl = null,
            string fileName = null, string fileSize = null,
            string fileType = null, string cloudUrl = null,
            string cloudPublicId = null)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_UpdateCourseMaterial", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@MaterialId",     SqlDbType.Int).Value           = materialId;
                cmd.Parameters.Add("@FacultyUserId",  SqlDbType.Int).Value           = facultyUserId;
                cmd.Parameters.Add("@Title",          SqlDbType.NVarChar, 255).Value = title;
                cmd.Parameters.Add("@Description",    SqlDbType.NVarChar, 500).Value = (object)description ?? DBNull.Value;
                cmd.Parameters.Add("@Category",       SqlDbType.NVarChar, 50).Value  = category;
                cmd.Parameters.Add("@ExternalUrl",    SqlDbType.NVarChar, 1000).Value = (object)externalUrl ?? DBNull.Value;
                cmd.Parameters.Add("@FileName",       SqlDbType.NVarChar, 255).Value = (object)fileName ?? DBNull.Value;
                cmd.Parameters.Add("@FileSize",       SqlDbType.NVarChar, 50).Value  = (object)fileSize ?? DBNull.Value;
                cmd.Parameters.Add("@FileType",       SqlDbType.NVarChar, 50).Value  = (object)fileType ?? DBNull.Value;
                cmd.Parameters.Add("@CloudUrl",       SqlDbType.NVarChar, 1000).Value = (object)cloudUrl ?? DBNull.Value;
                cmd.Parameters.Add("@CloudPublicId",  SqlDbType.NVarChar, 500).Value  = (object)cloudPublicId ?? DBNull.Value;

                var pOk  = cmd.Parameters.Add("@Success", SqlDbType.Bit);         pOk.Direction  = ParameterDirection.Output;
                var pMsg = cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 255); pMsg.Direction = ParameterDirection.Output;

                conn.Open();
                cmd.ExecuteNonQuery();
                return ((bool)pOk.Value, pMsg.Value?.ToString());
            }
        }

        public (bool success, string cloudPublicId, string message) DeleteMaterial(int materialId, int facultyUserId)
        {
            string cloudPublicId = null;
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                // 1. Fetch CloudPublicId if exists
                using (var qCmd = new SqlCommand("SELECT CloudPublicId FROM CourseMaterials WHERE MaterialId = @MaterialId AND FacultyUserId = @FacultyUserId", conn))
                {
                    qCmd.Parameters.Add("@MaterialId", SqlDbType.Int).Value = materialId;
                    qCmd.Parameters.Add("@FacultyUserId", SqlDbType.Int).Value = facultyUserId;
                    var obj = qCmd.ExecuteScalar();
                    if (obj != null && obj != DBNull.Value)
                    {
                        cloudPublicId = obj.ToString();
                    }
                }

                // 2. Execute deletion sp
                using (var cmd = new SqlCommand("sp_DeleteCourseMaterial", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@MaterialId",    SqlDbType.Int).Value = materialId;
                    cmd.Parameters.Add("@FacultyUserId", SqlDbType.Int).Value = facultyUserId;

                    var pOk  = cmd.Parameters.Add("@Success", SqlDbType.Bit);         pOk.Direction  = ParameterDirection.Output;
                    var pMsg = cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 255); pMsg.Direction = ParameterDirection.Output;

                    cmd.ExecuteNonQuery();
                    return ((bool)pOk.Value, cloudPublicId, pMsg.Value?.ToString());
                }
            }
        }

        public bool IsFacultyAuthorizedForClassroom(int facultyUserId, int classroomId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("SELECT 1 FROM Classrooms WHERE ClassroomId = @ClassroomId AND FacultyUserId = @FacultyUserId", conn))
            {
                cmd.Parameters.Add("@ClassroomId", SqlDbType.Int).Value = classroomId;
                cmd.Parameters.Add("@FacultyUserId", SqlDbType.Int).Value = facultyUserId;
                conn.Open();
                var result = cmd.ExecuteScalar();
                return result != null;
            }
        }

        public bool IsStudentEnrolledInClassroom(int studentUserId, int classroomId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("SELECT 1 FROM ClassroomEnrollments WHERE ClassroomId = @ClassroomId AND StudentUserId = @StudentUserId", conn))
            {
                cmd.Parameters.Add("@ClassroomId", SqlDbType.Int).Value = classroomId;
                cmd.Parameters.Add("@StudentUserId", SqlDbType.Int).Value = studentUserId;
                conn.Open();
                var result = cmd.ExecuteScalar();
                return result != null;
            }
        }

        // ── ANNOUNCEMENTS ────────────────────────────────────────────────────
        public List<AnnouncementItem> GetFacultyAnnouncements(int facultyUserId)
        {
            var list = new List<AnnouncementItem>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_GetFacultyAnnouncements", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@FacultyUserId", SqlDbType.Int).Value = facultyUserId;
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        list.Add(new AnnouncementItem
                        {
                            AnnouncementId = (int)r["AnnouncementId"],
                            ClassroomId    = r["ClassroomId"] != DBNull.Value ? (int?)r["ClassroomId"] : null,
                            CourseCode     = r["CourseCode"].ToString(),
                            CourseName     = r["CourseName"].ToString(),
                            Title          = r["Title"].ToString(),
                            Message        = r["Message"].ToString(),
                            Priority       = r["Priority"].ToString(),
                            IsPinned       = (bool)r["IsPinned"],
                            RequireAck     = (bool)r["RequireAck"],
                            PublishedAt    = Convert.ToDateTime(r["PublishedAt"]),
                            FacultyName    = r["FacultyName"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        public (bool success, int id, string message) PublishAnnouncement(
            int classroomId, int facultyUserId,
            string title, string message, string priority, bool isPinned, bool requireAck)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_PublishAnnouncement", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ClassroomId",   SqlDbType.Int).Value           = classroomId;
                cmd.Parameters.Add("@FacultyUserId", SqlDbType.Int).Value           = facultyUserId;
                cmd.Parameters.Add("@Title",         SqlDbType.NVarChar, 255).Value = title;
                cmd.Parameters.Add("@Message",       SqlDbType.NVarChar, -1).Value  = message;
                cmd.Parameters.Add("@Priority",      SqlDbType.NVarChar, 20).Value  = priority;
                cmd.Parameters.Add("@IsPinned",      SqlDbType.Bit).Value           = isPinned;
                cmd.Parameters.Add("@RequireAck",    SqlDbType.Bit).Value           = requireAck;
                var pId  = cmd.Parameters.Add("@AnnouncementId", SqlDbType.Int);           pId.Direction  = ParameterDirection.Output;
                var pOk  = cmd.Parameters.Add("@Success",        SqlDbType.Bit);           pOk.Direction  = ParameterDirection.Output;
                var pMsg = cmd.Parameters.Add("@Message_Out",    SqlDbType.NVarChar, 255); pMsg.Direction = ParameterDirection.Output;
                conn.Open();
                cmd.ExecuteNonQuery();
                return ((bool)pOk.Value, (int)pId.Value, pMsg.Value?.ToString());
            }
        }

        public (bool success, string message) DeleteAnnouncement(int announcementId, int facultyUserId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_DeleteAnnouncement", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@AnnouncementId", SqlDbType.Int).Value = announcementId;
                cmd.Parameters.Add("@FacultyUserId",  SqlDbType.Int).Value = facultyUserId;
                var pOk  = cmd.Parameters.Add("@Success",    SqlDbType.Bit);           pOk.Direction  = ParameterDirection.Output;
                var pMsg = cmd.Parameters.Add("@Message_Out",SqlDbType.NVarChar, 255); pMsg.Direction = ParameterDirection.Output;
                conn.Open();
                cmd.ExecuteNonQuery();
                return ((bool)pOk.Value, pMsg.Value?.ToString());
            }
        }

        // ── TASK EDIT / DELETE ────────────────────────────────────────────────
        public (bool success, string message) UpdateTask(int taskId, int facultyUserId, string title, string description, string taskType, int maxScore, DateTime dueDate)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_UpdateTask", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@TaskId",        SqlDbType.Int).Value           = taskId;
                cmd.Parameters.Add("@FacultyUserId", SqlDbType.Int).Value           = facultyUserId;
                cmd.Parameters.Add("@Title",         SqlDbType.NVarChar, 200).Value = title ?? "";
                cmd.Parameters.Add("@Description",   SqlDbType.NVarChar, -1).Value  = description ?? "";
                cmd.Parameters.Add("@TaskType",      SqlDbType.NVarChar, 50).Value  = taskType ?? "Assignment";
                cmd.Parameters.Add("@MaxScore",      SqlDbType.Int).Value           = maxScore;
                cmd.Parameters.Add("@DueDate",       SqlDbType.DateTime2).Value     = dueDate;
                var pOk  = cmd.Parameters.Add("@Success", SqlDbType.Bit);           pOk.Direction  = ParameterDirection.Output;
                var pMsg = cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 255); pMsg.Direction = ParameterDirection.Output;
                conn.Open();
                cmd.ExecuteNonQuery();
                return ((bool)pOk.Value, pMsg.Value?.ToString());
            }
        }

        public (bool success, string message) DeleteTask(int taskId, int facultyUserId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_DeleteTask", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@TaskId",        SqlDbType.Int).Value = taskId;
                cmd.Parameters.Add("@FacultyUserId", SqlDbType.Int).Value = facultyUserId;
                var pOk  = cmd.Parameters.Add("@Success", SqlDbType.Bit);           pOk.Direction  = ParameterDirection.Output;
                var pMsg = cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 255); pMsg.Direction = ParameterDirection.Output;
                conn.Open();
                cmd.ExecuteNonQuery();
                return ((bool)pOk.Value, pMsg.Value?.ToString());
            }
        }

        // ── TASK ATTACHMENTS & EDIT HELPERS ──────────────────────────────────
        public CreateTaskModel GetTaskForEdit(int taskId, int facultyUserId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                SELECT t.TaskId, t.ClassroomId, t.Title, t.Description, t.TaskType, t.MaxScore, t.DueDate
                FROM Tasks t
                INNER JOIN Classrooms c ON t.ClassroomId = c.ClassroomId
                WHERE t.TaskId = @TaskId AND c.FacultyUserId = @FacultyUserId", conn))
            {
                cmd.Parameters.Add("@TaskId", SqlDbType.Int).Value = taskId;
                cmd.Parameters.Add("@FacultyUserId", SqlDbType.Int).Value = facultyUserId;
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var model = new CreateTaskModel
                        {
                            TaskId = (int)reader["TaskId"],
                            ClassroomId = (int)reader["ClassroomId"],
                            Title = reader["Title"].ToString(),
                            Description = reader["Description"] != DBNull.Value ? reader["Description"].ToString() : "",
                            TaskType = reader["TaskType"].ToString(),
                            MaxScore = Convert.ToInt32(reader["MaxScore"]),
                            DueDate = Convert.ToDateTime(reader["DueDate"]),
                            ExistingAttachments = new List<TaskAttachmentItem>()
                        };
                        return model;
                    }
                }
            }
            return null;
        }

        public List<TaskAttachmentItem> GetTaskAttachments(int taskId)
        {
            var list = new List<TaskAttachmentItem>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                SELECT AttachmentId, TaskId, FileName, FileSize, FileType, FileUrl, CloudPublicId, FilePath, UploadedAt, IsActive
                FROM TaskAttachments
                WHERE TaskId = @TaskId AND IsActive = 1
                ORDER BY AttachmentId ASC", conn))
            {
                cmd.Parameters.Add("@TaskId", SqlDbType.Int).Value = taskId;
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new TaskAttachmentItem
                        {
                            AttachmentId = (int)reader["AttachmentId"],
                            TaskId = (int)reader["TaskId"],
                            FileName = reader["FileName"].ToString(),
                            FileSize = reader["FileSize"].ToString(),
                            FileType = reader["FileType"].ToString(),
                            FileUrl = reader["FileUrl"].ToString(),
                            CloudPublicId = reader["CloudPublicId"] != DBNull.Value ? reader["CloudPublicId"].ToString() : null,
                            FilePath = reader["FilePath"] != DBNull.Value ? reader["FilePath"].ToString() : null,
                            UploadedAt = Convert.ToDateTime(reader["UploadedAt"]),
                            IsActive = Convert.ToBoolean(reader["IsActive"])
                        });
                    }
                }
            }
            return list;
        }

        public (bool success, string message, int attachmentId) SaveTaskAttachment(int taskId, string fileName, string fileSize, string fileType, string fileUrl, string cloudPublicId, string filePath)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                INSERT INTO TaskAttachments (TaskId, FileName, FileSize, FileType, FileUrl, CloudPublicId, FilePath, UploadedAt, IsActive)
                VALUES (@TaskId, @FileName, @FileSize, @FileType, @FileUrl, @CloudPublicId, @FilePath, SYSUTCDATETIME(), 1);
                SELECT SCOPE_IDENTITY();", conn))
            {
                cmd.Parameters.Add("@TaskId", SqlDbType.Int).Value = taskId;
                cmd.Parameters.Add("@FileName", SqlDbType.NVarChar, 255).Value = fileName;
                cmd.Parameters.Add("@FileSize", SqlDbType.NVarChar, 50).Value = fileSize;
                cmd.Parameters.Add("@FileType", SqlDbType.NVarChar, 50).Value = fileType;
                cmd.Parameters.Add("@FileUrl", SqlDbType.NVarChar, 1000).Value = fileUrl;
                cmd.Parameters.Add("@CloudPublicId", SqlDbType.NVarChar, 500).Value = (object)cloudPublicId ?? DBNull.Value;
                cmd.Parameters.Add("@FilePath", SqlDbType.NVarChar, 500).Value = (object)filePath ?? DBNull.Value;

                conn.Open();
                var idObj = cmd.ExecuteScalar();
                int id = idObj != null && idObj != DBNull.Value ? Convert.ToInt32(idObj) : 0;
                return (id > 0, id > 0 ? "Attachment saved successfully." : "Failed to save attachment.", id);
            }
        }

        public (bool success, string message) DeleteTaskAttachment(int attachmentId, int facultyUserId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                UPDATE a
                SET a.IsActive = 0
                FROM TaskAttachments a
                INNER JOIN Tasks t ON a.TaskId = t.TaskId
                INNER JOIN Classrooms c ON t.ClassroomId = c.ClassroomId
                WHERE a.AttachmentId = @AttachmentId AND c.FacultyUserId = @FacultyUserId", conn))
            {
                cmd.Parameters.Add("@AttachmentId", SqlDbType.Int).Value = attachmentId;
                cmd.Parameters.Add("@FacultyUserId", SqlDbType.Int).Value = facultyUserId;
                conn.Open();
                int rows = cmd.ExecuteNonQuery();
                return (rows > 0, rows > 0 ? "Attachment deleted." : "Attachment not found or unauthorized.");
            }
        }

        public TaskAttachmentItem GetTaskAttachmentById(int attachmentId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(@"
                SELECT AttachmentId, TaskId, FileName, FileSize, FileType, FileUrl, CloudPublicId, FilePath, UploadedAt, IsActive
                FROM TaskAttachments
                WHERE AttachmentId = @AttachmentId AND IsActive = 1", conn))
            {
                cmd.Parameters.Add("@AttachmentId", SqlDbType.Int).Value = attachmentId;
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new TaskAttachmentItem
                        {
                            AttachmentId = (int)reader["AttachmentId"],
                            TaskId = (int)reader["TaskId"],
                            FileName = reader["FileName"].ToString(),
                            FileSize = reader["FileSize"].ToString(),
                            FileType = reader["FileType"].ToString(),
                            FileUrl = reader["FileUrl"].ToString(),
                            CloudPublicId = reader["CloudPublicId"] != DBNull.Value ? reader["CloudPublicId"].ToString() : null,
                            FilePath = reader["FilePath"] != DBNull.Value ? reader["FilePath"].ToString() : null,
                            UploadedAt = Convert.ToDateTime(reader["UploadedAt"]),
                            IsActive = Convert.ToBoolean(reader["IsActive"])
                        };
                    }
                }
            }
            return null;
        }

        // ── CLASSROOM DELETE ──────────────────────────────────────────────────
        public (bool success, string message) DeleteClassroom(int classroomId, int facultyUserId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_DeleteClassroom", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@ClassroomId",   SqlDbType.Int).Value = classroomId;
                cmd.Parameters.Add("@FacultyUserId", SqlDbType.Int).Value = facultyUserId;
                var pOk  = cmd.Parameters.Add("@Success", SqlDbType.Bit);           pOk.Direction  = ParameterDirection.Output;
                var pMsg = cmd.Parameters.Add("@Message", SqlDbType.NVarChar, 255); pMsg.Direction = ParameterDirection.Output;
                conn.Open();
                cmd.ExecuteNonQuery();
                return ((bool)pOk.Value, pMsg.Value?.ToString());
            }
        }

        // ============================================================
        // FACULTY STUDENT ANALYTICS & ACTIVITY REPORTS
        // ============================================================

        public FacultyAnalyticsViewModel GetFacultyAnalytics(int facultyUserId, int? classroomId = null)
        {
            var vm = new FacultyAnalyticsViewModel();
            vm.SelectedClassroomId = classroomId;

            // 1. Get faculty classrooms
            vm.Classrooms = GetFacultyClassrooms(facultyUserId);
            vm.TotalActiveClassrooms = vm.Classrooms.Count;

            if (classroomId.HasValue && classroomId.Value > 0)
            {
                var sel = vm.Classrooms.Find(c => c.ClassroomId == classroomId.Value);
                if (sel != null)
                {
                    vm.SelectedClassroomName = $"{sel.CourseName} ({sel.CourseCode})";
                }
            }

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // 2. Overview Statistics (Scoped to facultyUserId and optional classroomId)
                string statsSql = @"
                    -- Total Distinct Students
                    SELECT COUNT(DISTINCT ce.StudentUserId)
                    FROM ClassroomEnrollments ce
                    JOIN Classrooms c ON ce.ClassroomId = c.ClassroomId
                    WHERE c.FacultyUserId = @FacultyUserId AND c.IsActive = 1
                      AND (@ClassroomId IS NULL OR c.ClassroomId = @ClassroomId);

                    -- Total Tasks
                    SELECT COUNT(*)
                    FROM Tasks t
                    JOIN Classrooms c ON t.ClassroomId = c.ClassroomId
                    WHERE c.FacultyUserId = @FacultyUserId AND c.IsActive = 1
                      AND (@ClassroomId IS NULL OR c.ClassroomId = @ClassroomId);

                    -- Pending Reviews
                    SELECT COUNT(*)
                    FROM TaskSubmissions s
                    JOIN Tasks t ON s.TaskId = t.TaskId
                    JOIN Classrooms c ON t.ClassroomId = c.ClassroomId
                    WHERE c.FacultyUserId = @FacultyUserId AND c.IsActive = 1
                      AND s.Status = 'Pending Review'
                      AND (@ClassroomId IS NULL OR c.ClassroomId = @ClassroomId);

                    -- Approved Submissions
                    SELECT COUNT(*)
                    FROM TaskSubmissions s
                    JOIN Tasks t ON s.TaskId = t.TaskId
                    JOIN Classrooms c ON t.ClassroomId = c.ClassroomId
                    WHERE c.FacultyUserId = @FacultyUserId AND c.IsActive = 1
                      AND s.Status = 'Approved'
                      AND (@ClassroomId IS NULL OR c.ClassroomId = @ClassroomId);

                    -- Revision Submissions
                    SELECT COUNT(*)
                    FROM TaskSubmissions s
                    JOIN Tasks t ON s.TaskId = t.TaskId
                    JOIN Classrooms c ON t.ClassroomId = c.ClassroomId
                    WHERE c.FacultyUserId = @FacultyUserId AND c.IsActive = 1
                      AND (s.Status = 'Revision Required' OR s.Status = 'Revision')
                      AND (@ClassroomId IS NULL OR c.ClassroomId = @ClassroomId);

                    -- Overdue Tasks
                    SELECT COUNT(*)
                    FROM Tasks t
                    JOIN Classrooms c ON t.ClassroomId = c.ClassroomId
                    WHERE c.FacultyUserId = @FacultyUserId AND c.IsActive = 1
                      AND t.DueDate < SYSUTCDATETIME()
                      AND (@ClassroomId IS NULL OR c.ClassroomId = @ClassroomId);
                ";

                using (var cmd = new SqlCommand(statsSql, conn))
                {
                    cmd.Parameters.Add("@FacultyUserId", SqlDbType.Int).Value = facultyUserId;
                    cmd.Parameters.Add("@ClassroomId", SqlDbType.Int).Value = (object)classroomId ?? DBNull.Value;

                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read()) vm.TotalStudents = Convert.ToInt32(r[0]);
                        if (r.NextResult() && r.Read()) vm.TotalAssignments = Convert.ToInt32(r[0]);
                        if (r.NextResult() && r.Read()) vm.AwaitingReviewCount = Convert.ToInt32(r[0]);
                        if (r.NextResult() && r.Read()) vm.ApprovedCount = Convert.ToInt32(r[0]);
                        if (r.NextResult() && r.Read()) vm.RevisionCount = Convert.ToInt32(r[0]);
                        if (r.NextResult() && r.Read()) vm.OverdueAssignmentsCount = Convert.ToInt32(r[0]);
                    }
                }

                // 3. Task Reports
                string tasksSql = @"
                    SELECT t.TaskId, t.ClassroomId, c.CourseName, c.CourseCode, t.Title, t.TaskType, t.DueDate, t.Status,
                           (SELECT COUNT(*) FROM ClassroomEnrollments ce WHERE ce.ClassroomId = c.ClassroomId) AS TotalEnrolled,
                           (SELECT COUNT(*) FROM TaskSubmissions ts WHERE ts.TaskId = t.TaskId) AS SubmittedCount,
                           (SELECT COUNT(*) FROM TaskSubmissions ts WHERE ts.TaskId = t.TaskId AND ts.Status = 'Pending Review') AS PendingReviewCount,
                           (SELECT COUNT(*) FROM TaskSubmissions ts WHERE ts.TaskId = t.TaskId AND ts.Status = 'Approved') AS ApprovedCount,
                           (SELECT COUNT(*) FROM TaskSubmissions ts WHERE ts.TaskId = t.TaskId AND (ts.Status = 'Revision Required' OR ts.Status = 'Revision')) AS RevisionCount
                    FROM Tasks t
                    JOIN Classrooms c ON t.ClassroomId = c.ClassroomId
                    WHERE c.FacultyUserId = @FacultyUserId AND c.IsActive = 1
                      AND (@ClassroomId IS NULL OR c.ClassroomId = @ClassroomId)
                    ORDER BY t.DueDate DESC;
                ";

                int sumMissing = 0;
                using (var cmd = new SqlCommand(tasksSql, conn))
                {
                    cmd.Parameters.Add("@FacultyUserId", SqlDbType.Int).Value = facultyUserId;
                    cmd.Parameters.Add("@ClassroomId", SqlDbType.Int).Value = (object)classroomId ?? DBNull.Value;

                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            var item = new FacultyTaskAnalyticsItem
                            {
                                TaskId = (int)r["TaskId"],
                                ClassroomId = (int)r["ClassroomId"],
                                ClassroomName = r["CourseName"].ToString(),
                                CourseCode = r["CourseCode"].ToString(),
                                Title = r["Title"].ToString(),
                                TaskType = r["TaskType"].ToString(),
                                DueDate = Convert.ToDateTime(r["DueDate"]),
                                Status = r["Status"].ToString(),
                                TotalEnrolled = Convert.ToInt32(r["TotalEnrolled"]),
                                SubmittedCount = Convert.ToInt32(r["SubmittedCount"]),
                                PendingReviewCount = Convert.ToInt32(r["PendingReviewCount"]),
                                ApprovedCount = Convert.ToInt32(r["ApprovedCount"]),
                                RevisionCount = Convert.ToInt32(r["RevisionCount"])
                            };
                            sumMissing += item.MissingCount;
                            vm.TaskReports.Add(item);
                        }
                    }
                }
                vm.PendingSubmissions = sumMissing;

                // 4. Student Activity History
                string actSql = @"
                    SELECT TOP 30
                        s.SubmissionId,
                        (u.FirstName + ' ' + u.LastName) AS StudentName,
                        COALESCE(sp.EnrollmentNumber, sp.RollNumber, 'STU-' + CAST(u.UserId AS NVARCHAR)) AS RollNo,
                        t.Title AS TaskTitle,
                        c.CourseName AS ClassroomName,
                        CASE
                            WHEN s.Status = 'Approved' THEN 'Approved'
                            WHEN s.Status = 'Revision Required' OR s.Status = 'Revision' THEN 'Revision Requested'
                            WHEN s.ReviewedAt IS NOT NULL THEN 'Under Review'
                            ELSE 'Assignment Submitted'
                        END AS ActivityType,
                        s.Status,
                        COALESCE(s.ReviewedAt, s.SubmittedAt) AS [Timestamp],
                        s.FileName
                    FROM TaskSubmissions s
                    JOIN Tasks t ON s.TaskId = t.TaskId
                    JOIN Classrooms c ON t.ClassroomId = c.ClassroomId
                    JOIN Users u ON s.StudentUserId = u.UserId
                    LEFT JOIN StudentProfiles sp ON u.UserId = sp.UserId
                    WHERE c.FacultyUserId = @FacultyUserId AND c.IsActive = 1
                      AND (@ClassroomId IS NULL OR c.ClassroomId = @ClassroomId)
                    ORDER BY COALESCE(s.ReviewedAt, s.SubmittedAt) DESC;
                ";

                using (var cmd = new SqlCommand(actSql, conn))
                {
                    cmd.Parameters.Add("@FacultyUserId", SqlDbType.Int).Value = facultyUserId;
                    cmd.Parameters.Add("@ClassroomId", SqlDbType.Int).Value = (object)classroomId ?? DBNull.Value;

                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            vm.RecentActivity.Add(new FacultyStudentActivityItem
                            {
                                SubmissionId = (int)r["SubmissionId"],
                                StudentName = r["StudentName"].ToString(),
                                RollNo = r["RollNo"].ToString(),
                                TaskTitle = r["TaskTitle"].ToString(),
                                ClassroomName = r["ClassroomName"].ToString(),
                                ActivityType = r["ActivityType"].ToString(),
                                Status = r["Status"].ToString(),
                                Timestamp = Convert.ToDateTime(r["Timestamp"]),
                                FileName = r["FileName"] != DBNull.Value ? r["FileName"].ToString() : null
                            });
                        }
                    }
                }
            }

            return vm;
        }

        public string ExportClassroomSubmissionsCsv(int facultyUserId, int? classroomId = null)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Classroom,Course Code,Task Title,Task Type,Student Name,Roll No,Status,Score,Submitted Date,Reviewed Date,File Name,File Size");

            using (var conn = new SqlConnection(_connectionString))
            {
                string sql = @"
                    SELECT c.CourseName, c.CourseCode, t.Title AS TaskTitle, t.TaskType,
                           (u.FirstName + ' ' + u.LastName) AS StudentName,
                           COALESCE(sp.EnrollmentNumber, sp.RollNumber, 'STU-' + CAST(u.UserId AS NVARCHAR)) AS RollNo,
                           s.Status, s.Score, s.SubmittedAt, s.ReviewedAt, s.FileName, s.FileSize
                    FROM TaskSubmissions s
                    JOIN Tasks t ON s.TaskId = t.TaskId
                    JOIN Classrooms c ON t.ClassroomId = c.ClassroomId
                    JOIN Users u ON s.StudentUserId = u.UserId
                    LEFT JOIN StudentProfiles sp ON u.UserId = sp.UserId
                    WHERE c.FacultyUserId = @FacultyUserId AND c.IsActive = 1
                      AND (@ClassroomId IS NULL OR c.ClassroomId = @ClassroomId)
                    ORDER BY c.CourseName, t.Title, s.SubmittedAt DESC;
                ";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@FacultyUserId", SqlDbType.Int).Value = facultyUserId;
                    cmd.Parameters.Add("@ClassroomId", SqlDbType.Int).Value = (object)classroomId ?? DBNull.Value;
                    conn.Open();

                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string cName = EscapeCsv(r["CourseName"].ToString());
                            string cCode = EscapeCsv(r["CourseCode"].ToString());
                            string tTitle = EscapeCsv(r["TaskTitle"].ToString());
                            string tType = EscapeCsv(r["TaskType"].ToString());
                            string sName = EscapeCsv(r["StudentName"].ToString());
                            string roll = EscapeCsv(r["RollNo"].ToString());
                            string status = EscapeCsv(r["Status"].ToString());
                            string score = r["Score"] != DBNull.Value ? r["Score"].ToString() : "—";
                            string subAt = Convert.ToDateTime(r["SubmittedAt"]).ToString("yyyy-MM-dd HH:mm");
                            string revAt = r["ReviewedAt"] != DBNull.Value ? Convert.ToDateTime(r["ReviewedAt"]).ToString("yyyy-MM-dd HH:mm") : "—";
                            string fName = r["FileName"] != DBNull.Value ? EscapeCsv(r["FileName"].ToString()) : "—";
                            string fSize = r["FileSize"] != DBNull.Value ? EscapeCsv(r["FileSize"].ToString()) : "—";

                            sb.AppendLine($"{cName},{cCode},{tTitle},{tType},{sName},{roll},{status},{score},{subAt},{revAt},{fName},{fSize}");
                        }
                    }
                }
            }
            return sb.ToString();
        }

        private static string EscapeCsv(string field)
        {
            if (string.IsNullOrEmpty(field)) return "\"\"";
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }
    }
}
