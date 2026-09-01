using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.CustomerPortal
{
    public partial class Profile : SecurePage
    {
        protected TextBox txtFullName, txtEmail, txtPhone, txtClientType, txtAddress, txtCurrent, txtNew, txtConfirm;
        protected Button btnSave, btnChangePw;
        protected Label lblSaveMsg, lblPwMsg;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Customer");
            if (!IsPostBack) LoadProfile();
        }

        private void LoadProfile()
        {
            DataTable dt = Db.Query(
                @"SELECT u.FullName, u.Email, u.PhoneNumber, u.Address, ISNULL(c.ClientType,'Individual') AS ClientType
                  FROM dbo.[User] u LEFT JOIN dbo.[Client] c ON c.UserID = u.UserID
                  WHERE u.UserID = @u", "@u", UserId);
            if (dt.Rows.Count == 0) return;
            DataRow r = dt.Rows[0];
            txtFullName.Text = Db.ToStr(r["FullName"]);
            txtEmail.Text = Db.ToStr(r["Email"]);
            txtPhone.Text = Db.ToStr(r["PhoneNumber"]);
            txtAddress.Text = Db.ToStr(r["Address"]);
            txtClientType.Text = Db.ToStr(r["ClientType"]);
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            string name = txtFullName.Text.Trim();
            if (name.Length == 0)
            {
                Set(lblSaveMsg, "Full name is required.", false);
                return;
            }

            Db.NonQuery(
                @"UPDATE dbo.[User] SET FullName = @n, PhoneNumber = @p, Address = @a WHERE UserID = @u",
                "@n", name, "@p", txtPhone.Text.Trim(), "@a", txtAddress.Text.Trim(), "@u", UserId);

            Session["FullName"] = name; // keep the header greeting in sync
            Set(lblSaveMsg, "Your details have been updated.", true);
        }

        protected void btnChangePw_Click(object sender, EventArgs e)
        {
            string current = txtCurrent.Text;
            string newPw = txtNew.Text;

            if (newPw.Length < 6)
            {
                Set(lblPwMsg, "New password must be at least 6 characters.", false);
                return;
            }
            if (newPw != txtConfirm.Text)
            {
                Set(lblPwMsg, "The new passwords do not match.", false);
                return;
            }

            string storedHash = Db.ToStr(Db.Scalar("SELECT Password FROM dbo.[User] WHERE UserID = @u", "@u", UserId));
            if (!PasswordHelper.Verify(current, storedHash))
            {
                Set(lblPwMsg, "Your current password is incorrect.", false);
                return;
            }

            Db.NonQuery("UPDATE dbo.[User] SET Password = @p WHERE UserID = @u",
                "@p", PasswordHelper.Hash(newPw), "@u", UserId);

            txtCurrent.Text = txtNew.Text = txtConfirm.Text = string.Empty;
            Set(lblPwMsg, "Your password has been changed.", true);
        }

        private static void Set(Label lbl, string text, bool ok)
        {
            lbl.CssClass = UiHelper.AlertClass(ok) + " mt-3";
            lbl.Text = text;
        }
    }
}
