using System;
using System.Web;
using System.Web.Mvc;
using CampusVault.Models.Auth;
using CampusVault.Services;

namespace CampusVault.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthRepository _authRepo;
        private readonly JwtService _jwtService;

        public AccountController()
        {
            _authRepo = new AuthRepository();
            _jwtService = new JwtService();
        }

        // GET: /Account
        [HttpGet]
        public ActionResult Index()
        {
            return RedirectToAction("Login");
        }

        // GET: /Account/Login
        [HttpGet]
        public ActionResult Login()
        {
            bool isLoggedOut = string.Equals(Request.QueryString["loggedOut"], "true", StringComparison.OrdinalIgnoreCase);

            if (!isLoggedOut)
            {
                string currentRole = Session["Role"] as string;

                // Fallback to JWT cookie verification if session was not yet loaded
                if (string.IsNullOrEmpty(currentRole))
                {
                    var cookie = Request.Cookies["CampusVault_Token"];
                    if (cookie != null && !string.IsNullOrWhiteSpace(cookie.Value))
                    {
                        var claims = _jwtService.ValidateToken(cookie.Value);
                        if (claims != null && !string.IsNullOrEmpty(claims.Role))
                        {
                            currentRole = claims.Role;
                            Session["UserId"] = claims.UserId;
                            Session["UserEmail"] = claims.Email;
                            Session["Role"] = claims.Role;
                            Session["UserName"] = claims.FullName;
                        }
                    }
                }

                // If authenticated, redirect directly to role-specific dashboard
                if (!string.IsNullOrEmpty(currentRole))
                {
                    return RedirectToDashboard(currentRole);
                }
            }

            ViewBag.Title = "Sign In — CampusVault";
            return View();
        }

        // POST: /Account/Login (AJAX API returning JWT & Redirect URL)
        [HttpPost]
        public JsonResult Login(LoginRequest model)
        {
            string clientIp = Request.UserHostAddress;
            string userAgent = Request.UserAgent;

            if (model == null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                return Json(new AuthResponse { Success = false, Message = "Email and password are required." });
            }

            try
            {
                // 1. Fetch user by email using sp_AuthenticateUser
                var user = _authRepo.GetUserByEmail(model.Email);
                if (user == null)
                {
                    _authRepo.LogAuthAction(null, "LOGIN_FAILED", clientIp, userAgent, $"Invalid email attempt: {model.Email}");
                    return Json(new AuthResponse { Success = false, Message = "Invalid email or password." });
                }

                // 2. Check if account is active
                if (!user.IsActive)
                {
                    _authRepo.LogAuthAction(user.UserId, "LOGIN_FAILED", clientIp, userAgent, "Account disabled");
                    return Json(new AuthResponse { Success = false, Message = "Your account has been deactivated. Please contact the administrator." });
                }

                // 3. Verify salted password hash
                bool isPasswordValid = PasswordHasher.VerifyPassword(model.Password, user.PasswordHash, user.PasswordSalt);
                if (!isPasswordValid)
                {
                    _authRepo.LogAuthAction(user.UserId, "LOGIN_FAILED", clientIp, userAgent, "Incorrect password attempt");
                    return Json(new AuthResponse { Success = false, Message = "Invalid email or password." });
                }

                // 4. Generate RFC 7519 JSON Web Token & Refresh Token
                string jwtToken = _jwtService.GenerateToken(user, out string jwtId, out DateTime expiresAt);
                string refreshToken = _jwtService.GenerateRefreshToken();
                DateTime refreshExpiry = DateTime.UtcNow.AddDays(7);

                // 5. Store Refresh Token in DB via sp_SaveRefreshToken
                _authRepo.SaveRefreshToken(user.UserId, refreshToken, jwtId, refreshExpiry, clientIp);

                // 6. Log successful authentication in UserAuthLogs
                _authRepo.LogAuthAction(user.UserId, "LOGIN_SUCCESS", clientIp, userAgent, $"Successful login as {user.RoleName}");

                // 7. Store JWT in secure HttpOnly cookie for server-side Razor views
                var authCookie = new HttpCookie("CampusVault_Token", jwtToken)
                {
                    HttpOnly = true,
                    Secure = Request.IsSecureConnection,
                    Expires = model.RememberMe ? DateTime.Now.AddDays(7) : DateTime.MinValue,
                    Path = "/"
                };
                Response.Cookies.Add(authCookie);

                // 8. Establish session for active controller context
                Session["UserId"] = user.UserId;
                Session["UserEmail"] = user.Email;
                Session["Role"] = user.RoleName;
                Session["UserName"] = user.FullName;
                Session["UserInitials"] = GetInitials(user.FirstName, user.LastName);

                if (user.RoleName == "Student")
                {
                    Session["UserRoleDisplay"] = !string.IsNullOrEmpty(user.StudentDepartment) ? $"{user.StudentDepartment} - Sem {user.Semester}" : "Enrolled Student";
                    Session["UserRollNo"] = user.EnrollmentNumber ?? user.RollNumber ?? "";
                    Session["UserDepartment"] = user.StudentDepartment ?? "";
                    Session["UserSemester"] = user.Semester?.ToString() ?? "";
                    Session["UserDivision"] = user.Division ?? "";
                }
                else if (user.RoleName == "Faculty")
                {
                    Session["UserRoleDisplay"] = !string.IsNullOrEmpty(user.FacultyDepartment) ? $"{user.FacultyDesignation} ({user.FacultyDepartment})" : (!string.IsNullOrEmpty(user.FacultyDesignation) ? user.FacultyDesignation : "Faculty Mentor");
                    Session["UserEmployeeCode"] = user.EmployeeCode ?? "";
                    Session["UserDepartment"] = user.FacultyDepartment ?? "";
                }
                else
                {
                    Session["UserRoleDisplay"] = "System Administrator";
                }

                string redirectUrl = GetDashboardUrl(user.RoleName);

                return Json(new AuthResponse
                {
                    Success = true,
                    Message = "Authentication successful. Redirecting...",
                    AccessToken = jwtToken,
                    RefreshToken = refreshToken,
                    ExpiresIn = (int)(expiresAt - DateTime.UtcNow).TotalSeconds,
                    Role = user.RoleName,
                    RedirectUrl = redirectUrl,
                    User = user
                });
            }
            catch (Exception ex)
            {
                return Json(new AuthResponse { Success = false, Message = "An unexpected error occurred: " + ex.Message });
            }
        }

        // GET: /Account/Register
        [HttpGet]
        public ActionResult Register()
        {
            ViewBag.Title = "Create Account — CampusVault";
            var lookup = new CampusVault.Services.LookupRepository();
            ViewBag.Departments = lookup.GetDepartments();
            ViewBag.Semesters   = lookup.GetSemesters();
            ViewBag.Divisions   = lookup.GetDivisions();
            ViewBag.Institutions = lookup.GetVerifiedInstitutions();
            return View();
        }

        // POST: /Account/RegisterStudent (AJAX endpoint)
        [HttpPost]
        public JsonResult RegisterStudent(RegisterStudentRequest model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid data submitted." });

            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password) ||
                string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName))
            {
                return Json(new { success = false, message = "Name, email, and password are required." });
            }

            try
            {
                string salt = PasswordHasher.GenerateSalt();
                string hash = PasswordHasher.HashPassword(model.Password, salt);

                var (statusCode, message, newUserId) = _authRepo.RegisterStudent(model, hash, salt);

                if (statusCode == 1)
                {
                    _authRepo.LogAuthAction(newUserId, "REGISTER_STUDENT", Request.UserHostAddress, Request.UserAgent, $"Student registered: {model.Email}");
                    return Json(new { success = true, message = "Student account created successfully! You can now sign in." });
                }

                return Json(new { success = false, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Registration error: " + ex.Message });
            }
        }

        // POST: /Account/RegisterFaculty (AJAX endpoint)
        [HttpPost]
        public JsonResult RegisterFaculty(RegisterFacultyRequest model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid data submitted." });

            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password) ||
                string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName))
            {
                return Json(new { success = false, message = "Name, email, and password are required." });
            }

            try
            {
                string salt = PasswordHasher.GenerateSalt();
                string hash = PasswordHasher.HashPassword(model.Password, salt);

                var (statusCode, message, newUserId) = _authRepo.RegisterFaculty(model, hash, salt);

                if (statusCode == 1)
                {
                    _authRepo.LogAuthAction(newUserId, "REGISTER_FACULTY", Request.UserHostAddress, Request.UserAgent, $"Faculty registered: {model.Email}");
                    return Json(new { success = true, message = "Faculty account created successfully! You can now sign in." });
                }

                return Json(new { success = false, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Registration error: " + ex.Message });
            }
        }

        // POST: /Account/RegisterAdmin (AJAX endpoint)
        [HttpPost]
        public JsonResult RegisterAdmin(RegisterAdminRequest model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid data submitted." });

            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password) ||
                string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName))
            {
                return Json(new { success = false, message = "All required fields must be completed." });
            }

            try
            {
                string salt = PasswordHasher.GenerateSalt();
                string hash = PasswordHasher.HashPassword(model.Password, salt);

                var (statusCode, message, newUserId) = _authRepo.RegisterAdmin(model, hash, salt);

                if (statusCode == 1)
                {
                    _authRepo.LogAuthAction(newUserId, "REGISTER_ADMIN", Request.UserHostAddress, Request.UserAgent, $"Administrator registered: {model.Email}");
                    return Json(new { success = true, message = "Administrator account created successfully! You can now sign in." });
                }

                return Json(new { success = false, message = message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Registration error: " + ex.Message });
            }
        }

        // GET: /Account/Logout
        [HttpGet]
        public ActionResult Logout()
        {
            int? userId = Session["UserId"] as int?;
            _authRepo.LogAuthAction(userId, "LOGOUT", Request.UserHostAddress, Request.UserAgent, "User logged out");

            // 1. Destroy and clear all session variables
            Session.Clear();
            Session.RemoveAll();
            Session.Abandon();

            // 2. Expire authentication JWT cookie unconditionally
            var authCookie = new HttpCookie("CampusVault_Token", "")
            {
                Expires = DateTime.UtcNow.AddDays(-10),
                HttpOnly = true,
                Path = "/"
            };
            Response.Cookies.Add(authCookie);

            // 3. Expire ASP.NET session ID cookie unconditionally
            var sessionCookie = new HttpCookie("ASP.NET_SessionId", "")
            {
                Expires = DateTime.UtcNow.AddDays(-10),
                Path = "/"
            };
            Response.Cookies.Add(sessionCookie);

            System.Web.Security.FormsAuthentication.SignOut();

            // 4. Invalidate browser cache so back button cannot show authenticated data
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Cache.SetExpires(DateTime.UtcNow.AddMinutes(-1));

            return RedirectToAction("Login", "Account", new { loggedOut = "true" });
        }

        // GET: /Account/ForgotPassword
        [HttpGet]
        public ActionResult ForgotPassword()
        {
            ViewBag.Title = "Reset Password — CampusVault";
            return View();
        }

        // POST: /Account/ForgotPassword (AJAX)
        [HttpPost]
        public JsonResult ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return Json(new { success = false, message = "Please enter your registered email address." });

            try
            {
                // Generate a secure random token compatible with .NET Framework 4.7.2
                byte[] randomBytes = new byte[32];
                using (var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
                {
                    rng.GetBytes(randomBytes);
                }
                string token = System.Convert.ToBase64String(randomBytes)
                    .Replace("+", "-").Replace("/", "_").Replace("=", "");

                var result = _authRepo.CreatePasswordResetToken(email, token);
                if (!result.success)
                    return Json(new { success = false, message = result.message });

                // In production: send email with link. For now return token in response (dev mode).
                string resetUrl = Url.Action("ResetPassword", "Account", new { token = token }, Request.Url.Scheme);
                return Json(new { success = true, message = "Password reset link generated.", resetUrl = resetUrl, token = token });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // GET: /Account/ResetPassword
        [HttpGet]
        public ActionResult ResetPassword(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("ForgotPassword");
            ViewBag.Title = "Set New Password — CampusVault";
            ViewBag.Token = token;
            return View();
        }

        // POST: /Account/ResetPassword (AJAX)
        [HttpPost]
        public JsonResult ResetPassword(string token, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
                return Json(new { success = false, message = "Invalid request. Please use the reset link from your email." });

            if (newPassword != confirmPassword)
                return Json(new { success = false, message = "Passwords do not match." });

            if (newPassword.Length < 6)
                return Json(new { success = false, message = "Password must be at least 6 characters long." });

            try
            {
                var result = _authRepo.ResetPasswordWithToken(token, newPassword);
                return Json(new { success = result.success, message = result.message });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        private ActionResult RedirectToDashboard(string role)
        {
            return Redirect(GetDashboardUrl(role));
        }

        private string GetDashboardUrl(string role)
        {
            switch (role?.ToLowerInvariant())
            {
                case "admin":
                    return Url.Action("Dashboard", "Admin");
                case "faculty":
                    return Url.Action("Dashboard", "Faculty");
                case "student":
                default:
                    return Url.Action("Dashboard", "Student");
            }
        }

        private static string GetInitials(string first, string last)
        {
            string f = !string.IsNullOrEmpty(first) ? first.Substring(0, 1).ToUpper() : "";
            string l = !string.IsNullOrEmpty(last) ? last.Substring(0, 1).ToUpper() : "";
            return $"{f}{l}";
        }
    }
}
