-- ============================================================================
-- CAMPUSVAULT - SCRIPT 18: ALL FIXES & NEW FEATURES
-- Run this script in SSMS against CampusVaultDB
-- Covers: Student Materials, Announcements for Students, Leave Classroom,
--         Task Edit/Delete, Classroom Delete, Forgot Password Tokens,
--         Faculty Notifications fix, Admin User management improvements,
--         Profile picture upload, Pagination-ready SPs
-- ============================================================================
USE [CampusVaultDB];
GO

-- ============================================================================
-- 1. PASSWORD RESET TOKENS TABLE (for Forgot Password flow)
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PasswordResetTokens')
BEGIN
    CREATE TABLE [dbo].[PasswordResetTokens] (
        [TokenId]       INT IDENTITY(1,1) NOT NULL,
        [UserId]        INT NOT NULL,
        [Token]         NVARCHAR(128) NOT NULL,
        [ExpiresAt]     DATETIME2(7) NOT NULL,
        [IsUsed]        BIT NOT NULL DEFAULT (0),
        [CreatedAt]     DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [UsedAt]        DATETIME2(7) NULL,
        CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY CLUSTERED ([TokenId] ASC),
        CONSTRAINT [UQ_PasswordResetTokens_Token] UNIQUE NONCLUSTERED ([Token] ASC),
        CONSTRAINT [FK_PasswordResetTokens_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_PasswordResetTokens_Token] ON [dbo].[PasswordResetTokens] ([Token], [IsUsed], [ExpiresAt]);
    PRINT 'PasswordResetTokens table created.';
END
GO

-- SP: Create Password Reset Token
IF OBJECT_ID('dbo.sp_CreatePasswordResetToken', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_CreatePasswordResetToken;
GO
CREATE PROCEDURE dbo.sp_CreatePasswordResetToken
    @Email   NVARCHAR(150),
    @Token   NVARCHAR(128),
    @Success BIT OUTPUT,
    @Message NVARCHAR(255) OUTPUT,
    @UserId  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 0; SET @UserId = 0;
    DECLARE @Uid INT = (SELECT UserId FROM Users WHERE Email = @Email AND IsActive = 1);
    IF @Uid IS NULL
    BEGIN
        SET @Message = 'No active account found with this email address.';
        RETURN;
    END
    -- Invalidate previous tokens for this user
    UPDATE PasswordResetTokens SET IsUsed = 1 WHERE UserId = @Uid AND IsUsed = 0;
    -- Insert new token valid for 30 minutes
    INSERT INTO PasswordResetTokens (UserId, Token, ExpiresAt)
    VALUES (@Uid, @Token, DATEADD(MINUTE, 30, SYSUTCDATETIME()));
    SET @UserId  = @Uid;
    SET @Success = 1;
    SET @Message = 'Reset token created successfully.';
END
GO

-- SP: Verify and Reset Password
IF OBJECT_ID('dbo.sp_ResetPasswordWithToken', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_ResetPasswordWithToken;
GO
CREATE PROCEDURE dbo.sp_ResetPasswordWithToken
    @Token        NVARCHAR(128),
    @NewHash      NVARCHAR(500),
    @NewSalt      NVARCHAR(255),
    @Success      BIT OUTPUT,
    @Message      NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 0;
    DECLARE @TokenId INT, @Uid INT, @Expires DATETIME2(7), @Used BIT;
    SELECT @TokenId = TokenId, @Uid = UserId, @Expires = ExpiresAt, @Used = IsUsed
    FROM PasswordResetTokens WHERE Token = @Token;

    IF @TokenId IS NULL       BEGIN SET @Message = 'Invalid or expired reset link.'; RETURN; END
    IF @Used = 1              BEGIN SET @Message = 'This reset link has already been used.'; RETURN; END
    IF @Expires < SYSUTCDATETIME() BEGIN SET @Message = 'This reset link has expired. Please request a new one.'; RETURN; END

    BEGIN TRY
        BEGIN TRANSACTION;
        UPDATE Users SET PasswordHash = @NewHash, PasswordSalt = @NewSalt, UpdatedAt = SYSUTCDATETIME() WHERE UserId = @Uid;
        UPDATE PasswordResetTokens SET IsUsed = 1, UsedAt = SYSUTCDATETIME() WHERE TokenId = @TokenId;
        COMMIT TRANSACTION;
        SET @Success = 1;
        SET @Message = 'Password reset successfully. You can now sign in with your new password.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        SET @Message = ERROR_MESSAGE();
    END CATCH
END
GO

-- ============================================================================
-- 2. PROFILE PICTURE UPLOAD SP
-- ============================================================================
IF OBJECT_ID('dbo.sp_UpdateProfilePicture', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UpdateProfilePicture;
GO
CREATE PROCEDURE dbo.sp_UpdateProfilePicture
    @UserId          INT,
    @ProfilePicUrl   NVARCHAR(500),
    @Success         BIT OUTPUT,
    @Message         NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE Users SET ProfilePictureUrl = @ProfilePicUrl, UpdatedAt = SYSUTCDATETIME() WHERE UserId = @UserId;
    SET @Success = 1;
    SET @Message = 'Profile picture updated successfully.';
END
GO

-- ============================================================================
-- 3. LEAVE CLASSROOM for Students
-- ============================================================================
IF OBJECT_ID('dbo.sp_LeaveClassroom', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_LeaveClassroom;
GO
CREATE PROCEDURE dbo.sp_LeaveClassroom
    @StudentUserId  INT,
    @ClassroomId    INT,
    @Success        BIT OUTPUT,
    @Message        NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 0;
    IF NOT EXISTS (SELECT 1 FROM ClassroomEnrollments WHERE ClassroomId = @ClassroomId AND StudentUserId = @StudentUserId)
    BEGIN
        SET @Message = 'You are not enrolled in this classroom.';
        RETURN;
    END
    DELETE FROM ClassroomEnrollments WHERE ClassroomId = @ClassroomId AND StudentUserId = @StudentUserId;
    SET @Success = 1;
    SET @Message = 'Successfully left the classroom.';
END
GO

-- ============================================================================
-- 4. GET CLASSROOM MATERIALS FOR STUDENTS
-- ============================================================================
IF OBJECT_ID('dbo.sp_GetClassroomMaterials', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetClassroomMaterials;
GO
CREATE PROCEDURE dbo.sp_GetClassroomMaterials
    @ClassroomId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        m.MaterialId,
        m.ClassroomId,
        c.CourseCode,
        c.CourseName,
        m.Title,
        m.Description,
        m.FileName,
        m.FileSize,
        m.FileType,
        m.Category,
        ISNULL(m.CloudUrl, m.FilePath) AS FileUrl,
        m.UploadedAt,
        (u.FirstName + ' ' + u.LastName) AS UploaderName
    FROM CourseMaterials m
    JOIN Classrooms c ON m.ClassroomId = c.ClassroomId
    JOIN Users      u ON m.FacultyUserId = u.UserId
    WHERE m.ClassroomId = @ClassroomId
      AND m.IsActive = 1
    ORDER BY m.UploadedAt DESC;
END
GO

-- ============================================================================
-- 5. GET CLASSROOM ANNOUNCEMENTS FOR STUDENTS
-- ============================================================================
IF OBJECT_ID('dbo.sp_GetClassroomAnnouncements', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_GetClassroomAnnouncements;
GO
CREATE PROCEDURE dbo.sp_GetClassroomAnnouncements
    @ClassroomId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        a.AnnouncementId,
        a.ClassroomId,
        ISNULL(c.CourseCode, 'ALL') AS CourseCode,
        ISNULL(c.CourseName, 'All Classrooms') AS CourseName,
        a.Title,
        a.Message,
        a.Priority,
        a.IsPinned,
        a.RequireAck,
        a.PublishedAt,
        (u.FirstName + ' ' + u.LastName) AS FacultyName
    FROM Announcements a
    JOIN Users u ON a.FacultyUserId = u.UserId
    LEFT JOIN Classrooms c ON a.ClassroomId = c.ClassroomId
    WHERE a.ClassroomId = @ClassroomId
      AND a.IsActive = 1
    ORDER BY a.IsPinned DESC, a.PublishedAt DESC;
END
GO

-- ============================================================================
-- 6. UPDATE sp_GetClassroomDetails to include Announcements + Materials counts
-- ============================================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_GetClassroomDetails]
    @ClassroomId    INT,
    @StudentUserId  INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Result 1: Classroom Info
    SELECT
        c.[ClassroomId],
        c.[CourseCode],
        c.[CourseName],
        c.[Department],
        c.[Semester],
        c.[AcademicYear],
        c.[JoinCode],
        c.[ThemeGradient],
        u.[FirstName] + ' ' + u.[LastName] AS [FacultyName],
        u.[Email] AS [FacultyEmail],
        fp.[Designation] AS [FacultyDesignation],
        fp.[OfficeLocation] AS [FacultyOffice],
        (SELECT COUNT(*) FROM ClassroomEnrollments ce WHERE ce.ClassroomId = c.ClassroomId) AS [StudentCount],
        (SELECT COUNT(*) FROM CourseMaterials cm WHERE cm.ClassroomId = c.ClassroomId AND cm.IsActive = 1) AS [MaterialCount],
        (SELECT COUNT(*) FROM Announcements ann WHERE ann.ClassroomId = c.ClassroomId AND ann.IsActive = 1) AS [AnnouncementCount]
    FROM [dbo].[Classrooms] c
    INNER JOIN [dbo].[Users] u ON c.[FacultyUserId] = u.[UserId]
    LEFT JOIN [dbo].[FacultyProfiles] fp ON u.[UserId] = fp.[UserId]
    WHERE c.[ClassroomId] = @ClassroomId;

    -- Result 2: Tasks List with student submission status
    SELECT
        t.[TaskId],
        t.[ClassroomId],
        t.[Title],
        t.[Description],
        t.[TaskType],
        t.[MaxScore],
        t.[DueDate],
        t.[Status],
        s.[Status] AS [SubmissionStatus],
        s.[SubmissionId],
        s.[SubmittedAt],
        s.[Score]
    FROM [dbo].[Tasks] t
    LEFT JOIN [dbo].[TaskSubmissions] s ON s.[TaskId] = t.[TaskId] AND s.[StudentUserId] = @StudentUserId
    WHERE t.[ClassroomId] = @ClassroomId
    ORDER BY t.[DueDate] ASC;

    -- Result 3: Announcements
    SELECT
        a.AnnouncementId,
        a.Title,
        a.Message,
        a.Priority,
        a.IsPinned,
        a.PublishedAt,
        (u.FirstName + ' ' + u.LastName) AS FacultyName
    FROM Announcements a
    JOIN Users u ON a.FacultyUserId = u.UserId
    WHERE a.ClassroomId = @ClassroomId
      AND a.IsActive = 1
    ORDER BY a.IsPinned DESC, a.PublishedAt DESC;

    -- Result 4: Course Materials
    SELECT
        m.MaterialId,
        m.Title,
        m.Description,
        m.FileName,
        m.FileSize,
        m.FileType,
        m.Category,
        ISNULL(m.CloudUrl, m.FilePath) AS FileUrl,
        m.UploadedAt
    FROM CourseMaterials m
    WHERE m.ClassroomId = @ClassroomId
      AND m.IsActive = 1
    ORDER BY m.UploadedAt DESC;
END
GO

-- ============================================================================
-- 7. TASK EDIT stored procedure
-- ============================================================================
IF OBJECT_ID('dbo.sp_UpdateTask', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_UpdateTask;
GO
CREATE PROCEDURE dbo.sp_UpdateTask
    @TaskId         INT,
    @FacultyUserId  INT,
    @Title          NVARCHAR(200),
    @Description    NVARCHAR(MAX),
    @TaskType       NVARCHAR(50),
    @MaxScore       INT,
    @DueDate        DATETIME2(7),
    @Success        BIT OUTPUT,
    @Message        NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 0;
    -- Verify this task belongs to faculty's classroom
    IF NOT EXISTS (
        SELECT 1 FROM Tasks t
        JOIN Classrooms c ON t.ClassroomId = c.ClassroomId
        WHERE t.TaskId = @TaskId AND c.FacultyUserId = @FacultyUserId)
    BEGIN
        SET @Message = 'Task not found or access denied.';
        RETURN;
    END
    UPDATE Tasks SET
        Title       = @Title,
        Description = @Description,
        TaskType    = @TaskType,
        MaxScore    = @MaxScore,
        DueDate     = @DueDate
    WHERE TaskId = @TaskId;
    SET @Success = 1;
    SET @Message = 'Task updated successfully.';
END
GO

-- ============================================================================
-- 8. TASK DELETE stored procedure
-- ============================================================================
IF OBJECT_ID('dbo.sp_DeleteTask', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_DeleteTask;
GO
CREATE PROCEDURE dbo.sp_DeleteTask
    @TaskId         INT,
    @FacultyUserId  INT,
    @Success        BIT OUTPUT,
    @Message        NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 0;
    IF NOT EXISTS (
        SELECT 1 FROM Tasks t
        JOIN Classrooms c ON t.ClassroomId = c.ClassroomId
        WHERE t.TaskId = @TaskId AND c.FacultyUserId = @FacultyUserId)
    BEGIN
        SET @Message = 'Task not found or access denied.';
        RETURN;
    END
    DELETE FROM Tasks WHERE TaskId = @TaskId;
    SET @Success = 1;
    SET @Message = 'Task deleted successfully.';
END
GO

-- ============================================================================
-- 9. CLASSROOM ARCHIVE/DELETE stored procedure
-- ============================================================================
IF OBJECT_ID('dbo.sp_DeleteClassroom', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_DeleteClassroom;
GO
CREATE PROCEDURE dbo.sp_DeleteClassroom
    @ClassroomId    INT,
    @FacultyUserId  INT,
    @Success        BIT OUTPUT,
    @Message        NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 0;
    IF NOT EXISTS (SELECT 1 FROM Classrooms WHERE ClassroomId = @ClassroomId AND FacultyUserId = @FacultyUserId)
    BEGIN
        SET @Message = 'Classroom not found or access denied.';
        RETURN;
    END
    DELETE FROM Classrooms WHERE ClassroomId = @ClassroomId;
    SET @Success = 1;
    SET @Message = 'Classroom deleted successfully.';
END
GO

-- SP for Admin to delete classroom
IF OBJECT_ID('dbo.sp_AdminDeleteClassroom', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_AdminDeleteClassroom;
GO
CREATE PROCEDURE dbo.sp_AdminDeleteClassroom
    @ClassroomId    INT,
    @Success        BIT OUTPUT,
    @Message        NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 0;
    IF NOT EXISTS (SELECT 1 FROM Classrooms WHERE ClassroomId = @ClassroomId)
    BEGIN SET @Message = 'Classroom not found.'; RETURN; END
    DELETE FROM Classrooms WHERE ClassroomId = @ClassroomId;
    SET @Success = 1;
    SET @Message = 'Classroom deleted successfully.';
END
GO

-- ============================================================================
-- 10. ADMIN: Create User SP
-- ============================================================================
IF OBJECT_ID('dbo.sp_AdminCreateUser', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_AdminCreateUser;
GO
CREATE PROCEDURE dbo.sp_AdminCreateUser
    @RoleName       NVARCHAR(50),
    @Email          NVARCHAR(150),
    @PasswordHash   NVARCHAR(500),
    @PasswordSalt   NVARCHAR(255),
    @FirstName      NVARCHAR(100),
    @LastName       NVARCHAR(100),
    @PhoneNumber    NVARCHAR(20) = NULL,
    -- Student fields
    @EnrollmentNumber NVARCHAR(50) = NULL,
    @Department       NVARCHAR(100) = NULL,
    @Semester         INT = NULL,
    @Division         NVARCHAR(20) = NULL,
    -- Faculty fields
    @EmployeeCode   NVARCHAR(50) = NULL,
    @Designation    NVARCHAR(100) = NULL,
    @Success        BIT OUTPUT,
    @Message        NVARCHAR(255) OUTPUT,
    @NewUserId      INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 0; SET @NewUserId = 0;
    IF EXISTS (SELECT 1 FROM Users WHERE Email = @Email)
    BEGIN SET @Message = 'An account with this email already exists.'; RETURN; END

    DECLARE @RoleId INT = (SELECT RoleId FROM Roles WHERE RoleName = @RoleName);
    IF @RoleId IS NULL BEGIN SET @Message = 'Invalid role specified.'; RETURN; END

    BEGIN TRY
        BEGIN TRANSACTION;
        INSERT INTO Users (RoleId, Email, PasswordHash, PasswordSalt, FirstName, LastName, PhoneNumber, IsActive, IsEmailVerified)
        VALUES (@RoleId, @Email, @PasswordHash, @PasswordSalt, @FirstName, @LastName, @PhoneNumber, 1, 1);
        SET @NewUserId = SCOPE_IDENTITY();

        IF @RoleName = 'Student'
            INSERT INTO StudentProfiles (UserId, EnrollmentNumber, Department, Semester, Division)
            VALUES (@NewUserId, ISNULL(@EnrollmentNumber, 'ENR-' + CAST(@NewUserId AS NVARCHAR)), ISNULL(@Department, 'Computer Engineering'), ISNULL(@Semester, 1), @Division);
        ELSE IF @RoleName = 'Faculty'
            INSERT INTO FacultyProfiles (UserId, EmployeeCode, Department, Designation)
            VALUES (@NewUserId, ISNULL(@EmployeeCode, 'FAC-' + CAST(@NewUserId AS NVARCHAR)), ISNULL(@Department, 'Computer Engineering'), ISNULL(@Designation, 'Lecturer'));
        ELSE IF @RoleName = 'Admin'
            INSERT INTO AdminProfiles (UserId, Department, AccessLevel)
            VALUES (@NewUserId, 'Administration', 'Admin');

        COMMIT TRANSACTION;
        SET @Success = 1;
        SET @Message = @RoleName + ' account created successfully.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        SET @Message = ERROR_MESSAGE();
    END CATCH
END
GO

-- ============================================================================
-- 11. ADMIN: Delete User SP
-- ============================================================================
IF OBJECT_ID('dbo.sp_AdminDeleteUser', 'P') IS NOT NULL DROP PROCEDURE dbo.sp_AdminDeleteUser;
GO
CREATE PROCEDURE dbo.sp_AdminDeleteUser
    @UserId     INT,
    @Success    BIT OUTPUT,
    @Message    NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 0;
    IF NOT EXISTS (SELECT 1 FROM Users WHERE UserId = @UserId)
    BEGIN SET @Message = 'User not found.'; RETURN; END
    DELETE FROM Users WHERE UserId = @UserId;
    SET @Success = 1;
    SET @Message = 'User account deleted successfully.';
END
GO

-- ============================================================================
-- 12. Faculty Notifications - use real Notifications table  
--     (sp_GetUserNotifications already exists from Script 16)
-- ============================================================================

-- ============================================================================
-- 13. Add CloudUrl column to TaskSubmissions if missing
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('TaskSubmissions') AND name = 'CloudUrl')
BEGIN
    ALTER TABLE TaskSubmissions ADD CloudUrl NVARCHAR(1000) NULL;
    PRINT 'Added CloudUrl column to TaskSubmissions.';
END
GO

PRINT '============================================================';
PRINT 'Script 18 completed: All fixes and new features applied!';
PRINT 'The following features are now available:';
PRINT '  - Forgot Password (PasswordResetTokens table + SPs)';
PRINT '  - Profile Picture Upload (sp_UpdateProfilePicture)';
PRINT '  - Leave Classroom for Students (sp_LeaveClassroom)';
PRINT '  - Student-side Materials in Classroom (sp_GetClassroomMaterials)';
PRINT '  - Student-side Announcements in Classroom (sp_GetClassroomAnnouncements)';
PRINT '  - Updated sp_GetClassroomDetails with Materials + Announcements';
PRINT '  - Task Edit + Delete for Faculty (sp_UpdateTask, sp_DeleteTask)';
PRINT '  - Classroom Delete (sp_DeleteClassroom, sp_AdminDeleteClassroom)';
PRINT '  - Admin Create + Delete User (sp_AdminCreateUser, sp_AdminDeleteUser)';
PRINT '============================================================';
GO
