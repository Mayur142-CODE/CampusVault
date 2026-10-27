using System.Web.Mvc;
using CampusVault.Attributes;
using CampusVault.Services;

namespace CampusVault.Controllers
{
    [CampusVaultAuthorize("Admin")]
    public class AdminController : Controller
    {
        private readonly AdminRepository _adminRepo;
        private readonly AuthRepository _authRepo;

        public AdminController()
        {
            _adminRepo = new AdminRepository();
            _authRepo = new AuthRepository();
        }

        /// <summary>
        /// Sets common ViewBag properties for all admin views dynamically from Session.
        /// </summary>
        private void SetAdminContext(string title)
        {
            ViewBag.Title = title;
            ViewBag.Role = "Admin";
            ViewBag.UserName = Session["UserName"] as string ?? "Admin User";
            ViewBag.UserInitials = Session["UserInitials"] as string ?? "AU";
            ViewBag.UserRoleDisplay = Session["UserRoleDisplay"] as string ?? "System Administrator";
        }

        // GET: /Admin/Dashboard
        public ActionResult Dashboard()
        {
            SetAdminContext("Administrative Overview");
            var vm = _adminRepo.GetDashboardData();
            return View(vm);
        }

        // GET: /Admin/Users
        public ActionResult Users()
        {
            SetAdminContext("User Management");
            var users = _adminRepo.GetAllUsers();
            return View(users);
        }

        // GET: /Admin/Classrooms
        public ActionResult Classrooms()
        {
            SetAdminContext("Classroom Management");
            var classrooms = _adminRepo.GetAllClassrooms();
            return View(classrooms);
        }

        // GET: /Admin/Tasks
        public ActionResult Tasks()
        {
            SetAdminContext("Task Management");
            var tasks = _adminRepo.GetAllTasks();
            return View(tasks);
        }

        // GET: /Admin/Documents
        public ActionResult Documents()
        {
            SetAdminContext("Document Management");
            var docs = _adminRepo.GetAllDocuments();
            return View(docs);
        }

        // GET: /Admin/Reports
        public ActionResult Reports()
        {
            SetAdminContext("Reports & Analytics");
            var vm = _adminRepo.GetDashboardData();
            return View(vm);
        }

        // GET: /Admin/Activity
        public ActionResult Activity()
        {
            SetAdminContext("System Activity");
            var activities = _adminRepo.GetActivities(50);
            return View(activities);
        }

        // POST: /Admin/ToggleUser — activate / deactivate a user account
        [HttpPost]
        public JsonResult ToggleUser(int userId, bool isActive)
        {
            try
            {
                var result = _adminRepo.ToggleUserStatus(userId, isActive);
                return Json(new { success = result.success, message = result.message });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /Admin/DeleteUser — permanently remove a user account
        [HttpPost]
        public JsonResult DeleteUser(int userId)
        {
            try
            {
                var result = _adminRepo.DeleteUser(userId);
                return Json(new { success = result.success, message = result.message });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /Admin/DeleteClassroom — permanently remove a classroom
        [HttpPost]
        public JsonResult DeleteClassroom(int classroomId)
        {
            try
            {
                var result = _adminRepo.DeleteClassroom(classroomId);
                return Json(new { success = result.success, message = result.message });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /Admin/CreateUser — admin creates a new user
        [HttpPost]
        public JsonResult CreateUser(string roleName, string email, string password, string firstName, string lastName,
            string phoneNumber = null, string enrollmentNumber = null, string department = null,
            int? semester = null, string division = null, string employeeCode = null, string designation = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(roleName) || string.IsNullOrWhiteSpace(email) ||
                    string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(firstName) ||
                    string.IsNullOrWhiteSpace(lastName))
                {
                    return Json(new { success = false, message = "Please fill in all required fields." });
                }

                var result = _adminRepo.CreateUser(roleName, email, password, firstName, lastName,
                    phoneNumber, enrollmentNumber, department, semester, division, employeeCode, designation);
                return Json(new { success = result.success, message = result.message, userId = result.newUserId });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: /Admin/Profile
        public new ActionResult Profile()
        {
            SetAdminContext("Admin Profile");
            if (Session["UserId"] == null)
            {
                Response.Redirect("~/Account/Login");
                return new EmptyResult();
            }
            int userId = (int)Session["UserId"];
            var user = _authRepo.GetUserById(userId);
            return View(user);
        }

        // POST: /Admin/UpdateProfile (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateProfile(string firstName, string lastName, string phoneNumber, string department)
        {
            if (Session["UserId"] == null)
                return Json(new { success = false, message = "Session expired. Please log in again." });

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                return Json(new { success = false, message = "First name and last name are required." });

            int userId = (int)Session["UserId"];
            var result = _adminRepo.UpdateAdminProfile(userId, firstName, lastName, phoneNumber, department);
            if (result.success)
            {
                Session["UserName"] = $"{firstName.Trim()} {lastName.Trim()}";
                string f = firstName.Trim().Substring(0, 1).ToUpper();
                string l = lastName.Trim().Substring(0, 1).ToUpper();
                Session["UserInitials"] = $"{f}{l}";
            }
            return Json(new { success = result.success, message = result.message });
        }

        // POST: /Admin/ChangePassword (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (Session["UserId"] == null)
                return Json(new { success = false, message = "Session expired. Please log in again." });

            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
                return Json(new { success = false, message = "Current and new password are required." });

            if (newPassword.Length < 6)
                return Json(new { success = false, message = "New password must be at least 6 characters long." });

            if (newPassword != confirmPassword)
                return Json(new { success = false, message = "New password and confirmation do not match." });

            int userId = (int)Session["UserId"];
            var user = _authRepo.GetUserById(userId);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (!CampusVault.Services.PasswordHasher.VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt))
            {
                return Json(new { success = false, message = "Current password does not match our records." });
            }

            string newSalt = CampusVault.Services.PasswordHasher.GenerateSalt();
            string newHash = CampusVault.Services.PasswordHasher.HashPassword(newPassword, newSalt);

            var result = _authRepo.ChangePassword(userId, newHash, newSalt);
            return Json(new { success = result.success, message = result.message });
        }
    }
}

