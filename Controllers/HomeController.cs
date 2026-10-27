using System;
using System.Web.Mvc;
using CampusVault.Services;

namespace CampusVault.Controllers
{
    public class HomeController : Controller
    {
        private readonly JwtService _jwtService;

        public HomeController()
        {
            _jwtService = new JwtService();
        }

        // GET: / or /Home or /Home/Index
        public ActionResult Index()
        {
            string currentRole = Session["Role"] as string;

            // Fallback to JWT cookie verification if session is empty
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

            // If authenticated, redirect to appropriate role-specific dashboard
            if (!string.IsNullOrEmpty(currentRole))
            {
                switch (currentRole.ToLowerInvariant())
                {
                    case "admin":
                        return RedirectToAction("Dashboard", "Admin");
                    case "faculty":
                        return RedirectToAction("Dashboard", "Faculty");
                    case "student":
                    default:
                        return RedirectToAction("Dashboard", "Student");
                }
            }

            // Unauthenticated visitors go directly to Login
            return RedirectToAction("Login", "Account");
        }

        // GET: /Home/About
        public ActionResult About()
        {
            return RedirectToAction("Index");
        }
    }
}
