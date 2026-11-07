using System;
using System.Web;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp
{
    /// <summary>
    /// Self-service password recovery. There is no mail server, so the temporary password is
    /// shown to whoever asks: knowing a customer's email is enough to sign in as them. That was
    /// accepted for this project, which is why staff and admin accounts are refused and every
    /// request is written to the audit log.
    /// </summary>
    public partial class ForgotPasswordPage : BasePage
    {
        protected Panel pnlRequest;
        protected Panel pnlError;
        protected Panel pnlResult;
        protected Label lblStatus;
        protected TextBox txtEmail;
        protected Button btnRequest;
        protected Literal litTemporary;
        protected Literal litExpires;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && Session["UserID"] != null)
            {
                Response.Redirect(ResolveUrl(AuthService.DashboardUrl(
                    Session["Role"] as string, SecurePage.CurrentUserId())));
            }
        }

        protected void btnRequest_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            string email = InputValidator.NormalizeEmail(txtEmail.Text);
            if (!InputValidator.IsValidEmail(email))
            {
                ShowError("Please enter a valid email address.");
                return;
            }

            if (!RequestThrottle.Allow("forgot:" + Request.UserHostAddress, 5, TimeSpan.FromMinutes(10)) ||
                !RequestThrottle.Allow("forgot:" + email, 3, TimeSpan.FromMinutes(AuthService.TemporaryPasswordMinutes)))
            {
                ShowError("Too many requests. Please wait a few minutes and try again.");
                return;
            }

            TemporaryPassword issued = AuthService.IssueTemporaryPassword(email, Request.UserHostAddress);
            if (issued == null)
            {
                ShowError("We couldn't find a client account with that email.");
                return;
            }

            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            litTemporary.Text = issued.Password;
            litExpires.Text = issued.ExpiresUtc.ToLocalTime().ToString("HH:mm");
            pnlRequest.Visible = false;
            pnlResult.Visible = true;
        }

        private void ShowError(string message)
        {
            lblStatus.CssClass = UiHelper.AlertClass("error");
            lblStatus.Text = message;
            pnlError.Visible = true;
        }
    }
}
