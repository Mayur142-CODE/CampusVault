using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CampusVault.Attributes;
using CampusVault.Models.Student;

namespace CampusVault.Controllers
{
    [CampusVaultAuthorize("Student")]
    public class StudentController : Controller
    {
        /// <summary>
        /// Sets common ViewBag properties for all student views dynamically from Session.
        /// </summary>
        private void SetStudentContext(string title)
        {
            ViewBag.Title = title;
            ViewBag.Role = "Student";
            ViewBag.UserName = Session["UserName"] as string ?? "Student";
            ViewBag.UserInitials = Session["UserInitials"] as string ?? "ST";
            ViewBag.UserRoleDisplay = Session["UserRoleDisplay"] as string ?? "Enrolled Student";
        }

        private readonly CampusVault.Services.ClassroomRepository _classroomRepo = new CampusVault.Services.ClassroomRepository();
        private readonly CampusVault.Services.StudentRepository _studentRepo = new CampusVault.Services.StudentRepository();
        private readonly CampusVault.Services.NotificationRepository _notifRepo = new CampusVault.Services.NotificationRepository();
        private readonly CampusVault.Services.FacultyRepository _facultyRepo = new CampusVault.Services.FacultyRepository();

        private int GetCurrentUserId()
        {
            if (Session["UserId"] == null)
            {
                Response.Redirect("~/Account/Login");
                return 0;
            }
            return (int)Session["UserId"];
        }

        // GET: /Student/Dashboard
        public ActionResult Dashboard()
        {
            SetStudentContext("Dashboard");
            int userId = GetCurrentUserId();
            if (userId == 0) return new EmptyResult();
            var vm = _studentRepo.GetStudentDashboardData(userId);
            return View(vm);
        }

        // GET: /Student/Classrooms
        public ActionResult Classrooms()
        {
            SetStudentContext("My Classrooms");
            int userId = GetCurrentUserId();
            if (userId == 0) return new EmptyResult();
            var classrooms = _classroomRepo.GetStudentClassrooms(userId);
            return View(classrooms);
        }

        // GET: /Student/JoinClassroom
        [HttpGet]
        public ActionResult JoinClassroom()
        {
            SetStudentContext("Join Classroom");
            return View();
        }

        // POST: /Student/JoinClassroom (AJAX)
        [HttpPost]
        public JsonResult JoinClassroom(string joinCode)
        {
            if (Session["UserId"] == null)
                return Json(new { success = false, message = "Session expired." });

            if (string.IsNullOrWhiteSpace(joinCode))
                return Json(new { success = false, message = "Classroom join code is required." });

            int userId = (int)Session["UserId"];
            var result = _classroomRepo.JoinClassroom(userId, joinCode);
            return Json(new { success = result.success, message = result.message, classroomId = result.classroomId });
        }

        // GET: /Student/ClassroomDetails/{id}
        public ActionResult ClassroomDetails(int id = 0)
        {
            SetStudentContext("Classroom Details");
            int userId = GetCurrentUserId();
            if (userId == 0) return new EmptyResult();
            var vm = _classroomRepo.GetClassroomDetails(id, userId);
            if (vm == null || vm.Classroom == null)
            {
                return RedirectToAction("Classrooms");
            }
            ViewBag.ClassroomId = id;
            return View(vm);
        }

        // POST: /Student/LeaveClassroom (AJAX)
        [HttpPost]
        public JsonResult LeaveClassroom(int classroomId)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return Json(new { success = false, message = "Session expired." });
            var result = _classroomRepo.LeaveClassroom(userId, classroomId);
            return Json(new { success = result.success, message = result.message });
        }

        // GET: /Student/ClassroomTasks/{id}
        public ActionResult ClassroomTasks(int id = 0)
        {
            return RedirectToAction("ClassroomDetails", new { id = id });
        }

        // GET: /Student/ClassroomMaterials/{id}
        public ActionResult ClassroomMaterials(int id = 0)
        {
            return RedirectToAction("ClassroomDetails", new { id = id });
        }

        // GET: /Student/ClassroomAnnouncements/{id}
        public ActionResult ClassroomAnnouncements(int id = 0)
        {
            return RedirectToAction("ClassroomDetails", new { id = id });
        }

        // GET: /Student/ClassroomMembers/{id}
        public ActionResult ClassroomMembers(int id = 0)
        {
            return RedirectToAction("ClassroomDetails", new { id = id });
        }

        // GET: /Student/Tasks
        public ActionResult Tasks()
        {
            SetStudentContext("My Tasks");
            int userId = GetCurrentUserId();
            if (userId == 0) return new EmptyResult();
            var tasks = _studentRepo.GetStudentTasks(userId);
            return View(tasks);
        }

        // GET: /Student/Calendar
        public ActionResult Calendar()
        {
            SetStudentContext("Academic Calendar");
            int userId = GetCurrentUserId();
            if (userId == 0) return new EmptyResult();
            var tasks = _studentRepo.GetStudentTasks(userId);
            return View(tasks);
        }

        // GET: /Student/TaskDetails/{id}
        public ActionResult TaskDetails(int id = 0)
        {
            SetStudentContext("Task Details");
            int userId = GetCurrentUserId();
            if (userId == 0) return new EmptyResult();
            var task = _classroomRepo.GetTaskDetails(id, userId);
            if (task == null)
            {
                return RedirectToAction("Classrooms");
            }
            ViewBag.TaskId = id;
            var facultyRepo = new CampusVault.Services.FacultyRepository();
            ViewBag.Attachments = facultyRepo.GetTaskAttachments(id);
            return View(task);
        }

        // GET: /Student/DownloadAttachment/{id}
        public ActionResult DownloadAttachment(int id)
        {
            var repo = new CampusVault.Services.FacultyRepository();
            var att = repo.GetTaskAttachmentById(id);
            if (att == null)
            {
                TempData["ErrorMessage"] = "Attachment not found.";
                return RedirectToAction("Tasks");
            }

            if (!string.IsNullOrEmpty(att.FilePath) && System.IO.File.Exists(att.FilePath))
            {
                string contentType = MimeMapping.GetMimeMapping(att.FileName);
                return File(att.FilePath, contentType, att.FileName);
            }

            if (!string.IsNullOrEmpty(att.FileUrl))
            {
                return Redirect(att.FileUrl);
            }

            TempData["ErrorMessage"] = "Attachment file is not accessible.";
            return RedirectToAction("Tasks");
        }

        // GET: /Student/SubmitDocument/{id}
        public ActionResult SubmitDocument(int id = 0)
        {
            SetStudentContext("Submit Document");
            int userId = GetCurrentUserId();
            if (userId == 0) return new EmptyResult();
            var task = _classroomRepo.GetTaskDetails(id, userId);
            if (task == null)
            {
                return RedirectToAction("Tasks");
            }
            ViewBag.TaskId = id;
            return View(task);
        }

        // POST: /Student/SubmitDocument
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitDocument(HttpPostedFileBase document, int taskId)
        {
            int userId = GetCurrentUserId();
            bool isAjax = Request.IsAjaxRequest() || Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (userId == 0)
            {
                if (isAjax) return Json(new { success = false, message = "Session expired. Please sign in again." });
                return RedirectToAction("Login", "Account");
            }

            if (document == null || document.ContentLength == 0)
            {
                string msg = "Please select a valid coursework file to upload.";
                if (isAjax) return Json(new { success = false, message = msg });
                TempData["ErrorMessage"] = msg;
                return RedirectToAction("SubmitDocument", new { id = taskId });
            }

            // 1. Validate file extension
            string ext = System.IO.Path.GetExtension(document.FileName)?.ToLowerInvariant();
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".zip", ".rar", ".cpp", ".java", ".py", ".txt" };
            if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
            {
                string msg = $"File format '{ext}' is not permitted. Allowed formats: PDF, DOCX, PPTX, ZIP, RAR, CPP, JAVA, PY, TXT.";
                if (isAjax) return Json(new { success = false, message = msg });
                TempData["ErrorMessage"] = msg;
                return RedirectToAction("SubmitDocument", new { id = taskId });
            }

            // 2. Validate institutional file size limit (25 MB)
            if (document.ContentLength > 25 * 1024 * 1024)
            {
                string msg = "File size exceeds the 25 MB institutional quota limit.";
                if (isAjax) return Json(new { success = false, message = msg });
                TempData["ErrorMessage"] = msg;
                return RedirectToAction("SubmitDocument", new { id = taskId });
            }

            // 3. Authorize student and verify task deadline / accessibility
            var taskDetails = _classroomRepo.GetTaskDetails(taskId, userId);
            if (taskDetails == null)
            {
                string msg = "Task not found or you are not enrolled in this classroom.";
                if (isAjax) return Json(new { success = false, message = msg });
                TempData["ErrorMessage"] = msg;
                return RedirectToAction("Tasks");
            }

            // Upload to Cloudinary
            var cloudinary = new CampusVault.Services.CloudinaryService();
            var upload = cloudinary.UploadFile(document);

            if (!upload.success)
            {
                string msg = upload.error ?? "Failed to upload document to storage provider.";
                if (isAjax) return Json(new { success = false, message = msg });
                TempData["ErrorMessage"] = msg;
                return RedirectToAction("SubmitDocument", new { id = taskId });
            }

            // Compute real SHA-256 cryptographic hash from uploaded file stream
            string fileHash = null;
            try
            {
                if (document.InputStream != null && document.InputStream.CanSeek)
                {
                    document.InputStream.Position = 0;
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        byte[] hashBytes = sha256.ComputeHash(document.InputStream);
                        fileHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    }
                    document.InputStream.Position = 0;
                }
            }
            catch { }

            string fileSize = cloudinary.FormatFileSize(document.ContentLength);
            var result = _studentRepo.SubmitTaskDocument(taskId, userId, document.FileName, fileSize, upload.url, upload.url, upload.publicId, fileHash);

            if (result.success)
            {
                // Notify faculty mentor of this task's classroom
                try
                {
                    using (var conn = new System.Data.SqlClient.SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["CampusVaultConnection"].ConnectionString))
                    {
                        conn.Open();
                        using (var cmd = new System.Data.SqlClient.SqlCommand("SELECT c.FacultyUserId FROM Tasks t JOIN Classrooms c ON t.ClassroomId = c.ClassroomId WHERE t.TaskId = @TaskId", conn))
                        {
                            cmd.Parameters.Add("@TaskId", System.Data.SqlDbType.Int).Value = taskId;
                            var fIdObj = cmd.ExecuteScalar();
                            if (fIdObj != null && fIdObj != DBNull.Value)
                            {
                                int facultyUserId = Convert.ToInt32(fIdObj);
                                string studentName = Session["UserName"] as string ?? "A student";
                                _notifRepo.CreateNotification(
                                    facultyUserId,
                                    "submissions",
                                    $"New Submission: {taskDetails.Title}",
                                    $"{studentName} submitted coursework for '{taskDetails.Title}'.",
                                    "Needs Review",
                                    "warning",
                                    $"{taskDetails.CourseCode} • {taskDetails.CourseName}",
                                    Url.Action("Submissions", "Faculty"),
                                    "Review Coursework"
                                );
                            }
                        }
                    }
                }
                catch { }

                string submittedTimestamp = DateTime.Now.ToString("dd MMM yyyy, hh:mm tt");
                if (isAjax)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Assignment submitted successfully!",
                        submissionStatus = "Pending Review",
                        submittedAt = submittedTimestamp,
                        fileName = document.FileName,
                        fileSize = fileSize,
                        redirectUrl = Url.Action("Tasks", "Student")
                    });
                }

                TempData["SuccessMessage"] = "Work submitted successfully for review!";
                return RedirectToAction("Tasks");
            }

            if (isAjax)
            {
                return Json(new { success = false, message = result.message ?? "Database persistence failed." });
            }

            TempData["ErrorMessage"] = result.message;
            return RedirectToAction("SubmitDocument", new { id = taskId });
        }

        // GET: /Student/Revision/{id}
        public ActionResult Revision(int id = 0)
        {
            SetStudentContext("Revision");
            int userId = GetCurrentUserId();
            if (userId == 0) return new EmptyResult();
            var docs = _studentRepo.GetStudentDocuments(userId);
            var doc = docs.Find(d => d.SubmissionId == id);
            if (doc == null)
            {
                return RedirectToAction("Documents");
            }
            ViewBag.SubmissionId = id;
            return View(doc);
        }

        // GET: /Student/Documents
        public ActionResult Documents()
        {
            SetStudentContext("My Documents");
            int userId = GetCurrentUserId();
            if (userId == 0) return new EmptyResult();
            var docs = _studentRepo.GetStudentDocuments(userId);
            ViewBag.AvailableTasks = _studentRepo.GetStudentTasks(userId);
            return View(docs);
        }

        // POST: /Student/UploadDocument
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadDocument(HttpPostedFileBase document, int? taskId)
        {
            int userId = GetCurrentUserId();
            bool isAjax = Request.IsAjaxRequest() || Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (userId == 0)
            {
                if (isAjax) return Json(new { success = false, message = "Session expired. Please sign in again." });
                return RedirectToAction("Login", "Account");
            }

            if (document == null || document.ContentLength == 0)
            {
                string msg = "Please select a valid document to upload.";
                if (isAjax) return Json(new { success = false, message = msg });
                TempData["ErrorMessage"] = msg;
                return RedirectToAction("Documents");
            }

            int targetTaskId = taskId.GetValueOrDefault();
            if (targetTaskId <= 0)
            {
                var tasks = _studentRepo.GetStudentTasks(userId);
                if (tasks != null && tasks.Count > 0)
                {
                    targetTaskId = tasks[0].TaskId;
                }
            }

            if (targetTaskId <= 0)
            {
                string msg = "Please join a classroom or select an active task before uploading documents.";
                if (isAjax) return Json(new { success = false, message = msg });
                TempData["ErrorMessage"] = msg;
                return RedirectToAction("Documents");
            }

            return SubmitDocument(document, targetTaskId);
        }

        // POST: /Student/DeleteDocument
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteDocument(int id)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return Json(new { success = false, message = "Session expired. Please sign in again." });

            try
            {
                using (var conn = new System.Data.SqlClient.SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["CampusVaultConnection"].ConnectionString))
                {
                    conn.Open();
                    using (var checkCmd = new System.Data.SqlClient.SqlCommand("SELECT Status FROM TaskSubmissions WHERE SubmissionId = @SubmissionId AND StudentUserId = @StudentUserId", conn))
                    {
                        checkCmd.Parameters.Add("@SubmissionId", System.Data.SqlDbType.Int).Value = id;
                        checkCmd.Parameters.Add("@StudentUserId", System.Data.SqlDbType.Int).Value = userId;
                        var statusObj = checkCmd.ExecuteScalar();
                        if (statusObj == null)
                        {
                            return Json(new { success = false, message = "Document not found or access denied." });
                        }
                        string status = statusObj.ToString();
                        if (status.Equals("Approved", StringComparison.OrdinalIgnoreCase) || status.Equals("Graded", StringComparison.OrdinalIgnoreCase))
                        {
                            return Json(new { success = false, message = "Approved documents cannot be removed." });
                        }
                    }

                    using (var delCmd = new System.Data.SqlClient.SqlCommand("DELETE FROM TaskSubmissions WHERE SubmissionId = @SubmissionId AND StudentUserId = @StudentUserId", conn))
                    {
                        delCmd.Parameters.Add("@SubmissionId", System.Data.SqlDbType.Int).Value = id;
                        delCmd.Parameters.Add("@StudentUserId", System.Data.SqlDbType.Int).Value = userId;
                        int affected = delCmd.ExecuteNonQuery();
                        if (affected > 0)
                        {
                            return Json(new { success = true, message = "Document removed successfully." });
                        }
                    }
                }
                return Json(new { success = false, message = "Unable to delete document." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // GET: /Student/DocumentDetails/{id}
        public ActionResult DocumentDetails(int id = 0)
        {
            SetStudentContext("Document Details");
            int userId = GetCurrentUserId();
            if (userId == 0) return new EmptyResult();
            var docs = _studentRepo.GetStudentDocuments(userId);
            // Find ONLY by SubmissionId — never fall back to first doc
            var doc = docs.Find(d => d.SubmissionId == id);
            if (doc == null)
            {
                return RedirectToAction("Documents");
            }
            ViewBag.DocumentId = doc.SubmissionId;
            return View(doc);
        }

        // GET: /Student/DocumentVersions/{id}
        public ActionResult DocumentVersions(int id = 0)
        {
            return RedirectToAction("DocumentDetails", new { id = id });
        }

        // GET: /Student/SubmissionHistory
        public ActionResult SubmissionHistory()
        {
            SetStudentContext("Submissions");
            int userId = GetCurrentUserId();
            if (userId == 0) return new EmptyResult();
            var docs = _studentRepo.GetStudentDocuments(userId);
            return View(docs);
        }

        // GET: /Student/Notifications
        public ActionResult Notifications()
        {
            SetStudentContext("Notifications");
            int userId = GetCurrentUserId();
            if (userId == 0) return new EmptyResult();

            var notifs = _notifRepo.GetNotifications(userId, 50);

            // If no notifications exist yet in DB, sync initial notifications from tasks/docs
            if (notifs == null || notifs.Count == 0)
            {
                var tasks = _studentRepo.GetStudentTasks(userId);
                var docs = _studentRepo.GetStudentDocuments(userId);

                if (docs != null)
                {
                    foreach (var doc in docs)
                    {
                        if (string.Equals(doc.Status, "Needs Revision", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(doc.Status, "Revision Requested", StringComparison.OrdinalIgnoreCase))
                        {
                            _notifRepo.CreateNotification(
                                userId,
                                "revisions",
                                $"{doc.TaskTitle} requires revision",
                                !string.IsNullOrEmpty(doc.Feedback) ? $"Faculty feedback: \"{doc.Feedback}\"" : "Please review instructor feedback and submit updated coursework.",
                                "Revision Requested",
                                "warning",
                                $"{doc.CourseCode} • {doc.CourseName} • {doc.FacultyName}",
                                Url.Action("SubmitDocument", "Student", new { id = doc.TaskId }),
                                "Upload Revision"
                            );
                        }
                        else if (string.Equals(doc.Status, "Approved", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(doc.Status, "Graded", StringComparison.OrdinalIgnoreCase) ||
                                 doc.Score.HasValue)
                        {
                            _notifRepo.CreateNotification(
                                userId,
                                "submissions",
                                $"Submission evaluated: {doc.TaskTitle}",
                                doc.Score.HasValue ? $"Marks awarded: {doc.Score.Value} / {doc.MaxScore}. {doc.Feedback}" : $"Approved: {doc.Feedback}",
                                "Evaluation Approved",
                                "success",
                                $"{doc.CourseCode} • {doc.CourseName} • {doc.FacultyName}",
                                Url.Action("DocumentDetails", "Student", new { id = doc.SubmissionId }),
                                "View Document"
                            );
                        }
                    }
                }

                if (tasks != null)
                {
                    foreach (var t in tasks)
                    {
                        if (string.IsNullOrEmpty(t.SubmissionStatus) || string.Equals(t.SubmissionStatus, "Pending", StringComparison.OrdinalIgnoreCase))
                        {
                            bool isOverdue = t.DueDate < DateTime.Now;
                            _notifRepo.CreateNotification(
                                userId,
                                "tasks",
                                isOverdue ? $"Overdue task: {t.Title}" : $"Pending assignment: {t.Title}",
                                $"Due on {t.DueDate:MMM dd, yyyy}. Submit your coursework report.",
                                isOverdue ? "Overdue" : "Pending Task",
                                isOverdue ? "danger" : "task",
                                $"{t.CourseCode} • {t.CourseName} • {t.FacultyName}",
                                Url.Action("TaskDetails", "Student", new { id = t.TaskId }),
                                "Submit Assignment"
                            );
                        }
                    }
                }

                notifs = _notifRepo.GetNotifications(userId, 50);
            }

            return View(notifs);
        }

        // POST: /Student/MarkAllNotificationsAsRead (AJAX)
        [HttpPost]
        public JsonResult MarkAllNotificationsAsRead()
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return Json(new { success = false, message = "Session expired." });
            bool ok = _notifRepo.MarkAllAsRead(userId);
            return Json(new { success = ok });
        }

        // POST: /Student/MarkNotificationAsRead (AJAX)
        [HttpPost]
        public JsonResult MarkNotificationAsRead(int notificationId)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return Json(new { success = false, message = "Session expired." });
            bool ok = _notifRepo.MarkAsRead(notificationId, userId);
            return Json(new { success = ok });
        }

        private readonly CampusVault.Services.AuthRepository _authRepo = new CampusVault.Services.AuthRepository();

        // GET: /Student/Profile
        public new ActionResult Profile()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = (int)Session["UserId"];
            var user = _authRepo.GetUserById(userId);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            SetStudentContext("My Profile");

            // Pass real classrooms and documents for profile stats/tables
            var classrooms = _classroomRepo.GetStudentClassrooms(userId);
            var docs       = _studentRepo.GetStudentDocuments(userId);

            ViewBag.ProfileClassrooms = classrooms;
            ViewBag.ProfileDocs       = docs;

            return View(user);
        }

        // POST: /Student/UpdateProfile (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateProfile(string firstName, string lastName, string phoneNumber, string division)
        {
            if (Session["UserId"] == null)
                return Json(new { success = false, message = "Session expired. Please log in again." });

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                return Json(new { success = false, message = "First name and last name are required." });

            int userId = (int)Session["UserId"];
            var result = _authRepo.UpdateStudentProfile(userId, firstName, lastName, phoneNumber, division);
            if (result.success)
            {
                Session["UserName"] = $"{firstName.Trim()} {lastName.Trim()}";
                string f = firstName.Trim().Substring(0, 1).ToUpper();
                string l = lastName.Trim().Substring(0, 1).ToUpper();
                Session["UserInitials"] = $"{f}{l}";
            }
            return Json(new { success = result.success, message = result.message });
        }

        // POST: /Student/ChangePassword (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ChangePassword(string currentPassword, string newPassword, string confirmPassword = null)
        {
            if (Session["UserId"] == null)
                return Json(new { success = false, message = "Session expired. Please log in again." });

            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
                return Json(new { success = false, message = "Current and new password are required." });

            if (newPassword.Length < 6)
                return Json(new { success = false, message = "New password must be at least 6 characters long." });

            if (!string.IsNullOrEmpty(confirmPassword) && newPassword != confirmPassword)
                return Json(new { success = false, message = "New password and confirmation do not match." });

            int userId = (int)Session["UserId"];
            var user = _authRepo.GetUserById(userId);
            if (user == null) return Json(new { success = false, message = "User not found." });

            if (!CampusVault.Services.PasswordHasher.VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt))
            {
                return Json(new { success = false, message = "Current password does not match our records." });
            }

            string newSalt = CampusVault.Services.PasswordHasher.GenerateSalt();
            string newHash = CampusVault.Services.PasswordHasher.HashPassword(newPassword, newSalt);

            var result = _authRepo.ChangePassword(userId, newHash, newSalt);
            return Json(new { success = result.success, message = result.message });
        }


        // POST: /Student/UploadProfilePicture (AJAX)
        [HttpPost]
        public JsonResult UploadProfilePicture(System.Web.HttpPostedFileBase profilePic)
        {
            if (Session["UserId"] == null) return Json(new { success = false, message = "Session expired." });
            if (profilePic == null || profilePic.ContentLength == 0)
                return Json(new { success = false, message = "No file selected." });
            int userId = (int)Session["UserId"];
            var cloudinary = new CampusVault.Services.CloudinaryService();
            var upload = cloudinary.UploadFile(profilePic);
            if (!upload.success) return Json(new { success = false, message = upload.error });
            var result = _authRepo.UpdateProfilePicture(userId, upload.url);
            return Json(new { success = result.success, message = result.message, url = upload.url });
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        // GET: /Student/DownloadMaterial/{id}
        public ActionResult DownloadMaterial(int id)
        {
            int userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var material = _facultyRepo.GetMaterialById(id);
            if (material == null)
            {
                TempData["ErrorMessage"] = "The requested learning material could not be found.";
                return RedirectToAction("Classrooms");
            }

            // Verify student enrollment in the classroom
            if (!_facultyRepo.IsStudentEnrolledInClassroom(userId, material.ClassroomId))
            {
                TempData["ErrorMessage"] = "Access denied: you are not enrolled in this classroom.";
                return RedirectToAction("Classrooms");
            }

            if (!string.IsNullOrEmpty(material.ExternalUrl))
            {
                return Redirect(material.ExternalUrl);
            }

            if (!string.IsNullOrEmpty(material.FileUrl))
            {
                return Redirect(material.FileUrl);
            }

            TempData["ErrorMessage"] = "The material file is not available.";
            return RedirectToAction("ClassroomDetails", new { id = material.ClassroomId });
        }
    }
}
