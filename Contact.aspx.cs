using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp
{
    public partial class ContactPage : BasePage
    {
        protected TextBox txtName;
        protected TextBox txtEmail;
        protected TextBox txtMessage;
        protected Button btnSend;
        protected Label lblStatus;

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void btnSend_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            // Public form: every message notifies every administrator, so cap it per address.
            if (!RequestThrottle.Allow("contact:" + Request.UserHostAddress, 5, TimeSpan.FromMinutes(10)))
            {
                ShowError("You have sent several messages in a short time. Please wait a few minutes and try again.");
                return;
            }

            string name = InputValidator.NormalizeText(txtName.Text);
            string email = InputValidator.NormalizeEmail(txtEmail.Text);
            string message = InputValidator.NormalizeMultiline(txtMessage.Text);

            if (name.Length == 0 || message.Length == 0)
            {
                ShowError("Please enter your name and a message.");
                return;
            }
            if (name.Length > MaxName) { ShowError("Please shorten your name to " + MaxName + " characters."); return; }
            if (email.Length > 0 && !InputValidator.IsValidEmail(email)) { ShowError("Please check your email address, or leave it blank."); return; }
            if (message.Length > MaxMessage) { ShowError("Please shorten your message to " + MaxMessage + " characters."); return; }

            // Record the enquiry and put it in front of an administrator, rather than
            // telling the visitor we received it and then discarding it.
            Db.NonQuery(
                @"INSERT INTO dbo.[ContactMessage] (FullName, Email, Message)
                  VALUES (@n, @e, @m)",
                "@n", name, "@e", email.Length > 0 ? (object)email : DBNull.Value, "@m", message);

            // The enquiry is saved; a failed alert must not turn that into an error page.
            try
            {
                NotificationService.SendToRole("Admin", null,
                    "New website enquiry from " + name + (email.Length > 0 ? " (" + email + ")" : "") + ".",
                    "System");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Contact notification failed: " + ex);
            }

            lblStatus.CssClass = UiHelper.AlertClass("success") + " mt-3";
            lblStatus.Text = "Thank you, " + Server.HtmlEncode(name) +
                             "! Your message has been received. Our team will be in touch shortly.";
            txtName.Text = txtEmail.Text = txtMessage.Text = string.Empty;
        }

        // Column widths of dbo.ContactMessage.
        private const int MaxName = 150, MaxMessage = 2000;

        private void ShowError(string message)
        {
            lblStatus.CssClass = UiHelper.AlertClass("error") + " mt-3";
            lblStatus.Text = UiHelper.Enc(message);
        }
    }
}
