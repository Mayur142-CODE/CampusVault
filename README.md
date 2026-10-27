# CampusVault — University Document Management System

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)]()
[![Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue.svg)]()
[![Platform](https://img.shields.io/badge/ASP.NET-MVC%205-purple.svg)]()
[![Database](https://img.shields.io/badge/SQL%20Server-2016%2B-red.svg)]()
[![License](https://img.shields.io/badge/license-MIT-green.svg)]()

CampusVault is a secure, modern academic document lifecycle and classroom governance portal built using ASP.NET MVC 5 and Microsoft SQL Server. Designed for higher education institutions, it streamlines course management, assignment distribution, cryptographic file submission, multi-stage evaluation with revision loops, course syllabus repository management, broadcast announcements, and administrative governance.

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Key Features](#key-features)
3. [User Roles & Permissions](#user-roles--permissions)
4. [Assignment Lifecycle & Workflow](#assignment-lifecycle--workflow)
5. [Learning Materials & Reference Attachments](#learning-materials--reference-attachments)
6. [Technology Stack](#technology-stack)
7. [Project Folder Structure](#project-folder-structure)
8. [Prerequisites & System Requirements](#prerequisites--system-requirements)
9. [Database Setup Instructions](#database-setup-instructions)
10. [Configuration Instructions](#configuration-instructions)
11. [Build, Restore & Run Instructions](#build-restore--run-instructions)
12. [Default Startup & Login Behavior](#default-startup--login-behavior)
13. [Provisioning the Administrator Account](#provisioning-the-administrator-account)
14. [File Storage Configuration](#file-storage-configuration)
15. [Troubleshooting Common Issues](#troubleshooting-common-issues)
16. [Security Architecture](#security-architecture)
17. [Contribution Guidelines](#contribution-guidelines)

---

## Project Overview

In university academic environments, tracking student assignments, laboratory manuals, starter code, and project reports across disparate platforms often leads to lost submissions, unverifiable file integrity, and disorganized feedback loops.

**CampusVault** addresses this by providing a unified, role-based platform where:
- **Faculty** can organize courses into digital classrooms, attach reference guides, distribute tasks, evaluate submissions, assign scores, and request revisions.
- **Students** can join classrooms via join codes, download materials, upload assignments with cryptographic SHA-256 provenance hashes, view rubric grades, and resubmit revised work.
- **Administrators** have complete governance over users, classrooms, document telemetry, audit logs, and institutional policies.

---

## Key Features

- **Role-Based Authorization**: Distinct dashboards and granular permissions for `Student`, `Faculty`, and `Admin` users using custom `[CampusVaultAuthorize]` filters.
- **Digital Classrooms**: Faculty generate unique join codes (e.g. `CE-501-A`) with customizable UI themes; students enroll with a single click.
- **Task Management**: Faculty can publish tasks with deadline tracking, submission policies, maximum scoring thresholds, and downloadable attachments.
- **Cryptographic File Provenance**: Submissions compute a client-side and server-side SHA-256 checksum (`FileHash`) to guarantee submission non-repudiation and document integrity.
- **Multi-Stage Evaluation Loop**: Submissions can be marked as `Submitted`, `Reviewed`, `Graded`, or `Needs Revision`. Students receive instant alerts when a revision is requested.
- **Course Materials Library**: Faculty can upload syllabus copies, lecture slides, question banks, and reference manuals categorized by topic.
- **Real-Time Notification Hub**: Interactive in-app alerts notify students of newly posted tasks, grading outcomes, and faculty notices.
- **Broadcast Announcements**: Class-wide announcement stream with priority flags (Normal, High, Urgent) and read-acknowledgment requirements.
- **Profile & Password Modals**: In-place AJAX-driven profile editing and secure password changing across all panels.
- **Dual Storage Strategy**: Automatic support for Cloudinary CDN media delivery with seamless fallback to local disk storage (`Content/Uploads/TaskAttachments/`).

---

## User Roles & Permissions

CampusVault strictly enforces role separation at the routing, controller, and database levels:

| Capability | Student | Faculty | Admin |
|---|:---:|:---:|:---:|
| Join classrooms via code | Yes | No | View Only |
| Create and manage classrooms | No | Yes | Full Control |
| Create tasks and attach starter files | No | Yes | View Only |
| Upload assignment submissions | Yes | No | View Only |
| Grade submissions and request revisions | No | Yes | Override |
| Upload course materials & syllabi | No | Yes | Full Control |
| Broadcast classroom announcements | No | Yes | Full Control |
| Receive in-app notifications | Yes | Yes | Yes |
| Provision new users (Admin/Faculty/Student) | No | No | Yes |
| Deactivate/delete users or classrooms | No | Classroom only | Full Root |
| View system telemetry & audit logs | No | No | Yes |
| Update profile details & change password | Yes | Yes | Yes |

> **Note on Institutions**: The database schema includes an `Institutions` entity for multi-campus support. The application workflow is centered around Departmental Faculty, Students, and Administrators.

---

## Assignment Lifecycle & Workflow

The task evaluation workflow in CampusVault provides a closed feedback loop:

```
[ Faculty Creates Task ]
          │
          ▼
   (Task Published with Due Date & Reference Attachment)
          │
          ▼
[ Student Submits Assignment ] ──► (Computes SHA-256 Hash + Uploads File)
          │
          ▼
[ Faculty Reviews Submission ]
          │
   ───────┴────────────────────────────┐
   │                                   │
   ▼                                   ▼
[ Needs Revision ]               [ Graded / Approved ]
   │                                   │
   ▼                                   ▼
(Student Resubmits Document)     (Final Score & Rubric Feedback Logged)
   │                                   │
   └───────────► [ Faculty Re-Evaluates ]
```

1. **Publication**: Faculty creates a task (`Tasks` table) specifying Task Type (Lab Assignment, Term Work, Project Milestone, etc.), max points, and due date. Reference documents are linked via `TaskAttachments`.
2. **Submission**: Enrolled students upload their report (`TaskSubmissions` table). The server records the submission timestamp, file size, storage URL, and generates a SHA-256 hash.
3. **Assessment**: Faculty assesses the document and enters marks along with qualitative feedback.
4. **Revision Loop**: If requirements are unmet, faculty flags the document as `Needs Revision`. The student receives an in-app notification with an "Upload Revision" link, preserving previous attempts in the submission history.

---

## Learning Materials & Reference Attachments

CampusVault separates **Task Attachments** (instructional starter code and lab manuals) from **Course Materials** (semester-long reference documents):

- **Task Attachments**: Uploaded directly inside `/Faculty/CreateTask` or `/Faculty/Tasks`. Stored in `TaskAttachments` and automatically presented to students on `/Student/TaskDetails` with file size and type badges.
- **Course Materials**: Uploaded inside `/Faculty/Materials` with categorized filtering (`Lecture Notes`, `Syllabus`, `Lab Manual`, `Question Bank`). Students browse and download materials on `/Student/ClassroomDetails`.

---

## Technology Stack

### Backend
- **Framework**: C# (.NET Framework 4.7.2)
- **Web Layer**: ASP.NET MVC 5 (v5.2.9)
- **Security**: PBKDF2 Password Hashing with cryptographic salt generation, JWT Token Services (`System.IdentityModel.Tokens.Jwt` v5.1.2)
- **Data Access**: Direct ADO.NET with stored procedures, transactions, and parameterized SQL queries for optimal performance

### Database
- **Engine**: Microsoft SQL Server 2016 / 2019 / 2022 / SQL Server Express / LocalDB
- **Database Name**: `CampusVaultDB`
- **Objects**: 22 Tables, 50 Stored Procedures, Relational Foreign Keys with cascading deletes where appropriate

### Frontend
- **Views**: Razor View Engine (`.cshtml`)
- **Styling**: Vanilla CSS3 with CSS variables, gradient systems, glassmorphism, responsive CSS Grid and Flexbox layouts (`Content/css/campusvault.css`)
- **Scripts**: Vanilla JavaScript (ES6+ fetch API, asynchronous modal controllers, toast alert system)
- **Fonts & Icons**: System typography with inline SVG iconography

### Third-Party Services
- **Cloud Storage**: Cloudinary .NET SDK (`CloudinaryDotNet` v1.26.2) for document and avatar storage with local disk fallback

---

## Project Folder Structure

```text
CampusVault/
├── App_Start/
│   └── RouteConfig.cs             # ASP.NET MVC route registrations
├── Attributes/
│   └── CampusVaultAuthorizeAttribute.cs # Role-based authentication filters
├── Controllers/
│   ├── AccountController.cs       # Login, Register, Forgot/Reset Password
│   ├── AdminController.cs         # Administrative dashboard, user & classroom governance
│   ├── FacultyController.cs       # Course creation, task distribution, grading, materials
│   ├── HomeController.cs          # Entry-point controller with session redirection
│   └── StudentController.cs       # Classroom enrollment, submission, grades, calendar
├── Database/
│   ├── CampusVault.sql            # Master idempotent SQL setup script (Complete Schema & Seeds)
│   ├── CampusVaultDB.bak          # Full SQL Server database backup archive
│   └── Scripts/                   # 22 individual incremental migration scripts
├── DB/
│   └── campusvaultdb.bacpac       # Data-tier Application Export package
├── Models/
│   ├── Academic/ClassroomModels.cs# Classroom and Course models
│   ├── Admin/AdminModels.cs       # Admin management models
│   ├── Auth/AuthModels.cs         # User, Login, Register, Role, Token view models
│   ├── Faculty/FacultyModels.cs   # Tasks, Materials, Announcements models
│   └── Student/StudentModels.cs   # Enrolled classes, Submissions, Calendar models
├── Services/
│   ├── AdminRepository.cs         # Admin data access operations
│   ├── AuthRepository.cs          # User authentication and password hashing
│   ├── ClassroomRepository.cs     # Classroom enrollment queries
│   ├── CloudinaryService.cs       # Cloudinary file upload and CDN resolution
│   ├── FacultyRepository.cs       # Faculty tasks, grading, materials logic
│   ├── JwtService.cs              # JWT creation and validation
│   ├── LookupRepository.cs        # Department, Semester, Division dropdown lookups
│   ├── NotificationRepository.cs  # In-app notifications persistence
│   ├── PasswordHasher.cs          # PBKDF2/SHA-256 password security service
│   └── StudentRepository.cs       # Student submissions and dynamic queries
├── Views/
│   ├── Account/                   # Login, Register, ForgotPassword views
│   ├── Admin/                     # Dashboard, Users, Classrooms, Reports views
│   ├── Faculty/                   # Dashboard, CreateTask, Submissions, Materials views
│   ├── Shared/                    # _Layout, _Navbar, _Sidebar partial views
│   └── Student/                   # Dashboard, TaskDetails, SubmitDocument, Calendar views
├── Content/
│   ├── css/campusvault.css        # Core stylesheet
│   ├── images/campusvault-logo.svg# Vector application logo
│   ├── js/campusvault.js          # Shared client scripts and modals
│   └── Uploads/TaskAttachments/   # Local directory for reference attachments
├── Web.config                     # Primary configuration file (git-tracked with safe defaults)
├── Web.Secrets.config.example     # Template for private API credentials (copy to Web.Secrets.config)
├── CampusVault.csproj             # MSBuild C# project specification
├── CampusVault.slnx               # Visual Studio solution file
└── packages.config                # NuGet package dependency definitions
```

---

## Prerequisites & System Requirements

Before running CampusVault locally, ensure your machine meets these requirements:

1. **Operating System**: Windows 10 / 11 or Windows Server 2016+
2. **IDE**: Visual Studio 2019, 2022 (Community, Professional, or Enterprise)
   - Required Workload: **ASP.NET and web development**
   - Individual Component: **.NET Framework 4.7.2 targeting pack**
3. **Database Server**:
   - Microsoft SQL Server 2016 or newer (Developer, Standard, Enterprise, or Express edition)
   - Alternatively: SQL Server LocalDB (`(localdb)\MSSQLLocalDB`)
4. **Database Tooling**: SQL Server Management Studio (SSMS) 18+ or Azure Data Studio

---

## Database Setup Instructions

You can set up `CampusVaultDB` using any of the following methods.

### Method 1: Execute Master SQL Script (Recommended)

The repository includes a single, complete, idempotent setup script at [Database/CampusVault.sql](Database/CampusVault.sql). It automatically creates the database, all 22 tables, 50 stored procedures, and initial seed accounts.

1. Open **SQL Server Management Studio (SSMS)** and connect to your SQL Server instance.
2. Click **File -> Open -> File...** and select `Database/CampusVault.sql`.
3. Click **Execute** (or press `F5`).
4. Ensure the message window displays:
   ```text
   CampusVaultDB database setup completed successfully!
   ```

Alternatively, run via command prompt using `sqlcmd`:
```cmd
sqlcmd -S localhost -E -i "Database\CampusVault.sql"
```
*(Replace `localhost` with your SQL Server instance name, such as `.\SQLEXPRESS` if applicable).*

---

### Method 2: Restore from SQL Server Backup (.bak)

1. Open SSMS and connect to your SQL Server.
2. In Object Explorer, right-click **Databases -> Restore Database...**
3. Under **Source**, select **Device** and click the `...` browse button.
4. Click **Add**, navigate to the project directory, and select `Database\CampusVaultDB.bak`.
5. Click **OK**, verify the destination database name is `CampusVaultDB`, and click **OK** to restore.

---

### Method 3: Import Data-Tier Application (.bacpac)

1. In SSMS, right-click **Databases -> Import Data-tier Application...**
2. Browse to `DB\campusvaultdb.bacpac`.
3. Set the database name to `CampusVaultDB` and complete the wizard.

---

## Configuration Instructions

### 1. Connection String (`Web.config`)

Open [Web.config](Web.config) and locate the `<connectionStrings>` block:

```xml
<connectionStrings>
  <add name="CampusVaultConnection" 
       connectionString="Data Source=localhost;Initial Catalog=CampusVaultDB;Integrated Security=True;TrustServerCertificate=True;Pooling=True;Min Pool Size=3;Max Pool Size=100;Connection Timeout=10;" 
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

- If you use the default SQL Server instance on your machine, `Data Source=localhost` or `Data Source=.` will connect immediately.
- If you use SQL Server Express, update to: `Data Source=.\SQLEXPRESS`
- If you use SQL Server LocalDB, update to: `Data Source=(localdb)\MSSQLLocalDB`
- If using SQL Server Authentication with username and password:
  ```xml
  connectionString="Data Source=localhost;Initial Catalog=CampusVaultDB;User Id=your_username;Password=your_password;TrustServerCertificate=True;"
  ```

---

### 2. Environment Secrets (`Web.Secrets.config`)

To keep production credentials and private API keys off version control, CampusVault uses ASP.NET's `<appSettings file="Web.Secrets.config">` extension mechanism.

1. In the project root, make a copy of `Web.Secrets.config.example`:
   ```cmd
   copy Web.Secrets.config.example Web.Secrets.config
   ```
2. Open `Web.Secrets.config` and insert your private credentials if you wish to use Cloudinary:
   ```xml
   <?xml version="1.0" encoding="utf-8" ?>
   <appSettings>
     <add key="CloudinaryCloudName" value="your_cloudinary_cloud_name" />
     <add key="CloudinaryApiKey"    value="your_cloudinary_api_key" />
     <add key="CloudinaryApiSecret" value="your_cloudinary_api_secret" />
   </appSettings>
   ```
3. `Web.Secrets.config` is listed in `.gitignore` and will never be committed to Git. If `Web.Secrets.config` is absent, the application gracefully defaults to local disk storage (`Content/Uploads/`).

---

## Build, Restore & Run Instructions

### Using Visual Studio (GUI)
1. Double-click `CampusVault.slnx` (or open the project folder in Visual Studio).
2. Right-click the Solution in Solution Explorer and select **Restore NuGet Packages**.
3. Press **Ctrl + Shift + B** to build the solution. Ensure the output window reports `0 errors`.
4. Press **F5** (or **Ctrl + F5**) to launch the web application in IIS Express.

### Using Developer Command Prompt / MSBuild CLI
```cmd
# 1. Restore NuGet Dependencies
nuget restore CampusVault.slnx

# 2. Build the project in Debug configuration
msbuild CampusVault.csproj /p:Configuration=Debug /v:minimal

# 3. Launch with IIS Express (example port 44382)
"C:\Program Files\IIS Express\iisexpress.exe" /path:"%CD%" /port:5000
```

---

## Default Startup & Login Behavior

When opening the root URL (`http://localhost:5000/` or IIS Express port):
1. **Unauthenticated Visitors**: Automatically redirected to the Login screen at `/Account/Login`.
2. **Authenticated Users**: Evaluated by their active session role and redirected automatically:
   - `Admin` $\rightarrow$ `/Admin/Dashboard`
   - `Faculty` $\rightarrow$ `/Faculty/Dashboard`
   - `Student` $\rightarrow$ `/Student/Dashboard`

### Default Development Seed Accounts

The database initialization script automatically provisions these three accounts for local testing:

| Role | Email Address | Password | Name | Details |
|---|---|---|---|---|
| **Admin** | `admin@campusvault.edu` | `Campus@123` | System Administrator | IT Infrastructure Directorate (Root Clearance) |
| **Faculty** | `faculty@campusvault.edu` | `Campus@123` | Prof. Rajesh Patel | Computer Engineering (`FAC-CE-101`) |
| **Student** | `student@campusvault.edu` | `Campus@123` | Prit Pansuriya | Semester 5, Division A (`22012011001`) |

*(All passwords use cryptographically salted SHA-256 hashes generated via `PasswordHasher`).*

---

## Provisioning the Administrator Account

To provision an additional administrative user, you can use any of the following approaches:

### Method A: Execute Stored Procedure in SSMS
Run the built-in administrative registration procedure:
```sql
USE [CampusVaultDB];
GO

DECLARE @Success BIT, @Message NVARCHAR(255), @NewUserId INT;

-- Generate hash and salt using existing seed or application hasher
EXEC dbo.sp_AdminCreateUser
    @RoleName = 'Admin',
    @Email = 'new.admin@campusvault.edu',
    @Password = 'AdminSecurePass#2026',
    @FirstName = 'Mayur',
    @LastName = 'Chavda',
    @PhoneNumber = '+91 9876543219',
    @Department = 'IT Infrastructure',
    @Success = @Success OUTPUT,
    @Message = @Message OUTPUT,
    @UserId = @NewUserId OUTPUT;

SELECT @Success AS Success, @Message AS Message, @NewUserId AS NewUserId;
GO
```

### Method B: Via the Admin Portal UI
1. Log in with the existing default admin (`admin@campusvault.edu`).
2. Navigate to **User Management** (`/Admin/Users`).
3. Click **Add New User**, select the role **Admin**, enter the required profile details, and save.

---

## File Storage Configuration

CampusVault uses an adaptive storage strategy:

1. **Cloudinary CDN Integration**: When valid Cloudinary credentials are provided in `Web.Secrets.config`, document uploads and avatars are uploaded via TLS to Cloudinary's secure media storage. The resulting secure HTTPS URL (`https://res.cloudinary.com/...`) is saved in `TaskSubmissions.CloudUrl` or `Users.ProfilePictureUrl`.
2. **Local Disk Storage**: If Cloudinary is not configured or an upload is offline, the application saves files to `Content/Uploads/TaskAttachments/` on the local web server and generates a relative path.

---

## Troubleshooting Common Issues

### 1. SQL Server Connection Timeout / Server Not Found
- **Symptom**: `SqlException: A network-related or instance-specific error occurred while establishing a connection to SQL Server.`
- **Remedy**:
  - Verify SQL Server is running: Open Windows Services (`services.msc`) and verify **SQL Server (MSSQLSERVER)** or **SQL Server (SQLEXPRESS)** is Started.
  - In `Web.config`, confirm your instance name: try `Data Source=.`, `Data Source=localhost`, or `Data Source=.\SQLEXPRESS`.

### 2. Invalid Column Name 'IsActive'
- **Symptom**: Runtime exception `SqlException: Invalid column name 'IsActive'`.
- **Cause**: Using an older version of the database schema prior to classroom archiving features.
- **Remedy**: Run `Database/CampusVault.sql` or run the migration script [Database/Scripts/21_Add_IsActive_To_Classrooms.sql](Database/Scripts/21_Add_IsActive_To_Classrooms.sql) in SSMS:
  ```sql
  ALTER TABLE [dbo].[Classrooms] ADD [IsActive] BIT NOT NULL DEFAULT 1;
  ```

### 3. Missing Stored Procedure 'sp_UpdateAdminProfile'
- **Symptom**: `SqlException: Could not find stored procedure 'sp_UpdateAdminProfile'`.
- **Remedy**: Execute [Database/CampusVault.sql](Database/CampusVault.sql) in SSMS to ensure all 50 procedures are installed.

### 4. AntiForgeryToken Validation Errors
- **Symptom**: `HttpAntiForgeryException: The required anti-forgery form field "__RequestVerificationToken" is not present`.
- **Remedy**: Clear browser cookies for `localhost` and restart the browser session.

---

## Security Architecture

1. **Password Security**: Passwords are never stored in plaintext. CampusVault uses `PBKDF2` with a cryptographically generated 256-bit salt and multi-iteration SHA-256 hashing.
2. **SQL Injection Defense**: 100% of data access is performed through parameterized queries and stored procedures (`SqlCommand.Parameters.AddWithValue`), preventing SQL injection.
3. **Cross-Site Request Forgery (CSRF)**: All state-changing POST forms and AJAX modal endpoints validate anti-forgery tokens using `[ValidateAntiForgeryToken]`.
4. **Role Isolation**: Controller actions verify session identity and role clearance using `[CampusVaultAuthorize("RoleName")]`. Users cannot modify profiles or grades outside their authenticated identity.
5. **Secret Segregation**: API keys and environment credentials are separated into `Web.Secrets.config`, which is explicitly ignored by version control.

---

## Contribution Guidelines

1. Fork the repository and create a feature branch (`git checkout -b feature/AmazingFeature`).
2. Verify all code compiles cleanly with `msbuild CampusVault.csproj`.
3. Ensure schema changes are reflected both in incremental migration scripts in `Database/Scripts/` and merged into [Database/CampusVault.sql](Database/CampusVault.sql).
4. Do not commit sensitive local configurations, `.bak` dumps with real personal data, or build artifacts (`bin/`, `obj/`).
5. Commit your changes (`git commit -m 'Add some AmazingFeature'`).
6. Push to the branch (`git push origin feature/AmazingFeature`).
7. Open a Pull Request.

---

## License

This project is licensed under the MIT License — see the LICENSE file for details.