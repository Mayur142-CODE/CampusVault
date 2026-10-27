-- ============================================================================
-- CAMPUSVAULT ENTERPRISE DATABASE - SCRIPT 07: STUDENT DYNAMIC STORED PROCEDURES
-- ============================================================================

USE [CampusVaultDB];
GO

-- 1. STORED PROCEDURE: sp_GetStudentDashboardData
IF OBJECT_ID('dbo.sp_GetStudentDashboardData', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetStudentDashboardData;
GO

CREATE PROCEDURE dbo.sp_GetStudentDashboardData
    @StudentUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Student Identity & 4 Academic Scalar Stats
    DECLARE @EnrolledClassrooms INT = 0;
    DECLARE @PendingTasks INT = 0;
    DECLARE @UnderReview INT = 0;
    DECLARE @ApprovedDocs INT = 0;

    -- Total Enrolled Classrooms
    SELECT @EnrolledClassrooms = COUNT(1)
    FROM dbo.ClassroomEnrollments
    WHERE StudentUserId = @StudentUserId AND Status = 'Active';

    -- Pending Tasks (Tasks in enrolled classrooms that the student has not yet submitted)
    SELECT @PendingTasks = COUNT(1)
    FROM dbo.Tasks t
    INNER JOIN dbo.ClassroomEnrollments ce ON t.ClassroomId = ce.ClassroomId
    WHERE ce.StudentUserId = @StudentUserId 
      AND ce.Status = 'Active'
      AND t.Status = 'Active'
      AND NOT EXISTS (
          SELECT 1 FROM dbo.TaskSubmissions ts 
          WHERE ts.TaskId = t.TaskId AND ts.StudentUserId = @StudentUserId
      );

    -- Under Review Submissions
    SELECT @UnderReview = COUNT(1)
    FROM dbo.TaskSubmissions ts
    WHERE ts.StudentUserId = @StudentUserId 
      AND (ts.Status = 'PendingReview' OR ts.Status = 'Under Review' OR ts.Status = 'Pending');

    -- Approved / Graded Documents
    SELECT @ApprovedDocs = COUNT(1)
    FROM dbo.TaskSubmissions ts
    WHERE ts.StudentUserId = @StudentUserId 
      AND (ts.Status = 'Reviewed' OR ts.Status = 'Approved' OR ts.Status = 'Graded');

    -- Return Stats & Student Details (Result 1)
    SELECT 
        u.UserId,
        u.FirstName,
        u.LastName,
        (u.FirstName + ' ' + u.LastName) AS FullName,
        u.Email,
        sp.EnrollmentNumber,
        sp.Department,
        sp.Semester,
        sp.Division,
        @EnrolledClassrooms AS EnrolledClassrooms,
        @PendingTasks AS PendingTasks,
        @UnderReview AS UnderReview,
        @ApprovedDocs AS ApprovedDocs
    FROM dbo.Users u
    LEFT JOIN dbo.StudentProfiles sp ON u.UserId = sp.UserId
    WHERE u.UserId = @StudentUserId;

    -- Result 2: Due Soon Tasks (Top 5 Tasks for enrolled classrooms)
    SELECT TOP 5
        t.TaskId,
        t.ClassroomId,
        t.Title,
        t.TaskType,
        t.MaxScore,
        t.DueDate,
        c.CourseCode,
        c.CourseName,
        (fu.FirstName + ' ' + fu.LastName) AS FacultyName,
        ISNULL(ts.Status, 'Pending') AS SubmissionStatus,
        ts.SubmittedAt,
        ts.Score
    FROM dbo.Tasks t
    INNER JOIN dbo.Classrooms c ON t.ClassroomId = c.ClassroomId
    INNER JOIN dbo.ClassroomEnrollments ce ON c.ClassroomId = ce.ClassroomId
    LEFT JOIN dbo.Users fu ON c.FacultyUserId = fu.UserId
    LEFT JOIN dbo.TaskSubmissions ts ON t.TaskId = ts.TaskId AND ts.StudentUserId = @StudentUserId
    WHERE ce.StudentUserId = @StudentUserId AND ce.Status = 'Active' AND t.Status = 'Active'
    ORDER BY 
        CASE WHEN ts.Status IS NULL THEN 0 ELSE 1 END,
        t.DueDate ASC;

    -- Result 3: Enrolled Classrooms (Top 6)
    SELECT TOP 6
        c.ClassroomId,
        c.CourseCode,
        c.CourseName,
        c.Department,
        c.Semester,
        c.AcademicYear,
        c.JoinCode,
        c.ThemeGradient,
        (fu.FirstName + ' ' + fu.LastName) AS FacultyName,
        ISNULL(fp.Designation, 'Faculty Mentor') AS FacultyDesignation,
        (SELECT COUNT(1) FROM dbo.Tasks t WHERE t.ClassroomId = c.ClassroomId AND t.Status = 'Active') AS ActiveTaskCount,
        (SELECT COUNT(1) FROM dbo.ClassroomEnrollments ce2 WHERE ce2.ClassroomId = c.ClassroomId AND ce2.Status = 'Active') AS StudentCount
    FROM dbo.Classrooms c
    INNER JOIN dbo.ClassroomEnrollments ce ON c.ClassroomId = ce.ClassroomId
    LEFT JOIN dbo.Users fu ON c.FacultyUserId = fu.UserId
    LEFT JOIN dbo.FacultyProfiles fp ON fu.UserId = fp.UserId
    WHERE ce.StudentUserId = @StudentUserId AND ce.Status = 'Active'
    ORDER BY c.CourseName ASC;
END;
GO

-- 2. STORED PROCEDURE: sp_GetStudentTasks
IF OBJECT_ID('dbo.sp_GetStudentTasks', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetStudentTasks;
GO

CREATE PROCEDURE dbo.sp_GetStudentTasks
    @StudentUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        t.TaskId,
        t.ClassroomId,
        t.Title,
        t.Description,
        t.TaskType,
        t.MaxScore,
        t.DueDate,
        t.Status AS TaskStatus,
        c.CourseCode,
        c.CourseName,
        c.ThemeGradient,
        (fu.FirstName + ' ' + fu.LastName) AS FacultyName,
        ISNULL(ts.Status, 'Pending') AS SubmissionStatus,
        ts.SubmissionId,
        ts.FileName,
        ts.FileSize,
        ts.SubmittedAt,
        ts.Score,
        ts.Feedback
    FROM dbo.Tasks t
    INNER JOIN dbo.Classrooms c ON t.ClassroomId = c.ClassroomId
    INNER JOIN dbo.ClassroomEnrollments ce ON c.ClassroomId = ce.ClassroomId
    LEFT JOIN dbo.Users fu ON c.FacultyUserId = fu.UserId
    LEFT JOIN dbo.TaskSubmissions ts ON t.TaskId = ts.TaskId AND ts.StudentUserId = @StudentUserId
    WHERE ce.StudentUserId = @StudentUserId AND ce.Status = 'Active'
    ORDER BY 
        CASE WHEN ts.Status IS NULL THEN 0 ELSE 1 END,
        t.DueDate ASC;
END;
GO

-- 3. STORED PROCEDURE: sp_GetStudentDocuments
IF OBJECT_ID('dbo.sp_GetStudentDocuments', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetStudentDocuments;
GO

CREATE PROCEDURE dbo.sp_GetStudentDocuments
    @StudentUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        ts.SubmissionId,
        ts.TaskId,
        t.Title AS TaskTitle,
        c.CourseCode,
        c.CourseName,
        (fu.FirstName + ' ' + fu.LastName) AS FacultyName,
        ts.FileName,
        ts.FileSize,
        ts.Status,
        ts.Score,
        t.MaxScore,
        ts.Feedback,
        ts.SubmittedAt,
        ts.ReviewedAt
    FROM dbo.TaskSubmissions ts
    INNER JOIN dbo.Tasks t ON ts.TaskId = t.TaskId
    INNER JOIN dbo.Classrooms c ON t.ClassroomId = c.ClassroomId
    LEFT JOIN dbo.Users fu ON c.FacultyUserId = fu.UserId
    WHERE ts.StudentUserId = @StudentUserId
    ORDER BY ts.SubmittedAt DESC;
END;
GO

-- 4. STORED PROCEDURE: sp_SubmitTaskDocument
IF OBJECT_ID('dbo.sp_SubmitTaskDocument', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_SubmitTaskDocument;
GO

CREATE PROCEDURE dbo.sp_SubmitTaskDocument
    @TaskId INT,
    @StudentUserId INT,
    @FileName NVARCHAR(255),
    @FileSize NVARCHAR(50),
    @FilePath NVARCHAR(500),
    @Success BIT OUTPUT,
    @Message NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF EXISTS (SELECT 1 FROM dbo.TaskSubmissions WHERE TaskId = @TaskId AND StudentUserId = @StudentUserId)
        BEGIN
            UPDATE dbo.TaskSubmissions
            SET FileName = @FileName,
                FileSize = @FileSize,
                FilePath = @FilePath,
                Status = 'PendingReview',
                SubmittedAt = SYSUTCDATETIME()
            WHERE TaskId = @TaskId AND StudentUserId = @StudentUserId;

            SET @Success = 1;
            SET @Message = 'Submission document updated successfully.';
        END
        ELSE
        BEGIN
            INSERT INTO dbo.TaskSubmissions (TaskId, StudentUserId, SubmissionTitle, FileName, FilePath, FileSize, Status, SubmittedAt)
            VALUES (@TaskId, @StudentUserId, @FileName, @FileName, @FilePath, @FileSize, 'PendingReview', SYSUTCDATETIME());

            SET @Success = 1;
            SET @Message = 'Document submitted successfully.';
        END
    END TRY
    BEGIN CATCH
        SET @Success = 0;
        SET @Message = ERROR_MESSAGE();
    END CATCH
END;
GO
