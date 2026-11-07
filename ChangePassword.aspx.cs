using System;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp
{
    /// <summary>
    /// Where a temporary sign-in lands, and the only page it can reach until a new password is
    /// saved (see Global.asax). There is no "current password" field: spending the temporary
    /// password is the proof.
    /// </summary>
    public partial class ChangePasswordPage : SecurePage
    {
        protected Panel pnlForm;
        protected Panel pnlError;
        protected Panel pnlDone;
        protected Label lblStatus;
        protected Label lblDone;
        protected TextBox txtNew;
        protected TextBox txtConfirm;
        protected Button btnSave;
        protected LinkButton btnSignOut;
        protected HyperLink lnkDashboard;

        private string TemporaryHash
        {
            get { return Session[AuthService.PendingPasswordChangeKey] as string; }
        }

        private string Dashboard
        {
            get { return AuthService.DashboardUrl(Role, UserId); }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole();
            if (TemporaryHash == null) Response.Redirect(ResolveUrl(Dashboard));
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            string problem = AuthService.PasswordProblem(txtNew.Text, txtConfirm.Text, TemporaryHash);
            if (problem != null)
            {
                lblStatus.CssClass = UiHelper.AlertClass("error");
                lblStatus.Text = problem;
                pnlError.Visible = true;
                return;
            }

            string hash = AuthService.SetChosenPassword(UserId, txtNew.Text);
            // Their own change: keep this session signed in (other sessions are ended).
            SessionGuard.Restamp(Session, Role, hash);
            Session.Remove(AuthService.PendingPasswordChangeKey);

            lblDone.CssClass = UiHelper.AlertClass("success");
            lblDone.Text = "Your password has been changed.";
            lnkDashboard.NavigateUrl = Dashboard;
            pnlForm.Visible = false;
            pnlDone.Visible = true;
        }

        protected void btnSignOut_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();
            Response.Redirect(ResolveUrl("~/Login.aspx"));
        }
    }
}
