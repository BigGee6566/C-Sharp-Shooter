using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Finance
{
    public partial class Dashboard : SecurePage
    {
        protected Literal litSummary;
        protected Repeater rptInvoices;
        protected Panel pnlNone;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Admin");
            if (!IsPostBack) LoadData();
        }

        private void LoadData()
        {
            // Same definitions as the Revenue report: cancelled invoices excluded, outstanding
            // includes every unpaid status, overdue by due date.
            ChartAssets.Register(this);
            DateTime now = DateTime.Now;
            litSummary.Text = RoleDashboards.Render(() => RoleDashboards.Finance(now, ResolveUrl), RoleDashboards.CacheKey("finance", now, null));

            DataTable dt = Db.Query(
                @"SELECT TOP 15 p.InvoiceNumber, p.Amount, p.PaymentStatus, p.IssuedDate,
                         ISNULL(u.FullName, 'Walk-in') AS ClientName
                  FROM dbo.[Payment] p
                  LEFT JOIN dbo.[User] u ON u.UserID = p.ClientID
                  ORDER BY p.IssuedDate DESC");
            rptInvoices.DataSource = dt;
            rptInvoices.DataBind();
            pnlNone.Visible = dt.Rows.Count == 0;
        }
    }
}
