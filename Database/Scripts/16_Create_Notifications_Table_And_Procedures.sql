-- =============================================
-- Script 16: Create Notifications Table & Procedures
-- =============================================
USE CampusVaultDB;
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications')
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [NotificationId] INT IDENTITY(1,1) NOT NULL,
        [UserId]         INT NOT NULL,
        [Category]       NVARCHAR(50) NOT NULL DEFAULT ('general'), -- 'tasks', 'submissions', 'revisions', 'announcements', 'security'
        [Title]          NVARCHAR(255) NOT NULL,
        [Message]        NVARCHAR(1000) NOT NULL,
        [BadgeText]      NVARCHAR(50) NULL,
        [BadgeType]      NVARCHAR(50) NULL DEFAULT ('info'),        -- 'success', 'warning', 'danger', 'info', 'task'
        [ContextInfo]    NVARCHAR(255) NULL,
        [ActionUrl]      NVARCHAR(500) NULL,
        [ActionText]     NVARCHAR(100) NULL,
        [IsRead]         BIT NOT NULL DEFAULT (0),
        [CreatedAt]      DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([NotificationId] ASC),
        CONSTRAINT [FK_Notifications_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX [IX_Notifications_UserId] ON [dbo].[Notifications] ([UserId], [IsRead], [CreatedAt] DESC);
    PRINT 'Notifications table created successfully.';
END
ELSE
BEGIN
    PRINT 'Notifications table already exists.';
END
GO

-- Stored Procedure: GetUserNotifications
IF OBJECT_ID('dbo.sp_GetUserNotifications', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetUserNotifications;
GO

CREATE PROCEDURE dbo.sp_GetUserNotifications
    @UserId INT,
    @Top    INT = 50
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@Top)
        [NotificationId],
        [UserId],
        [Category],
        [Title],
        [Message],
        [BadgeText],
        [BadgeType],
        [ContextInfo],
        [ActionUrl],
        [ActionText],
        [IsRead],
        [CreatedAt]
    FROM [dbo].[Notifications]
    WHERE [UserId] = @UserId
    ORDER BY [CreatedAt] DESC;
END
GO

-- Stored Procedure: MarkNotificationAsRead
IF OBJECT_ID('dbo.sp_MarkNotificationAsRead', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_MarkNotificationAsRead;
GO

CREATE PROCEDURE dbo.sp_MarkNotificationAsRead
    @NotificationId INT,
    @UserId         INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Notifications]
    SET [IsRead] = 1
    WHERE [NotificationId] = @NotificationId AND [UserId] = @UserId;
END
GO

-- Stored Procedure: MarkAllNotificationsAsRead
IF OBJECT_ID('dbo.sp_MarkAllNotificationsAsRead', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_MarkAllNotificationsAsRead;
GO

CREATE PROCEDURE dbo.sp_MarkAllNotificationsAsRead
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Notifications]
    SET [IsRead] = 1
    WHERE [UserId] = @UserId AND [IsRead] = 0;
END
GO

-- Stored Procedure: CreateNotification
IF OBJECT_ID('dbo.sp_CreateNotification', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_CreateNotification;
GO

CREATE PROCEDURE dbo.sp_CreateNotification
    @UserId      INT,
    @Category    NVARCHAR(50),
    @Title       NVARCHAR(255),
    @Message     NVARCHAR(1000),
    @BadgeText   NVARCHAR(50) = NULL,
    @BadgeType   NVARCHAR(50) = 'info',
    @ContextInfo NVARCHAR(255) = NULL,
    @ActionUrl   NVARCHAR(500) = NULL,
    @ActionText  NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO [dbo].[Notifications] (
        [UserId], [Category], [Title], [Message], [BadgeText], [BadgeType], [ContextInfo], [ActionUrl], [ActionText], [IsRead], [CreatedAt]
    )
    VALUES (
        @UserId, @Category, @Title, @Message, @BadgeText, @BadgeType, @ContextInfo, @ActionUrl, @ActionText, 0, SYSUTCDATETIME()
    );
END
GO
