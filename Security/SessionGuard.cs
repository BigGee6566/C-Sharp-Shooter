using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using ForteLogisticsApp.Data;

namespace ForteLogisticsApp.Security
{
    /// <summary>
    /// Keeps a signed-in session honest with the database.
    ///
    /// The role is put in the session at sign-in. Without this, an admin demoted by another
    /// admin - or a user whose password an admin reset because the account was compromised -
    /// keeps every power they had until they sign out themselves. At sign-in the session gets
    /// a stamp: a fingerprint of the user's role and password hash. Every protected request
    /// recomputes it from the database; if the role or password has changed since, or the
    /// account is gone, the session ends and the user signs in again.
    /// </summary>
    public static class SessionGuard
    {
        public const string StampKey = "AuthStamp";
        private const string CheckedKey = "SessionGuard:Checked";

        /// <summary>The fingerprint of a role and password hash (pure). Never contains the hash itself.</summary>
        public static string Stamp(string role, string passwordHash)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes((role ?? string.Empty) + "|" + (passwordHash ?? string.Empty)));
                var sb = new StringBuilder(16);
                for (int i = 0; i < 8; i++) sb.Append(bytes[i].ToString("x2"));
                return sb.ToString();
            }
        }

        /// <summary>True when the session's stamp still matches the user's current role and password.</summary>
        public static bool StampMatches(string sessionStamp, string role, string passwordHash)
        {
            return !string.IsNullOrEmpty(sessionStamp) && sessionStamp == Stamp(role, passwordHash);
        }

        /// <summary>
        /// Checks the current request's session against the database once per request. Returns
        /// false (and clears the session) when it is no longer valid; also refreshes the role
        /// held in the session.
        /// </summary>
        public static bool IsCurrent(HttpContext context)
        {
            if (context == null || context.Session == null) return false;
            if (context.Items[CheckedKey] is bool) return (bool)context.Items[CheckedKey];

            bool ok = false;
            object uid = context.Session["UserID"];
            if (uid is int && (int)uid > 0)
            {
                DataTable dt = Db.Query("SELECT Role, Password FROM dbo.[User] WHERE UserID = @u", "@u", (int)uid);
                if (dt.Rows.Count == 1)
                {
                    string role = Db.ToStr(dt.Rows[0]["Role"]);
                    ok = StampMatches(context.Session[StampKey] as string, role, Db.ToStr(dt.Rows[0]["Password"]));
                    if (ok) context.Session["Role"] = role;
                }
            }

            if (!ok && uid != null)
            {
                // Keep the anti-forgery token so the sign-in form that follows still validates.
                object csrf = context.Session["CsrfToken"];
                context.Session.Clear();
                if (csrf != null) context.Session["CsrfToken"] = csrf;
            }
            context.Items[CheckedKey] = ok;
            return ok;
        }

        /// <summary>Stamps the session at sign-in, or again after the user legitimately changed their own password.</summary>
        public static void Restamp(System.Web.SessionState.HttpSessionState session, string role, string passwordHash)
        {
            if (session != null) session[StampKey] = Stamp(role, passwordHash);
        }
    }
}
