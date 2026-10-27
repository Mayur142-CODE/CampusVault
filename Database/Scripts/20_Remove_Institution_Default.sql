-- ============================================================================
-- 20_Remove_Institution_Default.sql
-- CampusVault: Remove hardcoded/invented institution defaults in registration SPs
-- ============================================================================

USE [CampusVaultDB];
GO

-- 1. Update sp_RegisterStudent
IF OBJECT_ID('dbo.sp_RegisterStudent', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_RegisterStudent;
GO

CREATE PROCEDURE dbo.sp_RegisterStudent
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

-- 2. Update sp_RegisterFaculty
IF OBJECT_ID('dbo.sp_RegisterFaculty', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_RegisterFaculty;
GO

CREATE PROCEDURE dbo.sp_RegisterFaculty
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

-- 3. Update sp_RegisterAdmin
IF OBJECT_ID('dbo.sp_RegisterAdmin', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_RegisterAdmin;
GO

CREATE PROCEDURE dbo.sp_RegisterAdmin
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
