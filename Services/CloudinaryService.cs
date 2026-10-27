using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using System;
using System.Configuration;
using System.IO;
using System.Web;

namespace CampusVault.Services
{
    /// <summary>
    /// Handles file uploads and deletes via Cloudinary free-tier cloud storage.
    /// Configure CloudinaryCloudName, CloudinaryApiKey, CloudinaryApiSecret in Web.config appSettings.
    /// </summary>
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService()
        {
            string cloudName  = ConfigurationManager.AppSettings["CloudinaryCloudName"];
            string apiKey     = ConfigurationManager.AppSettings["CloudinaryApiKey"];
            string apiSecret  = ConfigurationManager.AppSettings["CloudinaryApiSecret"];
            _cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
        }

        /// <summary>
        /// Uploads an academic document to Cloudinary.
        /// Returns (success, secureUrl, publicId, errorMessage).
        /// </summary>
        public (bool success, string url, string publicId, string error) UploadFile(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
                return (false, null, null, "No file provided.");

            string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            string[] allowed = { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".zip", ".rar" };
            if (!Array.Exists(allowed, e => e == ext))
                return (false, null, null, "Invalid file type. Allowed: PDF, DOC, DOCX, PPT, PPTX, ZIP, RAR.");

            if (file.ContentLength > 10 * 1024 * 1024)
                return (false, null, null, "File exceeds 10 MB limit.");

            try
            {
                byte[] fileBytes;
                using (var ms = new MemoryStream())
                {
                    file.InputStream.CopyTo(ms);
                    fileBytes = ms.ToArray();
                }

                var uploadParams = new RawUploadParams
                {
                    File      = new FileDescription(file.FileName, new MemoryStream(fileBytes)),
                    Folder    = "campusvault/submissions",
                    PublicId  = $"submission_{Guid.NewGuid():N}",
                    Overwrite = false
                };

                var result = _cloudinary.Upload(uploadParams);

                if (result.Error != null)
                    return (false, null, null, result.Error.Message);

                return (true, result.SecureUrl.ToString(), result.PublicId, null);
            }
            catch (Exception ex)
            {
                return (false, null, null, ex.Message);
            }
        }

        /// <summary>
        /// Uploads faculty learning materials (PDF, DOC/DOCX, PPT/PPTX, XLS/XLSX, TXT, images, archives) to Cloudinary.
        /// Up to 25 MB supported.
        /// </summary>
        public (bool success, string url, string publicId, string error) UploadLearningMaterial(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
                return (false, null, null, "No file provided.");

            string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            string[] allowed = { ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx", ".txt", ".png", ".jpg", ".jpeg", ".zip", ".rar" };
            if (!Array.Exists(allowed, e => e == ext))
                return (false, null, null, $"Invalid file type '{ext}'. Allowed: PDF, DOC, DOCX, PPT, PPTX, XLS, XLSX, TXT, PNG, JPG, ZIP, RAR.");

            if (file.ContentLength > 25 * 1024 * 1024)
                return (false, null, null, "File exceeds 25 MB limit.");

            try
            {
                byte[] fileBytes;
                using (var ms = new MemoryStream())
                {
                    file.InputStream.CopyTo(ms);
                    fileBytes = ms.ToArray();
                }

                var uploadParams = new RawUploadParams
                {
                    File      = new FileDescription(file.FileName, new MemoryStream(fileBytes)),
                    Folder    = "campusvault/materials",
                    PublicId  = $"material_{Guid.NewGuid():N}",
                    Overwrite = false
                };

                var result = _cloudinary.Upload(uploadParams);

                if (result.Error != null)
                    return (false, null, null, result.Error.Message);

                return (true, result.SecureUrl.ToString(), result.PublicId, null);
            }
            catch (Exception ex)
            {
                return (false, null, null, ex.Message);
            }
        }

        /// <summary>
        /// Deletes a file from Cloudinary by its public ID.
        /// </summary>
        public bool DeleteFile(string publicId)
        {
            if (string.IsNullOrEmpty(publicId)) return false;
            try
            {
                var result = _cloudinary.Destroy(new DeletionParams(publicId));
                return result.Result == "ok";
            }
            catch { return false; }
        }

        /// <summary>Returns a human-readable file size string, e.g. "2.4 MB".</summary>
        public string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1) { order++; size /= 1024; }
            return $"{size:0.##} {sizes[order]}";
        }
    }
}
