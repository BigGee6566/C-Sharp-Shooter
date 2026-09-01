using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;

namespace ForteLogisticsApp.Finance
{
    public partial class Dashboard : SecurePage
    {
        protected Literal litInvoiced, litPaid, litOutstanding, litUninvoiced;
        protected Repeater rptInvoices;
        protected Panel pnlNone;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Admin");
            if (!IsPostBack) LoadData();
        }

        private void LoadData()
        {
            litInvoiced.Text = Db.ToDecimal(Db.Scalar("SELECT ISNULL(SUM(Amount),0) FROM dbo.[Payment]")).ToString("N2");
            litPaid.Text = Db.ToDecimal(Db.Scalar("SELECT ISNULL(SUM(Amount),0) FROM dbo.[Payment] WHERE PaymentStatus = 'Paid'")).ToString("N2");
            litOutstanding.Text = Db.ToDecimal(Db.Scalar("SELECT ISNULL(SUM(Amount),0) FROM dbo.[Payment] WHERE PaymentStatus = 'Unpaid'")).ToString("N2");
            litUninvoiced.Text = Db.ToStr(Db.Scalar(
                @"SELECT COUNT(*) FROM dbo.[Shipment] s
                  WHERE s.DeliveryStatus = 'Delivered'
                    AND NOT EXISTS (SELECT 1 FROM dbo.[Payment] p WHERE p.ShipmentID = s.ShipmentID)"));

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
