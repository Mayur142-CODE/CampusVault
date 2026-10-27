using System;
using System.Collections.Generic;

namespace CampusVault.Models.Faculty
{
    public class FacultyDashboardViewModel
    {
        public int ActiveClassroomCount { get; set; }
        public int TotalStudentCount { get; set; }
        public int ActiveTaskCount { get; set; }
        public int PendingReviewCount { get; set; }

        public List<FacultyClassroomItem> Classrooms { get; set; } = new List<FacultyClassroomItem>();
        public List<FacultyTaskItem> RecentTasks { get; set; } = new List<FacultyTaskItem>();
        public List<FacultySubmissionItem> PendingSubmissions { get; set; } = new List<FacultySubmissionItem>();

        // Submission overview stats — populated from SP result set 5
        public int TotalSubmissions    { get; set; }
        public int SubmittedCount      { get; set; }
        public int UnderReviewCount    { get; set; }
        public int RevisionCount       { get; set; }
        public int ApprovedCount       { get; set; }

        // Computed percentages
        public double SubmittedPct   => TotalSubmissions > 0 ? Math.Round((double)SubmittedCount   / TotalSubmissions * 100, 1) : 0;
        public double UnderReviewPct => TotalSubmissions > 0 ? Math.Round((double)UnderReviewCount / TotalSubmissions * 100, 1) : 0;
        public double RevisionPct    => TotalSubmissions > 0 ? Math.Round((double)RevisionCount    / TotalSubmissions * 100, 1) : 0;
        public double ApprovedPct    => TotalSubmissions > 0 ? Math.Round((double)ApprovedCount    / TotalSubmissions * 100, 1) : 0;
        public int    EvaluatedCount => UnderReviewCount + ApprovedCount + RevisionCount;
        public double EvaluatedPct   => TotalSubmissions > 0 ? Math.Round((double)EvaluatedCount   / TotalSubmissions * 100, 1) : 0;
    }

    public class FacultyClassroomItem
    {
        public int ClassroomId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string Department { get; set; }
        public int Semester { get; set; }
        public string AcademicYear { get; set; }
        public string JoinCode { get; set; }
        public string ThemeGradient { get; set; }
        public int StudentCount { get; set; }
        public int TaskCount { get; set; }
        public int PendingReviewCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Division { get; set; }
        public string Room { get; set; }
        public string Schedule { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class FacultyTaskItem
    {
        public int TaskId { get; set; }
        public int ClassroomId { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string TaskType { get; set; }
        public int MaxScore { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; }
        public int SubmissionCount { get; set; }
        public int PendingReviewCount { get; set; }
    }

    public class FacultySubmissionItem
    {
        public int SubmissionId { get; set; }
        public int TaskId { get; set; }
        public string TaskTitle { get; set; }
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string StudentName { get; set; }
        public string RollNo { get; set; }
        public string ClassroomName { get => !string.IsNullOrEmpty(CourseName) ? CourseName : CourseCode; set => CourseName = value; }
        public string FileName { get; set; }
        public string FileSize { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string SubmittedDate { get => SubmittedAt != default(DateTime) ? SubmittedAt.ToString("dd MMM yyyy, hh:mm tt") : "Recent"; }
        public string Status { get; set; }
        public string Version { get; set; }
        public string Feedback { get; set; }
        public string CloudUrl { get; set; }
        public string FileHash { get; set; }
    }

    public class CreateClassroomModel
    {
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public string Department { get; set; }
        public int Semester { get; set; }
        public string AcademicYear { get; set; }
        public string JoinCode { get; set; }
        public string ThemeGradient { get; set; }
    }

    public class ReviewSubmissionViewModel
    {
        public int    SubmissionId  { get; set; }
        public int    TaskId        { get; set; }
        public int    StudentUserId { get; set; }
        public string TaskTitle     { get; set; }
        public string CourseCode    { get; set; }
        public string CourseName    { get; set; }
        public string StudentName   { get; set; }
        public string RollNo        { get; set; }
        public string FileName      { get; set; }
        public string FileSize      { get; set; }
        public string CloudUrl      { get; set; }
        public string Status        { get; set; }
        public int?   Score         { get; set; }
        public int    MaxScore      { get; set; }
        public string Feedback      { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime DueDate     { get; set; }
        public string FacultyName   { get; set; }
    }

    public class TaskAttachmentItem
    {
        public int AttachmentId { get; set; }
        public int TaskId { get; set; }
        public string FileName { get; set; }
        public string FileSize { get; set; }
        public string FileType { get; set; }
        public string FileUrl { get; set; }
        public string CloudPublicId { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class CreateTaskModel
    {
        public int TaskId { get; set; }
        public int ClassroomId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string TaskType { get; set; }
        public int MaxScore { get; set; } = 20;
        public DateTime DueDate { get; set; }
        public List<TaskAttachmentItem> ExistingAttachments { get; set; } = new List<TaskAttachmentItem>();
    }

    public class FacultyProfileViewModel
    {
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string EmployeeCode { get; set; }
        public string Department { get; set; }
        public string Designation { get; set; }
        public string Qualification { get; set; }
        public string OfficeLocation { get; set; }
        public string ProfilePictureUrl { get; set; }
    }
}

namespace CampusVault.Models.Faculty
{
    public class CourseMaterialItem
    {
        public int      MaterialId    { get; set; }
        public int      ClassroomId   { get; set; }
        public int      FacultyUserId { get; set; }
        public string   CourseCode    { get; set; }
        public string   CourseName    { get; set; }
        public string   Title         { get; set; }
        public string   Description   { get; set; }
        public string   FileName      { get; set; }
        public string   FileSize      { get; set; }
        public string   FileType      { get; set; }   // PDF, PPTX, DOCX, ZIP, Other
        public string   Category      { get; set; }   // Notes, PDF, Presentation, Reference, Other
        public string   FileUrl       { get; set; }   // Cloudinary or file path
        public string   CloudPublicId { get; set; }
        public string   ExternalUrl   { get; set; }
        public DateTime UploadedAt    { get; set; }
        public DateTime? UpdatedAt    { get; set; }
        public string   UploaderName  { get; set; }

        // Computed
        public string DisplayType =>
            !string.IsNullOrEmpty(ExternalUrl) ? "External Resource" :
            FileType?.ToUpper() == "PPTX" || FileType?.ToUpper() == "PPT" ? "Presentation (PPTX)"  :
            FileType?.ToUpper() == "DOCX" || FileType?.ToUpper() == "DOC" ? "Word Document (DOCX)"  :
            FileType?.ToUpper() == "XLSX" || FileType?.ToUpper() == "XLS" ? "Spreadsheet (XLSX)"    :
            FileType?.ToUpper() == "ZIP"  || FileType?.ToUpper() == "RAR" ? "Archive (ZIP)"          :
            FileType?.ToUpper() == "TXT"  ? "Text File" :
            FileType?.ToUpper() == "PNG" || FileType?.ToUpper() == "JPG" || FileType?.ToUpper() == "JPEG" ? "Image File" :
            "PDF Document";
    }
}

namespace CampusVault.Models.Faculty
{
    public class AnnouncementItem
    {
        public int      AnnouncementId { get; set; }
        public int?     ClassroomId    { get; set; }
        public string   CourseCode     { get; set; }
        public string   CourseName     { get; set; }
        public string   Title          { get; set; }
        public string   Message        { get; set; }
        public string   Priority       { get; set; }  // Urgent, High, Normal, Low
        public bool     IsPinned       { get; set; }
        public bool     RequireAck     { get; set; }
        public DateTime PublishedAt    { get; set; }
        public string   FacultyName    { get; set; }

        // Computed
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

    public class FacultyAnalyticsViewModel
    {
        // A. Overview Cards
        public int TotalStudents { get; set; }
        public int TotalActiveClassrooms { get; set; }
        public int TotalAssignments { get; set; }
        public int PendingSubmissions { get; set; }
        public int AwaitingReviewCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RevisionCount { get; set; }
        public int OverdueAssignmentsCount { get; set; }

        // Selected Classroom Drilldown
        public int? SelectedClassroomId { get; set; }
        public string SelectedClassroomName { get; set; }
        public List<FacultyClassroomItem> Classrooms { get; set; } = new List<FacultyClassroomItem>();

        // B. Submission & Assignment Reports
        public List<FacultyTaskAnalyticsItem> TaskReports { get; set; } = new List<FacultyTaskAnalyticsItem>();

        // D. Student Activity History
        public List<FacultyStudentActivityItem> RecentActivity { get; set; } = new List<FacultyStudentActivityItem>();
    }

    public class FacultyTaskAnalyticsItem
    {
        public int TaskId { get; set; }
        public int ClassroomId { get; set; }
        public string ClassroomName { get; set; }
        public string CourseCode { get; set; }
        public string Title { get; set; }
        public string TaskType { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsOverdue => DateTime.UtcNow > DueDate && (Status == "Active" || Status == "Open");
        public string Status { get; set; }
        public int TotalEnrolled { get; set; }
        public int SubmittedCount { get; set; }
        public int PendingReviewCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RevisionCount { get; set; }
        public int MissingCount => Math.Max(0, TotalEnrolled - SubmittedCount);
        public double CompletionRate => TotalEnrolled > 0 ? Math.Round((double)SubmittedCount / TotalEnrolled * 100, 1) : 0;
    }

    public class FacultyStudentActivityItem
    {
        public int SubmissionId { get; set; }
        public string StudentName { get; set; }
        public string RollNo { get; set; }
        public string TaskTitle { get; set; }
        public string ClassroomName { get; set; }
        public string ActivityType { get; set; }
        public string Status { get; set; }
        public DateTime Timestamp { get; set; }
        public string FileName { get; set; }
    }
}
