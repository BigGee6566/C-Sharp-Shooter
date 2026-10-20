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
            string name = InputValidator.NormalizeText(txtFullName.Text);
            string phone = InputValidator.NormalizePhone(txtPhone.Text);
            string address = InputValidator.NormalizeText(txtAddress.Text);
            if (name.Length == 0)
            {
                Set(lblSaveMsg, "Full name is required.", false);
                return;
            }
            if (name.Length > AccountService.NameMax)
            {
                Set(lblSaveMsg, "Please shorten your name to " + AccountService.NameMax + " characters.", false);
                return;
            }
            if (phone.Length > 0 && !InputValidator.IsValidPhone(phone))
            {
                Set(lblSaveMsg, "Please enter a phone number with 7 to 15 digits, or leave it blank.", false);
                return;
            }
            if (address.Length > AccountService.AddressMax)
            {
                Set(lblSaveMsg, "Please shorten your address to " + AccountService.AddressMax + " characters.", false);
                return;
            }

            Db.NonQuery(
                @"UPDATE dbo.[User] SET FullName = @n, PhoneNumber = @p, Address = @a WHERE UserID = @u",
                "@n", name, "@p", phone, "@a", address, "@u", UserId);

            Session["FullName"] = name; // keep the header greeting in sync
            Set(lblSaveMsg, "Your details have been updated.", true);
        }

        protected void btnChangePw_Click(object sender, EventArgs e)
        {
            string current = txtCurrent.Text;
            string newPw = txtNew.Text;

            if (newPw.Length < AccountService.MinPasswordLength || newPw.Length > AccountService.MaxPasswordLength)
            {
                Set(lblPwMsg, "Your new password must be " + AccountService.MinPasswordLength + " to " +
                              AccountService.MaxPasswordLength + " characters.", false);
                return;
            }
            if (newPw != txtConfirm.Text)
            {
                Set(lblPwMsg, "The new passwords do not match.", false);
                return;
            }

            // Guessing the current password from a hijacked session is limited like sign-in.
            string guessKey = "pwchange:" + UserId;
            if (RequestThrottle.Count(guessKey) >= 5)
            {
                Set(lblPwMsg, "Too many incorrect attempts. Please wait 15 minutes and try again.", false);
                return;
            }
            string storedHash = Db.ToStr(Db.Scalar("SELECT Password FROM dbo.[User] WHERE UserID = @u", "@u", UserId));
            if (!PasswordHelper.Verify(current, storedHash))
            {
                RequestThrottle.Allow(guessKey, 5, TimeSpan.FromMinutes(15));
                Set(lblPwMsg, "Your current password is incorrect.", false);
                return;
            }
            RequestThrottle.Reset(guessKey);

            string newHash = PasswordHelper.Hash(newPw);
            Db.NonQuery("UPDATE dbo.[User] SET Password = @p WHERE UserID = @u",
                "@p", newHash, "@u", UserId);
            // Their own change: keep this session signed in (other sessions are ended).
            SessionGuard.Restamp(Session, Role, newHash);

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
