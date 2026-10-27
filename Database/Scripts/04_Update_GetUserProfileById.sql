USE [CampusVaultDB];
GO

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
