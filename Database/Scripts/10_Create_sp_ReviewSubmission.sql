-- ============================================================
-- Script 10: sp_ReviewSubmission — Approve or Request Revision
-- ============================================================

IF OBJECT_ID('sp_ReviewSubmission', 'P') IS NOT NULL
    DROP PROCEDURE sp_ReviewSubmission;
GO

CREATE PROCEDURE sp_ReviewSubmission
    @SubmissionId   INT,
    @Action         NVARCHAR(20),   -- 'Approve' or 'Revision'
    @Score          INT = NULL,
    @Feedback       NVARCHAR(MAX) = NULL,
    @ReviewedBy     INT,
    @Success        BIT OUTPUT,
    @Message        NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 0;

    IF NOT EXISTS (SELECT 1 FROM TaskSubmissions WHERE SubmissionId = @SubmissionId)
    BEGIN
        SET @Message = 'Submission not found.';
        RETURN;
    END

    IF @Action = 'Approve'
    BEGIN
        UPDATE TaskSubmissions
        SET Status     = 'Approved',
            Score      = @Score,
            Feedback   = @Feedback,
            ReviewedAt = GETDATE()
        WHERE SubmissionId = @SubmissionId;

        SET @Success = 1;
        SET @Message = 'Submission approved successfully.';
    END
    ELSE IF @Action = 'Revision'
    BEGIN
        UPDATE TaskSubmissions
        SET Status     = 'Needs Revision',
            Feedback   = @Feedback,
            ReviewedAt = GETDATE()
        WHERE SubmissionId = @SubmissionId;

        SET @Success = 1;
        SET @Message = 'Revision requested successfully.';
    END
    ELSE
    BEGIN
        SET @Message = 'Invalid action. Use Approve or Revision.';
    END
END
GO
