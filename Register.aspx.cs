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

            // Public and unauthenticated: cap attempts per address so a script cannot create
            // accounts in bulk or probe which emails are registered.
            if (!RequestThrottle.Allow("register:" + Request.UserHostAddress, 5, TimeSpan.FromMinutes(10)))
            {
                SetStatus("error", "Too many sign-up attempts from this network. Please wait a few minutes and try again.");
                return;
            }

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

            // User + Client in one transaction; the email check (and its race) lives there too.
            AccountResult result = AccountService.Create(new NewAccount
            {
                FullName = fullName,
                Email = email,
                Phone = txtPhone.Text,
                Address = txtAddress.Text,
                Password = password,
                Role = "Customer",
                ClientType = string.Equals(hfRole.Value, "Business", StringComparison.OrdinalIgnoreCase) ? "Business" : "Individual"
            }, null);
            if (!result.Success)
            {
                SetStatus("error", result.Message == AccountService.EmailTaken
                    ? "An account with that email already exists. Please sign in instead."
                    : result.Message);
                return;
            }
            int userId = result.UserId;

            NotificationService.TrySend(userId, null,
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
