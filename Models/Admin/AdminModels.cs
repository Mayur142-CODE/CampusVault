using System;
using System.Collections.Generic;

namespace CampusVault.Models.Admin
{
    public class AdminDashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalFaculty { get; set; }
        public int TotalAdmins { get; set; }
        public int TotalClassrooms { get; set; }
        public int TotalTasks { get; set; }
        public int TotalDocuments { get; set; }
        public double StorageUsedMb { get; set; }
        public List<AdminActivityItem> RecentActivities { get; set; } = new List<AdminActivityItem>();
        public List<AdminUserItem> RecentUsers { get; set; } = new List<AdminUserItem>();
        public List<AdminDocumentItem> RecentDocuments { get; set; } = new List<AdminDocumentItem>();
    }

    public class AdminUserItem
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string RoleName { get; set; }
        public string Code { get; set; }
        public string Department { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ClassroomsCount { get; set; }
        public int SubmissionsCount { get; set; }
    }

    public class AdminClassroomItem
    {
        public int ClassroomId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string Department { get; set; }
        public string FacultyName { get; set; }
        public int StudentCount { get; set; }
        public int TaskCount { get; set; }
        public string JoinCode { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminTaskItem
    {
        public int TaskId { get; set; }
        public int ClassroomId { get; set; }
        public string Title { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string FacultyName { get; set; }
        public DateTime DueDate { get; set; }
        public int MaxScore { get; set; }
        public int SubmissionCount { get; set; }
    }

    public class AdminDocumentItem
    {
        public int SubmissionId { get; set; }
        public string FileName { get; set; }
        public string FileSize { get; set; }
        public string Sha256Hash { get; set; }
        public string StudentName { get; set; }
        public string RollNo { get; set; }
        public string FacultyName { get; set; }
        public string CourseName { get; set; }
        public string TaskTitle { get; set; }
        public string Status { get; set; }
        public DateTime SubmittedAt { get; set; }
    }

    public class AdminActivityItem
    {
        public int ActivityId { get; set; }
        public string ActivityType { get; set; }
        public string Module { get; set; }
        public string Description { get; set; }
        public string Details { get; set; }
        public string UserName { get; set; }
        public string UserRole { get; set; }
        public string UserInitials { get; set; }
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; }
    }
}
