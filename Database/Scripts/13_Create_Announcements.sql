-- =============================================
-- Script 13: Announcements Table + Stored Procedures
-- =============================================
USE CampusVaultDB;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Announcements')
BEGIN
    CREATE TABLE [dbo].[Announcements] (
        [AnnouncementId]  INT IDENTITY(1,1) NOT NULL,
        [ClassroomId]     INT NULL,           -- NULL = global broadcast to all faculty classrooms
        [FacultyUserId]   INT NOT NULL,
        [Title]           NVARCHAR(255) NOT NULL,
        [Message]         NVARCHAR(MAX) NOT NULL,
        [Priority]        NVARCHAR(20) NOT NULL DEFAULT ('Normal'),  -- Urgent, High, Normal, Low
        [IsPinned]        BIT NOT NULL DEFAULT (0),
        [RequireAck]      BIT NOT NULL DEFAULT (0),
        [IsActive]        BIT NOT NULL DEFAULT (1),
        [PublishedAt]     DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Announcements] PRIMARY KEY CLUSTERED ([AnnouncementId] ASC),
        CONSTRAINT [FK_Announcements_Faculty]   FOREIGN KEY ([FacultyUserId]) REFERENCES [dbo].[Users]      ([UserId]),
        CONSTRAINT [FK_Announcements_Classroom] FOREIGN KEY ([ClassroomId])   REFERENCES [dbo].[Classrooms] ([ClassroomId]) ON DELETE CASCADE
    );
    PRINT 'Announcements table created.';
END
GO

-- sp_GetFacultyAnnouncements
IF OBJECT_ID('dbo.sp_GetFacultyAnnouncements','P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetFacultyAnnouncements;
GO
CREATE PROCEDURE dbo.sp_GetFacultyAnnouncements
    @FacultyUserId INT
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
    WHERE a.FacultyUserId = @FacultyUserId
      AND a.IsActive = 1
    ORDER BY a.IsPinned DESC, a.PublishedAt DESC;
END
GO

-- sp_PublishAnnouncement
IF OBJECT_ID('dbo.sp_PublishAnnouncement','P') IS NOT NULL
    DROP PROCEDURE dbo.sp_PublishAnnouncement;
GO
CREATE PROCEDURE dbo.sp_PublishAnnouncement
    @ClassroomId    INT,
    @FacultyUserId  INT,
    @Title          NVARCHAR(255),
    @Message        NVARCHAR(MAX),
    @Priority       NVARCHAR(20),
    @IsPinned       BIT,
    @RequireAck     BIT,
    @AnnouncementId INT OUTPUT,
    @Success        BIT OUTPUT,
    @Message_Out    NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO Announcements (ClassroomId, FacultyUserId, Title, Message, Priority, IsPinned, RequireAck)
        VALUES (NULLIF(@ClassroomId,0), @FacultyUserId, @Title, @Message, @Priority, @IsPinned, @RequireAck);
        SET @AnnouncementId = SCOPE_IDENTITY();
        SET @Success = 1;
        SET @Message_Out = 'Announcement published successfully.';
    END TRY
    BEGIN CATCH
        SET @AnnouncementId = 0;
        SET @Success = 0;
        SET @Message_Out = ERROR_MESSAGE();
    END CATCH
END
GO

-- sp_DeleteAnnouncement
IF OBJECT_ID('dbo.sp_DeleteAnnouncement','P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DeleteAnnouncement;
GO
CREATE PROCEDURE dbo.sp_DeleteAnnouncement
    @AnnouncementId INT,
    @FacultyUserId  INT,
    @Success        BIT OUTPUT,
    @Message_Out    NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM Announcements WHERE AnnouncementId = @AnnouncementId AND FacultyUserId = @FacultyUserId)
    BEGIN
        UPDATE Announcements SET IsActive = 0 WHERE AnnouncementId = @AnnouncementId;
        SET @Success = 1; SET @Message_Out = 'Deleted.';
    END
    ELSE
    BEGIN
        SET @Success = 0; SET @Message_Out = 'Not found or access denied.';
    END
END
GO

PRINT 'Script 13: Announcements pipeline created successfully.';
