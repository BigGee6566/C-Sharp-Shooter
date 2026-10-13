using System;
using System.Web.UI;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp
{
    public partial class AccessDeniedPage : Page
    {
        /// <summary>Resolved URL the "home" button points at - the markup reads this.</summary>
        protected string HomeUrl;

        /// <summary>Label on the "home" button, matched to where it goes.</summary>
        protected string HomeText;

        protected void Page_Load(object sender, EventArgs e)
        {
            // A signed-in user who is merely on the wrong page should land back in their own
            // portal, not on the public marketing site. Drivers in particular were stranded:
            // the generic "Back to Home" dropped them out of the portal entirely. An
            // anonymous visitor still goes to the public home.
            bool loggedIn = Session != null && Session["UserID"] != null;
            if (loggedIn)
            {
                HomeUrl = ResolveUrl(AuthService.DashboardUrl(
                    Session["Role"] as string, SecurePage.CurrentUserId()));
                HomeText = "Back to My Dashboard";
            }
            else
            {
                HomeUrl = ResolveUrl("~/Default.aspx");
                HomeText = "Back to Home";
            }
        }
    }
}
