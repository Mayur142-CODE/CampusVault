using System;
using System.Web.Mvc;
using System.Web.Routing;
using CampusVault.Services;

namespace CampusVault.Attributes
{
    /// <summary>
    /// Enterprise Authorization filter:
    /// 1. Checks active Session.
    /// 2. Validates signed JWT Token from cookie if session is expired.
    /// 3. If unauthenticated, immediately redirects to /Account/Login.
    /// 4. If role does not match, prevents unauthorized cross-role access.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class CampusVaultAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string[] _allowedRoles;
        private readonly JwtService _jwtService;

        public CampusVaultAuthorizeAttribute(params string[] roles)
        {
            _allowedRoles = roles;
            _jwtService = new JwtService();
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var httpContext = filterContext.HttpContext;
            var session = httpContext.Session;

            int? userId = session?["UserId"] as int?;
            string role = session?["Role"] as string;

            // 1. Fallback to JWT Token verification if session was lost
            if (!userId.HasValue || string.IsNullOrEmpty(role))
            {
                var cookie = httpContext.Request.Cookies["CampusVault_Token"];
                if (cookie != null && !string.IsNullOrWhiteSpace(cookie.Value))
                {
                    var claims = _jwtService.ValidateToken(cookie.Value);
                    if (claims != null)
                    {
                        userId = claims.UserId;
                        role = claims.Role;

                        if (session != null)
                        {
                            session["UserId"] = claims.UserId;
                            session["UserEmail"] = claims.Email;
                            session["Role"] = claims.Role;
                            session["UserName"] = claims.FullName;
                        }
                    }
                }
            }

            // 2. If completely unauthenticated, force redirect to Login
            if (!userId.HasValue || string.IsNullOrEmpty(role))
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary
                    {
                        { "controller", "Account" },
                        { "action", "Login" },
                        { "returnUrl", httpContext.Request.RawUrl }
                    });
                return;
            }

            // 3. Role-based access control (RBAC) check
            if (_allowedRoles != null && _allowedRoles.Length > 0)
            {
                bool hasAccess = false;
                foreach (var allowed in _allowedRoles)
                {
                    if (string.Equals(allowed, role, StringComparison.OrdinalIgnoreCase))
                    {
                        hasAccess = true;
                        break;
                    }
                }

                if (!hasAccess)
                {
                    // If a student tries to access /Admin or /Faculty, redirect them to their own dashboard
                    string redirectController = role;
                    filterContext.Result = new RedirectToRouteResult(
                        new RouteValueDictionary
                        {
                            { "controller", redirectController },
                            { "action", "Dashboard" }
                        });
                    return;
                }
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
