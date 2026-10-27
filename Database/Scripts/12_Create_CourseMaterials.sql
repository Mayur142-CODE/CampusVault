-- =============================================
-- Script 12: Course Materials Table + Stored Procedures
-- =============================================
USE CampusVaultDB;
GO

-- 1. CREATE TABLE
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CourseMaterials')
BEGIN
    CREATE TABLE [dbo].[CourseMaterials] (
        [MaterialId]    INT IDENTITY(1,1) NOT NULL,
        [ClassroomId]   INT NOT NULL,
        [FacultyUserId] INT NOT NULL,
        [Title]         NVARCHAR(255) NOT NULL,
        [Description]   NVARCHAR(500) NULL,
        [FileName]      NVARCHAR(255) NOT NULL,
        [FileSize]      NVARCHAR(50)  NULL,
        [FileType]      NVARCHAR(50)  NOT NULL DEFAULT ('PDF'),   -- PDF, PPTX, DOCX, ZIP, Other
        [Category]      NVARCHAR(50)  NOT NULL DEFAULT ('Notes'), -- Notes, PDF, Presentation, Reference, Other
        [CloudUrl]      NVARCHAR(1000) NULL,
        [FilePath]      NVARCHAR(500)  NULL,
        [CloudPublicId] NVARCHAR(500)  NULL,
        [IsActive]      BIT NOT NULL DEFAULT (1),
        [UploadedAt]    DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_CourseMaterials] PRIMARY KEY CLUSTERED ([MaterialId] ASC),
        CONSTRAINT [FK_CourseMaterials_Classroom] FOREIGN KEY ([ClassroomId])   REFERENCES [dbo].[Classrooms] ([ClassroomId]) ON DELETE CASCADE,
        CONSTRAINT [FK_CourseMaterials_Faculty]   FOREIGN KEY ([FacultyUserId]) REFERENCES [dbo].[Users]      ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_CourseMaterials_Classroom] ON [dbo].[CourseMaterials] ([ClassroomId]);
    CREATE NONCLUSTERED INDEX [IX_CourseMaterials_Faculty]   ON [dbo].[CourseMaterials] ([FacultyUserId]);
    PRINT 'CourseMaterials table created.';
END
GO

-- 2. sp_GetFacultyMaterials
IF OBJECT_ID('dbo.sp_GetFacultyMaterials','P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetFacultyMaterials;
GO
CREATE PROCEDURE dbo.sp_GetFacultyMaterials
    @FacultyUserId INT
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
    JOIN Classrooms c ON m.ClassroomId   = c.ClassroomId
    JOIN Users      u ON m.FacultyUserId = u.UserId
    WHERE m.FacultyUserId = @FacultyUserId
      AND m.IsActive = 1
    ORDER BY m.UploadedAt DESC;
END
GO

-- 3. sp_UploadCourseMaterial
IF OBJECT_ID('dbo.sp_UploadCourseMaterial','P') IS NOT NULL
    DROP PROCEDURE dbo.sp_UploadCourseMaterial;
GO
CREATE PROCEDURE dbo.sp_UploadCourseMaterial
    @ClassroomId    INT,
    @FacultyUserId  INT,
    @Title          NVARCHAR(255),
    @Description    NVARCHAR(500),
    @FileName       NVARCHAR(255),
    @FileSize       NVARCHAR(50),
    @FileType       NVARCHAR(50),
    @Category       NVARCHAR(50),
    @CloudUrl       NVARCHAR(1000),
    @CloudPublicId  NVARCHAR(500),
    @MaterialId     INT OUTPUT,
    @Success        BIT OUTPUT,
    @Message        NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        INSERT INTO CourseMaterials
            (ClassroomId, FacultyUserId, Title, Description, FileName, FileSize, FileType, Category, CloudUrl, CloudPublicId)
        VALUES
            (@ClassroomId, @FacultyUserId, @Title, @Description, @FileName, @FileSize, @FileType, @Category, @CloudUrl, @CloudPublicId);

        SET @MaterialId = SCOPE_IDENTITY();
        SET @Success    = 1;
        SET @Message    = 'Material uploaded successfully.';
    END TRY
    BEGIN CATCH
        SET @MaterialId = 0;
        SET @Success    = 0;
        SET @Message    = ERROR_MESSAGE();
    END CATCH
END
GO

-- 4. sp_DeleteCourseMaterial
IF OBJECT_ID('dbo.sp_DeleteCourseMaterial','P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DeleteCourseMaterial;
GO
CREATE PROCEDURE dbo.sp_DeleteCourseMaterial
    @MaterialId     INT,
    @FacultyUserId  INT,
    @Success        BIT OUTPUT,
    @Message        NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM CourseMaterials WHERE MaterialId = @MaterialId AND FacultyUserId = @FacultyUserId)
    BEGIN
        UPDATE CourseMaterials SET IsActive = 0 WHERE MaterialId = @MaterialId;
        SET @Success = 1;
        SET @Message = 'Material deleted.';
    END
    ELSE
    BEGIN
        SET @Success = 0;
        SET @Message = 'Material not found or access denied.';
    END
END
GO

PRINT 'Script 12 completed: CourseMaterials table and stored procedures created.';
