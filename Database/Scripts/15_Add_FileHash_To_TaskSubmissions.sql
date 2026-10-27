-- ============================================================================
-- CAMPUSVAULT - ADD FileHash COLUMN TO TaskSubmissions
-- Database: CampusVaultDB
-- Description: Adds FileHash column to TaskSubmissions for SHA256 integrity validation
-- ============================================================================

USE [CampusVaultDB];
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.TaskSubmissions') AND name = 'FileHash')
BEGIN
    ALTER TABLE [dbo].[TaskSubmissions] ADD [FileHash] NVARCHAR(256) NULL;
    PRINT 'Added FileHash column to TaskSubmissions';
END
GO
