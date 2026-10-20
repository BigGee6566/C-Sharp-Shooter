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
        /// Cached for the duration of the request only.
        ///
        /// It used to be cached in Session, which meant an admin ticking or clearing the
        /// "Is a driver" box in Admin -> Users had no effect until that user signed out and
        /// back in - and there is no way for one request to reach another user's session to
        /// invalidate it. Per-request caching still collapses the repeated calls a single
        /// render makes, while picking up the change on the very next page load.
        /// </summary>
        public static bool IsDriver(int userId)
        {
            if (userId <= 0) return false;

            var context = HttpContext.Current;
            string cacheKey = "IsDriver:" + userId;

            if (context != null && context.Items[cacheKey] is bool)
            {
                return (bool)context.Items[cacheKey];
            }

            bool isDriver = Data.Db.ToInt(Data.Db.Scalar(
                "SELECT COUNT(*) FROM dbo.[Driver] WHERE UserID = @u", "@u", userId)) > 0;

            if (context != null) context.Items[cacheKey] = isDriver;
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

        /// <summary>
        /// Mints a fresh one-time submit token for <paramref name="scope"/>, stores it in the
        /// session, and returns it for the page to echo into a hidden field. Call this when
        /// the final confirm step is first shown. See <see cref="SubmitGuard"/> for why this
        /// is used instead of blanket Post-Redirect-Get.
        /// </summary>
        protected string IssueSubmitToken(string scope)
        {
            string token = Guid.NewGuid().ToString("N");
            Session["SubmitToken:" + scope] = token;
            return token;
        }

        /// <summary>
        /// Validates the <paramref name="presented"/> token (from the page's hidden field)
        /// against the one issued for <paramref name="scope"/> and consumes it on success, so
        /// a double-click or a refresh-and-resubmit of the same confirm POST is rejected
        /// rather than performing the action twice. Returns false when the token is missing,
        /// stale, or already spent.
        /// </summary>
        protected bool ConsumeSubmitToken(string scope, string presented)
        {
            string key = "SubmitToken:" + scope;
            bool ok;
            string remaining = SubmitGuard.Check(Session[key] as string, presented, out ok);
            Session[key] = remaining;
            return ok;
        }

        /// <summary>Redirects to Login if not authenticated or not in one of the allowed roles.</summary>
        protected void RequireRole(params string[] allowedRoles)
        {
            // A session whose role or password changed since sign-in (or whose account is gone)
            // is ended here, so a demotion or password reset takes effect on the next click.
            if (!IsLoggedIn || !SessionGuard.IsCurrent(Context))
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

        /// <summary>
        /// Gates route/vehicle/parcel-allocation pages (Route Planning, Warehouse, Parcel
        /// Registration, Fleet): Admin or Staff, but never a driver.
        ///
        /// "Staff" covers both office dispatch/warehouse people and drivers - membership of
        /// dbo.[Driver], not the Role column, is what actually makes someone a driver (see
        /// IsDriver above). Without this, a driver could open Route Planning and assign
        /// parcels/routes/vehicles to themselves or anyone else, which the business rules
        /// explicitly forbid: drivers may perform their own assigned deliveries and nothing
        /// route/vehicle/allocation-related.
        /// </summary>
        protected void RequireOperationsStaff()
        {
            RequireRole("Admin", "Staff");
            if (string.Equals(Role, "Staff", StringComparison.OrdinalIgnoreCase) && IsDriver(UserId))
            {
                Response.Redirect("~/AccessDenied.aspx");
            }
        }
    }
}
