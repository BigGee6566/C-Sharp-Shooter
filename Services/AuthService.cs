using System;
using System.Data;
using System.Web;
using System.Web.SessionState;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;

namespace ForteLogisticsApp.Services
{
    /// <summary>How a sign-in attempt matched the account.</summary>
    public enum PasswordMatch
    {
        Rejected,
        Permanent,
        Temporary,
        TemporaryExpired
    }

    /// <summary>A forgot-password credential. The plaintext exists only here and on the page that shows it.</summary>
    public sealed class TemporaryPassword
    {
        public string Password;
        public DateTime ExpiresUtc;
    }

    public static class AuthService
    {
        public const int TemporaryPasswordMinutes = 20;
        private const int TemporaryPasswordLength = 12;

        /// <summary>
        /// Session key set by a temporary sign-in. Its value is that password's hash, kept so it
        /// cannot be chosen as the new password; while it is present nothing else is reachable.
        /// </summary>
        public const string PendingPasswordChangeKey = "PendingPasswordChange";

        /// <summary>
        /// Validates credentials and, on success, populates the session and returns the role.
        /// Returns null on failure.
        /// </summary>
        /// <summary>Failed attempts allowed from one session before it is made to wait.</summary>
        private const int MaxAttempts = 5;
        private const int LockoutMinutes = 5;

        /// <summary>
        /// Failed attempts allowed against one ACCOUNT, from any number of sessions, before it is
        /// made to wait - the per-session limit alone is bypassed by simply dropping the cookie.
        /// </summary>
        public const int MaxAccountFailures = 10;
        public static readonly TimeSpan AccountLockout = TimeSpan.FromMinutes(15);

        public static string Login(string email, string password, HttpSessionState session)
        {
            PasswordMatch ignored;
            return Login(email, password, session, out ignored);
        }

        public static string Login(string email, string password, HttpSessionState session, out PasswordMatch status)
        {
            status = PasswordMatch.Rejected;
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) return null;
            if (session != null && IsLockedOut(session)) return null;
            string accountKey = "login:" + InputValidator.NormalizeEmail(email);
            if (RequestThrottle.Count(accountKey) >= MaxAccountFailures) return null;

            // The database clock alone decides expiry, so the web server's clock cannot extend it.
            DataTable dt = Db.Query(
                @"SELECT UserID, FullName, Role, Password, TempPassword,
                         CASE WHEN TempPasswordExpires > GETUTCDATE() THEN 1 ELSE 0 END AS TempLive
                    FROM dbo.[User] WHERE Email = @e", "@e", email.Trim());

            if (dt.Rows.Count == 0)
            {
                RecordFailure(session);
                RequestThrottle.Allow(accountKey, MaxAccountFailures, AccountLockout);
                return null;
            }

            DataRow row = dt.Rows[0];
            int userId = Db.ToInt(row["UserID"]);
            string stored = Db.ToStr(row["Password"]);
            string temporary = Db.ToStr(row["TempPassword"]);

            status = ClassifyPassword(password, stored, temporary, Db.ToInt(row["TempLive"]) == 1);
            if (status == PasswordMatch.Temporary && !ConsumeTemporaryPassword(userId, temporary))
            {
                status = PasswordMatch.Rejected;
            }

            if (status == PasswordMatch.Rejected || status == PasswordMatch.TemporaryExpired)
            {
                RecordFailure(session);
                RequestThrottle.Allow(accountKey, MaxAccountFailures, AccountLockout);
                return null;
            }
            RequestThrottle.Reset(accountKey);

            // The owner remembered: a temporary password someone else requested must not linger.
            if (status == PasswordMatch.Permanent && temporary.Length > 0)
            {
                Db.NonQuery("UPDATE dbo.[User] SET TempPassword = NULL, TempPasswordExpires = NULL WHERE UserID = @u", "@u", userId);
            }

            // Silently migrate accounts still holding a legacy unsalted SHA-256 hash. We only
            // have the plaintext at this instant, so this is the one place it can be done.
            if (status == PasswordMatch.Permanent && PasswordHelper.NeedsUpgrade(stored))
            {
                stored = PasswordHelper.Hash(password);
                Db.NonQuery("UPDATE dbo.[User] SET Password = @p WHERE UserID = @u",
                    "@p", stored, "@u", userId);
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
                // Ties this session to the role and password it signed in with (SessionGuard).
                SessionGuard.Restamp(session, Db.ToStr(row["Role"]), stored);
                if (status == PasswordMatch.Temporary) session[PendingPasswordChangeKey] = temporary;
            }
            return Db.ToStr(row["Role"]);
        }

        private static PasswordMatch ClassifyPassword(string password, string permanentHash, string temporaryHash, bool temporaryLive)
        {
            if (PasswordHelper.Verify(password, permanentHash)) return PasswordMatch.Permanent;
            if (!PasswordHelper.Verify(password, temporaryHash)) return PasswordMatch.Rejected;
            return temporaryLive ? PasswordMatch.Temporary : PasswordMatch.TemporaryExpired;
        }

        /// <summary>
        /// Spends a temporary password. One conditional UPDATE, so two simultaneous sign-ins
        /// cannot both succeed: only the one that clears the row wins.
        /// </summary>
        private static bool ConsumeTemporaryPassword(int userId, string temporaryHash)
        {
            bool spent = false;
            Db.RunInTransaction((conn, tx) =>
            {
                spent = Db.NonQuery(conn, tx,
                    @"UPDATE dbo.[User] SET TempPassword = NULL, TempPasswordExpires = NULL
                       WHERE UserID = @u AND TempPassword = @t AND TempPasswordExpires > GETUTCDATE()",
                    "@u", userId, "@t", temporaryHash) == 1;
                if (spent) Audit(conn, tx, userId, "Password.Temporary.Used", null);
            });
            return spent;
        }

        /// <summary>
        /// Creates a temporary password for a customer, replacing any earlier one. Returns null
        /// when no customer has that email. Staff and admins are refused: the password is shown
        /// to whoever asked, and those accounts are reset in Admin -> Users instead.
        /// </summary>
        public static TemporaryPassword IssueTemporaryPassword(string email, string requestedFrom)
        {
            string plain = PasswordHelper.GenerateTemporary(TemporaryPasswordLength);
            string hash = PasswordHelper.Hash(plain);
            TemporaryPassword issued = null;

            Db.RunInTransaction((conn, tx) =>
            {
                DataTable row = Db.Query(conn, tx,
                    @"UPDATE dbo.[User]
                         SET TempPassword = @h, TempPasswordExpires = DATEADD(MINUTE, @m, GETUTCDATE())
                      OUTPUT inserted.UserID, inserted.TempPasswordExpires
                       WHERE Email = @e AND Role = 'Customer'",
                    "@h", hash, "@m", TemporaryPasswordMinutes, "@e", InputValidator.NormalizeEmail(email));
                if (row.Rows.Count == 0) return;

                Audit(conn, tx, Db.ToInt(row.Rows[0]["UserID"]), "Password.Temporary.Issued", "Requested from " + requestedFrom);
                issued = new TemporaryPassword
                {
                    Password = plain,
                    ExpiresUtc = DateTime.SpecifyKind((DateTime)row.Rows[0]["TempPasswordExpires"], DateTimeKind.Utc)
                };
            });
            return issued;
        }

        /// <summary>The first problem with a chosen password, in words for the user; null when it is acceptable.</summary>
        public static string PasswordProblem(string newPassword, string confirmation, string temporaryHash)
        {
            if (newPassword.Length < AccountService.MinPasswordLength || newPassword.Length > AccountService.MaxPasswordLength)
            {
                return "Your new password must be " + AccountService.MinPasswordLength + " to " +
                       AccountService.MaxPasswordLength + " characters.";
            }
            if (newPassword != confirmation) return "The new passwords do not match.";
            if (PasswordHelper.Verify(newPassword, temporaryHash)) return "Please choose a password of your own, not the temporary one.";
            return null;
        }

        /// <summary>Saves the password chosen after a temporary sign-in and returns its hash.</summary>
        public static string SetChosenPassword(int userId, string newPassword)
        {
            string hash = PasswordHelper.Hash(newPassword);
            Db.RunInTransaction((conn, tx) =>
            {
                Db.NonQuery(conn, tx,
                    "UPDATE dbo.[User] SET Password = @p, TempPassword = NULL, TempPasswordExpires = NULL WHERE UserID = @u",
                    "@p", hash, "@u", userId);
                Audit(conn, tx, userId, "Password.Changed.AfterReset", null);
            });
            return hash;
        }

        /// <summary>The one page a session with a pending password change may open.</summary>
        public static bool IsAllowedDuringForcedChange(string appRelativePath)
        {
            return string.Equals(appRelativePath, "~/ChangePassword.aspx", StringComparison.OrdinalIgnoreCase);
        }

        private static void Audit(System.Data.SqlClient.SqlConnection conn, System.Data.SqlClient.SqlTransaction tx, int userId, string action, string details)
        {
            Db.NonQuery(conn, tx,
                @"INSERT INTO dbo.AuditLog (UserID, Action, Entity, EntityID, Details)
                  VALUES (@u, @a, 'User', @e, @d)",
                "@u", userId, "@a", action, "@e", userId.ToString(), "@d", (object)details ?? DBNull.Value);
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
