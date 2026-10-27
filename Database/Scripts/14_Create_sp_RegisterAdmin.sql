-- ============================================================================
-- CAMPUSVAULT - STORED PROCEDURE: sp_RegisterAdmin
-- Database: CampusVaultDB
-- Description: Registers a new Administrator user and initializes AdminProfile
-- ============================================================================

USE [CampusVaultDB];
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_RegisterAdmin]
    @Email              NVARCHAR(150),
    @PasswordHash       NVARCHAR(500),
    @PasswordSalt       NVARCHAR(255),
    @FirstName          NVARCHAR(100),
    @LastName           NVARCHAR(100),
    @PhoneNumber        NVARCHAR(20) = NULL,
    @Department         NVARCHAR(100) = 'Administration',
    @AccessLevel        NVARCHAR(50) = 'Tier 1 - Full Campus Access',
    @NewUserId          INT OUTPUT,
    @StatusCode         INT OUTPUT,       -- 1 = Success, -1 = Email Exists, -99 = Error
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

    DECLARE @AdminRoleId INT;
    SELECT @AdminRoleId = [RoleId] FROM [dbo].[Roles] WHERE [RoleName] = 'Admin';

    IF @AdminRoleId IS NULL
    BEGIN
        SET @StatusCode = -99;
        SET @StatusMessage = 'Admin role not found in system.';
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Insert into Users
        INSERT INTO [dbo].[Users] 
            ([RoleId], [Email], [PasswordHash], [PasswordSalt], [FirstName], [LastName], [PhoneNumber], [IsActive], [IsEmailVerified])
        VALUES 
            (@AdminRoleId, @Email, @PasswordHash, @PasswordSalt, @FirstName, @LastName, @PhoneNumber, 1, 1);

        SET @NewUserId = SCOPE_IDENTITY();

        -- Insert into AdminProfiles
        INSERT INTO [dbo].[AdminProfiles] 
            ([UserId], [Department], [AccessLevel])
        VALUES 
            (@NewUserId, ISNULL(@Department, 'Administration'), ISNULL(@AccessLevel, 'SuperAdmin'));

        COMMIT TRANSACTION;
        SET @StatusMessage = 'Administrator registered successfully.';
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
