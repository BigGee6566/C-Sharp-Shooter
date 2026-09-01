using System;
using System.Diagnostics;
using System.Web;
using ForteLogisticsApp.Data;

namespace ForteLogisticsApp
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // Create and seed the LocalDB database on first run.
            DbInitializer.EnsureCreated();
        }

 void Session_Start(object sender, EventArgs e)
 {
     // Temporary test session for local development
     Session["IsLoggedIn"] = true;
     Session["UserType"] = "Business"; // Or "Individual"
     Session["UserEmail"] = "admin@logisticore.com";
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
