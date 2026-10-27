using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CampusVault.Attributes;
using CampusVault.Models.Faculty;
using CampusVault.Services;

namespace CampusVault.Controllers
{
    [CampusVaultAuthorize("Faculty")]
    public class FacultyController : Controller
    {
        private readonly FacultyRepository _facultyRepo;
        private readonly ClassroomRepository _classroomRepo;
        private readonly AuthRepository _authRepo;
        private readonly NotificationRepository _notifRepo;

        public FacultyController()
        {
            _facultyRepo = new FacultyRepository();
            _classroomRepo = new ClassroomRepository();
            _authRepo = new AuthRepository();
            _notifRepo = new NotificationRepository();
        }

        /// <summary>
        /// Gets current logged-in faculty user ID. Redirects to login if not authenticated.
        /// </summary>
        private int GetCurrentFacultyId()
        {
            if (Session["UserId"] == null)
            {
                Response.Redirect("~/Account/Login");
                return 0;
            }
            return (int)Session["UserId"];
        }

        /// <summary>
        /// Sets common ViewBag properties for all faculty views dynamically from Session.
        /// </summary>
        private void SetFacultyContext(string title)
        {
            ViewBag.Title = title;
            ViewBag.Role = "Faculty";
            ViewBag.UserName = Session["UserName"] as string ?? "Prof. Patel";
            ViewBag.UserInitials = Session["UserInitials"] as string ?? "PP";
            ViewBag.UserRoleDisplay = Session["UserRoleDisplay"] as string ?? "Department Faculty";
        }

        // GET: /Faculty/Dashboard
        public ActionResult Dashboard()
        {
            SetFacultyContext("Faculty Dashboard");
            int userId = GetCurrentFacultyId();
            if (userId == 0) return new EmptyResult();
            var vm = _facultyRepo.GetDashboardData(userId);
            return View(vm);
        }

        // GET: /Faculty/Classrooms
        public ActionResult Classrooms()
        {
            SetFacultyContext("My Classrooms");
            int userId = GetCurrentFacultyId();
            if (userId == 0) return new EmptyResult();
            var classrooms = _facultyRepo.GetFacultyClassrooms(userId);
            return View(classrooms);
        }

        // GET: /Faculty/CreateClassroom
        public ActionResult CreateClassroom()
        {
            SetFacultyContext("Create Classroom");
            var lookup = new CampusVault.Services.LookupRepository();
            ViewBag.Departments   = lookup.GetDepartments();
            ViewBag.Semesters     = lookup.GetSemesters();
            ViewBag.Divisions     = lookup.GetDivisions();
            ViewBag.AcademicYears = lookup.GetAcademicYears();
            return View(new CreateClassroomModel());
        }

        // POST: /Faculty/CreateClassroom
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateClassroom(CreateClassroomModel model)
        {
            SetFacultyContext("Create Classroom");
            int userId = GetCurrentFacultyId();
            if (userId == 0) return new EmptyResult();

            if (string.IsNullOrWhiteSpace(model.CourseCode) || string.IsNullOrWhiteSpace(model.CourseName) || string.IsNullOrWhiteSpace(model.JoinCode))
            {
                var lookup = new CampusVault.Services.LookupRepository();
                ViewBag.Departments   = lookup.GetDepartments();
                ViewBag.Semesters     = lookup.GetSemesters();
                ViewBag.Divisions     = lookup.GetDivisions();
                ViewBag.AcademicYears = lookup.GetAcademicYears();
                ViewBag.ErrorMessage = "Course Code, Course Name, and Section Key / Join Code are required.";
                return View(model);
            }

            var result = _facultyRepo.CreateClassroom(userId, model);
            if (result.success)
            {
                TempData["SuccessMessage"] = "Classroom '" + model.CourseName + "' (" + model.CourseCode + ") created successfully!";
                return RedirectToAction("Classrooms");
            }

            var lookup2 = new CampusVault.Services.LookupRepository();
            ViewBag.Departments   = lookup2.GetDepartments();
            ViewBag.Semesters     = lookup2.GetSemesters();
            ViewBag.Divisions     = lookup2.GetDivisions();
            ViewBag.AcademicYears = lookup2.GetAcademicYears();
            ViewBag.ErrorMessage = result.message;
            return View(model);
        }

        // GET: /Faculty/ClassroomDetails/{id}
        public ActionResult ClassroomDetails(int id = 0)
        {
            SetFacultyContext("Classroom Details");
            int userId = GetCurrentFacultyId();
            if (userId == 0) return new EmptyResult();
            var vm = _classroomRepo.GetClassroomDetails(id, userId);
            if (vm == null || vm.Classroom == null)
            {
                return RedirectToAction("Classrooms");
            }
            ViewBag.ClassroomId = id;
            return View(vm);
        }

        // GET: /Faculty/Tasks
        public ActionResult Tasks()
        {
            SetFacultyContext("Coursework Tasks");
            int userId = GetCurrentFacultyId();
            if (userId == 0) return new EmptyResult();
            var tasks = _facultyRepo.GetFacultyTasks(userId);
            return View(tasks);
        }

        // GET: /Faculty/CreateTask
        public ActionResult CreateTask(int? classroomId, int? id, int? taskId)
        {
            int userId = GetCurrentFacultyId();
            if (userId == 0) return new EmptyResult();

            int targetTaskId = id ?? taskId ?? 0;
            CreateTaskModel model;

            if (targetTaskId > 0)
            {
                model = _facultyRepo.GetTaskForEdit(targetTaskId, userId);
                if (model == null)
                {
                    TempData["ErrorMessage"] = "Task not found or you are not authorized to edit this task.";
                    return RedirectToAction("Tasks");
                }
                model.ExistingAttachments = _facultyRepo.GetTaskAttachments(targetTaskId);
                ViewBag.IsEdit = true;
                ViewBag.Title = "Edit Academic Task";
                SetFacultyContext("Edit Task");
            }
            else
            {
                model = new CreateTaskModel
                {
                    ClassroomId = classroomId ?? 0,
                    DueDate = DateTime.Now.AddDays(7),
                    MaxScore = 20,
                    TaskType = "Assignment",
                    ExistingAttachments = new List<TaskAttachmentItem>()
                };
                ViewBag.IsEdit = false;
                ViewBag.Title = "Create Task";
                SetFacultyContext("Create Task");
            }

            var lookup = new CampusVault.Services.LookupRepository();
            ViewBag.Classrooms = _facultyRepo.GetFacultyClassrooms(userId);
            ViewBag.TaskTypes  = lookup.GetTaskTypes();

            return View(model);
        }

        // POST: /Faculty/CreateTask
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateTask(CreateTaskModel model, IEnumerable<HttpPostedFileBase> referenceFiles, string removedAttachmentIds)
        {
            int userId = GetCurrentFacultyId();
            if (userId == 0) return new EmptyResult();

            bool isEdit = model != null && model.TaskId > 0;
            ViewBag.IsEdit = isEdit;
            ViewBag.Title = isEdit ? "Edit Academic Task" : "Create Task";
            SetFacultyContext(isEdit ? "Edit Task" : "Create Task");

            var lookup = new CampusVault.Services.LookupRepository();
            ViewBag.Classrooms = _facultyRepo.GetFacultyClassrooms(userId);
            ViewBag.TaskTypes  = lookup.GetTaskTypes();

            if (model == null)
            {
                ViewBag.ErrorMessage = "Invalid task submission.";
                return View(new CreateTaskModel());
            }

            // Reload existing attachments if editing so the view displays them on validation errors
            if (isEdit && (model.ExistingAttachments == null || model.ExistingAttachments.Count == 0))
            {
                model.ExistingAttachments = _facultyRepo.GetTaskAttachments(model.TaskId);
            }

            // 1. Basic validation
            if (model.ClassroomId <= 0 || string.IsNullOrWhiteSpace(model.Title))
            {
                ViewBag.ErrorMessage = "Please select a classroom and enter a valid task title.";
                return View(model);
            }

            // 2. Classroom authorization check
            if (!_facultyRepo.IsFacultyAuthorizedForClassroom(userId, model.ClassroomId))
            {
                ViewBag.ErrorMessage = "You are not authorized to assign coursework to the selected classroom.";
                return View(model);
            }

            // 3. Defaults & normalization
            if (model.MaxScore <= 0 || model.MaxScore > 100)
            {
                model.MaxScore = 20;
            }
            if (model.DueDate == default(DateTime) || model.DueDate < new DateTime(2000, 1, 1))
            {
                model.DueDate = DateTime.Now.AddDays(7);
            }
            if (string.IsNullOrWhiteSpace(model.TaskType))
            {
                model.TaskType = "Assignment";
            }

            // 4. Collect and validate reference files
            var filesList = new List<HttpPostedFileBase>();
            if (referenceFiles != null)
            {
                filesList.AddRange(referenceFiles.Where(f => f != null && f.ContentLength > 0));
            }
            if (Request.Files != null && Request.Files.Count > 0)
            {
                for (int i = 0; i < Request.Files.Count; i++)
                {
                    var file = Request.Files[i];
                    if (file != null && file.ContentLength > 0 &&
                        !filesList.Any(f => f.FileName == file.FileName && f.ContentLength == file.ContentLength))
                    {
                        filesList.Add(file);
                    }
                }
            }

            var allowedExtensions = new[] { ".pdf", ".docx", ".doc", ".zip", ".rar" };
            const long maxFileSize = 25 * 1024 * 1024; // 25 MB

            foreach (var file in filesList)
            {
                string ext = System.IO.Path.GetExtension(file.FileName)?.ToLowerInvariant();
                if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                {
                    ViewBag.ErrorMessage = $"Unsupported file format '{ext}' in '{file.FileName}'. Allowed formats: PDF, DOCX, DOC, ZIP, RAR.";
                    return View(model);
                }

                if (file.ContentLength > maxFileSize)
                {
                    ViewBag.ErrorMessage = $"File '{file.FileName}' exceeds the maximum allowed size of 25 MB.";
                    return View(model);
                }
            }

            int finalTaskId = model.TaskId;

            if (isEdit)
            {
                var updateRes = _facultyRepo.UpdateTask(model.TaskId, userId, model.Title, model.Description, model.TaskType, model.MaxScore, model.DueDate);
                if (!updateRes.success)
                {
                    ViewBag.ErrorMessage = updateRes.message ?? "Failed to update task.";
                    return View(model);
                }

                // Delete removed attachments if any
                if (!string.IsNullOrWhiteSpace(removedAttachmentIds))
                {
                    var ids = removedAttachmentIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var sId in ids)
                    {
                        if (int.TryParse(sId.Trim(), out int attId))
                        {
                            _facultyRepo.DeleteTaskAttachment(attId, userId);
                        }
                    }
                }
            }
            else
            {
                var createRes = _facultyRepo.CreateTask(model);
                if (!createRes.success || createRes.newTaskId <= 0)
                {
                    ViewBag.ErrorMessage = createRes.message ?? "Failed to create task.";
                    return View(model);
                }
                finalTaskId = createRes.newTaskId;

                // Send notification to classroom students
                try
                {
                    _notifRepo.NotifyClassroomStudents(
                        model.ClassroomId,
                        "tasks",
                        $"New Task: {model.Title}",
                        $"A new task '{model.Title}' has been assigned. Due on {model.DueDate:MMM dd, yyyy}.",
                        "New Task",
                        "task",
                        null,
                        Url.Action("Tasks", "Student"),
                        "View Task"
                    );
                }
                catch { }
            }

            // 5. Persist uploaded reference files
            if (filesList.Count > 0 && finalTaskId > 0)
            {
                var cloudinary = new CampusVault.Services.CloudinaryService();
                string uploadFolder = Server.MapPath("~/Content/Uploads/TaskAttachments");
                if (!System.IO.Directory.Exists(uploadFolder))
                {
                    System.IO.Directory.CreateDirectory(uploadFolder);
                }

                foreach (var file in filesList)
                {
                    string fileName = System.IO.Path.GetFileName(file.FileName);
                    string ext = System.IO.Path.GetExtension(fileName)?.TrimStart('.')?.ToUpperInvariant() ?? "FILE";
                    string fileSize = cloudinary.FormatFileSize(file.ContentLength);
                    string cloudUrl = null;
                    string cloudPublicId = null;
                    string localPath = null;

                    // Cloudinary upload attempt
                    try
                    {
                        var uploadRes = cloudinary.UploadLearningMaterial(file);
                        if (uploadRes.success)
                        {
                            cloudUrl = uploadRes.url;
                            cloudPublicId = uploadRes.publicId;
                        }
                    }
                    catch { }

                    // Local storage fallback / mirror
                    try
                    {
                        string safeName = $"{Guid.NewGuid():N}_{fileName}";
                        localPath = System.IO.Path.Combine(uploadFolder, safeName);
                        file.SaveAs(localPath);
                        if (string.IsNullOrEmpty(cloudUrl))
                        {
                            cloudUrl = Url.Content($"~/Content/Uploads/TaskAttachments/{safeName}");
                        }
                    }
                    catch { }

                    if (!string.IsNullOrEmpty(cloudUrl))
                    {
                        _facultyRepo.SaveTaskAttachment(finalTaskId, fileName, fileSize, ext, cloudUrl, cloudPublicId, localPath);
                    }
                }
            }

            TempData["SuccessMessage"] = isEdit
                ? $"Task '{model.Title}' updated successfully!"
                : $"Task '{model.Title}' created successfully!";
            return RedirectToAction("Tasks");
        }

        // GET: /Faculty/DownloadAttachment/{id}
        public ActionResult DownloadAttachment(int id)
        {
            var att = _facultyRepo.GetTaskAttachmentById(id);
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

        // GET: /Faculty/TaskDetails/{id}
        public ActionResult TaskDetails(int id = 1)
        {
            return RedirectToAction("Tasks");
        }

        // GET: /Faculty/Submissions
        public ActionResult Submissions()
        {
            SetFacultyContext("Student Submissions");
            int userId = GetCurrentFacultyId();
            if (userId == 0) return new EmptyResult();
            var submissions = _facultyRepo.GetAllSubmissions(userId);
            return View(submissions);
        }

        // GET: /Faculty/ReviewSubmission/{id}
        public ActionResult ReviewSubmission(int id = 0)
        {
            SetFacultyContext("Review Submission");
            int userId = GetCurrentFacultyId();
            if (userId == 0) return new EmptyResult();

            var sub = _facultyRepo.GetSubmissionById(id);
            if (sub == null)
                return RedirectToAction("Submissions");

            ViewBag.Title = "Review — " + sub.TaskTitle;
            return View(sub);
        }

        // POST: /Faculty/ReviewSubmission (AJAX)
        [HttpPost]
        public JsonResult ReviewSubmission(int submissionId, string action, int? score, string feedback)
        {
            int userId = GetCurrentFacultyId();
            if (userId == 0)
                return Json(new { success = false, message = "Session expired." });

            if (string.IsNullOrWhiteSpace(action))
                return Json(new { success = false, message = "Action is required." });

            if (action == "Approve" && (score == null || score < 0))
                return Json(new { success = false, message = "Please enter a valid score." });

            if (action == "Revision" && string.IsNullOrWhiteSpace(feedback))
                return Json(new { success = false, message = "Please enter revision feedback." });

            var result = _facultyRepo.ReviewSubmission(submissionId, action, score, feedback, userId);
            if (result.success)
            {
                // Dynamically notify the student about evaluation or revision request
                try
                {
                    var sub = _facultyRepo.GetSubmissionById(submissionId);
                    if (sub != null && sub.StudentUserId > 0)
                    {
                        if (action == "Approve")
                        {
                            _notifRepo.CreateNotification(
                                sub.StudentUserId,
                                "submissions",
                                $"Submission Evaluated: {sub.TaskTitle}",
                                $"Your coursework for '{sub.TaskTitle}' was evaluated and approved. Marks: {score}/{sub.MaxScore}. Feedback: {feedback}",
                                "Approved",
                                "success",
                                $"{sub.CourseCode} • {sub.CourseName}",
                                Url.Action("DocumentDetails", "Student", new { id = submissionId }),
                                "View Document"
                            );
                        }
                        else
                        {
                            _notifRepo.CreateNotification(
                                sub.StudentUserId,
                                "revisions",
                                $"Revision Requested: {sub.TaskTitle}",
                                $"Instructor feedback: \"{feedback}\". Please submit updated coursework.",
                                "Revision Requested",
                                "warning",
                                $"{sub.CourseCode} • {sub.CourseName}",
                                Url.Action("SubmitDocument", "Student", new { id = sub.TaskId }),
                                "Upload Revision"
                            );
                        }
                    }
                }
                catch { }
            }
            return Json(new { success = result.success, message = result.message });
        }

        // GET: /Faculty/Revisions
        public ActionResult Revisions()
        {
            SetFacultyContext("Revision Requests");
            int userId = GetCurrentFacultyId();
            if (userId == 0) return new EmptyResult();
            var all = _facultyRepo.GetAllSubmissions(userId);
            var revisions = all.Where(s =>
                (s.Status ?? "").IndexOf("Revision", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (s.Status ?? "").IndexOf("Needs Revision", StringComparison.OrdinalIgnoreCase) >= 0 ||
                (s.Status ?? "").IndexOf("Resubmitted", StringComparison.OrdinalIgnoreCase) >= 0
            ).ToList();
            return View(revisions);
        }

        // GET: /Faculty/Documents
        public ActionResult Documents()
        {
            SetFacultyContext("Documents");
            int userId = GetCurrentFacultyId();
            if (userId == 0) return new EmptyResult();
            var allSubmissions = _facultyRepo.GetAllSubmissions(userId);
            return View(allSubmissions);
        }

        // GET: /Faculty/Announcements
        public ActionResult Announcements()
        {
            SetFacultyContext("Announcements");
            int userId = GetCurrentFacultyId();
            if (userId == 0) return new EmptyResult();

            List<CampusVault.Models.Faculty.AnnouncementItem> announcements;
            try
            {
                announcements = _facultyRepo.GetFacultyAnnouncements(userId);
            }
            catch
            {
                announcements = new List<CampusVault.Models.Faculty.AnnouncementItem>();
                TempData["ErrorMessage"] = "Announcements table not yet created. Please run Database/Scripts/13_Create_Announcements.sql in SSMS.";
            }

            var classrooms = _facultyRepo.GetFacultyClassrooms(userId);
            ViewBag.FacultyClassrooms = classrooms;
            return View(announcements);
        }

        // POST: /Faculty/PublishAnnouncement
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PublishAnnouncement(
            int classroomId, string title, string message,
            string priority, bool isPinned = false, bool requireAck = false)
        {
            int userId = GetCurrentFacultyId();
            if (userId == 0) return new EmptyResult();

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "Title and message are required.";
                return RedirectToAction("Announcements");
            }

            try
            {
                var result = _facultyRepo.PublishAnnouncement(
                    classroomId, userId, title.Trim(), message.Trim(),
                    priority ?? "Normal", isPinned, requireAck);

                if (result.success)
                {
                    // Dynamically notify enrolled students of this classroom
                    try
                    {
                        _notifRepo.NotifyClassroomStudents(
                            classroomId,
                            "announcements",
                            $"Announcement: {title.Trim()}",
                            message.Trim(),
                            priority ?? "Normal",
                            "info",
                            null,
                            Url.Action("ClassroomDetails", "Student", new { id = classroomId }),
                            "View Details"
                        );
                    }
                    catch { }
                }

                TempData[result.success ? "SuccessMessage" : "ErrorMessage"] =
                    result.success ? "Announcement published successfully!" : result.message;
            }
            catch
            {
                TempData["ErrorMessage"] = "Could not publish. Please run Database/Scripts/13_Create_Announcements.sql in SSMS first.";
            }

            return RedirectToAction("Announcements");
        }

        // POST: /Faculty/DeleteAnnouncement (AJAX)
        [HttpPost]
        public JsonResult DeleteAnnouncement(int announcementId)
        {
            int userId = GetCurrentFacultyId();
            if (userId == 0) return Json(new { success = false, message = "Session expired." });
            try
            {
                var result = _facultyRepo.DeleteAnnouncement(announcementId, userId);
                return Json(new { success = result.success, message = result.message });
            }
            catch
            {
                return Json(new { success = false, message = "Database not ready. Run Script 13 in SSMS." });
            }
        }

        // GET: /Faculty/Notifications
        public ActionResult Notifications()
        {
            SetFacultyContext("Notifications");
            int userId = GetCurrentFacultyId();
            if (userId == 0) return new EmptyResult();
            var notifications = _notifRepo.GetNotifications(userId, 100);
            return View(notifications);
        }

        // POST: /Faculty/MarkAllNotificationsAsRead (AJAX)
        [HttpPost]
        public JsonResult MarkAllNotificationsAsRead()
        {
            int userId = GetCurrentFacultyId();
            if (userId == 0) return Json(new { success = false, message = "Session expired." });
            bool ok = _notifRepo.MarkAllAsRead(userId);
            return Json(new { success = ok });
        }

        // POST: /Faculty/MarkNotificationAsRead (AJAX)
        [HttpPost]
        public JsonResult MarkNotificationAsRead(int notificationId)
        {
            int userId = GetCurrentFacultyId();
            if (userId == 0) return Json(new { success = false, message = "Session expired." });
            bool ok = _notifRepo.MarkAsRead(notificationId, userId);
            return Json(new { success = ok });
        }

        // GET: /Faculty/Profile
        public new ActionResult Profile()
        {
            SetFacultyContext("My Profile");
            int userId = GetCurrentFacultyId();
            if (userId == 0) return new EmptyResult();
            var profile = _facultyRepo.GetFacultyProfile(userId);
            return View(profile);
        }

        // POST: /Faculty/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(FacultyProfileViewModel model)
        {
            int userId = GetCurrentFacultyId();
            if (userId == 0) return Json(new { success = false, message = "Session expired. Please log in again." });
            model.UserId = userId;

            if (string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName))
                return Json(new { success = false, message = "First name and last name are required." });

            var result = _facultyRepo.UpdateFacultyProfile(model);
            if (result.success)
            {
                Session["UserName"] = $"{model.FirstName.Trim()} {model.LastName.Trim()}";
                string f = model.FirstName.Trim().Substring(0, 1).ToUpper();
                string l = model.LastName.Trim().Substring(0, 1).ToUpper();
                Session["UserInitials"] = $"{f}{l}";
            }

            if (Request.IsAjaxRequest() || Request.AcceptTypes != null)
            {
                return Json(new { success = result.success, message = result.message });
            }

            if (result.success)
            {
                TempData["SuccessMessage"] = "Profile updated successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = result.message;
            }
            return RedirectToAction("Profile");
        }

        // POST: /Faculty/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            int userId = GetCurrentFacultyId();
            if (userId == 0) return Json(new { success = false, message = "Session expired. Please log in again." });

            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
                return Json(new { success = false, message = "Please enter both current and new password." });

            if (newPassword.Length < 6)
                return Json(new { success = false, message = "Password must be at least 6 characters long." });

            if (newPassword != confirmPassword)
                return Json(new { success = false, message = "New password and confirmation do not match." });

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


        // POST: /Faculty/UpdateTask (AJAX)
        [HttpPost]
        public JsonResult UpdateTask(int taskId, string title, string description, string taskType, int maxScore, string dueDate)
        {
            int userId = GetCurrentFacultyId();
            if (userId == 0) return Json(new { success = false, message = "Session expired." });
            if (string.IsNullOrWhiteSpace(title)) return Json(new { success = false, message = "Task title is required." });
            DateTime parsedDate;
            if (!DateTime.TryParse(dueDate, out parsedDate))
                return Json(new { success = false, message = "Invalid due date format." });
            var result = _facultyRepo.UpdateTask(taskId, userId, title, description, taskType, maxScore, parsedDate);
            return Json(new { success = result.success, message = result.message });
        }

        // POST: /Faculty/DeleteTask (AJAX)
        [HttpPost]
        public JsonResult DeleteTask(int taskId)
        {
            int userId = GetCurrentFacultyId();
            if (userId == 0) return Json(new { success = false, message = "Session expired." });
            var result = _facultyRepo.DeleteTask(taskId, userId);
            return Json(new { success = result.success, message = result.message });
        }

        // POST: /Faculty/DeleteClassroom (AJAX)
        [HttpPost]
        public JsonResult DeleteClassroom(int classroomId)
        {
            int userId = GetCurrentFacultyId();
            if (userId == 0) return Json(new { success = false, message = "Session expired." });
            var result = _facultyRepo.DeleteClassroom(classroomId, userId);
            return Json(new { success = result.success, message = result.message });
        }

        // POST: /Faculty/UploadProfilePicture (AJAX)
        [HttpPost]
        public JsonResult UploadProfilePicture(System.Web.HttpPostedFileBase profilePic)
        {
            int userId = GetCurrentFacultyId();
            if (userId == 0) return Json(new { success = false, message = "Session expired." });
            if (profilePic == null || profilePic.ContentLength == 0)
                return Json(new { success = false, message = "No file selected." });
            var cloudinary = new CampusVault.Services.CloudinaryService();
            var upload = cloudinary.UploadFile(profilePic);
            if (!upload.success) return Json(new { success = false, message = upload.error });
            var result = _authRepo.UpdateProfilePicture(userId, upload.url);
            return Json(new { success = result.success, message = result.message, url = upload.url });
        }

        // ============================================================
        // LEARNING MATERIALS (E-CONTENT MODULE)
        // ============================================================

        // GET: /Faculty/Materials
        public ActionResult Materials()
        {
            SetFacultyContext("Learning Materials");
            int userId = GetCurrentFacultyId();
            if (userId == 0) return new EmptyResult();

            var materials = _facultyRepo.GetFacultyMaterials(userId);
            var classrooms = _facultyRepo.GetFacultyClassrooms(userId);
            ViewBag.FacultyClassrooms = classrooms;

            return View(materials);
        }

        // POST: /Faculty/UploadMaterial
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadMaterial(int classroomId, string title, string description, string category, string externalUrl, System.Web.HttpPostedFileBase materialFile)
        {
            int userId = GetCurrentFacultyId();
            if (userId == 0) return new EmptyResult();

            // Validate classroom authorization
            if (!_facultyRepo.IsFacultyAuthorizedForClassroom(userId, classroomId))
            {
                TempData["ErrorMessage"] = "You are not authorized to upload materials to this classroom.";
                return RedirectToAction("Materials");
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["ErrorMessage"] = "Material title is required.";
                return RedirectToAction("Materials");
            }

            string fileName = null;
            string fileSize = null;
            string fileType = null;
            string cloudUrl = null;
            string cloudPublicId = null;

            bool hasFile = materialFile != null && materialFile.ContentLength > 0;
            bool hasLink = !string.IsNullOrWhiteSpace(externalUrl);

            if (!hasFile && !hasLink)
            {
                TempData["ErrorMessage"] = "Please attach a document file or provide an external educational link.";
                return RedirectToAction("Materials");
            }

            if (hasFile)
            {
                var cloudinary = new CampusVault.Services.CloudinaryService();
                var upload = cloudinary.UploadLearningMaterial(materialFile);
                if (!upload.success)
                {
                    TempData["ErrorMessage"] = upload.error ?? "Failed to upload file to cloud storage.";
                    return RedirectToAction("Materials");
                }

                cloudUrl = upload.url;
                cloudPublicId = upload.publicId;
                fileName = System.IO.Path.GetFileName(materialFile.FileName);
                fileSize = cloudinary.FormatFileSize(materialFile.ContentLength);

                string ext = System.IO.Path.GetExtension(fileName)?.TrimStart('.')?.ToUpperInvariant() ?? "FILE";
                fileType = ext;
            }
            else
            {
                fileType = "LINK";
                string trimmedLink = externalUrl.Trim();
                if (!trimmedLink.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !trimmedLink.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    trimmedLink = "https://" + trimmedLink;
                }
                externalUrl = trimmedLink;
            }

            var result = _facultyRepo.UploadMaterial(
                classroomId, userId, title?.Trim(), description?.Trim(),
                fileName, fileSize, fileType, category ?? "Other",
                cloudUrl, cloudPublicId, hasLink ? externalUrl.Trim() : null
            );

            if (result.success)
            {
                TempData["SuccessMessage"] = "Learning material published successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = result.message ?? "Could not save material.";
            }

            return RedirectToAction("Materials");
        }

        // POST: /Faculty/UpdateMaterial (AJAX / Form)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateMaterial(int materialId, string title, string description, string category, string externalUrl, System.Web.HttpPostedFileBase replaceFile)
        {
            int userId = GetCurrentFacultyId();
            if (userId == 0) return Json(new { success = false, message = "Session expired." });

            var existing = _facultyRepo.GetMaterialById(materialId);
            if (existing == null || existing.FacultyUserId != userId)
            {
                return Json(new { success = false, message = "Material not found or access denied." });
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                return Json(new { success = false, message = "Title is required." });
            }

            string fileName = null;
            string fileSize = null;
            string fileType = null;
            string cloudUrl = null;
            string cloudPublicId = null;

            if (replaceFile != null && replaceFile.ContentLength > 0)
            {
                var cloudinary = new CampusVault.Services.CloudinaryService();
                var upload = cloudinary.UploadLearningMaterial(replaceFile);
                if (!upload.success)
                {
                    return Json(new { success = false, message = upload.error ?? "File upload failed." });
                }

                // Delete old file from Cloudinary to prevent orphans
                if (!string.IsNullOrEmpty(existing.CloudPublicId))
                {
                    try { cloudinary.DeleteFile(existing.CloudPublicId); } catch { }
                }

                cloudUrl = upload.url;
                cloudPublicId = upload.publicId;
                fileName = System.IO.Path.GetFileName(replaceFile.FileName);
                fileSize = cloudinary.FormatFileSize(replaceFile.ContentLength);
                fileType = System.IO.Path.GetExtension(fileName)?.TrimStart('.')?.ToUpperInvariant() ?? "FILE";
            }

            string formattedExtUrl = null;
            if (!string.IsNullOrWhiteSpace(externalUrl))
            {
                formattedExtUrl = externalUrl.Trim();
                if (!formattedExtUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !formattedExtUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    formattedExtUrl = "https://" + formattedExtUrl;
                }
            }

            var result = _facultyRepo.UpdateMaterial(
                materialId, userId, title?.Trim(), description?.Trim(), category ?? "Other",
                formattedExtUrl, fileName, fileSize, fileType, cloudUrl, cloudPublicId
            );

            return Json(new { success = result.success, message = result.message });
        }

        // POST: /Faculty/DeleteMaterial (AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult DeleteMaterial(int materialId)
        {
            int userId = GetCurrentFacultyId();
            if (userId == 0) return Json(new { success = false, message = "Session expired." });

            var result = _facultyRepo.DeleteMaterial(materialId, userId);
            if (result.success && !string.IsNullOrEmpty(result.cloudPublicId))
            {
                try
                {
                    var cloudinary = new CampusVault.Services.CloudinaryService();
                    cloudinary.DeleteFile(result.cloudPublicId);
                }
                catch { }
            }

            return Json(new { success = result.success, message = result.message });
        }

        // GET: /Faculty/DownloadMaterial/{id}
        public ActionResult DownloadMaterial(int id)
        {
            int userId = GetCurrentFacultyId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var material = _facultyRepo.GetMaterialById(id);
            if (material == null)
            {
                TempData["ErrorMessage"] = "The requested learning material could not be found.";
                return RedirectToAction("Materials");
            }

            // Verify faculty authorization: owner or owner of the classroom
            if (material.FacultyUserId != userId && !_facultyRepo.IsFacultyAuthorizedForClassroom(userId, material.ClassroomId))
            {
                TempData["ErrorMessage"] = "Access denied: you do not have permission to access this material.";
                return RedirectToAction("Materials");
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
            return RedirectToAction("Materials");
        }

        // GET: /Faculty/Analytics
        public ActionResult Analytics(int? classroomId)
        {
            SetFacultyContext("Student Analytics");
            int userId = GetCurrentFacultyId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var vm = _facultyRepo.GetFacultyAnalytics(userId, classroomId);
            return View(vm);
        }

        // GET: /Faculty/ExportAnalyticsCsv
        public ActionResult ExportAnalyticsCsv(int? classroomId)
        {
            int userId = GetCurrentFacultyId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            string csv = _facultyRepo.ExportClassroomSubmissionsCsv(userId, classroomId);
            string fileName = classroomId.HasValue 
                ? $"Classroom_{classroomId.Value}_Submissions_Report.csv" 
                : "Faculty_All_Submissions_Report.csv";
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
        }
    }
}
