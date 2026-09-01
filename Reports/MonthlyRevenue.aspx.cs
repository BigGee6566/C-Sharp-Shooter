using System;
using System.Data;
using System.Globalization;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Reports
{
    public partial class MonthlyRevenue : SecurePage
    {
        protected TextBox txtMonth;
        protected Button btnApply, btnExport;
        protected GridView gv;
        protected Panel pnlEmpty;
        protected Literal litCount, litTotal, litPaid, litOutstanding;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Revenue is Admin-only: with Finance folded into Staff, leaving this open to Staff
            // would expose it to every warehouse and dispatch user.
            RequireRole("Admin");
            if (!IsPostBack)
            {
                txtMonth.Text = DateTime.Today.ToString("yyyy-MM");
                BindReport();
            }
        }

        private DateTime MonthStart
        {
            get
            {
                if (DateTime.TryParseExact(txtMonth.Text + "-01", "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
                    return d;
                return new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            }
        }

        private DataTable GetData()
        {
            DateTime from = MonthStart;
            DateTime to = from.AddMonths(1);
            return Db.Query(
                @"SELECT p.InvoiceNumber, ISNULL(u.FullName, 'Walk-in') AS Client,
                         s.TrackingNumber AS Waybill, p.Amount, p.PaymentStatus,
                         CONVERT(varchar(10), p.IssuedDate, 120) AS Issued
                  FROM dbo.[Payment] p
                  LEFT JOIN dbo.[User] u ON u.UserID = p.ClientID
                  LEFT JOIN dbo.[Shipment] s ON s.ShipmentID = p.ShipmentID
                  WHERE p.IssuedDate >= @from AND p.IssuedDate < @to
                  ORDER BY p.IssuedDate", "@from", from, "@to", to);
        }

        private void BindReport()
        {
            DateTime from = MonthStart;
            DateTime to = from.AddMonths(1);

            DataTable dt = GetData();
            gv.DataSource = dt;
            gv.DataBind();
            pnlEmpty.Visible = dt.Rows.Count == 0;

            litCount.Text = dt.Rows.Count.ToString();
            litTotal.Text = Db.ToDecimal(Db.Scalar(
                "SELECT ISNULL(SUM(Amount),0) FROM dbo.[Payment] WHERE IssuedDate >= @f AND IssuedDate < @t", "@f", from, "@t", to)).ToString("N2");
            litPaid.Text = Db.ToDecimal(Db.Scalar(
                "SELECT ISNULL(SUM(Amount),0) FROM dbo.[Payment] WHERE PaymentStatus = 'Paid' AND IssuedDate >= @f AND IssuedDate < @t", "@f", from, "@t", to)).ToString("N2");
            litOutstanding.Text = Db.ToDecimal(Db.Scalar(
                "SELECT ISNULL(SUM(Amount),0) FROM dbo.[Payment] WHERE PaymentStatus = 'Unpaid' AND IssuedDate >= @f AND IssuedDate < @t", "@f", from, "@t", to)).ToString("N2");
        }

        protected void btnApply_Click(object sender, EventArgs e) { BindReport(); }

        protected void btnExport_Click(object sender, EventArgs e)
        {
            ReportExport.ToCsv(Response, GetData(), "MonthlyRevenue.csv");
        }
    }
}
