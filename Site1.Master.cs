using System;
using System.Web.UI;

namespace LogistiCoreV5
{
    public partial class SiteMaster : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Verify active session
                if (Session["IsLoggedIn"] == null || !(bool)Session["IsLoggedIn"])
                {
                    Response.Redirect("~/Login.aspx");
                    return;
                }

                // Hydrate user info from session
                string userType = Convert.ToString(Session["UserType"]);
                string userEmail = Convert.ToString(Session["UserEmail"]);

                if (userType == "Individual")
                {
                    litNavUserName.Text = string.IsNullOrEmpty(userEmail) ? "Jane Doe" : userEmail.Split('@')[0];
                    litNavUserType.Text = "Personal Account";
                }
                else
                {
                    litNavUserName.Text = "LogistiCore Admin";
                    litNavUserType.Text = "Enterprise Account";
                }
            }
        }

        protected void btnCreateOrder_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/BookShipment.aspx");
        }

        protected void btnLogout_Click(object sender, EventArgs e)
        {
            Session.Clear();
            Session.Abandon();
            Response.Redirect("~/Login.aspx");
        }
    }
}