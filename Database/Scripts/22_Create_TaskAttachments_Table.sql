USE [CampusVaultDB];
GO

-- ============================================================================
-- CAMPUSVAULT - CREATE TaskAttachments TABLE
-- Database: CampusVaultDB
-- Description: Stores faculty reference attachments (PDF, DOCX, ZIP) for tasks
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TaskAttachments')
BEGIN
    CREATE TABLE [dbo].[TaskAttachments] (
        [AttachmentId]      INT IDENTITY(1,1) NOT NULL,
        [TaskId]            INT NOT NULL,
        [FileName]          NVARCHAR(255) NOT NULL,
        [FileSize]          NVARCHAR(50) NOT NULL,
        [FileType]          NVARCHAR(50) NOT NULL,
        [FileUrl]           NVARCHAR(1000) NOT NULL,
        [CloudPublicId]     NVARCHAR(500) NULL,
        [FilePath]          NVARCHAR(500) NULL,
        [UploadedAt]        DATETIME2(7) NOT NULL CONSTRAINT [DF_TaskAttachments_UploadedAt] DEFAULT SYSUTCDATETIME(),
        [IsActive]          BIT NOT NULL CONSTRAINT [DF_TaskAttachments_IsActive] DEFAULT (1),
        CONSTRAINT [PK_TaskAttachments] PRIMARY KEY CLUSTERED ([AttachmentId] ASC),
        CONSTRAINT [FK_TaskAttachments_Task] FOREIGN KEY ([TaskId]) REFERENCES [dbo].[Tasks] ([TaskId]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_TaskAttachments_TaskId] ON [dbo].[TaskAttachments] ([TaskId] ASC);
    PRINT 'Created TaskAttachments table successfully.';
END
ELSE
BEGIN
    PRINT 'TaskAttachments table already exists.';
END
GO
