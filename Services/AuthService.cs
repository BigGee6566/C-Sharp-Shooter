using System;
using System.Data;
using System.Web;
using System.Web.SessionState;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;

namespace ForteLogisticsApp.Services
{
    public static class AuthService
    {
        /// <summary>
        /// Validates credentials and, on success, populates the session and returns the role.
        /// Returns null on failure.
        /// </summary>
        /// <summary>Failed attempts allowed from one session before it is made to wait.</summary>
        private const int MaxAttempts = 5;
        private const int LockoutMinutes = 5;

        public static string Login(string email, string password, HttpSessionState session)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) return null;
            if (session != null && IsLockedOut(session)) return null;

            DataTable dt = Db.Query(
                "SELECT UserID, FullName, Role, Password FROM dbo.[User] WHERE Email = @e", "@e", email.Trim());

            if (dt.Rows.Count == 0)
            {
                RecordFailure(session);
                return null;
            }

            DataRow row = dt.Rows[0];
            string stored = Db.ToStr(row["Password"]);
            if (!PasswordHelper.Verify(password, stored))
            {
                RecordFailure(session);
                return null;
            }

            int userId = Db.ToInt(row["UserID"]);

            // Silently migrate accounts still holding a legacy unsalted SHA-256 hash. We only
            // have the plaintext at this instant, so this is the one place it can be done.
            if (PasswordHelper.NeedsUpgrade(stored))
            {
                Db.NonQuery("UPDATE dbo.[User] SET Password = @p WHERE UserID = @u",
                    "@p", PasswordHelper.Hash(password), "@u", userId);
            }

            if (session != null)
            {
                // Drop everything the pre-login session held, then issue a fresh CSRF token so
                // ViewState minted before sign-in cannot be replayed afterwards. Note this does
                // not change the ASP.NET session id itself, so it limits - but does not fully
                // eliminate - session fixation; doing that needs SessionIDManager.
                session.Clear();
                session["CsrfToken"] = Guid.NewGuid().ToString("N");
                ClearFailures(session);

                session["UserID"] = userId;
                session["FullName"] = Db.ToStr(row["FullName"]);
                session["Role"] = Db.ToStr(row["Role"]);
            }
            return Db.ToStr(row["Role"]);
        }

        /// <summary>True while this session is serving a lockout after repeated failures.</summary>
        public static bool IsLockedOut(HttpSessionState session)
        {
            if (session == null) return false;
            object until = session["LoginLockedUntil"];
            if (until == null) return false;

            if ((DateTime)until > DateTime.UtcNow) return true;

            session["LoginLockedUntil"] = null;
            session["LoginFailures"] = 0;
            return false;
        }

        public static int LockoutMinutesRemaining(HttpSessionState session)
        {
            if (session == null || session["LoginLockedUntil"] == null) return 0;
            var remaining = (DateTime)session["LoginLockedUntil"] - DateTime.UtcNow;
            return remaining.TotalMinutes <= 0 ? 0 : (int)Math.Ceiling(remaining.TotalMinutes);
        }

        private static void RecordFailure(HttpSessionState session)
        {
            if (session == null) return;

            int failures = session["LoginFailures"] == null ? 0 : (int)session["LoginFailures"];
            failures++;
            session["LoginFailures"] = failures;

            if (failures >= MaxAttempts)
            {
                session["LoginLockedUntil"] = DateTime.UtcNow.AddMinutes(LockoutMinutes);
            }
        }

        private static void ClearFailures(HttpSessionState session)
        {
            if (session == null) return;
            session["LoginFailures"] = 0;
            session["LoginLockedUntil"] = null;
        }

        /// <summary>
        /// The landing page for a role after sign-in.
        ///
        /// For Staff, <paramref name="userId"/> decides between the two kinds of staff: a
        /// driver (a Staff user with a row in dbo.[Driver]) lands on their own delivery
        /// manifest, while office operations staff land on the Ops dashboard. Landing a
        /// driver on Ops is wrong because every quick-action on that page (Warehouse, Route
        /// Planning, Parcel Registration) is gated by RequireOperationsStaff and bounces a
        /// driver straight to AccessDenied - the "dead links" drivers reported. When no
        /// userId is supplied (0) the role alone is used, so Staff falls back to Ops.
        /// </summary>
        public static string DashboardUrl(string role, int userId = 0)
        {
            switch (role)
            {
                case "Customer": return "~/Customer/Dashboard.aspx";
                case "Admin":    return "~/Admin/Dashboard.aspx";
                case "Staff":
                    return (userId > 0 && SecurePage.IsDriver(userId))
                        ? "~/Driver/Dashboard.aspx"
                        : "~/Ops/Dashboard.aspx";
                default:         return "~/Default.aspx";
            }
        }
    }
}
