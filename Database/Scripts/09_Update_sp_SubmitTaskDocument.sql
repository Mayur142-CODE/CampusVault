-- ============================================================
-- Script 09: Update sp_SubmitTaskDocument for Cloudinary columns
-- ============================================================

IF OBJECT_ID('sp_SubmitTaskDocument', 'P') IS NOT NULL
    DROP PROCEDURE sp_SubmitTaskDocument;
GO

CREATE PROCEDURE sp_SubmitTaskDocument
    @TaskId         INT,
    @StudentUserId  INT,
    @FileName       NVARCHAR(255),
    @FileSize       NVARCHAR(50),
    @FilePath       NVARCHAR(1000),
    @CloudPublicId  NVARCHAR(500) = NULL,
    @Success        BIT OUTPUT,
    @Message        NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Validate task exists
    IF NOT EXISTS (SELECT 1 FROM Tasks WHERE TaskId = @TaskId)
    BEGIN
        SET @Success = 0;
        SET @Message = 'Task not found.';
        RETURN;
    END

    -- Validate student exists
    IF NOT EXISTS (
        SELECT 1 FROM Users u
        JOIN Roles r ON u.RoleId = r.RoleId
        WHERE u.UserId = @StudentUserId AND r.RoleName = 'Student'
    )
    BEGIN
        SET @Success = 0;
        SET @Message = 'Invalid student account.';
        RETURN;
    END

    -- Check for existing submission and update if found, otherwise insert
    IF EXISTS (SELECT 1 FROM TaskSubmissions WHERE TaskId = @TaskId AND StudentUserId = @StudentUserId)
    BEGIN
        UPDATE TaskSubmissions
        SET
            FileName       = @FileName,
            FileSize       = @FileSize,
            FilePath       = @FilePath,
            CloudPublicId  = @CloudPublicId,
            CloudUrl       = @FilePath,
            Status         = 'Pending',
            SubmittedAt    = GETDATE()
        WHERE TaskId = @TaskId AND StudentUserId = @StudentUserId;

        SET @Success = 1;
        SET @Message = 'Document resubmitted successfully.';
    END
    ELSE
    BEGIN
        INSERT INTO TaskSubmissions (TaskId, StudentUserId, FileName, FileSize, FilePath, CloudPublicId, CloudUrl, Status, SubmittedAt)
        VALUES (@TaskId, @StudentUserId, @FileName, @FileSize, @FilePath, @CloudPublicId, @FilePath, 'PendingReview', GETDATE());

        SET @Success = 1;
        SET @Message = 'Document submitted successfully.';
    END
END
GO
