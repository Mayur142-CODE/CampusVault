using System;

namespace CampusVault.Models.Auth
{
    public class User
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string PhoneNumber { get; set; }
        public string ProfilePictureUrl { get; set; }
        public bool IsActive { get; set; }
        public bool IsEmailVerified { get; set; }

        // Student Profile (if Role == "Student")
        public string EnrollmentNumber { get; set; }
        public string StudentDepartment { get; set; }
        public int? Semester { get; set; }
        public string Division { get; set; }
        public string RollNumber { get; set; }

        // Faculty Profile (if Role == "Faculty")
        public string EmployeeCode { get; set; }
        public string FacultyDepartment { get; set; }
        public string FacultyDesignation { get; set; }

        // Admin Profile (if Role == "Admin")
        public string AdminDepartment { get; set; }
        public string AdminAccessLevel { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
    }

    public class RegisterStudentRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
        public string EnrollmentNumber { get; set; }
        public string Department { get; set; }
        public int Semester { get; set; }
        public string Division { get; set; }
        public string RollNumber { get; set; }
        public int? BatchYear { get; set; }
    }

    public class RegisterFacultyRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
        public string EmployeeCode { get; set; }
        public string Department { get; set; }
        public string Designation { get; set; }
        public string Qualification { get; set; }
        public string OfficeLocation { get; set; }
    }

    public class RegisterAdminRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
        public string Department { get; set; }
        public string Designation { get; set; }
        public string AccessLevel { get; set; }
        public string StaffId { get; set; }
    }

    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string Role { get; set; }
        public string RedirectUrl { get; set; }
        public User User { get; set; }
    }

    public class JwtClaims
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string FullName { get; set; }
        public string Jti { get; set; }
        public long Exp { get; set; }
        public long Iat { get; set; }
    }
}
