using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.AdminPortal
{
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
            if (!IsPostBack) LoadUsers();
        }

        private void LoadUsers()
        {
            DataTable dt = Db.Query("SELECT UserID, FullName, Email, Role, CreatedDate FROM dbo.[User] ORDER BY CreatedDate DESC");
            rptUsers.DataSource = dt;
            rptUsers.DataBind();
        }

        /// <summary>Ensures the role-specific record (Client/Admin) exists for a user.</summary>
        private void EnsureRoleRecord(int userId, string role)
        {
            switch (role)
            {
                case "Customer":
                    if (Db.ToInt(Db.Scalar("SELECT COUNT(*) FROM dbo.[Client] WHERE UserID = @u", "@u", userId)) == 0)
                        Db.NonQuery("INSERT INTO dbo.[Client] (UserID, ClientType) VALUES (@u, 'Individual')", "@u", userId);
                    break;
                case "Staff":
                case "Admin":
                    if (Db.ToInt(Db.Scalar("SELECT COUNT(*) FROM dbo.[Admin] WHERE UserID = @u", "@u", userId)) == 0)
                        Db.NonQuery("INSERT INTO dbo.[Admin] (UserID, Department) VALUES (@u, @d)",
                            "@u", userId, "@d", role == "Admin" ? "IT / Systems" : "Operations");
                    break;
            }
        }

        /// <summary>
        /// Adds or removes the dbo.[Driver] row for a user.
        ///
        /// Driver is no longer a role, so this is the only way to enrol one. Membership of
        /// this table is what Route Planning, Fleet and Driver Performance all join on.
        /// A driver holding assigned work is kept, so their shipments never lose their owner.
        /// </summary>
        private bool SetDriverEnrolment(int userId, bool shouldBeDriver)
        {
            bool isDriver = Db.ToInt(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.[Driver] WHERE UserID = @u", "@u", userId)) > 0;

            if (shouldBeDriver && !isDriver)
            {
                Db.NonQuery(
                    "INSERT INTO dbo.[Driver] (UserID, AvailabilityStatus) VALUES (@u, 'Available')",
                    "@u", userId);
            }
            else if (!shouldBeDriver && isDriver)
            {
                int active = Db.ToInt(Db.Scalar(
                    @"SELECT COUNT(*) FROM dbo.[Shipment]
                      WHERE DriverID = @u AND DeliveryStatus IN ('Assigned','OutForDelivery')",
                    "@u", userId));

                if (active > 0) return false;

                Db.NonQuery("DELETE FROM dbo.[Driver] WHERE UserID = @u", "@u", userId);
            }
            return true;
        }

        protected void btnAdd_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();
            string email = txtEmail.Text.Trim();
            string pwd = txtPassword.Text;
            string role = ddlRole.SelectedValue;

            if (name.Length == 0 || email.Length == 0)
            {
                SetMsg("Name and email are required.", false);
                return;
            }
            if (pwd.Length > 0 && pwd.Length < 6)
            {
                SetMsg("A password must be at least 6 characters, or leave it blank to auto-generate one.", false);
                return;
            }
            if (Db.ToInt(Db.Scalar("SELECT COUNT(*) FROM dbo.[User] WHERE Email = @e", "@e", email)) > 0)
            {
                SetMsg("That email is already registered.", false);
                return;
            }

            // Left blank, the system invents the temporary password so nobody has to.
            bool generated = pwd.Length == 0;
            if (generated) pwd = PasswordHelper.GenerateTemporary();

            int userId = Db.Insert(
                @"INSERT INTO dbo.[User] (FullName, Email, Password, Role, PhoneNumber)
                  VALUES (@n, @e, @p, @r, @ph)",
                "@n", name, "@e", email, "@p", PasswordHelper.Hash(pwd), "@r", role, "@ph", txtPhone.Text.Trim());

            EnsureRoleRecord(userId, role);

            // Driver is not a role any more; enrolment is an explicit flag on a Staff account.
            if (chkIsDriver.Checked && role == "Staff") SetDriverEnrolment(userId, true);

            // The new user finds their credentials waiting in their own inbox.
            NotificationService.Send(userId, null,
                "Welcome to Logisticore. Your temporary password is " + pwd +
                " - please change it under My Profile after signing in.", "System");

            SetMsg(generated
                    ? "User created. Temporary password: " + pwd + " — copy it now, it will not be shown again."
                    : "User created.", true);
            txtName.Text = txtEmail.Text = txtPhone.Text = txtPassword.Text = string.Empty;
            chkIsDriver.Checked = false;
            LoadUsers();
        }

        protected void rptUsers_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "edit") return;

            int userId = int.Parse((string)e.CommandArgument);
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
            if (!int.TryParse(hfEditId.Value, out int userId)) return;

            string name = txtEName.Text.Trim();
            if (name.Length == 0)
            {
                lblEditMsg.CssClass = UiHelper.AlertClass("error");
                lblEditMsg.Text = "Full name is required.";
                pnlEdit.Visible = true;
                return;
            }

            string role = ddlERole.SelectedValue;
            Db.NonQuery(
                @"UPDATE dbo.[User] SET FullName = @n, PhoneNumber = @ph, Address = @a, Role = @r WHERE UserID = @u",
                "@n", name, "@ph", txtEPhone.Text.Trim(), "@a", txtEAddress.Text.Trim(), "@r", role, "@u", userId);

            // Optional password reset.
            if (txtEResetPw.Text.Length > 0)
            {
                if (txtEResetPw.Text.Length < 6)
                {
                    lblEditMsg.CssClass = UiHelper.AlertClass("error");
                    lblEditMsg.Text = "New password must be at least 6 characters.";
                    pnlEdit.Visible = true;
                    return;
                }
                Db.NonQuery("UPDATE dbo.[User] SET Password = @p WHERE UserID = @u",
                    "@p", PasswordHelper.Hash(txtEResetPw.Text), "@u", userId);
            }

            // If the role changed, make sure the matching role record exists.
            EnsureRoleRecord(userId, role);

            if (role == "Customer")
            {
                SetClientBilling(userId, ddlEClientType.SelectedValue, txtEBusinessName.Text.Trim());
            }

            // Only Staff can drive; moving someone to Customer or Admin un-enrols them.
            bool wantsDriver = chkEIsDriver.Checked && role == "Staff";
            if (!SetDriverEnrolment(userId, wantsDriver))
            {
                lblEditMsg.CssClass = UiHelper.AlertClass("error");
                lblEditMsg.Text = "Saved, but this user is still a driver: they have shipments " +
                                  "assigned or out for delivery. Reassign those first.";
                pnlEdit.Visible = true;
                LoadUsers();
                return;
            }

            pnlEdit.Visible = false;
            SetMsg("User updated.", true);
            LoadUsers();
        }

        /// <summary>
        /// Sets a customer's billing type and, for Business, links them to a Business row
        /// (found by name, or created if this is the first Business client with that name).
        /// Clients that share a Business see and pay each other's invoices together under
        /// My Invoices - see PaymentService.PayInvoices.
        /// </summary>
        private void SetClientBilling(int userId, string clientType, string businessName)
        {
            int? businessId = null;
            if (clientType == "Business" && businessName.Length > 0)
            {
                object existing = Db.Scalar(
                    "SELECT BusinessID FROM dbo.Business WHERE BusinessName = @n", "@n", businessName);
                businessId = (existing != null && existing != DBNull.Value)
                    ? Db.ToInt(existing)
                    : Db.Insert("INSERT INTO dbo.Business (BusinessName) VALUES (@n)", "@n", businessName);
            }

            Db.NonQuery(
                "UPDATE dbo.[Client] SET ClientType = @t, BusinessID = @b WHERE UserID = @u",
                "@t", clientType, "@b", businessId, "@u", userId);
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

        private void SetMsg(string text, bool ok)
        {
            lblMsg.CssClass = UiHelper.AlertClass(ok) + " mt-1";
            lblMsg.Text = text;
        }
    }
}
