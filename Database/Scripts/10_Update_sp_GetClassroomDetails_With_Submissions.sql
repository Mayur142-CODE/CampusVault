-- =============================================
-- Update sp_GetClassroomDetails to include submission status
-- This allows students to see which tasks are Pending, Submitted, Approved, or need Revision
-- =============================================

USE CampusVaultDB;
GO

ALTER PROCEDURE sp_GetClassroomDetails
    @ClassroomId INT,
    @StudentUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Classroom basic info
    SELECT 
        c.ClassroomId,
        c.CourseCode,
        c.CourseName,
        c.Department,
        c.Semester,
        c.AcademicYear,
        c.JoinCode,
        c.ThemeGradient,
        (u.FirstName + ' ' + u.LastName) AS FacultyName,
        u.Email AS FacultyEmail,
        fp.Designation AS FacultyDesignation,
        fp.OfficeLocation AS FacultyOffice
    FROM Classrooms c
    INNER JOIN Users u ON c.FacultyUserId = u.UserId
    LEFT JOIN FacultyProfiles fp ON u.UserId = fp.UserId
    WHERE c.ClassroomId = @ClassroomId;

    -- 2. Tasks with submission status for the specific student
    SELECT 
        t.TaskId,
        t.ClassroomId,
        t.Title,
        t.Description,
        t.TaskType,
        t.MaxScore,
        t.DueDate,
        t.Status,
        -- Submission details
        ts.Status AS SubmissionStatus,
        ts.SubmissionId,
        ts.SubmittedAt,
        ts.Score
    FROM Tasks t
    LEFT JOIN TaskSubmissions ts 
        ON t.TaskId = ts.TaskId 
        AND ts.StudentUserId = @StudentUserId
    WHERE t.ClassroomId = @ClassroomId
      AND t.Status = 'Active'
    ORDER BY t.DueDate ASC, t.CreatedAt DESC;

END;
GO

PRINT 'Stored procedure sp_GetClassroomDetails updated successfully with submission status tracking.';
