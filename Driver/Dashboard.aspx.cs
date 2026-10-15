using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.DriverPortal
{
    public partial class Dashboard : SecurePage
    {
        protected Literal litAssigned, litDelivered, litFailed;
        protected Repeater rptManifest;
        protected Panel pnlEmpty;
        protected Label lblMsg;
        protected Button btnCollectAll;
        protected Literal litSummary;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Drivers are Staff. The manifest is scoped by DriverID = UserId below, so a
            // non-driver staff member reaching this page simply sees nothing.
            RequireRole("Admin", "Staff");
            if (!IsPostBack) LoadData();
        }

        private void LoadData()
        {
            litAssigned.Text = Db.ToStr(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.[Shipment] WHERE DriverID = @d AND DeliveryStatus IN ('Assigned','OutForDelivery')", "@d", UserId));
            litDelivered.Text = Db.ToStr(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.[Shipment] WHERE DriverID = @d AND DeliveryStatus = 'Delivered'", "@d", UserId));
            litFailed.Text = Db.ToStr(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.[Shipment] WHERE DriverID = @d AND DeliveryStatus = 'Failed'", "@d", UserId));

            DataTable dt = Db.Query(
                @"SELECT ShipmentID, TrackingNumber, ReceiverName, DeliveryAddress, ReceiverPhone, DeliveryStatus
                  FROM dbo.[Shipment]
                  WHERE DriverID = @d AND DeliveryStatus IN ('Assigned','OutForDelivery')
                  ORDER BY CASE DeliveryStatus WHEN 'Assigned' THEN 0 ELSE 1 END, ShipmentDate", "@d", UserId);
            rptManifest.DataSource = dt;
            rptManifest.DataBind();
            pnlEmpty.Visible = dt.Rows.Count == 0;

            // Only a registered driver has a performance record to show.
            if (IsDriver(UserId))
            {
                ChartAssets.Register(this);
                litSummary.Text = RoleDashboards.Render(() => RoleDashboards.Driver(UserId, DateTime.Now, ResolveUrl), null);
            }
        }

        protected void rptManifest_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "pickup") return;

            int shipId = int.Parse((string)e.CommandArgument);
            bool ok = ConfirmPickup(shipId);
            SetMsg(ok
                    ? "Pickup confirmed. Parcel is now out for delivery."
                    : "That parcel could not be collected (not assigned to you or already collected).",
                   ok);
            LoadData();
        }

        /// <summary>Collects every parcel still awaiting pickup on this driver's manifest.</summary>
        protected void btnCollectAll_Click(object sender, EventArgs e)
        {
            DataTable dt = Db.Query(
                @"SELECT ShipmentID FROM dbo.[Shipment]
                  WHERE DriverID = @d AND DeliveryStatus = 'Assigned'", "@d", UserId);

            int collected = 0;
            foreach (DataRow r in dt.Rows)
            {
                if (ConfirmPickup(Db.ToInt(r["ShipmentID"]))) collected++;
            }

            SetMsg(collected > 0
                    ? collected + " parcel(s) collected and out for delivery."
                    : "There are no parcels awaiting pickup on your manifest.",
                   collected > 0);
            LoadData();
        }

        /// <summary>
        /// Marks one parcel as collected. The ownership check lives here, so it applies
        /// identically whether the driver collects one parcel or the whole manifest.
        /// </summary>
        private bool ConfirmPickup(int shipId)
        {
            // Only the assigned driver can confirm pickup, and only while 'Assigned'.
            DataTable dt = Db.Query(
                @"SELECT SenderID, TrackingNumber FROM dbo.[Shipment]
                  WHERE ShipmentID = @s AND DriverID = @d AND DeliveryStatus = 'Assigned'",
                "@s", shipId, "@d", UserId);
            if (dt.Rows.Count == 0) return false;

            // The ownership + 'Assigned' check above already scoped this to the driver's own
            // parcel; the shared service re-validates the transition and guards against a
            // concurrent change (two tabs, double-click) before writing, replacing the old
            // inline UPDATE.
            try
            {
                ShipmentTransitionService.TransitionDelivery(shipId, UserId, "OutForDelivery",
                    "Collected from sender", "Parcel collected by driver - out for delivery");
            }
            catch (InvalidOperationException)
            {
                return false;   // beaten by a concurrent change; the manifest reload shows the truth
            }

            if (dt.Rows[0]["SenderID"] != DBNull.Value)
            {
                NotificationService.Send(Db.ToInt(dt.Rows[0]["SenderID"]), shipId,
                    "Your parcel " + Db.ToStr(dt.Rows[0]["TrackingNumber"]) + " has been collected and is out for delivery.",
                    "Dispatch");
            }
            return true;
        }

        private void SetMsg(string text, bool ok)
        {
            lblMsg.CssClass = UiHelper.AlertClass(ok) + " mb-3";
            lblMsg.Text = text;
        }
    }
}
