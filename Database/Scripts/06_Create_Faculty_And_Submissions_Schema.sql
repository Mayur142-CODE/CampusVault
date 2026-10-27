-- ============================================================================
-- CAMPUSVAULT - FACULTY PORTAL & SUBMISSIONS SCHEMA & PROCEDURES
-- Target: SQL Server 2016+
-- ============================================================================

USE [CampusVaultDB];
GO

-- 1. TASK SUBMISSIONS TABLE
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TaskSubmissions')
BEGIN
    CREATE TABLE [dbo].[TaskSubmissions] (
        [SubmissionId]      INT IDENTITY(1,1) NOT NULL,
        [TaskId]            INT NOT NULL,
        [StudentUserId]     INT NOT NULL,
        [SubmissionTitle]   NVARCHAR(200) NOT NULL CONSTRAINT [DF_Submissions_Title] DEFAULT ('Assignment Submission'),
        [FileName]          NVARCHAR(255) NOT NULL,
        [FilePath]          NVARCHAR(500) NULL,
        [FileSize]          NVARCHAR(50) NOT NULL CONSTRAINT [DF_Submissions_Size] DEFAULT ('2.4 MB'),
        [Status]            NVARCHAR(50) NOT NULL CONSTRAINT [DF_Submissions_Status] DEFAULT ('PendingReview'),
        [Score]             INT NULL,
        [Feedback]          NVARCHAR(MAX) NULL,
        [SubmittedAt]       DATETIME2(7) NOT NULL CONSTRAINT [DF_Submissions_SubmittedAt] DEFAULT SYSUTCDATETIME(),
        [ReviewedAt]        DATETIME2(7) NULL,
        CONSTRAINT [PK_TaskSubmissions] PRIMARY KEY CLUSTERED ([SubmissionId] ASC),
        CONSTRAINT [FK_TaskSubmissions_Task] FOREIGN KEY ([TaskId]) REFERENCES [dbo].[Tasks] ([TaskId]) ON DELETE CASCADE,
        CONSTRAINT [FK_TaskSubmissions_Student] FOREIGN KEY ([StudentUserId]) REFERENCES [dbo].[Users] ([UserId])
    );
    CREATE NONCLUSTERED INDEX [IX_TaskSubmissions_Task] ON [dbo].[TaskSubmissions] ([TaskId] ASC);
    CREATE NONCLUSTERED INDEX [IX_TaskSubmissions_Student] ON [dbo].[TaskSubmissions] ([StudentUserId] ASC);
END
GO

-- 2. SEED SAMPLE SUBMISSIONS IF EMPTY
IF NOT EXISTS (SELECT 1 FROM [dbo].[TaskSubmissions])
BEGIN
    DECLARE @StudentId INT = (SELECT TOP 1 [UserId] FROM [dbo].[Users] WHERE [RoleId] = 1);
    IF (@StudentId IS NOT NULL)
    BEGIN
        INSERT INTO [dbo].[TaskSubmissions] ([TaskId], [StudentUserId], [SubmissionTitle], [FileName], [FileSize], [Status], [Score], [Feedback], [SubmittedAt])
        VALUES
        (1, @StudentId, 'SQL Stored Procedures Lab Work', 'Prit_Pansuriyaa_Practical_04.pdf', '3.2 MB', 'PendingReview', NULL, NULL, DATEADD(hour, -5, SYSUTCDATETIME())),
        (2, @StudentId, 'B-Tree Query Optimization Report', 'Prit_Pansuriyaa_Practical_05.pdf', '1.8 MB', 'PendingReview', NULL, NULL, DATEADD(day, -1, SYSUTCDATETIME())),
        (5, @StudentId, 'REST API & JWT Token Auth Implementation', 'Prit_Pansuriyaa_Milestone_2.zip', '4.5 MB', 'Reviewed', 24, 'Excellent modular architecture and clean token claims handling.', DATEADD(day, -2, SYSUTCDATETIME()));
    END
END
GO

-- 3. STORED PROCEDURE: sp_GetFacultyDashboardData
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_GetFacultyDashboardData')
    DROP PROCEDURE [dbo].[sp_GetFacultyDashboardData];
GO

CREATE PROCEDURE [dbo].[sp_GetFacultyDashboardData]
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

-- 4. STORED PROCEDURE: sp_GetFacultyClassrooms
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_GetFacultyClassrooms')
    DROP PROCEDURE [dbo].[sp_GetFacultyClassrooms];
GO

CREATE PROCEDURE [dbo].[sp_GetFacultyClassrooms]
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

-- 5. STORED PROCEDURE: sp_CreateClassroom
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_CreateClassroom')
    DROP PROCEDURE [dbo].[sp_CreateClassroom];
GO

CREATE PROCEDURE [dbo].[sp_CreateClassroom]
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

-- 6. STORED PROCEDURE: sp_GetFacultyTasks
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_GetFacultyTasks')
    DROP PROCEDURE [dbo].[sp_GetFacultyTasks];
GO

CREATE PROCEDURE [dbo].[sp_GetFacultyTasks]
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

-- 7. STORED PROCEDURE: sp_CreateTask
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_CreateTask')
    DROP PROCEDURE [dbo].[sp_CreateTask];
GO

CREATE PROCEDURE [dbo].[sp_CreateTask]
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

-- 8. STORED PROCEDURE: sp_GetFacultyProfile
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_GetFacultyProfile')
    DROP PROCEDURE [dbo].[sp_GetFacultyProfile];
GO

CREATE PROCEDURE [dbo].[sp_GetFacultyProfile]
    @UserId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        u.[UserId],
        u.[FirstName],
        u.[LastName],
        (u.[FirstName] + ' ' + u.[LastName]) AS [FullName],
        u.[Email],
        u.[PhoneNumber],
        fp.[EmployeeCode],
        fp.[Department],
        fp.[Designation],
        fp.[Qualification],
        fp.[OfficeLocation]
    FROM [dbo].[Users] u
    LEFT JOIN [dbo].[FacultyProfiles] fp ON u.[UserId] = fp.[UserId]
    WHERE u.[UserId] = @UserId;
END
GO

-- 9. STORED PROCEDURE: sp_UpdateFacultyProfile
IF EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = 'sp_UpdateFacultyProfile')
    DROP PROCEDURE [dbo].[sp_UpdateFacultyProfile];
GO

CREATE PROCEDURE [dbo].[sp_UpdateFacultyProfile]
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

PRINT 'Faculty portal & submissions schema created successfully.';
