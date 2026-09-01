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

            if (txtName.Text.Trim().Length == 0 || txtMessage.Text.Trim().Length == 0)
            {
                lblStatus.CssClass = UiHelper.AlertClass("error") + " mt-3";
                lblStatus.Text = "Please enter your name and a message.";
                return;
            }

            string name = txtName.Text.Trim();
            string email = txtEmail.Text.Trim();

            // Record the enquiry and put it in front of an administrator, rather than
            // telling the visitor we received it and then discarding it.
            Db.NonQuery(
                @"INSERT INTO dbo.[ContactMessage] (FullName, Email, Message)
                  VALUES (@n, @e, @m)",
                "@n", name, "@e", email, "@m", txtMessage.Text.Trim());

            NotificationService.SendToRole("Admin", null,
                "New website enquiry from " + name + (email.Length > 0 ? " (" + email + ")" : "") + ".",
                "System");

            lblStatus.CssClass = UiHelper.AlertClass("success") + " mt-3";
            lblStatus.Text = "Thank you, " + Server.HtmlEncode(name) +
                             "! Your message has been received. Our team will be in touch shortly.";
            txtName.Text = txtEmail.Text = txtMessage.Text = string.Empty;
        }
    }
}
