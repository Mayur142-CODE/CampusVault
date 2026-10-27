-- Script 11: Update sp_GetStudentDocuments to include FilePath/CloudUrl
IF OBJECT_ID('sp_GetStudentDocuments','P') IS NOT NULL
    DROP PROCEDURE sp_GetStudentDocuments;
GO

CREATE PROCEDURE dbo.sp_GetStudentDocuments
    @StudentUserId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        ts.SubmissionId,
        ts.TaskId,
        t.Title             AS TaskTitle,
        c.CourseCode,
        c.CourseName,
        (fu.FirstName + ' ' + fu.LastName) AS FacultyName,
        ts.FileName,
        ts.FileSize,
        ISNULL(ts.CloudUrl, ts.FilePath) AS FilePath,
        ts.Status,
        ts.Score,
        t.MaxScore,
        ts.Feedback,
        ts.SubmittedAt,
        ts.ReviewedAt
    FROM TaskSubmissions ts
    JOIN Tasks t             ON ts.TaskId        = t.TaskId
    JOIN Classrooms c        ON t.ClassroomId    = c.ClassroomId
    JOIN Users fu            ON c.FacultyUserId  = fu.UserId
    WHERE ts.StudentUserId   = @StudentUserId
    ORDER BY ts.SubmittedAt DESC;
END
GO
