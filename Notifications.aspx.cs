using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp
{
    /// <summary>
    /// In-app inbox for every role. Automation notifies Dispatch, Warehouse, Finance,
    /// Managers and Drivers as well as customers, so all of them need somewhere to read it.
    /// </summary>
    public partial class NotificationsPage : SecurePage
    {
        protected Repeater rptNotifications;
        protected Panel pnlNone;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole();          // any authenticated user
            if (IsPostBack) return;

            DataTable dt = NotificationService.Recent(UserId, 50);
            rptNotifications.DataSource = dt;
            rptNotifications.DataBind();
            pnlNone.Visible = dt.Rows.Count == 0;

            // Bind first, then mark read, so unread items are still highlighted this once.
            NotificationService.MarkAllRead(UserId);
        }

        /// <summary>Deep-links a notification to the parcel it refers to, per role.</summary>
        protected string TrackUrl(object shipmentId)
        {
            if (shipmentId == null || shipmentId == DBNull.Value) return "#";

            int id = Db.ToInt(shipmentId);
            if (Role == "Admin")
                return ResolveUrl("~/Admin/Shipments.aspx?id=" + id);

            string tracking = Db.ToStr(Db.Scalar(
                "SELECT TrackingNumber FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", id));
            if (tracking.Length == 0) return "#";

            return Role == "Customer"
                ? ResolveUrl("~/Customer/Track.aspx?wb=" + Server.UrlEncode(tracking))
                : ResolveUrl("~/Default.aspx#track");
        }
    }
}
