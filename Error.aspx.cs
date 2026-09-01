using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Security;

namespace ForteLogisticsApp
{
    /// <summary>
    /// Generic error page. It deliberately shows the user nothing about the failure -
    /// no exception type, no SQL, no stack trace - because a detailed error page is what
    /// turns a blind injection attempt into a readable one. The correlation reference lets
    /// support tie a user report to the entry Global.asax wrote to the event log.
    /// </summary>
    public partial class ErrorPage : BasePage
    {
        protected Literal litRef;

        protected void Page_Load(object sender, EventArgs e)
        {
            string reference = Session["LastErrorRef"] as string;
            litRef.Text = Server.HtmlEncode(string.IsNullOrEmpty(reference) ? "not recorded" : reference);
        }
    }
}
