using System;
using System.Web.UI;

namespace ForteLogisticsApp.Security
{
    /// <summary>
    /// Base for every page in the application, authenticated or not.
    ///
    /// Its job is to bind the encrypted ViewState to the current session
    /// (<see cref="Page.ViewStateUserKey"/>). Another site can cause a visitor's browser to
    /// POST to one of our forms, but it cannot forge ViewState containing our session id,
    /// so a cross-site request fails validation before any event handler runs.
    ///
    /// This matters on anonymous forms too: login CSRF lets an attacker sign a victim into
    /// an account the attacker controls, and the contact form would otherwise accept
    /// off-site submissions.
    /// </summary>
    public class BasePage : Page
    {
        private const string TokenKey = "CsrfToken";

        protected override void OnInit(EventArgs e)
        {
            if (Session != null) ViewStateUserKey = GetOrCreateToken();
            base.OnInit(e);
        }

        /// <summary>
        /// Returns a stable per-session token to use as the ViewState user key.
        ///
        /// Session.SessionID is deliberately NOT used: ASP.NET generates an id on first
        /// access but only persists it (and issues the cookie) once something is actually
        /// written to the session. A read-only page therefore sees a different id on the
        /// GET than on the POST, the ViewState user keys disagree, and every form submit
        /// fails. Storing a token forces the session to materialise and keeps the key fixed.
        /// </summary>
        private string GetOrCreateToken()
        {
            string token = Session[TokenKey] as string;
            if (string.IsNullOrEmpty(token))
            {
                token = Guid.NewGuid().ToString("N");
                Session[TokenKey] = token;
            }
            return token;
        }
    }
}
