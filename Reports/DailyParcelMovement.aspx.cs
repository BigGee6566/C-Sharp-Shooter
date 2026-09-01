using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Reports
{
    public partial class DailyParcelMovement : SecurePage
    {
        protected TextBox txtFrom, txtTo;
        protected Button btnApply, btnExport;
        protected GridView gv;
        protected Panel pnlEmpty;
        protected Literal litReg, litDisp, litDel, litTotal;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Admin", "Staff");
            if (!IsPostBack)
            {
                txtFrom.Text = DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd");
                txtTo.Text = DateTime.Today.ToString("yyyy-MM-dd");
                BindReport();
            }
        }

        private DateTime From { get { return DateTime.TryParse(txtFrom.Text, out DateTime d) ? d.Date : DateTime.Today.AddDays(-7); } }
        private DateTime To { get { return (DateTime.TryParse(txtTo.Text, out DateTime d) ? d.Date : DateTime.Today).AddDays(1); } }

        private DataTable GetData()
        {
            return Db.Query(
                @"SELECT s.TrackingNumber AS Waybill, th.Status, th.Location,
                         s.OriginCity + ' -> ' + s.DestCity AS Route,
                         ISNULL(u.FullName, '-') AS Driver,
                         CONVERT(varchar(16), th.[Timestamp], 120) AS EventTime, th.Remarks
                  FROM dbo.[TrackingHistory] th
                  JOIN dbo.[Shipment] s ON s.ShipmentID = th.ShipmentID
                  LEFT JOIN dbo.[User] u ON u.UserID = s.DriverID
                  WHERE th.[Timestamp] >= @from AND th.[Timestamp] < @to
                  ORDER BY th.[Timestamp] DESC",
                "@from", From, "@to", To);
        }

        private void BindReport()
        {
            DataTable dt = GetData();
            gv.DataSource = dt;
            gv.DataBind();
            pnlEmpty.Visible = dt.Rows.Count == 0;

            litTotal.Text = dt.Rows.Count.ToString();
            litReg.Text = CountStatus(dt, "Registered");
            litDisp.Text = (int.Parse("0" + CountStatus(dt, "Assigned")) + int.Parse("0" + CountStatus(dt, "OutForDelivery"))).ToString();
            litDel.Text = CountStatus(dt, "Delivered");
        }

        private static string CountStatus(DataTable dt, string status)
        {
            int n = 0;
            foreach (DataRow r in dt.Rows)
                if (string.Equals(Db.ToStr(r["Status"]), status, StringComparison.OrdinalIgnoreCase)) n++;
            return n.ToString();
        }

        protected void btnApply_Click(object sender, EventArgs e) { BindReport(); }

        protected void btnExport_Click(object sender, EventArgs e)
        {
            ReportExport.ToCsv(Response, GetData(), "DailyParcelMovement.csv");
        }
    }
}
