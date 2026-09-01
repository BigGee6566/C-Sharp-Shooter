using System;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp
{
    /// <summary>
    /// Sign-in. All credential handling lives in AuthService: it verifies the PBKDF2 hash,
    /// silently upgrades any account still on a legacy hash, enforces the failed-attempt
    /// lockout, and populates the session keys the rest of the app reads (UserID, Role,
    /// FullName). This page only collects input and routes the result.
    /// </summary>
    public partial class LoginPage : BasePage
    {
        protected HiddenField hfUserRole;
        protected Panel pnlError;
        protected Label lblStatus;
        protected TextBox txtEmail;
        protected TextBox txtPassword;
        protected Button btnLogin;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            // Already signed in - go straight to the right dashboard.
            if (Session["UserID"] != null)
            {
                Response.Redirect(ResolveUrl(AuthService.DashboardUrl(Session["Role"] as string)));
                return;
            }

            if (Request.QueryString["registered"] == "1")
            {
                ShowStatus("success", "Account created successfully - please sign in.");
            }
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            string role = AuthService.Login(txtEmail.Text, txtPassword.Text, Session);

            if (role == null)
            {
                int minutes = AuthService.LockoutMinutesRemaining(Session);
                ShowStatus("error", minutes > 0
                    ? "Too many failed attempts. Please try again in " + minutes + " minute(s)."
                    : "Incorrect email or password. Please try again.");
                return;
            }

            // The portal toggle is a UI affordance; the account's real role decides access.
            // Tell the user plainly when the two disagree rather than silently ignoring it.
            bool wantsStaffPortal = string.Equals(hfUserRole.Value, "Staff", StringComparison.OrdinalIgnoreCase);
            bool isCustomer = string.Equals(role, "Customer", StringComparison.OrdinalIgnoreCase);

            if (wantsStaffPortal && isCustomer)
            {
                Session.Clear();
                ShowStatus("error", "That account is a customer account. Please use the Client Portal tab.");
                return;
            }
            if (!wantsStaffPortal && !isCustomer)
            {
                Session.Clear();
                ShowStatus("error", "That is a staff account. Please use the Staff Portal tab.");
                return;
            }

            string returnUrl = Request.QueryString["returnUrl"];
            if (SecurePage.IsSafeReturnUrl(returnUrl))
            {
                Response.Redirect(returnUrl);
            }
            else
            {
                Response.Redirect(ResolveUrl(AuthService.DashboardUrl(role)));
            }
        }

        private void ShowStatus(string kind, string message)
        {
            lblStatus.CssClass = UiHelper.AlertClass(kind);
            lblStatus.Text = message;
            pnlError.Visible = true;
        }
    }
}
