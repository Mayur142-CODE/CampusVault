using System;
using System.Collections.Generic;

namespace CampusVault.Models.Student
{
    public class StudentDashboardViewModel
    {
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string EnrollmentNumber { get; set; }
        public string Department { get; set; }
        public int Semester { get; set; }
        public string Division { get; set; }

        public int EnrolledClassroomsCount { get; set; }
        public int PendingTasksCount { get; set; }
        public int UnderReviewCount { get; set; }
        public int ApprovedDocsCount { get; set; }

        public List<StudentDueSoonTaskItem> DueSoonTasks { get; set; } = new List<StudentDueSoonTaskItem>();
        public List<StudentClassroomSummaryItem> EnrolledClassrooms { get; set; } = new List<StudentClassroomSummaryItem>();
    }

    public class StudentDueSoonTaskItem
    {
        public int TaskId { get; set; }
        public int ClassroomId { get; set; }
        public string Title { get; set; }
        public string TaskType { get; set; }
        public int MaxScore { get; set; }
        public DateTime DueDate { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string FacultyName { get; set; }
        public string SubmissionStatus { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int? Score { get; set; }
    }

    public class StudentClassroomSummaryItem
    {
        public int ClassroomId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string Department { get; set; }
        public int Semester { get; set; }
        public string AcademicYear { get; set; }
        public string JoinCode { get; set; }
        public string ThemeGradient { get; set; }
        public string FacultyName { get; set; }
        public string FacultyDesignation { get; set; }
        public int ActiveTaskCount { get; set; }
        public int StudentCount { get; set; }
    }

    public class StudentTaskItem
    {
        public int TaskId { get; set; }
        public int ClassroomId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string TaskType { get; set; }
        public int MaxScore { get; set; }
        public DateTime DueDate { get; set; }
        public string TaskStatus { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string ThemeGradient { get; set; }
        public string FacultyName { get; set; }
        public string SubmissionStatus { get; set; }
        public int? SubmissionId { get; set; }
        public string FileName { get; set; }
        public string FileSize { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int? Score { get; set; }
        public string Feedback { get; set; }
    }

    public class StudentDocumentItem
    {
        public int SubmissionId { get; set; }
        public int TaskId { get; set; }
        public string TaskTitle { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string FacultyName { get; set; }
        public string FileName      { get; set; }
        public string FileSize      { get; set; }
        public string FilePath      { get; set; }   // Cloudinary URL
        public string Status        { get; set; }
        public int? Score { get; set; }
        public int MaxScore { get; set; }
        public string Feedback { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }

    public class StudentNotificationItem
    {
        public string Id { get; set; }
        public string Category { get; set; } // tasks, submissions, revisions, classrooms, materials
        public string Title { get; set; }
        public string Message { get; set; }
        public string BadgeText { get; set; }
        public string BadgeType { get; set; } // warning, success, danger, info
        public string ContextInfo { get; set; }
        public DateTime Timestamp { get; set; }
        public string ActionUrl { get; set; }
        public string ActionText { get; set; }
        public bool IsUnread { get; set; }
    }
}
