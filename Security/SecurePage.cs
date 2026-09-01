using System;
using System.Linq;
using System.Web;
using System.Web.UI;

namespace ForteLogisticsApp.Security
{
    /// <summary>
    /// Base page for role-restricted pages. Call RequireRole() in Page_Load.
    /// Session keys: UserID (int), Role (string), FullName (string).
    /// </summary>
    public class SecurePage : BasePage
    {
        public static int CurrentUserId(HttpSessionStateBase _ = null)
        {
            var s = HttpContext.Current.Session;
            return (s != null && s["UserID"] != null) ? (int)s["UserID"] : 0;
        }

        public int UserId
        {
            get { return (Session["UserID"] != null) ? (int)Session["UserID"] : 0; }
        }

        public string Role
        {
            get { return (Session["Role"] as string) ?? string.Empty; }
        }

        public string FullName
        {
            get { return (Session["FullName"] as string) ?? string.Empty; }
        }

        public bool IsLoggedIn { get { return UserId > 0; } }

        /// <summary>
        /// True if this user is in the driver registry.
        ///
        /// Role is "Staff" for drivers, exactly as it is for dispatch or warehouse people.
        /// Membership of dbo.[Driver] is what actually makes someone a driver, and it is what
        /// every assignment query joins on - so this, not the role, is the question to ask
        /// when deciding whether to show driver-only navigation.
        ///
        /// Cached per session because the master page asks on every render.
        /// </summary>
        public static bool IsDriver(int userId)
        {
            if (userId <= 0) return false;

            var session = HttpContext.Current != null ? HttpContext.Current.Session : null;
            if (session != null && session["IsDriver"] is bool)
            {
                return (bool)session["IsDriver"];
            }

            bool isDriver = Data.Db.ToInt(Data.Db.Scalar(
                "SELECT COUNT(*) FROM dbo.[Driver] WHERE UserID = @u", "@u", userId)) > 0;

            if (session != null) session["IsDriver"] = isDriver;
            return isDriver;
        }


        /// <summary>
        /// Validates a returnUrl before redirecting to it.
        ///
        /// Checking only for a leading "/" is not enough: "//evil.com" and "/\evil.com" are
        /// protocol-relative URLs that browsers resolve to an external host, which turns the
        /// login page into an open redirect usable for phishing. Anything that is not a
        /// single-slash, same-site path is discarded.
        /// </summary>
        public static bool IsSafeReturnUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            if (url[0] != '/') return false;
            if (url.Length > 1 && (url[1] == '/' || url[1] == '\\')) return false;
            if (url.IndexOf(':') >= 0) return false;                    // scheme, e.g. javascript:
            if (url.IndexOf('\\') >= 0) return false;                   // backslash tricks
            return Uri.IsWellFormedUriString(url, UriKind.Relative);
        }

        /// <summary>Redirects to Login if not authenticated or not in one of the allowed roles.</summary>
        protected void RequireRole(params string[] allowedRoles)
        {
            if (!IsLoggedIn)
            {
                Response.Redirect("~/Login.aspx?returnUrl=" + Server.UrlEncode(Request.RawUrl));
                return;
            }
            if (allowedRoles != null && allowedRoles.Length > 0 &&
                !allowedRoles.Contains(Role, StringComparer.OrdinalIgnoreCase))
            {
                Response.Redirect("~/AccessDenied.aspx");
            }
        }
    }
}
