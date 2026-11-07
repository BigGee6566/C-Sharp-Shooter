using System;
using System.Diagnostics;
using System.Web;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // Create and seed the LocalDB database on first run.
            DbInitializer.EnsureCreated();
        }

        /// <summary>
        /// Holds a session that signed in with a temporary password on the change page. It sits
        /// here, not in SecurePage, because not every page goes through RequireRole.
        /// </summary>
        protected void Application_PostAcquireRequestState(object sender, EventArgs e)
        {
            if (Context.Session == null || Context.Session[AuthService.PendingPasswordChangeKey] == null) return;
            if (AuthService.IsAllowedDuringForcedChange(Request.AppRelativeCurrentExecutionFilePath)) return;

            Response.Redirect(VirtualPathUtility.ToAbsolute("~/ChangePassword.aspx"), false);
            CompleteRequest();
        }

        /// <summary>
        /// Captures the real exception server-side and hands the user only a reference code.
        /// SQL exception text names tables and columns, so it must never reach the browser.
        /// </summary>
        protected void Application_Error(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError();
            if (ex == null) return;

            string reference = DateTime.UtcNow.ToString("yyyyMMddHHmmss") +
                               "-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
            try
            {
                if (Session != null) Session["LastErrorRef"] = reference;
            }
            catch
            {
                // Session is not always available this early in the pipeline.
            }

            try
            {
                EventLog.WriteEntry("Application",
                    "Logisticore error " + reference + Environment.NewLine +
                    "Path: " + (Request != null ? Request.RawUrl : "(none)") + Environment.NewLine +
                    ex, EventLogEntryType.Error);
            }
            catch
            {
                // Never let logging failure replace the original error.
            }
        }
    }
}
