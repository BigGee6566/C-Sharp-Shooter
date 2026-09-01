using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Reports
{
    public partial class DriverPerformance : SecurePage
    {
        protected Button btnExport;
        protected GridView gv;
        protected Panel pnlEmpty;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Admin", "Staff");
            if (!IsPostBack) BindReport();
        }

        private DataTable GetData()
        {
            return Db.Query(
                @"SELECT u.FullName AS Driver,
                         COUNT(s.ShipmentID) AS TotalAssigned,
                         SUM(CASE WHEN s.DeliveryStatus = 'Delivered' THEN 1 ELSE 0 END) AS Delivered,
                         SUM(CASE WHEN s.DeliveryStatus = 'Failed' THEN 1 ELSE 0 END) AS Failed,
                         SUM(CASE WHEN s.DeliveryStatus IN ('InTransit','OutForDelivery') THEN 1 ELSE 0 END) AS InProgress,
                         CAST(CASE WHEN COUNT(s.ShipmentID) = 0 THEN 0
                              ELSE 100.0 * SUM(CASE WHEN s.DeliveryStatus = 'Delivered' THEN 1 ELSE 0 END) / COUNT(s.ShipmentID)
                              END AS DECIMAL(5,1)) AS SuccessRatePct,
                         ISNULL((SELECT CAST(AVG(CAST(RatingScore AS DECIMAL(4,2))) AS DECIMAL(4,2))
                                 FROM dbo.[Rating] rt WHERE rt.DriverID = d.UserID), 0) AS AvgRating
                  FROM dbo.[Driver] d
                  JOIN dbo.[User] u ON u.UserID = d.UserID
                  LEFT JOIN dbo.[Shipment] s ON s.DriverID = d.UserID
                  GROUP BY u.FullName, d.UserID
                  ORDER BY Delivered DESC");
        }

        private void BindReport()
        {
            DataTable dt = GetData();
            gv.DataSource = dt;
            gv.DataBind();
            pnlEmpty.Visible = dt.Rows.Count == 0;
        }

        protected void btnExport_Click(object sender, EventArgs e)
        {
            ReportExport.ToCsv(Response, GetData(), "DriverPerformance.csv");
        }
    }
}
