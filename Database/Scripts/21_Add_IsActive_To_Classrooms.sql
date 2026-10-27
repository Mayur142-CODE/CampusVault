-- ============================================================================
-- CAMPUSVAULT - ADD IsActive COLUMN TO Classrooms
-- Database: CampusVaultDB
-- Description: Adds IsActive BIT NOT NULL column (default 1) to Classrooms table
-- ============================================================================

USE [CampusVaultDB];
GO

IF COL_LENGTH('dbo.Classrooms', 'IsActive') IS NULL
BEGIN
    ALTER TABLE [dbo].[Classrooms]
    ADD [IsActive] BIT NOT NULL
        CONSTRAINT [DF_Classrooms_IsActive] DEFAULT (1);
    PRINT 'Added IsActive column with DF_Classrooms_IsActive default to Classrooms table.';
END
ELSE
BEGIN
    PRINT 'IsActive column already exists on Classrooms table.';
END
GO
