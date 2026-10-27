-- ============================================================================
-- CAMPUSVAULT - AUTHENTICATION & IDENTITY STORED PROCEDURES
-- Database: CampusVaultDB
-- ============================================================================

USE [CampusVaultDB];
GO

-- ----------------------------------------------------------------------------
-- 1. SP: Authenticate User by Email
-- ----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[sp_AuthenticateUser]
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

-- ----------------------------------------------------------------------------
-- 2. SP: Register Student (Atomic Transaction)
-- ----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[sp_RegisterStudent]
    @Email              NVARCHAR(150),
    @PasswordHash       NVARCHAR(500),
    @PasswordSalt       NVARCHAR(255),
    @FirstName          NVARCHAR(100),
    @LastName           NVARCHAR(100),
    @PhoneNumber        NVARCHAR(20) = NULL,
    @EnrollmentNumber   NVARCHAR(50),
    @Department         NVARCHAR(100),
    @Semester           INT,
    @Division           NVARCHAR(20) = NULL,
    @RollNumber         NVARCHAR(30) = NULL,
    @BatchYear          INT = NULL,
    @NewUserId          INT OUTPUT,
    @StatusCode         INT OUTPUT,       -- 1 = Success, -1 = Email Exists, -2 = Enrollment Exists, -99 = Error
    @StatusMessage      NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @StatusCode = 1;
    SET @StatusMessage = 'Success';
    SET @NewUserId = 0;

    -- 1. Check if Email already exists
    IF EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = @Email)
    BEGIN
        SET @StatusCode = -1;
        SET @StatusMessage = 'Email is already registered.';
        RETURN;
    END

    -- 2. Check if EnrollmentNumber already exists
    IF EXISTS (SELECT 1 FROM [dbo].[StudentProfiles] WHERE [EnrollmentNumber] = @EnrollmentNumber)
    BEGIN
        SET @StatusCode = -2;
        SET @StatusMessage = 'Enrollment Number is already registered.';
        RETURN;
    END

    DECLARE @StudentRoleId INT;
    SELECT @StudentRoleId = [RoleId] FROM [dbo].[Roles] WHERE [RoleName] = 'Student';

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Insert into Users
        INSERT INTO [dbo].[Users] 
            ([RoleId], [Email], [PasswordHash], [PasswordSalt], [FirstName], [LastName], [PhoneNumber], [IsActive], [IsEmailVerified])
        VALUES 
            (@StudentRoleId, @Email, @PasswordHash, @PasswordSalt, @FirstName, @LastName, @PhoneNumber, 1, 1);

        SET @NewUserId = SCOPE_IDENTITY();

        -- Insert into StudentProfiles
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
        SET @NewUserId = 0;
    END CATCH
END
GO

-- ----------------------------------------------------------------------------
-- 3. SP: Register Faculty (Atomic Transaction)
-- ----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[sp_RegisterFaculty]
    @Email              NVARCHAR(150),
    @PasswordHash       NVARCHAR(500),
    @PasswordSalt       NVARCHAR(255),
    @FirstName          NVARCHAR(100),
    @LastName           NVARCHAR(100),
    @PhoneNumber        NVARCHAR(20) = NULL,
    @EmployeeCode       NVARCHAR(50),
    @Department         NVARCHAR(100),
    @Designation        NVARCHAR(100),
    @Qualification      NVARCHAR(100) = NULL,
    @OfficeLocation     NVARCHAR(100) = NULL,
    @NewUserId          INT OUTPUT,
    @StatusCode         INT OUTPUT,       -- 1 = Success, -1 = Email Exists, -2 = EmployeeCode Exists, -99 = Error
    @StatusMessage      NVARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @StatusCode = 1;
    SET @StatusMessage = 'Success';
    SET @NewUserId = 0;

    -- 1. Check if Email already exists
    IF EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = @Email)
    BEGIN
        SET @StatusCode = -1;
        SET @StatusMessage = 'Email is already registered.';
        RETURN;
    END

    -- 2. Check if EmployeeCode already exists
    IF EXISTS (SELECT 1 FROM [dbo].[FacultyProfiles] WHERE [EmployeeCode] = @EmployeeCode)
    BEGIN
        SET @StatusCode = -2;
        SET @StatusMessage = 'Employee code is already registered.';
        RETURN;
    END

    DECLARE @FacultyRoleId INT;
    SELECT @FacultyRoleId = [RoleId] FROM [dbo].[Roles] WHERE [RoleName] = 'Faculty';

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Insert into Users
        INSERT INTO [dbo].[Users] 
            ([RoleId], [Email], [PasswordHash], [PasswordSalt], [FirstName], [LastName], [PhoneNumber], [IsActive], [IsEmailVerified])
        VALUES 
            (@FacultyRoleId, @Email, @PasswordHash, @PasswordSalt, @FirstName, @LastName, @PhoneNumber, 1, 1);

        SET @NewUserId = SCOPE_IDENTITY();

        -- Insert into FacultyProfiles
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
        SET @NewUserId = 0;
    END CATCH
END
GO

-- ----------------------------------------------------------------------------
-- 4. SP: Save Refresh Token
-- ----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[sp_SaveRefreshToken]
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

-- ----------------------------------------------------------------------------
-- 5. SP: Validate and Rotate Refresh Token
-- ----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[sp_ValidateAndRotateRefreshToken]
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

-- ----------------------------------------------------------------------------
-- 6. SP: Log User Authentication Action (Audit)
-- ----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[sp_LogUserAuth]
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
