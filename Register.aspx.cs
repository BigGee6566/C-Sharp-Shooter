using System;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp
{
    /// <summary>
    /// Public self-registration.
    ///
    /// This page only ever creates a Customer. Staff and Admin accounts are provisioned in
    /// Admin -> Users, which also creates the matching dbo.[Admin] / dbo.[Driver] records.
    /// Letting a visitor pick their own role here would be straightforward privilege
    /// escalation, so the account type chosen on the form is stored as Client.ClientType
    /// (Individual or Business) and the role is hardcoded.
    /// </summary>
    public partial class RegisterPage : BasePage
    {
        protected HiddenField hfRole;
        protected Panel pnlStaffNotice;
        protected Panel pnlForm;
        protected TextBox txtFullName;
        protected TextBox txtEmail;
        protected TextBox txtPhone;
        protected TextBox txtAddress;
        protected TextBox txtPassword;
        protected TextBox txtConfirm;
        protected CheckBox chkTerms;
        protected Label lblStatus;
        protected Button btnRegister;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && Session["UserID"] != null)
            {
                Response.Redirect(ResolveUrl(AuthService.DashboardUrl(Session["Role"] as string)));
            }
        }

        protected void btnRegister_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            string fullName = txtFullName.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text;

            // The client-side tabs hide the form for Staff, but never trust that alone.
            if (string.Equals(hfRole.Value, "Staff", StringComparison.OrdinalIgnoreCase))
            {
                SetStatus("error", "Staff accounts are created by an administrator, not through self-registration.");
                return;
            }

            if (fullName.Length == 0 || email.Length == 0 || password.Length == 0)
            {
                SetStatus("error", "Please complete all required fields (marked *).");
                return;
            }
            if (password.Length < 6)
            {
                SetStatus("error", "Password must be at least 6 characters long.");
                return;
            }
            if (password != txtConfirm.Text)
            {
                SetStatus("error", "The passwords you entered do not match.");
                return;
            }
            if (!chkTerms.Checked)
            {
                SetStatus("error", "Please accept the Terms of Service to continue.");
                return;
            }

            if (Db.ToInt(Db.Scalar("SELECT COUNT(*) FROM dbo.[User] WHERE Email = @e", "@e", email)) > 0)
            {
                SetStatus("error", "An account with that email already exists. Please sign in instead.");
                return;
            }

            int userId = Db.Insert(
                @"INSERT INTO dbo.[User] (FullName, Email, Password, Role, PhoneNumber, Address)
                  VALUES (@n, @e, @p, 'Customer', @ph, @a)",
                "@n", fullName,
                "@e", email,
                "@p", PasswordHelper.Hash(password),
                "@ph", txtPhone.Text.Trim(),
                "@a", txtAddress.Text.Trim());

            string clientType = string.Equals(hfRole.Value, "Business", StringComparison.OrdinalIgnoreCase)
                ? "Business"
                : "Individual";

            Db.NonQuery("INSERT INTO dbo.[Client] (UserID, ClientType) VALUES (@u, @t)",
                "@u", userId, "@t", clientType);

            NotificationService.Send(userId, null,
                "Welcome to Logisticore Courier Service! Your account is ready to use.", "System");

            Response.Redirect(ResolveUrl("~/Login.aspx?registered=1"));
        }

        private void SetStatus(string kind, string message)
        {
            lblStatus.CssClass = UiHelper.AlertClass(kind);
            lblStatus.Text = message;
            lblStatus.Visible = true;
        }
    }
}
