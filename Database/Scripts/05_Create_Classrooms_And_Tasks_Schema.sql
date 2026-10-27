-- ============================================================================
-- CAMPUSVAULT - CLASSROOMS, ENROLLMENTS & TASKS SCHEMA & PROCEDURES
-- Target: SQL Server 2016+
-- ============================================================================

USE [CampusVaultDB];
GO

-- 1. CLASSROOMS TABLE
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Classrooms')
BEGIN
    CREATE TABLE [dbo].[Classrooms] (
        [ClassroomId]       INT IDENTITY(1,1) NOT NULL,
        [CourseCode]        NVARCHAR(20) NOT NULL,
        [CourseName]        NVARCHAR(150) NOT NULL,
        [Department]        NVARCHAR(100) NOT NULL,
        [Semester]          INT NOT NULL,
        [AcademicYear]      NVARCHAR(20) NOT NULL CONSTRAINT [DF_Classrooms_Year] DEFAULT ('2026-2027'),
        [JoinCode]          NVARCHAR(20) NOT NULL,
        [FacultyUserId]     INT NOT NULL,
        [ThemeGradient]     NVARCHAR(100) NOT NULL CONSTRAINT [DF_Classrooms_Theme] DEFAULT ('linear-gradient(135deg, #1e3a8a 0%, #3b82f6 100%)'),
        [CreatedAt]         DATETIME2(7) NOT NULL CONSTRAINT [DF_Classrooms_Created] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Classrooms] PRIMARY KEY CLUSTERED ([ClassroomId] ASC),
        CONSTRAINT [UQ_Classrooms_JoinCode] UNIQUE NONCLUSTERED ([JoinCode] ASC),
        CONSTRAINT [FK_Classrooms_Faculty] FOREIGN KEY ([FacultyUserId]) REFERENCES [dbo].[Users] ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_Classrooms_Department_Semester] ON [dbo].[Classrooms] ([Department] ASC, [Semester] ASC);
END
GO

-- 2. CLASSROOM ENROLLMENTS TABLE
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClassroomEnrollments')
BEGIN
    CREATE TABLE [dbo].[ClassroomEnrollments] (
        [EnrollmentId]      INT IDENTITY(1,1) NOT NULL,
        [ClassroomId]       INT NOT NULL,
        [StudentUserId]     INT NOT NULL,
        [EnrolledAt]        DATETIME2(7) NOT NULL CONSTRAINT [DF_Enrollments_EnrolledAt] DEFAULT SYSUTCDATETIME(),
        [Status]            NVARCHAR(20) NOT NULL CONSTRAINT [DF_Enrollments_Status] DEFAULT ('Active'),
        CONSTRAINT [PK_ClassroomEnrollments] PRIMARY KEY CLUSTERED ([EnrollmentId] ASC),
        CONSTRAINT [UQ_Enrollments_StudentClass] UNIQUE NONCLUSTERED ([ClassroomId] ASC, [StudentUserId] ASC),
        CONSTRAINT [FK_Enrollments_Classroom] FOREIGN KEY ([ClassroomId]) REFERENCES [dbo].[Classrooms] ([ClassroomId]) ON DELETE CASCADE,
        CONSTRAINT [FK_Enrollments_Student] FOREIGN KEY ([StudentUserId]) REFERENCES [dbo].[Users] ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_Enrollments_Student] ON [dbo].[ClassroomEnrollments] ([StudentUserId] ASC);
END
GO

-- 3. TASKS TABLE
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tasks')
BEGIN
    CREATE TABLE [dbo].[Tasks] (
        [TaskId]            INT IDENTITY(1,1) NOT NULL,
        [ClassroomId]       INT NOT NULL,
        [Title]             NVARCHAR(200) NOT NULL,
        [Description]       NVARCHAR(MAX) NULL,
        [TaskType]          NVARCHAR(50) NOT NULL CONSTRAINT [DF_Tasks_Type] DEFAULT ('Assignment'),
        [MaxScore]          INT NOT NULL CONSTRAINT [DF_Tasks_MaxScore] DEFAULT (20),
        [DueDate]           DATETIME2(7) NOT NULL,
        [Status]            NVARCHAR(20) NOT NULL CONSTRAINT [DF_Tasks_Status] DEFAULT ('Active'),
        [CreatedAt]         DATETIME2(7) NOT NULL CONSTRAINT [DF_Tasks_Created] DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Tasks] PRIMARY KEY CLUSTERED ([TaskId] ASC),
        CONSTRAINT [FK_Tasks_Classroom] FOREIGN KEY ([ClassroomId]) REFERENCES [dbo].[Classrooms] ([ClassroomId]) ON DELETE CASCADE
    );
    CREATE NONCLUSTERED INDEX [IX_Tasks_ClassroomId] ON [dbo].[Tasks] ([ClassroomId] ASC);
END
GO

-- 4. SEED SAMPLE FACULTY USER IF NEEDED
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = 'faculty.demo@campusvault.edu')
BEGIN
    INSERT INTO [dbo].[Users] ([RoleId], [Email], [PasswordHash], [PasswordSalt], [FirstName], [LastName], [PhoneNumber], [IsActive], [IsEmailVerified])
    VALUES (
        2,
        'faculty.demo@campusvault.edu',
        'C0B820624BE7274E8EF64AC44DEAE5D2F6433F6E20786DF9C1B8EF04FE2952A3',
        'demo_salt_sample_123',
        'Vikram',
        'Sharma',
        '+91 98250 11223',
        1,
        1
    );

    DECLARE @FacultyId INT = SCOPE_IDENTITY();
    INSERT INTO [dbo].[FacultyProfiles] ([UserId], [EmployeeCode], [Department], [Designation], [Qualification], [OfficeLocation])
    VALUES (@FacultyId, 'EMP-1042', 'Computer Engineering', 'Associate Professor', 'Ph.D in Database Systems', 'Lab Block B-302');
END
GO

-- 5. SEED SAMPLE CLASSROOMS & TASKS
DECLARE @FacultyUserId INT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [RoleId] = 2);

IF NOT EXISTS (SELECT 1 FROM [dbo].[Classrooms] WHERE [CourseCode] = 'DBMS-501')
BEGIN
    INSERT INTO [dbo].[Classrooms] ([CourseCode], [CourseName], [Department], [Semester], [AcademicYear], [JoinCode], [FacultyUserId], [ThemeGradient])
    VALUES ('DBMS-501', 'Database Management Systems', 'Computer Engineering', 5, '2026-2027', 'DBMS-501', @FacultyUserId, 'linear-gradient(135deg, #1e3a8a 0%, #3b82f6 100%)');
    
    DECLARE @C1 INT = SCOPE_IDENTITY();
    INSERT INTO [dbo].[Tasks] ([ClassroomId], [Title], [Description], [TaskType], [MaxScore], [DueDate])
    VALUES 
        (@C1, 'Practical 04: SQL Stored Procedures & Indexing', 'Implement stored procedures with output parameters, transaction handling, and index optimization.', 'Assignment', 20, DATEADD(DAY, 4, SYSUTCDATETIME())),
        (@C1, 'Practical 05: B-Tree Indexes & Query Execution Plans', 'Analyze execution plans using SQL Server Profiler and optimize slow queries.', 'Assignment', 20, DATEADD(DAY, 10, SYSUTCDATETIME()));
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Classrooms] WHERE [CourseCode] = 'NSC-502')
BEGIN
    INSERT INTO [dbo].[Classrooms] ([CourseCode], [CourseName], [Department], [Semester], [AcademicYear], [JoinCode], [FacultyUserId], [ThemeGradient])
    VALUES ('NSC-502', 'Network Security & Cryptography', 'Computer Engineering', 5, '2026-2027', 'NSC-502', @FacultyUserId, 'linear-gradient(135deg, #4c1d95 0%, #7c3aed 100%)');
    
    DECLARE @C2 INT = SCOPE_IDENTITY();
    INSERT INTO [dbo].[Tasks] ([ClassroomId], [Title], [Description], [TaskType], [MaxScore], [DueDate])
    VALUES 
        (@C2, 'Lab 03: Penetration Testing & Vulnerability Assessment', 'Perform vulnerability scan on test Linux VM using OpenVAS and produce an executive audit report.', 'Lab', 20, DATEADD(DAY, 6, SYSUTCDATETIME())),
        (@C2, 'Lab 04: RSA Asymmetric Key Cryptography in C#', 'Implement 2048-bit RSA key exchange with digital signatures.', 'Lab', 20, DATEADD(DAY, 14, SYSUTCDATETIME()));
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Classrooms] WHERE [CourseCode] = 'SWE-503')
BEGIN
    INSERT INTO [dbo].[Classrooms] ([CourseCode], [CourseName], [Department], [Semester], [AcademicYear], [JoinCode], [FacultyUserId], [ThemeGradient])
    VALUES ('SWE-503', 'Full Stack Web Engineering', 'Computer Engineering', 5, '2026-2027', 'SWE-503', @FacultyUserId, 'linear-gradient(135deg, #065f46 0%, #10b981 100%)');

    DECLARE @C3 INT = SCOPE_IDENTITY();
    INSERT INTO [dbo].[Tasks] ([ClassroomId], [Title], [Description], [TaskType], [MaxScore], [DueDate])
    VALUES 
        (@C3, 'Project Milestone 2: RESTful API Integration & JWT', 'Develop modular REST API endpoints in ASP.NET MVC with token authentication.', 'Project', 25, DATEADD(DAY, 8, SYSUTCDATETIME()));
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[Classrooms] WHERE [CourseCode] = 'CID-504')
BEGIN
    INSERT INTO [dbo].[Classrooms] ([CourseCode], [CourseName], [Department], [Semester], [AcademicYear], [JoinCode], [FacultyUserId], [ThemeGradient])
    VALUES ('CID-504', 'Cloud Infrastructure & DevOps', 'Computer Engineering', 5, '2026-2027', 'CID-504', @FacultyUserId, 'linear-gradient(135deg, #9a3412 0%, #f97316 100%)');

    DECLARE @C4 INT = SCOPE_IDENTITY();
    INSERT INTO [dbo].[Tasks] ([ClassroomId], [Title], [Description], [TaskType], [MaxScore], [DueDate])
    VALUES 
        (@C4, 'Lab 02: Multi-Container Docker Compose Deployment', 'Configure docker-compose.yml for web application, database, and reverse proxy.', 'Assignment', 20, DATEADD(DAY, 12, SYSUTCDATETIME()));
END
GO

-- 6. ENROLL EXISTING STUDENT (UserId = 1) IN DEFAULT CLASSROOMS
DECLARE @StudentId INT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [RoleId] = 1);
IF @StudentId IS NOT NULL
BEGIN
    INSERT INTO [dbo].[ClassroomEnrollments] ([ClassroomId], [StudentUserId])
    SELECT [ClassroomId], @StudentId
    FROM [dbo].[Classrooms]
    WHERE [ClassroomId] NOT IN (
        SELECT [ClassroomId] FROM [dbo].[ClassroomEnrollments] WHERE [StudentUserId] = @StudentId
    );
END
GO

-- ----------------------------------------------------------------------------
-- STORED PROCEDURES
-- ----------------------------------------------------------------------------

-- SP 1: Get Student Classrooms
CREATE OR ALTER PROCEDURE [dbo].[sp_GetStudentClassrooms]
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

-- SP 2: Join Classroom by Code
CREATE OR ALTER PROCEDURE [dbo].[sp_JoinClassroom]
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

-- SP 3: Get Classroom Details & Tasks
CREATE OR ALTER PROCEDURE [dbo].[sp_GetClassroomDetails]
    @ClassroomId    INT,
    @StudentUserId INT
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
        fp.[OfficeLocation] AS [FacultyOffice]
    FROM [dbo].[Classrooms] c
    INNER JOIN [dbo].[Users] u ON c.[FacultyUserId] = u.[UserId]
    LEFT JOIN [dbo].[FacultyProfiles] fp ON u.[UserId] = fp.[UserId]
    WHERE c.[ClassroomId] = @ClassroomId;

    -- Result 2: Tasks List
    SELECT 
        t.[TaskId],
        t.[ClassroomId],
        t.[Title],
        t.[Description],
        t.[TaskType],
        t.[MaxScore],
        t.[DueDate],
        t.[Status]
    FROM [dbo].[Tasks] t
    WHERE t.[ClassroomId] = @ClassroomId
    ORDER BY t.[DueDate] ASC;
END
GO

-- SP 4: Get Specific Task Details
CREATE OR ALTER PROCEDURE [dbo].[sp_GetTaskDetails]
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
