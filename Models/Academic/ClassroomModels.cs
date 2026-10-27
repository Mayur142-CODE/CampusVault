using System;
using System.Collections.Generic;

namespace CampusVault.Models.Academic
{
    public class Classroom
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
        public string FacultyEmail { get; set; }
        public string FacultyOffice { get; set; }
        public DateTime EnrolledAt { get; set; }
        public string EnrollmentStatus { get; set; }
        public int ActiveTaskCount { get; set; }
        public int StudentCount { get; set; }
        public int MaterialCount { get; set; }
        public int AnnouncementCount { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ClassroomTask
    {
        public int TaskId { get; set; }
        public int ClassroomId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string TaskType { get; set; }
        public int MaxScore { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string FacultyName { get; set; }
        public string FacultyEmail { get; set; }

        // Submission tracking
        public string SubmissionStatus { get; set; }
        public int? SubmissionId { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public int? Score { get; set; }
    }

    public class ClassroomMaterialItem
    {
        public int MaterialId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string FileName { get; set; }
        public string FileSize { get; set; }
        public string FileType { get; set; }
        public string Category { get; set; }
        public string FileUrl { get; set; }
        public string ExternalUrl { get; set; }
        public DateTime UploadedAt { get; set; }
        public string FacultyName { get; set; }
    }

    public class ClassroomAnnouncementItem
    {
        public int AnnouncementId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Priority { get; set; }
        public bool IsPinned { get; set; }
        public DateTime PublishedAt { get; set; }
        public string FacultyName { get; set; }
        public bool IsUrgent => Priority == "Urgent" || Priority == "High";
        public string PriorityColor =>
            Priority == "Urgent" ? "#dc2626" :
            Priority == "High"   ? "#d97706" :
            Priority == "Low"    ? "#64748b" : "#2563eb";
        public string PriorityBg =>
            Priority == "Urgent" ? "#fee2e2" :
            Priority == "High"   ? "#fef3c7" :
            Priority == "Low"    ? "#f1f5f9" : "#eff6ff";
    }

    public class ClassroomDetailsViewModel
    {
        public Classroom Classroom { get; set; }
        public List<ClassroomTask> Tasks { get; set; } = new List<ClassroomTask>();
        public List<EnrolledStudent> EnrolledStudents { get; set; } = new List<EnrolledStudent>();
        public List<ClassroomMaterialItem> Materials { get; set; } = new List<ClassroomMaterialItem>();
        public List<ClassroomAnnouncementItem> Announcements { get; set; } = new List<ClassroomAnnouncementItem>();
        public int PendingSubmissionCount { get; set; }
        public int ApprovedCount { get; set; }
    }

    public class EnrolledStudent
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string RollNo { get; set; }
        public string Email { get; set; }
        public int TasksSubmitted { get; set; }
        public int TotalTasks { get; set; }
        public string Status { get; set; }
        public string OnTimeRate { get => TotalTasks > 0 ? Math.Round((double)TasksSubmitted / TotalTasks * 100, 0) + "%" : "N/A"; }
    }
}
