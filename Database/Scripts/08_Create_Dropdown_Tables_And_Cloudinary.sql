-- ============================================================
-- Script 08: Dropdown Lookup Tables + Cloudinary Columns
-- ============================================================

-- 1. Departments
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Departments')
BEGIN
    CREATE TABLE Departments (
        DepartmentId   INT           PRIMARY KEY IDENTITY(1,1),
        DepartmentName NVARCHAR(200) NOT NULL,
        DepartmentCode NVARCHAR(20)  DEFAULT NULL,
        IsActive       BIT           DEFAULT 1
    );

    INSERT INTO Departments (DepartmentName, DepartmentCode) VALUES
    ('Computer Engineering',      'CE'),
    ('Information Technology',    'IT'),
    ('Electronics Engineering',   'EC'),
    ('Mechanical Engineering',    'ME'),
    ('Civil Engineering',         'CV'),
    ('Electrical Engineering',    'EE');
END
GO

-- 2. Semesters
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Semesters')
BEGIN
    CREATE TABLE Semesters (
        SemesterId     INT          PRIMARY KEY IDENTITY(1,1),
        SemesterNumber INT          NOT NULL,
        SemesterName   NVARCHAR(50) NOT NULL,
        IsActive       BIT          DEFAULT 1
    );

    INSERT INTO Semesters (SemesterNumber, SemesterName) VALUES
    (1, 'Semester 1'), (2, 'Semester 2'), (3, 'Semester 3'),
    (4, 'Semester 4'), (5, 'Semester 5'), (6, 'Semester 6'),
    (7, 'Semester 7'), (8, 'Semester 8');
END
GO

-- 3. TaskTypes
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TaskTypes')
BEGIN
    CREATE TABLE TaskTypes (
        TaskTypeId INT           PRIMARY KEY IDENTITY(1,1),
        TypeName   NVARCHAR(100) NOT NULL,
        IsActive   BIT           DEFAULT 1
    );

    INSERT INTO TaskTypes (TypeName) VALUES
    ('Assignment'),
    ('Lab Work'),
    ('Project'),
    ('Presentation'),
    ('Report');
END
GO

-- 4. Divisions
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Divisions')
BEGIN
    CREATE TABLE Divisions (
        DivisionId   INT          PRIMARY KEY IDENTITY(1,1),
        DivisionName NVARCHAR(50) NOT NULL,
        IsActive     BIT          DEFAULT 1
    );

    INSERT INTO Divisions (DivisionName) VALUES
    ('A'), ('B'), ('C'), ('D'), ('E'), ('F');
END
GO

-- 5. AcademicYears
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AcademicYears')
BEGIN
    CREATE TABLE AcademicYears (
        AcademicYearId INT          PRIMARY KEY IDENTITY(1,1),
        YearName       NVARCHAR(50) NOT NULL,
        IsActive       BIT          DEFAULT 1
    );

    INSERT INTO AcademicYears (YearName) VALUES
    ('2024-2025'), ('2025-2026'), ('2026-2027'), ('2027-2028');
END
GO

-- 6. Add Cloudinary columns to TaskSubmissions (safe - checks first)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TaskSubmissions' AND COLUMN_NAME = 'CloudPublicId')
    ALTER TABLE TaskSubmissions ADD CloudPublicId NVARCHAR(500) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TaskSubmissions' AND COLUMN_NAME = 'CloudUrl')
    ALTER TABLE TaskSubmissions ADD CloudUrl NVARCHAR(1000) NULL;
GO
