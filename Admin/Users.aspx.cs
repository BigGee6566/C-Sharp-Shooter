using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.AdminPortal
{
    /// <summary>
    /// Admin -> Users. Creating a user goes through AccountService and editing one through
    /// UserAdminService: each is one transaction with server-side validation and an audit
    /// entry, and the role rules (no removing your own admin access, never demoting the last
    /// admin, no un-enrolling a driver who still holds parcels) are enforced there.
    /// </summary>
    public partial class Users : SecurePage
    {
        protected TextBox txtName, txtEmail, txtPhone, txtPassword;
        protected DropDownList ddlRole;
        protected CheckBox chkIsDriver;
        protected Button btnAdd;
        protected Label lblMsg;
        protected Repeater rptUsers;

        // Edit panel
        protected Panel pnlEdit;
        protected TextBox txtEName, txtEEmail, txtEPhone, txtEAddress, txtEResetPw;
        protected DropDownList ddlERole;
        protected CheckBox chkEIsDriver;
        protected HiddenField hfEditId;
        protected Button btnSaveEdit, btnCancelEdit;
        protected Label lblEditMsg;
        protected Panel pnlEClientType;
        protected DropDownList ddlEClientType;
        protected TextBox txtEBusinessName;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Admin");
            // A temporary password may be on this response: never cache it (Back must not show it again).
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            if (!IsPostBack) LoadUsers();
        }

        private void LoadUsers()
        {
            DataTable dt = Db.Query("SELECT UserID, FullName, Email, Role, CreatedDate FROM dbo.[User] ORDER BY CreatedDate DESC");
            rptUsers.DataSource = dt;
            rptUsers.DataBind();
        }

        protected void btnAdd_Click(object sender, EventArgs e)
        {
            // Left blank, the system invents the temporary password so nobody has to.
            string pwd = txtPassword.Text;
            bool generated = pwd.Length == 0;
            if (generated) pwd = PasswordHelper.GenerateTemporary();

            string role = ddlRole.SelectedValue;
            AccountResult result = AccountService.Create(new NewAccount
            {
                FullName = txtName.Text,
                Email = txtEmail.Text,
                Phone = txtPhone.Text,
                Password = pwd,
                Role = role,
                // Driver is not a role; enrolment is an explicit flag on a Staff account.
                IsDriver = chkIsDriver.Checked && role == "Staff"
            }, UserId);

            if (!result.Success)
            {
                SetMsg(UiHelper.Enc(result.Message), false);
                return;
            }

            // The password is handed over by the admin, never stored in the user's inbox.
            NotificationService.TrySend(result.UserId, null,
                "Welcome to Logisticore. Your administrator will give you a temporary password - " +
                "please change it under My Profile after signing in.", "System");

            SetMsg(generated
                    ? "User created. Temporary password: " + UiHelper.Enc(pwd) + " &mdash; copy it now and give it to them; it will not be shown again."
                    : "User created.", true);
            txtName.Text = txtEmail.Text = txtPhone.Text = txtPassword.Text = string.Empty;
            chkIsDriver.Checked = false;
            LoadUsers();
        }

        protected void rptUsers_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int userId;
            if (e.CommandName != "edit" || !int.TryParse(Convert.ToString(e.CommandArgument), out userId)) return;

            DataTable dt = Db.Query(
                "SELECT FullName, Email, Role, PhoneNumber, Address FROM dbo.[User] WHERE UserID = @u", "@u", userId);
            if (dt.Rows.Count == 0) return;

            DataRow r = dt.Rows[0];
            hfEditId.Value = userId.ToString();
            txtEName.Text = Db.ToStr(r["FullName"]);
            txtEEmail.Text = Db.ToStr(r["Email"]);
            txtEPhone.Text = Db.ToStr(r["PhoneNumber"]);
            txtEAddress.Text = Db.ToStr(r["Address"]);
            txtEResetPw.Text = string.Empty;
            SelectValue(ddlERole, Db.ToStr(r["Role"]));
            chkEIsDriver.Checked = Db.ToInt(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.[Driver] WHERE UserID = @u", "@u", userId)) > 0;

            pnlEClientType.Visible = Db.ToStr(r["Role"]) == "Customer";
            if (pnlEClientType.Visible)
            {
                DataTable ct = Db.Query(
                    @"SELECT c.ClientType, b.BusinessName FROM dbo.[Client] c
                      LEFT JOIN dbo.Business b ON b.BusinessID = c.BusinessID
                      WHERE c.UserID = @u", "@u", userId);
                SelectValue(ddlEClientType, ct.Rows.Count > 0 ? Db.ToStr(ct.Rows[0]["ClientType"]) : "Individual");
                txtEBusinessName.Text = ct.Rows.Count > 0 ? Db.ToStr(ct.Rows[0]["BusinessName"]) : string.Empty;
            }

            lblEditMsg.Text = string.Empty;
            pnlEdit.Visible = true;
        }

        protected void btnSaveEdit_Click(object sender, EventArgs e)
        {
            int userId;
            if (!int.TryParse(hfEditId.Value, out userId)) return;

            string role = ddlERole.SelectedValue;
            AccountResult result = UserAdminService.Update(new UserEdit
            {
                UserId = userId,
                FullName = txtEName.Text,
                Phone = txtEPhone.Text,
                Address = txtEAddress.Text,
                Role = role,
                ResetPassword = txtEResetPw.Text,
                IsDriver = chkEIsDriver.Checked,
                ClientType = ddlEClientType.SelectedValue,
                BusinessName = txtEBusinessName.Text,
                // Billing is only applied when its fields were on screen for this user.
                UpdateBilling = pnlEClientType.Visible
            }, UserId);

            if (!result.Success)
            {
                lblEditMsg.CssClass = UiHelper.AlertClass("error");
                lblEditMsg.Text = UiHelper.Enc(result.Message);
                pnlEdit.Visible = true;
                return;
            }

            pnlEdit.Visible = false;
            SetMsg(UiHelper.Enc(result.Message), true);
            LoadUsers();
        }

        protected void btnCancelEdit_Click(object sender, EventArgs e)
        {
            pnlEdit.Visible = false;
            LoadUsers();
        }

        private static void SelectValue(DropDownList ddl, string value)
        {
            ListItem item = ddl.Items.FindByValue(value);
            ddl.ClearSelection();
            if (item != null) item.Selected = true;
            else if (ddl.Items.Count > 0) ddl.SelectedIndex = 0;
        }

        /// <summary><paramref name="html"/> is shown as-is: encode any user text before passing it.</summary>
        private void SetMsg(string html, bool ok)
        {
            lblMsg.CssClass = UiHelper.AlertClass(ok) + " mt-1";
            lblMsg.Text = html;
        }
    }
}
