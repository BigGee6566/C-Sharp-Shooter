using System;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.AdminPortal
{
    public partial class Dashboard : SecurePage
    {
        protected Literal litSummary;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Admin");
            if (!IsPostBack)
            {
                ChartAssets.Register(this);
                DateTime now = DateTime.Now;
                litSummary.Text = RoleDashboards.Render(() => RoleDashboards.Admin(now, ResolveUrl), RoleDashboards.CacheKey("admin", now, null));
            }
        }
    }
}
