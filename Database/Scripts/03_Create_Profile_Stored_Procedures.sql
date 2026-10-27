-- ============================================================================
-- CAMPUSVAULT - STUDENT PROFILE & SETTINGS STORED PROCEDURES
-- Database: CampusVaultDB
-- ============================================================================

USE [CampusVaultDB];
GO

-- 1. SP: Get User Profile By ID
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
        sp.[BatchYear]
    FROM [dbo].[Users] u
    INNER JOIN [dbo].[Roles] r ON u.[RoleId] = r.[RoleId]
    LEFT JOIN [dbo].[StudentProfiles] sp ON u.[UserId] = sp.[UserId]
    WHERE u.[UserId] = @UserId;
END
GO

-- 2. SP: Update Student Profile
CREATE OR ALTER PROCEDURE [dbo].[sp_UpdateStudentProfile]
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

-- 3. SP: Change User Password
CREATE OR ALTER PROCEDURE [dbo].[sp_ChangeUserPassword]
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
