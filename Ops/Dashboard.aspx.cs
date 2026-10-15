using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Ops
{
    public partial class Dashboard : SecurePage
    {
        protected Literal litRegistered, litWarehouse, litTransit, litDelivered, litExceptions;
        protected Repeater rptRecent;
        protected Literal litSummary;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Company-wide operations data: office staff and Admin, never drivers.
            RequireOperationsStaff();
            if (!IsPostBack) LoadData();
        }

        private void LoadData()
        {
            // One fully static, parameterless aggregate. The previous Count(string where, ...)
            // helper took a raw SQL fragment from its caller; every call site passed a literal,
            // but a method that accepts SQL text is an injection waiting to happen, so the
            // shape is gone rather than merely unused.
            DataTable totals = Db.Query(
                @"SELECT
                    SUM(CASE WHEN DeliveryStatus = 'Registered' THEN 1 ELSE 0 END) AS Registered,
                    SUM(CASE WHEN WarehouseStatus IN ('Received','Sorted') THEN 1 ELSE 0 END) AS InWarehouse,
                    SUM(CASE WHEN DeliveryStatus IN ('Assigned','InTransit','OutForDelivery') THEN 1 ELSE 0 END) AS InTransit,
                    SUM(CASE WHEN DeliveryStatus = 'Delivered' THEN 1 ELSE 0 END) AS Delivered,
                    SUM(CASE WHEN ExceptionType <> 'None' THEN 1 ELSE 0 END) AS Exceptions
                  FROM dbo.[Shipment]");

            DataRow t = totals.Rows.Count > 0 ? totals.Rows[0] : null;
            litRegistered.Text = t == null ? "0" : Db.ToInt(t["Registered"]).ToString();
            litWarehouse.Text  = t == null ? "0" : Db.ToInt(t["InWarehouse"]).ToString();
            litTransit.Text    = t == null ? "0" : Db.ToInt(t["InTransit"]).ToString();
            litDelivered.Text  = t == null ? "0" : Db.ToInt(t["Delivered"]).ToString();
            litExceptions.Text = t == null ? "0" : Db.ToInt(t["Exceptions"]).ToString();

            DataTable dt = Db.Query(
                @"SELECT TOP 15 s.TrackingNumber, s.OriginCity, s.DestCity, s.WarehouseStatus,
                         s.DeliveryStatus, s.ShipmentDate, ISNULL(u.FullName, '-') AS DriverName
                  FROM dbo.[Shipment] s
                  LEFT JOIN dbo.[User] u ON u.UserID = s.DriverID
                  ORDER BY s.ShipmentDate DESC");
            rptRecent.DataSource = dt;
            rptRecent.DataBind();

            ChartAssets.Register(this);
            DateTime now = DateTime.Now;
            bool admin = string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);
            litSummary.Text = RoleDashboards.Render(() => RoleDashboards.Operations(now, ResolveUrl, admin),
                RoleDashboards.CacheKey("ops", now, admin ? "admin" : "staff"));
        }
    }
}
