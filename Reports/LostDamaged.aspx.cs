using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Reports
{
    public partial class LostDamaged : SecurePage
    {
        protected Button btnExport;
        protected GridView gv;
        protected Panel pnlEmpty;
        protected Literal litDamaged, litLost, litTotal;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Admin", "Staff");
            if (!IsPostBack) BindReport();
        }

        private DataTable GetData()
        {
            return Db.Query(
                @"SELECT s.TrackingNumber AS Waybill, s.ExceptionType AS Exception,
                         s.OriginCity + ' -> ' + s.DestCity AS Route,
                         s.DeliveryStatus AS Status, ISNULL(u.FullName, '-') AS Driver,
                         CONVERT(varchar(10), s.ShipmentDate, 120) AS Registered
                  FROM dbo.[Shipment] s
                  LEFT JOIN dbo.[User] u ON u.UserID = s.DriverID
                  WHERE s.ExceptionType <> 'None'
                  ORDER BY s.ShipmentDate DESC");
        }

        private void BindReport()
        {
            DataTable dt = GetData();
            gv.DataSource = dt;
            gv.DataBind();
            pnlEmpty.Visible = dt.Rows.Count == 0;

            litTotal.Text = dt.Rows.Count.ToString();
            litDamaged.Text = Db.ToStr(Db.Scalar("SELECT COUNT(*) FROM dbo.[Shipment] WHERE ExceptionType = 'Damaged'"));
            litLost.Text = Db.ToStr(Db.Scalar("SELECT COUNT(*) FROM dbo.[Shipment] WHERE ExceptionType = 'Lost'"));
        }

        protected void btnExport_Click(object sender, EventArgs e)
        {
            ReportExport.ToCsv(Response, GetData(), "LostDamagedExceptions.csv");
        }
    }
}
