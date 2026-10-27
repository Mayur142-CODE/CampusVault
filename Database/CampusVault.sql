-- ============================================================================
-- CAMPUSVAULT â€” University Document Management System
-- Master Database Initialization & Setup Script
-- Target Engine : Microsoft SQL Server 2016+ / Express / LocalDB / Azure SQL
-- Database Name : CampusVaultDB
-- Generated     : 2026-10-27 11:42:31
-- ============================================================================

PRINT 'Starting CampusVaultDB installation...';
GO

-- 1. DATABASE CREATION
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'CampusVaultDB')
BEGIN
    PRINT 'Creating database [CampusVaultDB]...';
    CREATE DATABASE [CampusVaultDB];
END
GO

USE [CampusVaultDB];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- 2. CORE AUTH & IDENTITY TABLES
-- ============================================================================

-- Roles Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Roles')
BEGIN
    CREATE TABLE [dbo].[Roles] (
        [RoleId]      INT IDENTITY(1,1) NOT NULL,
        [RoleName]    NVARCHAR(50) NOT NULL,
        [Description] NVARCHAR(255) NULL,
        [CreatedAt]   DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([RoleId] ASC),
        CONSTRAINT [UQ_Roles_RoleName] UNIQUE NONCLUSTERED ([RoleName] ASC)
    );
    PRINT 'Created table [Roles].';
END
GO

-- Users Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE [dbo].[Users] (
        [UserId]            INT IDENTITY(1,1) NOT NULL,
        [RoleId]            INT NOT NULL,
        [Email]             NVARCHAR(150) NOT NULL,
        [PasswordHash]      NVARCHAR(500) NOT NULL,
        [PasswordSalt]      NVARCHAR(255) NOT NULL,
        [FirstName]         NVARCHAR(100) NOT NULL,
        [LastName]          NVARCHAR(100) NOT NULL,
        [PhoneNumber]       NVARCHAR(20) NULL,
        [ProfilePictureUrl] NVARCHAR(500) NULL,
        [IsActive]          BIT NOT NULL DEFAULT 1,
        [IsEmailVerified]   BIT NOT NULL DEFAULT 0,
        [LastLoginAt]       DATETIME2(7) NULL,
        [CreatedAt]         DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]         DATETIME2(7) NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([UserId] ASC),
        CONSTRAINT [UQ_Users_Email] UNIQUE NONCLUSTERED ([Email] ASC),
        CONSTRAINT [FK_Users_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([RoleId])
    );
    CREATE NONCLUSTERED INDEX [IX_Users_Email] ON [dbo].[Users] ([Email]);
    CREATE NONCLUSTERED INDEX [IX_Users_RoleId] ON [dbo].[Users] ([RoleId]);
    PRINT 'Created table [Users].';
END
GO

-- UserAuthLogs Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserAuthLogs')
BEGIN
    CREATE TABLE [dbo].[UserAuthLogs] (
        [LogId]     BIGINT IDENTITY(1,1) NOT NULL,
        [UserId]    INT NULL,
        [Action]    NVARCHAR(50) NOT NULL,
        [IpAddress] NVARCHAR(50) NULL,
        [UserAgent] NVARCHAR(500) NULL,
        [Details]   NVARCHAR(MAX) NULL,
        [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_UserAuthLogs] PRIMARY KEY CLUSTERED ([LogId] ASC),
        CONSTRAINT [FK_UserAuthLogs_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE SET NULL
    );
    CREATE NONCLUSTERED INDEX [IX_UserAuthLogs_UserId] ON [dbo].[UserAuthLogs] ([UserId]);
    PRINT 'Created table [UserAuthLogs].';
END
GO

-- RefreshTokens Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RefreshTokens')
BEGIN
    CREATE TABLE [dbo].[RefreshTokens] (
        [TokenId]        BIGINT IDENTITY(1,1) NOT NULL,
        [UserId]         INT NOT NULL,
        [Token]          NVARCHAR(500) NOT NULL,
        [JwtId]          NVARCHAR(100) NULL,
        [ExpiresAt]      DATETIME2(7) NOT NULL,
        [CreatedAt]      DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [CreatedByIp]    NVARCHAR(50) NULL,
        [RevokedAt]      DATETIME2(7) NULL,
        [RevokedByIp]    NVARCHAR(50) NULL,
        [ReplacedByToken] NVARCHAR(500) NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY CLUSTERED ([TokenId] ASC),
        CONSTRAINT [FK_RefreshTokens_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_RefreshTokens_UserId] ON [dbo].[RefreshTokens] ([UserId]);
    CREATE NONCLUSTERED INDEX [IX_RefreshTokens_Token] ON [dbo].[RefreshTokens] ([Token]);
    PRINT 'Created table [RefreshTokens].';
END
GO

-- PasswordResetTokens Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PasswordResetTokens')
BEGIN
    CREATE TABLE [dbo].[PasswordResetTokens] (
        [TokenId]   INT IDENTITY(1,1) NOT NULL,
        [UserId]    INT NOT NULL,
        [Token]     NVARCHAR(128) NOT NULL,
        [ExpiresAt] DATETIME2(7) NOT NULL,
        [IsUsed]    BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [UsedAt]    DATETIME2(7) NULL,
        CONSTRAINT [PK_PasswordResetTokens] PRIMARY KEY CLUSTERED ([TokenId] ASC),
        CONSTRAINT [UQ_PasswordResetTokens_Token] UNIQUE NONCLUSTERED ([Token] ASC),
        CONSTRAINT [FK_PasswordResetTokens_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_PasswordResetTokens_Token] ON [dbo].[PasswordResetTokens] ([Token], [IsUsed], [ExpiresAt]);
    PRINT 'Created table [PasswordResetTokens].';
END
GO

-- ============================================================================
-- 3. INSTITUTION & LOOKUP TABLES
-- ============================================================================

-- Institutions Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Institutions')
BEGIN
    CREATE TABLE [dbo].[Institutions] (
        [InstitutionId]   INT IDENTITY(1,1) NOT NULL,
        [InstitutionName] NVARCHAR(200) NOT NULL,
        [Code]            NVARCHAR(50) NOT NULL,
        [City]            NVARCHAR(100) NULL,
        [IsVerified]      BIT NULL DEFAULT 1,
        [IsActive]        BIT NULL DEFAULT 1,
        [CreatedAt]       DATETIME NULL DEFAULT GETDATE(),
        CONSTRAINT [PK_Institutions] PRIMARY KEY CLUSTERED ([InstitutionId] ASC)
    );
    PRINT 'Created table [Institutions].';
END
GO

-- Departments Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Departments')
BEGIN
    CREATE TABLE [dbo].[Departments] (
        [DepartmentId]   INT IDENTITY(1,1) NOT NULL,
        [DepartmentName] NVARCHAR(200) NOT NULL,
        [DepartmentCode] NVARCHAR(20) NULL,
        [IsActive]       BIT NULL DEFAULT 1,
        CONSTRAINT [PK_Departments] PRIMARY KEY CLUSTERED ([DepartmentId] ASC)
    );
    PRINT 'Created table [Departments].';
END
GO

-- AcademicYears Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AcademicYears')
BEGIN
    CREATE TABLE [dbo].[AcademicYears] (
        [AcademicYearId] INT IDENTITY(1,1) NOT NULL,
        [YearName]       NVARCHAR(50) NOT NULL,
        [IsActive]       BIT NULL DEFAULT 1,
        CONSTRAINT [PK_AcademicYears] PRIMARY KEY CLUSTERED ([AcademicYearId] ASC)
    );
    PRINT 'Created table [AcademicYears].';
END
GO

-- Semesters Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Semesters')
BEGIN
    CREATE TABLE [dbo].[Semesters] (
        [SemesterId]     INT IDENTITY(1,1) NOT NULL,
        [SemesterNumber] INT NOT NULL,
        [SemesterName]   NVARCHAR(50) NOT NULL,
        [IsActive]       BIT NULL DEFAULT 1,
        CONSTRAINT [PK_Semesters] PRIMARY KEY CLUSTERED ([SemesterId] ASC)
    );
    PRINT 'Created table [Semesters].';
END
GO

-- Divisions Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Divisions')
BEGIN
    CREATE TABLE [dbo].[Divisions] (
        [DivisionId]   INT IDENTITY(1,1) NOT NULL,
        [DivisionName] NVARCHAR(50) NOT NULL,
        [IsActive]     BIT NULL DEFAULT 1,
        CONSTRAINT [PK_Divisions] PRIMARY KEY CLUSTERED ([DivisionId] ASC)
    );
    PRINT 'Created table [Divisions].';
END
GO

-- TaskTypes Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TaskTypes')
BEGIN
    CREATE TABLE [dbo].[TaskTypes] (
        [TaskTypeId] INT IDENTITY(1,1) NOT NULL,
        [TypeName]   NVARCHAR(100) NOT NULL,
        [IsActive]   BIT NULL DEFAULT 1,
        CONSTRAINT [PK_TaskTypes] PRIMARY KEY CLUSTERED ([TaskTypeId] ASC)
    );
    PRINT 'Created table [TaskTypes].';
END
GO

-- ============================================================================
-- 4. USER ROLE PROFILES
-- ============================================================================

-- AdminProfiles Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AdminProfiles')
BEGIN
    CREATE TABLE [dbo].[AdminProfiles] (
        [UserId]      INT NOT NULL,
        [Department]  NVARCHAR(100) NULL,
        [AccessLevel] NVARCHAR(50) NULL DEFAULT 'Level 5 (Full Root)',
        CONSTRAINT [PK_AdminProfiles] PRIMARY KEY CLUSTERED ([UserId] ASC),
        CONSTRAINT [FK_AdminProfiles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );
    PRINT 'Created table [AdminProfiles].';
END
GO

-- FacultyProfiles Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FacultyProfiles')
BEGIN
    CREATE TABLE [dbo].[FacultyProfiles] (
        [UserId]         INT NOT NULL,
        [EmployeeCode]   NVARCHAR(50) NULL,
        [Department]     NVARCHAR(100) NULL,
        [Designation]    NVARCHAR(100) NULL,
        [Qualification]  NVARCHAR(100) NULL,
        [OfficeLocation] NVARCHAR(100) NULL,
        CONSTRAINT [PK_FacultyProfiles] PRIMARY KEY CLUSTERED ([UserId] ASC),
        CONSTRAINT [FK_FacultyProfiles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );
    PRINT 'Created table [FacultyProfiles].';
END
GO

-- StudentProfiles Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StudentProfiles')
BEGIN
    CREATE TABLE [dbo].[StudentProfiles] (
        [UserId]           INT NOT NULL,
        [EnrollmentNumber] NVARCHAR(50) NULL,
        [Department]       NVARCHAR(100) NULL,
        [Semester]         INT NULL,
        [Division]         NVARCHAR(20) NULL,
        [RollNumber]       NVARCHAR(30) NULL,
        [BatchYear]        INT NULL,
        CONSTRAINT [PK_StudentProfiles] PRIMARY KEY CLUSTERED ([UserId] ASC),
        CONSTRAINT [FK_StudentProfiles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );
    PRINT 'Created table [StudentProfiles].';
END
GO

-- ============================================================================
-- 5. ACADEMIC & CLASSROOM TABLES
-- ============================================================================

-- Classrooms Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Classrooms')
BEGIN
    CREATE TABLE [dbo].[Classrooms] (
        [ClassroomId]   INT IDENTITY(1,1) NOT NULL,
        [CourseCode]    NVARCHAR(20) NOT NULL,
        [CourseName]    NVARCHAR(150) NOT NULL,
        [Department]    NVARCHAR(100) NOT NULL,
        [Semester]      INT NOT NULL,
        [AcademicYear]  NVARCHAR(20) NOT NULL,
        [JoinCode]      NVARCHAR(20) NOT NULL,
        [FacultyUserId] INT NOT NULL,
        [ThemeGradient] NVARCHAR(100) NOT NULL DEFAULT 'gradient-ocean',
        [CreatedAt]     DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [IsActive]      BIT NOT NULL DEFAULT 1,
        CONSTRAINT [PK_Classrooms] PRIMARY KEY CLUSTERED ([ClassroomId] ASC),
        CONSTRAINT [UQ_Classrooms_JoinCode] UNIQUE NONCLUSTERED ([JoinCode] ASC),
        CONSTRAINT [FK_Classrooms_Faculty] FOREIGN KEY ([FacultyUserId]) REFERENCES [dbo].[Users] ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_Classrooms_FacultyUserId] ON [dbo].[Classrooms] ([FacultyUserId]);
    CREATE NONCLUSTERED INDEX [IX_Classrooms_JoinCode] ON [dbo].[Classrooms] ([JoinCode]);
    PRINT 'Created table [Classrooms].';
END
GO

-- Ensure IsActive column exists on Classrooms
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Classrooms' AND COLUMN_NAME = 'IsActive')
BEGIN
    ALTER TABLE [dbo].[Classrooms] ADD [IsActive] BIT NOT NULL DEFAULT 1;
    PRINT 'Added [IsActive] column to [Classrooms].';
END
GO

-- ClassroomEnrollments Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClassroomEnrollments')
BEGIN
    CREATE TABLE [dbo].[ClassroomEnrollments] (
        [EnrollmentId]  INT IDENTITY(1,1) NOT NULL,
        [ClassroomId]   INT NOT NULL,
        [StudentUserId] INT NOT NULL,
        [EnrolledAt]    DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [Status]        NVARCHAR(20) NOT NULL DEFAULT 'Active',
        CONSTRAINT [PK_ClassroomEnrollments] PRIMARY KEY CLUSTERED ([EnrollmentId] ASC),
        CONSTRAINT [UQ_ClassroomEnrollments] UNIQUE NONCLUSTERED ([ClassroomId], [StudentUserId]),
        CONSTRAINT [FK_ClassroomEnrollments_Classrooms] FOREIGN KEY ([ClassroomId]) REFERENCES [dbo].[Classrooms] ([ClassroomId]) ON DELETE CASCADE,
        CONSTRAINT [FK_ClassroomEnrollments_Students] FOREIGN KEY ([StudentUserId]) REFERENCES [dbo].[Users] ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_ClassroomEnrollments_Student] ON [dbo].[ClassroomEnrollments] ([StudentUserId]);
    PRINT 'Created table [ClassroomEnrollments].';
END
GO

-- Tasks Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tasks')
BEGIN
    CREATE TABLE [dbo].[Tasks] (
        [TaskId]        INT IDENTITY(1,1) NOT NULL,
        [ClassroomId]   INT NOT NULL,
        [Title]         NVARCHAR(200) NOT NULL,
        [Description]   NVARCHAR(MAX) NULL,
        [TaskType]      NVARCHAR(50) NOT NULL,
        [MaxScore]      INT NOT NULL DEFAULT 100,
        [DueDate]       DATETIME2(7) NOT NULL,
        [Status]        NVARCHAR(20) NOT NULL DEFAULT 'Active',
        [CreatedAt]     DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Tasks] PRIMARY KEY CLUSTERED ([TaskId] ASC),
        CONSTRAINT [FK_Tasks_Classrooms] FOREIGN KEY ([ClassroomId]) REFERENCES [dbo].[Classrooms] ([ClassroomId]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_Tasks_ClassroomId] ON [dbo].[Tasks] ([ClassroomId]);
    PRINT 'Created table [Tasks].';
END
GO

-- TaskAttachments Table (Faculty Starter Files & Manuals)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TaskAttachments')
BEGIN
    CREATE TABLE [dbo].[TaskAttachments] (
        [AttachmentId]  INT IDENTITY(1,1) NOT NULL,
        [TaskId]        INT NOT NULL,
        [FileName]      NVARCHAR(255) NOT NULL,
        [FileSize]      NVARCHAR(50) NOT NULL,
        [FileType]      NVARCHAR(50) NOT NULL,
        [FileUrl]       NVARCHAR(1000) NOT NULL,
        [CloudPublicId] NVARCHAR(500) NULL,
        [FilePath]      NVARCHAR(500) NULL,
        [UploadedAt]    DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [IsActive]      BIT NOT NULL DEFAULT 1,
        CONSTRAINT [PK_TaskAttachments] PRIMARY KEY CLUSTERED ([AttachmentId] ASC),
        CONSTRAINT [FK_TaskAttachments_Tasks] FOREIGN KEY ([TaskId]) REFERENCES [dbo].[Tasks] ([TaskId]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_TaskAttachments_TaskId] ON [dbo].[TaskAttachments] ([TaskId]);
    PRINT 'Created table [TaskAttachments].';
END
GO

-- TaskSubmissions Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TaskSubmissions')
BEGIN
    CREATE TABLE [dbo].[TaskSubmissions] (
        [SubmissionId]    INT IDENTITY(1,1) NOT NULL,
        [TaskId]          INT NOT NULL,
        [StudentUserId]   INT NOT NULL,
        [SubmissionTitle] NVARCHAR(200) NOT NULL,
        [FileName]        NVARCHAR(255) NOT NULL,
        [FilePath]        NVARCHAR(500) NULL,
        [FileSize]        NVARCHAR(50) NOT NULL,
        [Status]          NVARCHAR(50) NOT NULL DEFAULT 'Submitted',
        [Score]           INT NULL,
        [Feedback]        NVARCHAR(MAX) NULL,
        [SubmittedAt]     DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [ReviewedAt]      DATETIME2(7) NULL,
        [CloudPublicId]   NVARCHAR(500) NULL,
        [CloudUrl]        NVARCHAR(1000) NULL,
        [FileHash]        NVARCHAR(256) NULL,
        CONSTRAINT [PK_TaskSubmissions] PRIMARY KEY CLUSTERED ([SubmissionId] ASC),
        CONSTRAINT [FK_TaskSubmissions_Tasks] FOREIGN KEY ([TaskId]) REFERENCES [dbo].[Tasks] ([TaskId]) ON DELETE CASCADE,
        CONSTRAINT [FK_TaskSubmissions_Students] FOREIGN KEY ([StudentUserId]) REFERENCES [dbo].[Users] ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_TaskSubmissions_TaskId] ON [dbo].[TaskSubmissions] ([TaskId]);
    CREATE NONCLUSTERED INDEX [IX_TaskSubmissions_Student] ON [dbo].[TaskSubmissions] ([StudentUserId]);
    PRINT 'Created table [TaskSubmissions].';
END
GO

-- CourseMaterials Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CourseMaterials')
BEGIN
    CREATE TABLE [dbo].[CourseMaterials] (
        [MaterialId]    INT IDENTITY(1,1) NOT NULL,
        [ClassroomId]   INT NOT NULL,
        [FacultyUserId] INT NOT NULL,
        [Title]         NVARCHAR(255) NOT NULL,
        [Description]   NVARCHAR(500) NULL,
        [FileName]      NVARCHAR(255) NULL,
        [FileSize]      NVARCHAR(50) NULL,
        [FileType]      NVARCHAR(50) NULL,
        [Category]      NVARCHAR(50) NOT NULL DEFAULT 'Lecture Notes',
        [CloudUrl]      NVARCHAR(1000) NULL,
        [FilePath]      NVARCHAR(500) NULL,
        [CloudPublicId] NVARCHAR(500) NULL,
        [IsActive]      BIT NOT NULL DEFAULT 1,
        [UploadedAt]    DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        [ExternalUrl]   NVARCHAR(1000) NULL,
        [UpdatedAt]     DATETIME2(7) NULL,
        CONSTRAINT [PK_CourseMaterials] PRIMARY KEY CLUSTERED ([MaterialId] ASC),
        CONSTRAINT [FK_CourseMaterials_Classrooms] FOREIGN KEY ([ClassroomId]) REFERENCES [dbo].[Classrooms] ([ClassroomId]) ON DELETE CASCADE,
        CONSTRAINT [FK_CourseMaterials_Faculty] FOREIGN KEY ([FacultyUserId]) REFERENCES [dbo].[Users] ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_CourseMaterials_Classroom] ON [dbo].[CourseMaterials] ([ClassroomId]);
    PRINT 'Created table [CourseMaterials].';
END
GO

-- Announcements Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Announcements')
BEGIN
    CREATE TABLE [dbo].[Announcements] (
        [AnnouncementId] INT IDENTITY(1,1) NOT NULL,
        [ClassroomId]    INT NULL,
        [FacultyUserId]  INT NOT NULL,
        [Title]          NVARCHAR(255) NOT NULL,
        [Message]        NVARCHAR(MAX) NOT NULL,
        [Priority]       NVARCHAR(20) NOT NULL DEFAULT 'Normal',
        [IsPinned]       BIT NOT NULL DEFAULT 0,
        [RequireAck]     BIT NOT NULL DEFAULT 0,
        [IsActive]       BIT NOT NULL DEFAULT 1,
        [PublishedAt]    DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Announcements] PRIMARY KEY CLUSTERED ([AnnouncementId] ASC),
        CONSTRAINT [FK_Announcements_Faculty] FOREIGN KEY ([FacultyUserId]) REFERENCES [dbo].[Users] ([UserId]),
        CONSTRAINT [FK_Announcements_Classrooms] FOREIGN KEY ([ClassroomId]) REFERENCES [dbo].[Classrooms] ([ClassroomId]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_Announcements_Classroom] ON [dbo].[Announcements] ([ClassroomId]);
    PRINT 'Created table [Announcements].';
END
GO

-- Notifications Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications')
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [NotificationId] INT IDENTITY(1,1) NOT NULL,
        [UserId]         INT NOT NULL,
        [Category]       NVARCHAR(50) NOT NULL,
        [Title]          NVARCHAR(255) NOT NULL,
        [Message]        NVARCHAR(1000) NOT NULL,
        [BadgeText]      NVARCHAR(50) NULL,
        [BadgeType]      NVARCHAR(50) NULL,
        [ContextInfo]    NVARCHAR(255) NULL,
        [ActionUrl]      NVARCHAR(500) NULL,
        [ActionText]     NVARCHAR(100) NULL,
        [IsRead]         BIT NOT NULL DEFAULT 0,
        [CreatedAt]      DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([NotificationId] ASC),
        CONSTRAINT [FK_Notifications_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_Notifications_UserId] ON [dbo].[Notifications] ([UserId], [IsRead]);
    PRINT 'Created table [Notifications].';
END
GO

-- ============================================================================
-- 6. STORED PROCEDURES (50 PROCEDURES)
-- ============================================================================
PRINT 'Creating / Updating all 50 Stored Procedures...';
GO
-- SP: sp_AdminCreateUser
CREATE OR ALTER PROCEDURE dbo.sp_AdminCreateUser
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

-- SP: sp_AdminDeleteClassroom
CREATE OR ALTER PROCEDURE dbo.sp_AdminDeleteClassroom
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

-- SP: sp_AdminDeleteUser
CREATE OR ALTER PROCEDURE dbo.sp_AdminDeleteUser
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

-- SP: sp_AuthenticateUser
-- ----------------------------------------------------------------------------
-- 1. SP: Authenticate User by Email
-- ----------------------------------------------------------------------------
CREATE   PROCEDURE [dbo].[sp_AuthenticateUser]
    @Email NVARCHAR(150)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        u.[UserId],
        u.[RoleId],
        r.[RoleName],
        u.[Email],
        u.[PasswordHash],
        u.[PasswordSalt],
        u.[FirstName],
        u.[LastName],
        u.[PhoneNumber],
        u.[ProfilePictureUrl],
        u.[IsActive],
        u.[IsEmailVerified],
        -- Student specific info (if student)
        sp.[EnrollmentNumber],
        sp.[Department] AS [StudentDepartment],
        sp.[Semester],
        sp.[Division],
        sp.[RollNumber],
        -- Faculty specific info (if faculty)
        fp.[EmployeeCode],
        fp.[Department] AS [FacultyDepartment],
        fp.[Designation] AS [FacultyDesignation],
        -- Admin specific info (if admin)
        ap.[Department] AS [AdminDepartment],
        ap.[AccessLevel] AS [AdminAccessLevel]
    FROM [dbo].[Users] u
    INNER JOIN [dbo].[Roles] r ON u.[RoleId] = r.[RoleId]
    LEFT JOIN [dbo].[StudentProfiles] sp ON u.[UserId] = sp.[UserId]
    LEFT JOIN [dbo].[FacultyProfiles] fp ON u.[UserId] = fp.[UserId]
    LEFT JOIN [dbo].[AdminProfiles] ap ON u.[UserId] = ap.[UserId]
    WHERE u.[Email] = @Email;
END
GO

-- SP: sp_ChangeUserPassword
-- 3. SP: Change User Password
CREATE   PROCEDURE [dbo].[sp_ChangeUserPassword]
    @UserId             INT,
    @NewPasswordHash    NVARCHAR(500),
    @NewPasswordSalt    NVARCHAR(255),
    @Success            BIT OUTPUT,
    @Message            NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 0;
    SET @Message = 'User not found.';

    IF EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [UserId] = @UserId)
    BEGIN
        UPDATE [dbo].[Users]
        SET [PasswordHash] = @NewPasswordHash,
            [PasswordSalt] = @NewPasswordSalt,
            [UpdatedAt] = SYSUTCDATETIME()
        WHERE [UserId] = @UserId;

        SET @Success = 1;
        SET @Message = 'Password changed successfully.';
    END
END
GO

-- SP: sp_CreateClassroom
CREATE OR ALTER PROCEDURE [dbo].[sp_CreateClassroom]
    @FacultyUserId      INT,
    @CourseCode         NVARCHAR(20),
    @CourseName         NVARCHAR(150),
    @Department         NVARCHAR(100),
    @Semester           INT,
    @AcademicYear       NVARCHAR(20),
    @JoinCode           NVARCHAR(20),
    @ThemeGradient      NVARCHAR(100),
    @Success            BIT OUTPUT,
    @Message            NVARCHAR(255) OUTPUT,
    @NewClassroomId     INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 0;
    SET @NewClassroomId = 0;

    -- Check if JoinCode already exists
    IF EXISTS (SELECT 1 FROM [dbo].[Classrooms] WHERE [JoinCode] = @JoinCode)
    BEGIN
        SET @Message = 'Join Code / Section Key already exists. Please choose a unique code.';
        RETURN;
    END

    -- Fallback theme gradient if empty
    IF (@ThemeGradient IS NULL OR LEN(LTRIM(RTRIM(@ThemeGradient))) = 0)
    BEGIN
        SET @ThemeGradient = 'linear-gradient(135deg, #1e3a8a 0%, #3b82f6 100%)';
    END

    INSERT INTO [dbo].[Classrooms]
        ([CourseCode], [CourseName], [Department], [Semester], [AcademicYear], [JoinCode], [FacultyUserId], [ThemeGradient])
    VALUES
        (UPPER(LTRIM(RTRIM(@CourseCode))), LTRIM(RTRIM(@CourseName)), LTRIM(RTRIM(@Department)), @Semester, @AcademicYear, UPPER(LTRIM(RTRIM(@JoinCode))), @FacultyUserId, @ThemeGradient);

    SET @NewClassroomId = SCOPE_IDENTITY();
    SET @Success = 1;
    SET @Message = 'Classroom created successfully.';
END
GO

-- SP: sp_CreateNotification
CREATE OR ALTER PROCEDURE dbo.sp_CreateNotification
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

-- SP: sp_CreatePasswordResetToken
CREATE OR ALTER PROCEDURE dbo.sp_CreatePasswordResetToken
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

-- SP: sp_CreateTask
CREATE OR ALTER PROCEDURE [dbo].[sp_CreateTask]
    @ClassroomId    INT,
    @Title          NVARCHAR(200),
    @Description    NVARCHAR(MAX),
    @TaskType       NVARCHAR(50),
    @MaxScore       INT,
    @DueDate        DATETIME2(7),
    @Success        BIT OUTPUT,
    @Message        NVARCHAR(255) OUTPUT,
    @NewTaskId      INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 0;
    SET @NewTaskId = 0;

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Classrooms] WHERE [ClassroomId] = @ClassroomId)
    BEGIN
        SET @Message = 'Invalid classroom specified.';
        RETURN;
    END

    INSERT INTO [dbo].[Tasks] ([ClassroomId], [Title], [Description], [TaskType], [MaxScore], [DueDate], [Status])
    VALUES (@ClassroomId, LTRIM(RTRIM(@Title)), @Description, @TaskType, @MaxScore, @DueDate, 'Active');

    SET @NewTaskId = SCOPE_IDENTITY();
    SET @Success = 1;
    SET @Message = 'Assignment / Task created successfully.';
END
GO

-- SP: sp_DeleteAnnouncement
CREATE OR ALTER PROCEDURE dbo.sp_DeleteAnnouncement
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

-- SP: sp_DeleteClassroom
CREATE OR ALTER PROCEDURE dbo.sp_DeleteClassroom
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

-- SP: sp_DeleteCourseMaterial
CREATE OR ALTER PROCEDURE dbo.sp_DeleteCourseMaterial
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

-- SP: sp_DeleteTask
CREATE OR ALTER PROCEDURE dbo.sp_DeleteTask
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

-- SP: sp_GetClassroomAnnouncements
CREATE OR ALTER PROCEDURE dbo.sp_GetClassroomAnnouncements
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

-- SP: sp_GetClassroomDetails
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
        m.ExternalUrl,
        m.UploadedAt,
        (u.FirstName + ' ' + u.LastName) AS FacultyName
    FROM CourseMaterials m
    JOIN Users u ON m.FacultyUserId = u.UserId
    WHERE m.ClassroomId = @ClassroomId
      AND m.IsActive = 1
    ORDER BY m.UploadedAt DESC;
END;
GO

-- SP: sp_GetClassroomMaterials
CREATE OR ALTER PROCEDURE dbo.sp_GetClassroomMaterials
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

-- SP: sp_GetFacultyAnnouncements
CREATE OR ALTER PROCEDURE dbo.sp_GetFacultyAnnouncements
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

-- SP: sp_GetFacultyClassrooms
CREATE OR ALTER PROCEDURE [dbo].[sp_GetFacultyClassrooms]
    @FacultyUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        c.[ClassroomId],
        c.[CourseCode],
        c.[CourseName],
        c.[Department],
        c.[Semester],
        c.[AcademicYear],
        c.[JoinCode],
        c.[ThemeGradient],
        c.[CreatedAt],
        (SELECT COUNT(*) FROM [dbo].[ClassroomEnrollments] ce WHERE ce.[ClassroomId] = c.[ClassroomId]) AS [StudentCount],
        (SELECT COUNT(*) FROM [dbo].[Tasks] t WHERE t.[ClassroomId] = c.[ClassroomId]) AS [TaskCount],
        (SELECT COUNT(*) 
         FROM [dbo].[TaskSubmissions] s 
         JOIN [dbo].[Tasks] t ON s.[TaskId] = t.[TaskId] 
         WHERE t.[ClassroomId] = c.[ClassroomId] AND s.[Status] = 'PendingReview') AS [PendingReviewCount]
    FROM [dbo].[Classrooms] c
    WHERE c.[FacultyUserId] = @FacultyUserId
    ORDER BY c.[ClassroomId] DESC;
END
GO

-- SP: sp_GetFacultyDashboardData
CREATE OR ALTER PROCEDURE [dbo].[sp_GetFacultyDashboardData]
    @FacultyUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Metrics Counts
    SELECT 
        (SELECT COUNT(*) FROM [dbo].[Classrooms] WHERE [FacultyUserId] = @FacultyUserId) AS [ActiveClassroomCount],
        (SELECT COUNT(DISTINCT ce.[StudentUserId]) 
         FROM [dbo].[ClassroomEnrollments] ce 
         JOIN [dbo].[Classrooms] c ON ce.[ClassroomId] = c.[ClassroomId] 
         WHERE c.[FacultyUserId] = @FacultyUserId) AS [TotalStudentCount],
        (SELECT COUNT(*) 
         FROM [dbo].[Tasks] t 
         JOIN [dbo].[Classrooms] c ON t.[ClassroomId] = c.[ClassroomId] 
         WHERE c.[FacultyUserId] = @FacultyUserId AND t.[Status] = 'Active') AS [ActiveTaskCount],
        (SELECT COUNT(*) 
         FROM [dbo].[TaskSubmissions] s
         JOIN [dbo].[Tasks] t ON s.[TaskId] = t.[TaskId]
         JOIN [dbo].[Classrooms] c ON t.[ClassroomId] = c.[ClassroomId]
         WHERE c.[FacultyUserId] = @FacultyUserId AND s.[Status] = 'PendingReview') AS [PendingReviewCount];

    -- 2. Classrooms summary (Top 4)
    SELECT TOP 4
        c.[ClassroomId],
        c.[CourseCode],
        c.[CourseName],
        c.[Department],
        c.[Semester],
        c.[JoinCode],
        c.[ThemeGradient],
        (SELECT COUNT(*) FROM [dbo].[ClassroomEnrollments] ce WHERE ce.[ClassroomId] = c.[ClassroomId]) AS [StudentCount],
        (SELECT COUNT(*) FROM [dbo].[Tasks] t WHERE t.[ClassroomId] = c.[ClassroomId]) AS [TaskCount],
        (SELECT COUNT(*) 
         FROM [dbo].[TaskSubmissions] s 
         JOIN [dbo].[Tasks] t ON s.[TaskId] = t.[TaskId] 
         WHERE t.[ClassroomId] = c.[ClassroomId] AND s.[Status] = 'PendingReview') AS [PendingReviews]
    FROM [dbo].[Classrooms] c
    WHERE c.[FacultyUserId] = @FacultyUserId
    ORDER BY c.[ClassroomId] DESC;

    -- 3. Recent Tasks (Top 5)
    SELECT TOP 5
        t.[TaskId],
        t.[ClassroomId],
        c.[CourseCode],
        c.[CourseName],
        t.[Title],
        t.[TaskType],
        t.[MaxScore],
        t.[DueDate],
        t.[Status],
        (SELECT COUNT(*) FROM [dbo].[TaskSubmissions] s WHERE s.[TaskId] = t.[TaskId]) AS [SubmissionCount]
    FROM [dbo].[Tasks] t
    JOIN [dbo].[Classrooms] c ON t.[ClassroomId] = c.[ClassroomId]
    WHERE c.[FacultyUserId] = @FacultyUserId
    ORDER BY t.[DueDate] ASC;

    -- 4. Pending Submissions (Top 5 needing attention)
    SELECT TOP 5
        s.[SubmissionId],
        s.[TaskId],
        t.[Title] AS [TaskTitle],
        c.[CourseCode],
        c.[CourseName],
        (u.[FirstName] + ' ' + u.[LastName]) AS [StudentName],
        s.[FileName],
        s.[FileSize],
        s.[SubmittedAt],
        s.[Status]
    FROM [dbo].[TaskSubmissions] s
    JOIN [dbo].[Tasks] t ON s.[TaskId] = t.[TaskId]
    JOIN [dbo].[Classrooms] c ON t.[ClassroomId] = c.[ClassroomId]
    JOIN [dbo].[Users] u ON s.[StudentUserId] = u.[UserId]
    WHERE c.[FacultyUserId] = @FacultyUserId AND s.[Status] = 'PendingReview'
    ORDER BY s.[SubmittedAt] ASC;
END
GO

-- SP: sp_GetFacultyMaterials
CREATE OR ALTER PROCEDURE dbo.sp_GetFacultyMaterials
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
        m.ExternalUrl,
        m.UploadedAt,
        m.UpdatedAt,
        (u.FirstName + ' ' + u.LastName) AS UploaderName
    FROM CourseMaterials m
    JOIN Classrooms c ON m.ClassroomId   = c.ClassroomId
    JOIN Users      u ON m.FacultyUserId = u.UserId
    WHERE m.FacultyUserId = @FacultyUserId
      AND m.IsActive = 1
    ORDER BY m.UploadedAt DESC;
END;
GO

-- SP: sp_GetFacultyProfile
CREATE OR ALTER PROCEDURE dbo.sp_GetFacultyProfile @UserId INT AS BEGIN SET NOCOUNT ON; SELECT u.UserId, u.FirstName, u.LastName, (u.FirstName + ' ' + u.LastName) AS FullName, u.Email, u.PhoneNumber, u.ProfilePictureUrl, fp.EmployeeCode, fp.Department, fp.Designation, fp.Qualification, fp.OfficeLocation FROM dbo.Users u LEFT JOIN dbo.FacultyProfiles fp ON u.UserId = fp.UserId WHERE u.UserId = @UserId; END;
GO

-- SP: sp_GetFacultyTasks
CREATE OR ALTER PROCEDURE [dbo].[sp_GetFacultyTasks]
    @FacultyUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        t.[TaskId],
        t.[ClassroomId],
        c.[CourseCode],
        c.[CourseName],
        t.[Title],
        t.[Description],
        t.[TaskType],
        t.[MaxScore],
        t.[DueDate],
        t.[Status],
        (SELECT COUNT(*) FROM [dbo].[TaskSubmissions] s WHERE s.[TaskId] = t.[TaskId]) AS [SubmissionCount],
        (SELECT COUNT(*) FROM [dbo].[TaskSubmissions] s WHERE s.[TaskId] = t.[TaskId] AND s.[Status] = 'PendingReview') AS [PendingReviewCount]
    FROM [dbo].[Tasks] t
    JOIN [dbo].[Classrooms] c ON t.[ClassroomId] = c.[ClassroomId]
    WHERE c.[FacultyUserId] = @FacultyUserId
    ORDER BY t.[DueDate] DESC;
END
GO

-- SP: sp_GetStudentClassrooms
-- ----------------------------------------------------------------------------
-- STORED PROCEDURES
-- ----------------------------------------------------------------------------

-- SP 1: Get Student Classrooms
CREATE   PROCEDURE [dbo].[sp_GetStudentClassrooms]
    @StudentUserId INT
AS
BEGIN
    SET NOCOUNT ON;

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
        fp.[Designation] AS [FacultyDesignation],
        ce.[EnrolledAt],
        ce.[Status] AS [EnrollmentStatus],
        (SELECT COUNT(1) FROM [dbo].[Tasks] t WHERE t.[ClassroomId] = c.[ClassroomId] AND t.[Status] = 'Active') AS [ActiveTaskCount]
    FROM [dbo].[ClassroomEnrollments] ce
    INNER JOIN [dbo].[Classrooms] c ON ce.[ClassroomId] = c.[ClassroomId]
    INNER JOIN [dbo].[Users] u ON c.[FacultyUserId] = u.[UserId]
    LEFT JOIN [dbo].[FacultyProfiles] fp ON u.[UserId] = fp.[UserId]
    WHERE ce.[StudentUserId] = @StudentUserId
    ORDER BY c.[CourseCode] ASC;
END
GO

-- SP: sp_GetStudentDashboardData
CREATE OR ALTER PROCEDURE dbo.sp_GetStudentDashboardData
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

-- SP: sp_GetStudentDocuments
CREATE OR ALTER PROCEDURE dbo.sp_GetStudentDocuments
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

-- SP: sp_GetStudentTasks
CREATE OR ALTER PROCEDURE dbo.sp_GetStudentTasks
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

-- SP: sp_GetTaskDetails
-- SP 4: Get Specific Task Details
CREATE   PROCEDURE [dbo].[sp_GetTaskDetails]
    @TaskId         INT,
    @StudentUserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        t.[TaskId],
        t.[ClassroomId],
        t.[Title],
        t.[Description],
        t.[TaskType],
        t.[MaxScore],
        t.[DueDate],
        t.[Status] AS [TaskStatus],
        c.[CourseCode],
        c.[CourseName],
        u.[FirstName] + ' ' + u.[LastName] AS [FacultyName],
        u.[Email] AS [FacultyEmail]
    FROM [dbo].[Tasks] t
    INNER JOIN [dbo].[Classrooms] c ON t.[ClassroomId] = c.[ClassroomId]
    INNER JOIN [dbo].[Users] u ON c.[FacultyUserId] = u.[UserId]
    WHERE t.[TaskId] = @TaskId;
END
GO

-- SP: sp_GetUserNotifications
CREATE OR ALTER PROCEDURE dbo.sp_GetUserNotifications
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

-- SP: sp_GetUserProfileById
CREATE OR ALTER PROCEDURE [dbo].[sp_GetUserProfileById]
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        u.[UserId],
        u.[RoleId],
        r.[RoleName],
        u.[Email],
        u.[PasswordHash],
        u.[PasswordSalt],
        u.[FirstName],
        u.[LastName],
        u.[PhoneNumber],
        u.[ProfilePictureUrl],
        u.[IsActive],
        u.[IsEmailVerified],
        u.[CreatedAt],
        u.[LastLoginAt],
        -- Student specific info
        sp.[EnrollmentNumber],
        sp.[Department] AS [StudentDepartment],
        sp.[Semester],
        sp.[Division],
        sp.[RollNumber],
        sp.[BatchYear],
        -- Faculty specific info
        fp.[EmployeeCode],
        fp.[Department] AS [FacultyDepartment],
        fp.[Designation] AS [FacultyDesignation],
        -- Admin specific info
        ap.[Department] AS [AdminDepartment],
        ap.[AccessLevel] AS [AdminAccessLevel]
    FROM [dbo].[Users] u
    INNER JOIN [dbo].[Roles] r ON u.[RoleId] = r.[RoleId]
    LEFT JOIN [dbo].[StudentProfiles] sp ON u.[UserId] = sp.[UserId]
    LEFT JOIN [dbo].[FacultyProfiles] fp ON u.[UserId] = fp.[UserId]
    LEFT JOIN [dbo].[AdminProfiles] ap ON u.[UserId] = ap.[UserId]
    WHERE u.[UserId] = @UserId;
END
GO

-- SP: sp_JoinClassroom
-- SP 2: Join Classroom by Code
CREATE   PROCEDURE [dbo].[sp_JoinClassroom]
    @StudentUserId INT,
    @JoinCode       NVARCHAR(20),
    @Success        BIT OUTPUT,
    @Message        NVARCHAR(255) OUTPUT,
    @ClassroomId    INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 0;
    SET @ClassroomId = 0;

    DECLARE @TargetClassroomId INT;
    SELECT @TargetClassroomId = [ClassroomId]
    FROM [dbo].[Classrooms]
    WHERE UPPER(LTRIM(RTRIM([JoinCode]))) = UPPER(LTRIM(RTRIM(@JoinCode)));

    IF @TargetClassroomId IS NULL
    BEGIN
        SET @Message = 'Classroom with join code ' + @JoinCode + ' was not found.';
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM [dbo].[ClassroomEnrollments] WHERE [ClassroomId] = @TargetClassroomId AND [StudentUserId] = @StudentUserId)
    BEGIN
        SET @Success = 0;
        SET @Message = 'You are already enrolled in this classroom.';
        SET @ClassroomId = @TargetClassroomId;
        RETURN;
    END

    INSERT INTO [dbo].[ClassroomEnrollments] ([ClassroomId], [StudentUserId], [Status])
    VALUES (@TargetClassroomId, @StudentUserId, 'Active');

    SET @Success = 1;
    SET @Message = 'Successfully joined classroom!';
    SET @ClassroomId = @TargetClassroomId;
END
GO

-- SP: sp_LeaveClassroom
CREATE OR ALTER PROCEDURE dbo.sp_LeaveClassroom
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

-- SP: sp_LogUserAuth
-- ----------------------------------------------------------------------------
-- 6. SP: Log User Authentication Action (Audit)
-- ----------------------------------------------------------------------------
CREATE   PROCEDURE [dbo].[sp_LogUserAuth]
    @UserId     INT = NULL,
    @Action     NVARCHAR(50),
    @IpAddress  NVARCHAR(50) = NULL,
    @UserAgent  NVARCHAR(500) = NULL,
    @Details    NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[UserAuthLogs] 
        ([UserId], [Action], [IpAddress], [UserAgent], [Details], [CreatedAt])
    VALUES 
        (@UserId, @Action, @IpAddress, @UserAgent, @Details, SYSUTCDATETIME());

    -- Also update LastLoginAt if successful login
    IF (@Action = 'LOGIN_SUCCESS' AND @UserId IS NOT NULL)
    BEGIN
        UPDATE [dbo].[Users] 
        SET [LastLoginAt] = SYSUTCDATETIME()
        WHERE [UserId] = @UserId;
    END
END
GO

-- SP: sp_MarkAllNotificationsAsRead
CREATE OR ALTER PROCEDURE dbo.sp_MarkAllNotificationsAsRead
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [dbo].[Notifications]
    SET [IsRead] = 1
    WHERE [UserId] = @UserId AND [IsRead] = 0;
END
GO

-- SP: sp_MarkNotificationAsRead
CREATE OR ALTER PROCEDURE dbo.sp_MarkNotificationAsRead
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

-- SP: sp_PublishAnnouncement
CREATE OR ALTER PROCEDURE dbo.sp_PublishAnnouncement
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

-- SP: sp_RegisterAdmin
CREATE OR ALTER PROCEDURE dbo.sp_RegisterAdmin
    @Email              NVARCHAR(150),
    @PasswordHash       NVARCHAR(500),
    @PasswordSalt       NVARCHAR(255),
    @FirstName          NVARCHAR(100),
    @LastName           NVARCHAR(100),
    @PhoneNumber        NVARCHAR(20) = NULL,
    @Department         NVARCHAR(100) = NULL,
    @AccessLevel        NVARCHAR(50) = NULL,
    @NewUserId          INT OUTPUT,
    @StatusCode         INT OUTPUT,
    @StatusMessage      NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @StatusCode = 1;
    SET @StatusMessage = 'Success';
    SET @NewUserId = 0;

    -- Check if Email already exists
    IF EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = @Email)
    BEGIN
        SET @StatusCode = -1;
        SET @StatusMessage = 'Email is already registered.';
        RETURN;
    END

    DECLARE @AdminRoleId INT;
    SELECT @AdminRoleId = [RoleId] FROM [dbo].[Roles] WHERE [RoleName] = 'Admin';

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO [dbo].[Users] 
            ([RoleId], [Email], [PasswordHash], [PasswordSalt], [FirstName], [LastName], [PhoneNumber], [IsActive], [IsEmailVerified])
        VALUES 
            (@AdminRoleId, @Email, @PasswordHash, @PasswordSalt, @FirstName, @LastName, @PhoneNumber, 1, 1);

        SET @NewUserId = SCOPE_IDENTITY();

        IF @AccessLevel IS NULL SET @AccessLevel = 'Tier 1 - Full Campus Access';

        INSERT INTO [dbo].[AdminProfiles] 
            ([UserId], [Department], [AccessLevel])
        VALUES 
            (@NewUserId, @Department, @AccessLevel);

        COMMIT TRANSACTION;
        SET @StatusMessage = 'Administrator registered successfully.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @StatusCode = -99;
        SET @StatusMessage = ERROR_MESSAGE();
    END CATCH
END;
GO

-- SP: sp_RegisterFaculty
CREATE OR ALTER PROCEDURE dbo.sp_RegisterFaculty
    @Email              NVARCHAR(150),
    @PasswordHash       NVARCHAR(500),
    @PasswordSalt       NVARCHAR(255),
    @FirstName          NVARCHAR(100),
    @LastName           NVARCHAR(100),
    @PhoneNumber        NVARCHAR(20) = NULL,
    @EmployeeCode       NVARCHAR(50) = NULL,
    @Department         NVARCHAR(100) = NULL,
    @Designation        NVARCHAR(100) = NULL,
    @Qualification      NVARCHAR(100) = NULL,
    @OfficeLocation     NVARCHAR(100) = NULL,
    @NewUserId          INT OUTPUT,
    @StatusCode         INT OUTPUT,
    @StatusMessage      NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @StatusCode = 1;
    SET @StatusMessage = 'Success';
    SET @NewUserId = 0;

    -- Check if Email already exists
    IF EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = @Email)
    BEGIN
        SET @StatusCode = -1;
        SET @StatusMessage = 'Email is already registered.';
        RETURN;
    END

    -- If EmployeeCode provided and non-empty, check uniqueness
    IF @EmployeeCode IS NOT NULL AND LTRIM(RTRIM(@EmployeeCode)) <> ''
    BEGIN
        IF EXISTS (SELECT 1 FROM [dbo].[FacultyProfiles] WHERE [EmployeeCode] = @EmployeeCode)
        BEGIN
            SET @StatusCode = -2;
            SET @StatusMessage = 'Employee code is already registered.';
            RETURN;
        END
    END

    DECLARE @FacultyRoleId INT;
    SELECT @FacultyRoleId = [RoleId] FROM [dbo].[Roles] WHERE [RoleName] = 'Faculty';

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO [dbo].[Users] 
            ([RoleId], [Email], [PasswordHash], [PasswordSalt], [FirstName], [LastName], [PhoneNumber], [IsActive], [IsEmailVerified])
        VALUES 
            (@FacultyRoleId, @Email, @PasswordHash, @PasswordSalt, @FirstName, @LastName, @PhoneNumber, 1, 1);

        SET @NewUserId = SCOPE_IDENTITY();

        -- Keep Department as NULL if not supplied (do not invent default institution)
        IF @Designation IS NULL SET @Designation = 'Faculty Mentor';

        INSERT INTO [dbo].[FacultyProfiles] 
            ([UserId], [EmployeeCode], [Department], [Designation], [Qualification], [OfficeLocation])
        VALUES 
            (@NewUserId, @EmployeeCode, @Department, @Designation, @Qualification, @OfficeLocation);

        COMMIT TRANSACTION;
        SET @StatusMessage = 'Faculty registered successfully.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @StatusCode = -99;
        SET @StatusMessage = ERROR_MESSAGE();
    END CATCH
END;
GO

-- SP: sp_RegisterStudent
CREATE OR ALTER PROCEDURE dbo.sp_RegisterStudent
    @Email              NVARCHAR(150),
    @PasswordHash       NVARCHAR(500),
    @PasswordSalt       NVARCHAR(255),
    @FirstName          NVARCHAR(100),
    @LastName           NVARCHAR(100),
    @PhoneNumber        NVARCHAR(20) = NULL,
    @EnrollmentNumber   NVARCHAR(50) = NULL,
    @Department         NVARCHAR(100) = NULL,
    @Semester           INT = NULL,
    @Division           NVARCHAR(20) = NULL,
    @RollNumber         NVARCHAR(30) = NULL,
    @BatchYear          INT = NULL,
    @NewUserId          INT OUTPUT,
    @StatusCode         INT OUTPUT,
    @StatusMessage      NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @StatusCode = 1;
    SET @StatusMessage = 'Success';
    SET @NewUserId = 0;

    -- Check if Email already exists
    IF EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = @Email)
    BEGIN
        SET @StatusCode = -1;
        SET @StatusMessage = 'Email is already registered.';
        RETURN;
    END

    -- If EnrollmentNumber is provided and non-empty, check uniqueness
    IF @EnrollmentNumber IS NOT NULL AND LTRIM(RTRIM(@EnrollmentNumber)) <> ''
    BEGIN
        IF EXISTS (SELECT 1 FROM [dbo].[StudentProfiles] WHERE [EnrollmentNumber] = @EnrollmentNumber)
        BEGIN
            SET @StatusCode = -2;
            SET @StatusMessage = 'Enrollment Number is already registered.';
            RETURN;
        END
    END

    DECLARE @StudentRoleId INT;
    SELECT @StudentRoleId = [RoleId] FROM [dbo].[Roles] WHERE [RoleName] = 'Student';

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO [dbo].[Users] 
            ([RoleId], [Email], [PasswordHash], [PasswordSalt], [FirstName], [LastName], [PhoneNumber], [IsActive], [IsEmailVerified])
        VALUES 
            (@StudentRoleId, @Email, @PasswordHash, @PasswordSalt, @FirstName, @LastName, @PhoneNumber, 1, 1);

        SET @NewUserId = SCOPE_IDENTITY();

        -- Keep Department as NULL if not supplied (do not invent default institution)
        IF @Semester IS NULL OR @Semester <= 0 SET @Semester = 1;

        INSERT INTO [dbo].[StudentProfiles] 
            ([UserId], [EnrollmentNumber], [Department], [Semester], [Division], [RollNumber], [BatchYear])
        VALUES 
            (@NewUserId, @EnrollmentNumber, @Department, @Semester, @Division, @RollNumber, @BatchYear);

        COMMIT TRANSACTION;
        SET @StatusMessage = 'Student registered successfully.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @StatusCode = -99;
        SET @StatusMessage = ERROR_MESSAGE();
    END CATCH
END;
GO

-- SP: sp_ResetPasswordWithToken
CREATE OR ALTER PROCEDURE dbo.sp_ResetPasswordWithToken
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

-- SP: sp_ReviewSubmission
CREATE OR ALTER PROCEDURE sp_ReviewSubmission
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

-- SP: sp_SaveRefreshToken
-- ----------------------------------------------------------------------------
-- 4. SP: Save Refresh Token
-- ----------------------------------------------------------------------------
CREATE   PROCEDURE [dbo].[sp_SaveRefreshToken]
    @UserId         INT,
    @Token          NVARCHAR(500),
    @JwtId          NVARCHAR(100),
    @ExpiresAt      DATETIME2(7),
    @CreatedByIp    NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[RefreshTokens] 
        ([UserId], [Token], [JwtId], [ExpiresAt], [CreatedAt], [CreatedByIp])
    VALUES 
        (@UserId, @Token, @JwtId, @ExpiresAt, SYSUTCDATETIME(), @CreatedByIp);
END
GO

-- SP: sp_SubmitTaskDocument
CREATE OR ALTER PROCEDURE sp_SubmitTaskDocument
    @TaskId         INT,
    @StudentUserId  INT,
    @FileName       NVARCHAR(255),
    @FileSize       NVARCHAR(50),
    @FilePath       NVARCHAR(1000),
    @CloudPublicId  NVARCHAR(500) = NULL,
    @FileHash       NVARCHAR(256) = NULL,
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
            FileHash       = ISNULL(@FileHash, FileHash),
            Status         = 'PendingReview',
            SubmittedAt    = GETDATE()
        WHERE TaskId = @TaskId AND StudentUserId = @StudentUserId;

        SET @Success = 1;
        SET @Message = 'Document resubmitted successfully.';
    END
    ELSE
    BEGIN
        INSERT INTO TaskSubmissions (TaskId, StudentUserId, FileName, FileSize, FilePath, CloudPublicId, CloudUrl, FileHash, Status, SubmittedAt)
        VALUES (@TaskId, @StudentUserId, @FileName, @FileSize, @FilePath, @CloudPublicId, @FilePath, @FileHash, 'PendingReview', GETDATE());

        SET @Success = 1;
        SET @Message = 'Document submitted successfully.';
    END
END
GO

-- SP: sp_UpdateAdminProfile
CREATE OR ALTER PROCEDURE [dbo].[sp_UpdateAdminProfile]
    @UserId         INT,
    @FirstName      NVARCHAR(100),
    @LastName       NVARCHAR(100),
    @PhoneNumber    NVARCHAR(20) = NULL,
    @Department     NVARCHAR(100) = NULL,
    @Success        BIT OUTPUT,
    @Message        NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 0;
    SET @Message = 'Admin account not found.';

    IF EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [UserId] = @UserId)
    BEGIN
        BEGIN TRY
            BEGIN TRANSACTION;

            UPDATE [dbo].[Users]
            SET [FirstName] = @FirstName,
                [LastName] = @LastName,
                [PhoneNumber] = @PhoneNumber,
                [UpdatedAt] = SYSUTCDATETIME()
            WHERE [UserId] = @UserId;

            IF EXISTS (SELECT 1 FROM [dbo].[AdminProfiles] WHERE [UserId] = @UserId)
            BEGIN
                UPDATE [dbo].[AdminProfiles]
                SET [Department] = @Department
                WHERE [UserId] = @UserId;
            END
            ELSE
            BEGIN
                INSERT INTO [dbo].[AdminProfiles] ([UserId], [Department], [AccessLevel])
                VALUES (@UserId, @Department, 'Level 5 (Full Root)');
            END

            COMMIT TRANSACTION;

            SET @Success = 1;
            SET @Message = 'Administrator profile updated successfully.';
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
            SET @Success = 0;
            SET @Message = ERROR_MESSAGE();
        END CATCH
    END
END
GO

-- SP: sp_UpdateCourseMaterial
CREATE OR ALTER PROCEDURE dbo.sp_UpdateCourseMaterial
    @MaterialId     INT,
    @FacultyUserId  INT,
    @Title          NVARCHAR(255),
    @Description    NVARCHAR(500),
    @Category       NVARCHAR(50),
    @ExternalUrl    NVARCHAR(1000) = NULL,
    @FileName       NVARCHAR(255) = NULL,
    @FileSize       NVARCHAR(50) = NULL,
    @FileType       NVARCHAR(50) = NULL,
    @CloudUrl       NVARCHAR(1000) = NULL,
    @CloudPublicId  NVARCHAR(500) = NULL,
    @Success        BIT OUTPUT,
    @Message        NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM CourseMaterials WHERE MaterialId = @MaterialId AND FacultyUserId = @FacultyUserId AND IsActive = 1)
        BEGIN
            SET @Success = 0;
            SET @Message = 'Material not found or access denied.';
            RETURN;
        END

        IF @FileName IS NOT NULL AND @CloudUrl IS NOT NULL
        BEGIN
            UPDATE CourseMaterials
            SET Title = @Title,
                Description = @Description,
                Category = @Category,
                ExternalUrl = @ExternalUrl,
                FileName = @FileName,
                FileSize = @FileSize,
                FileType = @FileType,
                CloudUrl = @CloudUrl,
                CloudPublicId = @CloudPublicId,
                UpdatedAt = SYSUTCDATETIME()
            WHERE MaterialId = @MaterialId;
        END
        ELSE
        BEGIN
            UPDATE CourseMaterials
            SET Title = @Title,
                Description = @Description,
                Category = @Category,
                ExternalUrl = @ExternalUrl,
                UpdatedAt = SYSUTCDATETIME()
            WHERE MaterialId = @MaterialId;
        END

        SET @Success = 1;
        SET @Message = 'Material updated successfully.';
    END TRY
    BEGIN CATCH
        SET @Success = 0;
        SET @Message = ERROR_MESSAGE();
    END CATCH
END;
GO

-- SP: sp_UpdateFacultyProfile
CREATE OR ALTER PROCEDURE [dbo].[sp_UpdateFacultyProfile]
    @UserId         INT,
    @FirstName      NVARCHAR(100),
    @LastName       NVARCHAR(100),
    @PhoneNumber    NVARCHAR(20),
    @Department     NVARCHAR(100),
    @Designation    NVARCHAR(100),
    @Qualification  NVARCHAR(150),
    @OfficeLocation NVARCHAR(100),
    @Success        BIT OUTPUT,
    @Message        NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 0;

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE [dbo].[Users]
        SET [FirstName] = @FirstName,
            [LastName] = @LastName,
            [PhoneNumber] = @PhoneNumber,
            [UpdatedAt] = SYSUTCDATETIME()
        WHERE [UserId] = @UserId;

        IF EXISTS (SELECT 1 FROM [dbo].[FacultyProfiles] WHERE [UserId] = @UserId)
        BEGIN
            UPDATE [dbo].[FacultyProfiles]
            SET [Department] = @Department,
                [Designation] = @Designation,
                [Qualification] = @Qualification,
                [OfficeLocation] = @OfficeLocation
            WHERE [UserId] = @UserId;
        END
        ELSE
        BEGIN
            INSERT INTO [dbo].[FacultyProfiles] ([UserId], [EmployeeCode], [Department], [Designation], [Qualification], [OfficeLocation])
            VALUES (@UserId, 'FAC-' + CAST(@UserId AS NVARCHAR(10)), @Department, @Designation, @Qualification, @OfficeLocation);
        END

        COMMIT TRANSACTION;
        SET @Success = 1;
        SET @Message = 'Faculty profile updated successfully.';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        SET @Success = 0;
        SET @Message = ERROR_MESSAGE();
    END CATCH
END
GO

-- SP: sp_UpdateProfilePicture
CREATE OR ALTER PROCEDURE dbo.sp_UpdateProfilePicture
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

-- SP: sp_UpdateStudentProfile
-- 2. SP: Update Student Profile
CREATE   PROCEDURE [dbo].[sp_UpdateStudentProfile]
    @UserId         INT,
    @FirstName      NVARCHAR(100),
    @LastName       NVARCHAR(100),
    @PhoneNumber    NVARCHAR(20) = NULL,
    @Division       NVARCHAR(20) = NULL,
    @Success        BIT OUTPUT,
    @Message        NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @Success = 0;
    SET @Message = 'Profile not found.';

    IF EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [UserId] = @UserId)
    BEGIN
        BEGIN TRY
            BEGIN TRANSACTION;

            UPDATE [dbo].[Users]
            SET [FirstName] = @FirstName,
                [LastName] = @LastName,
                [PhoneNumber] = @PhoneNumber,
                [UpdatedAt] = SYSUTCDATETIME()
            WHERE [UserId] = @UserId;

            UPDATE [dbo].[StudentProfiles]
            SET [Division] = @Division
            WHERE [UserId] = @UserId;

            COMMIT TRANSACTION;

            SET @Success = 1;
            SET @Message = 'Profile updated successfully.';
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0
                ROLLBACK TRANSACTION;

            SET @Success = 0;
            SET @Message = ERROR_MESSAGE();
        END CATCH
    END
END
GO

-- SP: sp_UpdateTask
CREATE OR ALTER PROCEDURE dbo.sp_UpdateTask
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

-- SP: sp_UploadCourseMaterial
CREATE OR ALTER PROCEDURE dbo.sp_UploadCourseMaterial
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
    @ExternalUrl    NVARCHAR(1000) = NULL,
    @MaterialId     INT OUTPUT,
    @Success        BIT OUTPUT,
    @Message        NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM Classrooms WHERE ClassroomId = @ClassroomId AND FacultyUserId = @FacultyUserId)
        BEGIN
            SET @MaterialId = 0;
            SET @Success    = 0;
            SET @Message    = 'You are not authorized to upload materials to this classroom.';
            RETURN;
        END

        INSERT INTO CourseMaterials
            (ClassroomId, FacultyUserId, Title, Description, FileName, FileSize, FileType, Category, CloudUrl, CloudPublicId, ExternalUrl)
        VALUES
            (@ClassroomId, @FacultyUserId, @Title, @Description, @FileName, @FileSize, @FileType, @Category, @CloudUrl, @CloudPublicId, @ExternalUrl);

        SET @MaterialId = SCOPE_IDENTITY();
        SET @Success    = 1;
        SET @Message    = 'Material uploaded successfully.';
    END TRY
    BEGIN CATCH
        SET @MaterialId = 0;
        SET @Success    = 0;
        SET @Message    = ERROR_MESSAGE();
    END CATCH
END;
GO

-- SP: sp_ValidateAndRotateRefreshToken
-- ----------------------------------------------------------------------------
-- 5. SP: Validate and Rotate Refresh Token
-- ----------------------------------------------------------------------------
CREATE   PROCEDURE [dbo].[sp_ValidateAndRotateRefreshToken]
    @OldToken       NVARCHAR(500),
    @NewToken       NVARCHAR(500),
    @NewJwtId       NVARCHAR(100),
    @NewExpiresAt   DATETIME2(7),
    @IpAddress      NVARCHAR(50) = NULL,
    @UserId         INT OUTPUT,
    @IsValid        BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET @IsValid = 0;
    SET @UserId = 0;

    DECLARE @TokenId BIGINT;
    DECLARE @TokenUserId INT;
    DECLARE @ExpiresAt DATETIME2(7);
    DECLARE @RevokedAt DATETIME2(7);

    SELECT 
        @TokenId = [TokenId],
        @TokenUserId = [UserId],
        @ExpiresAt = [ExpiresAt],
        @RevokedAt = [RevokedAt]
    FROM [dbo].[RefreshTokens]
    WHERE [Token] = @OldToken;

    -- Valid if token exists, not revoked, and not expired
    IF (@TokenId IS NOT NULL AND @RevokedAt IS NULL AND @ExpiresAt > SYSUTCDATETIME())
    BEGIN
        -- Revoke the old token
        UPDATE [dbo].[RefreshTokens]
        SET [RevokedAt] = SYSUTCDATETIME(),
            [RevokedByIp] = @IpAddress,
            [ReplacedByToken] = @NewToken
        WHERE [TokenId] = @TokenId;

        -- Insert the new rotated token
        INSERT INTO [dbo].[RefreshTokens] 
            ([UserId], [Token], [JwtId], [ExpiresAt], [CreatedAt], [CreatedByIp])
        VALUES 
            (@TokenUserId, @NewToken, @NewJwtId, @NewExpiresAt, SYSUTCDATETIME(), @IpAddress);

        SET @UserId = @TokenUserId;
        SET @IsValid = 1;
    END
END
GO

-- ============================================================================
-- 7. INITIAL LOOKUP & DEVELOPMENT SEED DATA
-- ============================================================================
PRINT 'Seeding initial lookup records...';
GO

-- Seed Roles
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [RoleName] = 'Admin')
    INSERT INTO [dbo].[Roles] ([RoleName], [Description]) VALUES ('Admin', 'System Administrator with root management');
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [RoleName] = 'Faculty')
    INSERT INTO [dbo].[Roles] ([RoleName], [Description]) VALUES ('Faculty', 'Faculty / Instructor managing courses and grading');
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [RoleName] = 'Student')
    INSERT INTO [dbo].[Roles] ([RoleName], [Description]) VALUES ('Student', 'Student enrolled in courses and submitting tasks');
GO

-- Seed Departments
IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments] WHERE [DepartmentCode] = 'CE')
    INSERT INTO [dbo].[Departments] ([DepartmentName], [DepartmentCode], [IsActive]) VALUES ('Computer Engineering', 'CE', 1);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments] WHERE [DepartmentCode] = 'IT')
    INSERT INTO [dbo].[Departments] ([DepartmentName], [DepartmentCode], [IsActive]) VALUES ('Information Technology', 'IT', 1);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments] WHERE [DepartmentCode] = 'EC')
    INSERT INTO [dbo].[Departments] ([DepartmentName], [DepartmentCode], [IsActive]) VALUES ('Electronics & Communication', 'EC', 1);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments] WHERE [DepartmentCode] = 'ME')
    INSERT INTO [dbo].[Departments] ([DepartmentName], [DepartmentCode], [IsActive]) VALUES ('Mechanical Engineering', 'ME', 1);
IF NOT EXISTS (SELECT 1 FROM [dbo].[Departments] WHERE [DepartmentCode] = 'CL')
    INSERT INTO [dbo].[Departments] ([DepartmentName], [DepartmentCode], [IsActive]) VALUES ('Civil Engineering', 'CL', 1);
GO

-- Seed Semesters
IF NOT EXISTS (SELECT 1 FROM [dbo].[Semesters] WHERE [SemesterNumber] = 1)
BEGIN
    INSERT INTO [dbo].[Semesters] ([SemesterNumber], [SemesterName], [IsActive]) VALUES (1, 'Semester 1', 1);
    INSERT INTO [dbo].[Semesters] ([SemesterNumber], [SemesterName], [IsActive]) VALUES (2, 'Semester 2', 1);
    INSERT INTO [dbo].[Semesters] ([SemesterNumber], [SemesterName], [IsActive]) VALUES (3, 'Semester 3', 1);
    INSERT INTO [dbo].[Semesters] ([SemesterNumber], [SemesterName], [IsActive]) VALUES (4, 'Semester 4', 1);
    INSERT INTO [dbo].[Semesters] ([SemesterNumber], [SemesterName], [IsActive]) VALUES (5, 'Semester 5', 1);
    INSERT INTO [dbo].[Semesters] ([SemesterNumber], [SemesterName], [IsActive]) VALUES (6, 'Semester 6', 1);
    INSERT INTO [dbo].[Semesters] ([SemesterNumber], [SemesterName], [IsActive]) VALUES (7, 'Semester 7', 1);
    INSERT INTO [dbo].[Semesters] ([SemesterNumber], [SemesterName], [IsActive]) VALUES (8, 'Semester 8', 1);
END
GO

-- Seed Divisions
IF NOT EXISTS (SELECT 1 FROM [dbo].[Divisions] WHERE [DivisionName] = 'A')
BEGIN
    INSERT INTO [dbo].[Divisions] ([DivisionName], [IsActive]) VALUES ('A', 1);
    INSERT INTO [dbo].[Divisions] ([DivisionName], [IsActive]) VALUES ('B', 1);
    INSERT INTO [dbo].[Divisions] ([DivisionName], [IsActive]) VALUES ('C', 1);
    INSERT INTO [dbo].[Divisions] ([DivisionName], [IsActive]) VALUES ('D', 1);
END
GO

-- Seed Academic Years
IF NOT EXISTS (SELECT 1 FROM [dbo].[AcademicYears] WHERE [YearName] = '2025-2026')
BEGIN
    INSERT INTO [dbo].[AcademicYears] ([YearName], [IsActive]) VALUES ('2024-2025', 1);
    INSERT INTO [dbo].[AcademicYears] ([YearName], [IsActive]) VALUES ('2025-2026', 1);
    INSERT INTO [dbo].[AcademicYears] ([YearName], [IsActive]) VALUES ('2026-2027', 1);
END
GO

-- Seed Task Types
IF NOT EXISTS (SELECT 1 FROM [dbo].[TaskTypes] WHERE [TypeName] = 'Laboratory Assignment')
BEGIN
    INSERT INTO [dbo].[TaskTypes] ([TypeName], [IsActive]) VALUES ('Laboratory Assignment', 1);
    INSERT INTO [dbo].[TaskTypes] ([TypeName], [IsActive]) VALUES ('Term Work', 1);
    INSERT INTO [dbo].[TaskTypes] ([TypeName], [IsActive]) VALUES ('Theory Assignment', 1);
    INSERT INTO [dbo].[TaskTypes] ([TypeName], [IsActive]) VALUES ('Quiz / Assessment', 1);
    INSERT INTO [dbo].[TaskTypes] ([TypeName], [IsActive]) VALUES ('Project Milestone', 1);
    INSERT INTO [dbo].[TaskTypes] ([TypeName], [IsActive]) VALUES ('Research Paper', 1);
END
GO

-- Seed Institutions
IF NOT EXISTS (SELECT 1 FROM [dbo].[Institutions] WHERE [Code] = 'MU')
BEGIN
    INSERT INTO [dbo].[Institutions] ([InstitutionName], [Code], [City], [IsVerified], [IsActive])
    VALUES ('Marwadi University', 'MU', 'Rajkot', 1, 1);
END
GO

-- ============================================================================
-- 8. DEFAULT DEVELOPMENT ACCOUNTS (Password: Campus@123)
-- ============================================================================
PRINT 'Seeding default development accounts (Password: Campus@123)...';
GO

DECLARE @AdminRoleId   INT = (SELECT RoleId FROM Roles WHERE RoleName = 'Admin');
DECLARE @FacultyRoleId INT = (SELECT RoleId FROM Roles WHERE RoleName = 'Faculty');
DECLARE @StudentRoleId INT = (SELECT RoleId FROM Roles WHERE RoleName = 'Student');

-- SHA256 PBKDF2 hash & salt for default development password "Campus@123"
DECLARE @PassHash NVARCHAR(500) = '085B2D1EE0E5BFFCE71C569D1C34BC99E9BD72DE7178AA070E3A2F99266E394A';
DECLARE @PassSalt NVARCHAR(255) = 'jD1k4d92kFm10sA92kSls92mK19sl20';

-- 1. Default Admin Account
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'admin@campusvault.edu')
BEGIN
    INSERT INTO [dbo].[Users] ([RoleId], [Email], [PasswordHash], [PasswordSalt], [FirstName], [LastName], [PhoneNumber], [IsActive], [IsEmailVerified])
    VALUES (@AdminRoleId, 'admin@campusvault.edu', @PassHash, @PassSalt, 'System', 'Administrator', '+91 9876543210', 1, 1);
    
    DECLARE @AdminId INT = SCOPE_IDENTITY();
    INSERT INTO [dbo].[AdminProfiles] ([UserId], [Department], [AccessLevel])
    VALUES (@AdminId, 'IT & Infrastructure Directorate', 'Level 5 (Full Root)');
    PRINT 'Created default Admin account: admin@campusvault.edu / Campus@123';
END
GO

-- 2. Default Faculty Account
DECLARE @FacultyRoleId INT = (SELECT RoleId FROM Roles WHERE RoleName = 'Faculty');
DECLARE @PassHash NVARCHAR(500) = '085B2D1EE0E5BFFCE71C569D1C34BC99E9BD72DE7178AA070E3A2F99266E394A';
DECLARE @PassSalt NVARCHAR(255) = 'jD1k4d92kFm10sA92kSls92mK19sl20';

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'faculty@campusvault.edu')
BEGIN
    INSERT INTO [dbo].[Users] ([RoleId], [Email], [PasswordHash], [PasswordSalt], [FirstName], [LastName], [PhoneNumber], [IsActive], [IsEmailVerified])
    VALUES (@FacultyRoleId, 'faculty@campusvault.edu', @PassHash, @PassSalt, 'Rajesh', 'Patel', '+91 9876543211', 1, 1);
    
    DECLARE @FacId INT = SCOPE_IDENTITY();
    INSERT INTO [dbo].[FacultyProfiles] ([UserId], [EmployeeCode], [Department], [Designation], [Qualification], [OfficeLocation])
    VALUES (@FacId, 'FAC-CE-101', 'Computer Engineering', 'Associate Professor', 'Ph.D. in Computer Engineering', 'Block A, Room 408');
    PRINT 'Created default Faculty account: faculty@campusvault.edu / Campus@123';
END
GO

-- 3. Default Student Account
DECLARE @StudentRoleId INT = (SELECT RoleId FROM Roles WHERE RoleName = 'Student');
DECLARE @PassHash NVARCHAR(500) = '085B2D1EE0E5BFFCE71C569D1C34BC99E9BD72DE7178AA070E3A2F99266E394A';
DECLARE @PassSalt NVARCHAR(255) = 'jD1k4d92kFm10sA92kSls92mK19sl20';

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'student@campusvault.edu')
BEGIN
    INSERT INTO [dbo].[Users] ([RoleId], [Email], [PasswordHash], [PasswordSalt], [FirstName], [LastName], [PhoneNumber], [IsActive], [IsEmailVerified])
    VALUES (@StudentRoleId, 'student@campusvault.edu', @PassHash, @PassSalt, 'Prit', 'Pansuriya', '+91 9876543212', 1, 1);
    
    DECLARE @StudId INT = SCOPE_IDENTITY();
    INSERT INTO [dbo].[StudentProfiles] ([UserId], [EnrollmentNumber], [Department], [Semester], [Division], [RollNumber], [BatchYear])
    VALUES (@StudId, '22012011001', 'Computer Engineering', 5, 'A', '22CE042', 2022);
    PRINT 'Created default Student account: student@campusvault.edu / Campus@123';
END
GO

PRINT '============================================================';
PRINT 'CampusVaultDB database setup completed successfully!';
PRINT '============================================================';
GO
