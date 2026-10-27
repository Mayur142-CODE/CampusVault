-- ============================================================================
-- CAMPUSVAULT - ENTERPRISE AUTHENTICATION & IDENTITY DATABASE SCHEMA
-- Target Database: SQL Server 2016+ / 2022
-- ============================================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'CampusVaultDB')
BEGIN
    CREATE DATABASE [CampusVaultDB];
END
GO

USE [CampusVaultDB];
GO

-- 1. ROLES TABLE
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Roles')
BEGIN
    CREATE TABLE [dbo].[Roles] (
        [RoleId]        INT IDENTITY(1,1) NOT NULL,
        [RoleName]      NVARCHAR(50) NOT NULL,
        [Description]   NVARCHAR(255) NULL,
        [CreatedAt]     DATETIME2(7) NOT NULL CONSTRAINT [DF_Roles_CreatedAt] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([RoleId] ASC),
        CONSTRAINT [UQ_Roles_RoleName] UNIQUE NONCLUSTERED ([RoleName] ASC)
    );
END
GO

-- 2. USERS TABLE
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
        [IsActive]          BIT NOT NULL CONSTRAINT [DF_Users_IsActive] DEFAULT (1),
        [IsEmailVerified]   BIT NOT NULL CONSTRAINT [DF_Users_IsEmailVerified] DEFAULT (0),
        [LastLoginAt]       DATETIME2(7) NULL,
        [CreatedAt]         DATETIME2(7) NOT NULL CONSTRAINT [DF_Users_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [UpdatedAt]         DATETIME2(7) NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([UserId] ASC),
        CONSTRAINT [UQ_Users_Email] UNIQUE NONCLUSTERED ([Email] ASC),
        CONSTRAINT [FK_Users_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([RoleId])
    );
    CREATE NONCLUSTERED INDEX [IX_Users_RoleId] ON [dbo].[Users] ([RoleId] ASC);
    CREATE NONCLUSTERED INDEX [IX_Users_Email_IsActive] ON [dbo].[Users] ([Email] ASC, [IsActive] ASC);
END
GO

-- 3. STUDENT PROFILES (1-to-1 with Users where Role = 'Student')
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StudentProfiles')
BEGIN
    CREATE TABLE [dbo].[StudentProfiles] (
        [UserId]            INT NOT NULL,
        [EnrollmentNumber]  NVARCHAR(50) NOT NULL,
        [Department]        NVARCHAR(100) NOT NULL,
        [Semester]          INT NOT NULL CONSTRAINT [CK_Student_Semester] CHECK ([Semester] BETWEEN 1 AND 12),
        [Division]          NVARCHAR(20) NULL,
        [RollNumber]        NVARCHAR(30) NULL,
        [BatchYear]         INT NULL,
        CONSTRAINT [PK_StudentProfiles] PRIMARY KEY CLUSTERED ([UserId] ASC),
        CONSTRAINT [UQ_StudentProfiles_Enrollment] UNIQUE NONCLUSTERED ([EnrollmentNumber] ASC),
        CONSTRAINT [FK_StudentProfiles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );
END
GO

-- 4. FACULTY PROFILES (1-to-1 with Users where Role = 'Faculty')
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FacultyProfiles')
BEGIN
    CREATE TABLE [dbo].[FacultyProfiles] (
        [UserId]            INT NOT NULL,
        [EmployeeCode]      NVARCHAR(50) NOT NULL,
        [Department]        NVARCHAR(100) NOT NULL,
        [Designation]       NVARCHAR(100) NOT NULL,
        [Qualification]     NVARCHAR(100) NULL,
        [OfficeLocation]    NVARCHAR(100) NULL,
        CONSTRAINT [PK_FacultyProfiles] PRIMARY KEY CLUSTERED ([UserId] ASC),
        CONSTRAINT [UQ_FacultyProfiles_EmployeeCode] UNIQUE NONCLUSTERED ([EmployeeCode] ASC),
        CONSTRAINT [FK_FacultyProfiles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );
END
GO

-- 5. ADMIN PROFILES (1-to-1 with Users where Role = 'Admin')
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AdminProfiles')
BEGIN
    CREATE TABLE [dbo].[AdminProfiles] (
        [UserId]            INT NOT NULL,
        [Department]        NVARCHAR(100) NOT NULL CONSTRAINT [DF_AdminProfiles_Dept] DEFAULT ('Administration'),
        [AccessLevel]       NVARCHAR(50) NOT NULL CONSTRAINT [DF_AdminProfiles_Access] DEFAULT ('SuperAdmin'),
        CONSTRAINT [PK_AdminProfiles] PRIMARY KEY CLUSTERED ([UserId] ASC),
        CONSTRAINT [FK_AdminProfiles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );
END
GO

-- 6. REFRESH TOKENS TABLE (For Enterprise JWT Authentication)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RefreshTokens')
BEGIN
    CREATE TABLE [dbo].[RefreshTokens] (
        [TokenId]           BIGINT IDENTITY(1,1) NOT NULL,
        [UserId]            INT NOT NULL,
        [Token]             NVARCHAR(500) NOT NULL,
        [JwtId]             NVARCHAR(100) NULL,
        [ExpiresAt]         DATETIME2(7) NOT NULL,
        [CreatedAt]         DATETIME2(7) NOT NULL CONSTRAINT [DF_RefreshTokens_CreatedAt] DEFAULT SYSUTCDATETIME(),
        [CreatedByIp]       NVARCHAR(50) NULL,
        [RevokedAt]         DATETIME2(7) NULL,
        [RevokedByIp]       NVARCHAR(50) NULL,
        [ReplacedByToken]   NVARCHAR(500) NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY CLUSTERED ([TokenId] ASC),
        CONSTRAINT [UQ_RefreshTokens_Token] UNIQUE NONCLUSTERED ([Token] ASC),
        CONSTRAINT [FK_RefreshTokens_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_RefreshTokens_UserId] ON [dbo].[RefreshTokens] ([UserId] ASC);
END
GO

-- 7. USER AUTH AUDIT LOG TABLE
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserAuthLogs')
BEGIN
    CREATE TABLE [dbo].[UserAuthLogs] (
        [LogId]         BIGINT IDENTITY(1,1) NOT NULL,
        [UserId]        INT NULL,
        [Action]        NVARCHAR(50) NOT NULL, -- 'LOGIN_SUCCESS', 'LOGIN_FAILED', 'LOGOUT', 'REGISTER', 'TOKEN_REFRESH'
        [IpAddress]     NVARCHAR(50) NULL,
        [UserAgent]     NVARCHAR(500) NULL,
        [Details]       NVARCHAR(MAX) NULL,
        [CreatedAt]     DATETIME2(7) NOT NULL CONSTRAINT [DF_UserAuthLogs_CreatedAt] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_UserAuthLogs] PRIMARY KEY CLUSTERED ([LogId] ASC)
    );
    CREATE NONCLUSTERED INDEX [IX_UserAuthLogs_UserId_CreatedAt] ON [dbo].[UserAuthLogs] ([UserId] ASC, [CreatedAt] DESC);
END
GO

-- ============================================================================
-- SEED INITIAL SYSTEM DATA (Roles & Default Test Accounts)
-- Password for all seed accounts is: "Campus@123"
-- Salt: "CV_SECURE_SALT_2026_CAMPUSVAULT"
-- SHA256 Hash of ("Campus@123" + Salt):
-- ============================================================================

-- Seed Roles
IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [RoleName] = 'Admin')
    INSERT INTO [dbo].[Roles] ([RoleName], [Description]) VALUES ('Admin', 'System Administrator with full access');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [RoleName] = 'Faculty')
    INSERT INTO [dbo].[Roles] ([RoleName], [Description]) VALUES ('Faculty', 'Faculty / Professor managing classrooms and evaluations');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Roles] WHERE [RoleName] = 'Student')
    INSERT INTO [dbo].[Roles] ([RoleName], [Description]) VALUES ('Student', 'Enrolled student attending classes and submitting tasks');
GO

-- Seed Admin User (Email: admin@campusvault.edu / Pass: Campus@123)
-- Hash calculation: SHA256('Campus@123CV_SECURE_SALT_2026_CAMPUSVAULT')
DECLARE @Salt NVARCHAR(255) = N'CV_SECURE_SALT_2026_CAMPUSVAULT';
DECLARE @PassHash NVARCHAR(500) = CONVERT(NVARCHAR(500), HASHBYTES('SHA2_256', N'Campus@123' + @Salt), 2);
DECLARE @AdminRoleId INT = (SELECT [RoleId] FROM [dbo].[Roles] WHERE [RoleName] = 'Admin');
DECLARE @FacultyRoleId INT = (SELECT [RoleId] FROM [dbo].[Roles] WHERE [RoleName] = 'Faculty');
DECLARE @StudentRoleId INT = (SELECT [RoleId] FROM [dbo].[Roles] WHERE [RoleName] = 'Student');

IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'admin@campusvault.edu')
BEGIN
    INSERT INTO [dbo].[Users] ([RoleId], [Email], [PasswordHash], [PasswordSalt], [FirstName], [LastName], [PhoneNumber], [IsActive], [IsEmailVerified])
    VALUES (@AdminRoleId, 'admin@campusvault.edu', @PassHash, @Salt, 'System', 'Administrator', '+91 9876543210', 1, 1);

    DECLARE @AdminUserId INT = SCOPE_IDENTITY();
    INSERT INTO [dbo].[AdminProfiles] ([UserId], [Department], [AccessLevel])
    VALUES (@AdminUserId, 'IT Administration', 'SuperAdmin');
END

-- Seed Faculty User (Email: faculty@campusvault.edu / Pass: Campus@123)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'faculty@campusvault.edu')
BEGIN
    INSERT INTO [dbo].[Users] ([RoleId], [Email], [PasswordHash], [PasswordSalt], [FirstName], [LastName], [PhoneNumber], [IsActive], [IsEmailVerified])
    VALUES (@FacultyRoleId, 'faculty@campusvault.edu', @PassHash, @Salt, 'Prof. Rahul', 'Patel', '+91 9876543211', 1, 1);

    DECLARE @FacultyUserId INT = SCOPE_IDENTITY();
    INSERT INTO [dbo].[FacultyProfiles] ([UserId], [EmployeeCode], [Department], [Designation], [Qualification], [OfficeLocation])
    VALUES (@FacultyUserId, 'FAC-CE-101', 'Computer Engineering', 'Associate Professor', 'Ph.D. in Computer Science', 'Block B, Room 304');
END

-- Seed Student User (Email: student@campusvault.edu / Pass: Campus@123)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'student@campusvault.edu')
BEGIN
    INSERT INTO [dbo].[Users] ([RoleId], [Email], [PasswordHash], [PasswordSalt], [FirstName], [LastName], [PhoneNumber], [IsActive], [IsEmailVerified])
    VALUES (@StudentRoleId, 'student@campusvault.edu', @PassHash, @Salt, 'Prit', 'Pansuriyaa', '+91 9876543212', 1, 1);

    DECLARE @StudentUserId INT = SCOPE_IDENTITY();
    INSERT INTO [dbo].[StudentProfiles] ([UserId], [EnrollmentNumber], [Department], [Semester], [Division], [RollNumber], [BatchYear])
    VALUES (@StudentUserId, '22012011001', 'Computer Engineering', 3, 'EC6', '22CE042', 2022);
END
GO
