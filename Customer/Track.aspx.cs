using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.CustomerPortal
{
    public partial class Track : SecurePage
    {
        protected TextBox txtWaybill, txtComment;
        protected Button btnTrack, btnRate;
        protected Label lblMsg, lblRateMsg;
        protected Panel pnlResult, pnlNoneMine, pnlRate;
        protected Literal litWaybill, litStatus, litRoute;
        protected Repeater rptTimeline, rptMine;
        protected DropDownList ddlScore;
        protected HiddenField hfDriverId;
        protected HiddenField hfTrackWb;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Customer");
            if (!IsPostBack)
            {
                LoadMine();
                string wb = InputValidator.NormalizeTrackingNumber(Request.QueryString["wb"]);
                if (wb.Length > 0)
                {
                    txtWaybill.Text = wb;
                    DoTrack(wb);
                }
            }
        }

        private void LoadMine()
        {
            string clientType = Db.ToStr(Db.Scalar(
                "SELECT ClientType FROM dbo.[Client] WHERE UserID = @u", "@u", UserId));

            bool isBusiness = string.Equals(clientType, "Business", StringComparison.OrdinalIgnoreCase);

            DataTable dt;
            if (isBusiness)
            {
                dt = Db.Query(
                    @"SELECT TOP 50 TrackingNumber, DestCity, DeliveryStatus
                      FROM dbo.[Shipment] WHERE SenderID = @u ORDER BY ShipmentDate DESC", "@u", UserId);
            }
            else
            {
                dt = Db.Query(
                    @"SELECT TrackingNumber, DestCity, DeliveryStatus
                      FROM dbo.[Shipment]
                      WHERE SenderID = @u AND DeliveryStatus NOT IN ('Delivered', 'Failed', 'Cancelled')
                      ORDER BY ShipmentDate DESC", "@u", UserId);
            }

            rptMine.DataSource = dt;
            rptMine.DataBind();
            pnlNoneMine.Visible = dt.Rows.Count == 0;
        }

        protected void btnTrack_Click(object sender, EventArgs e)
        {
            DoTrack(InputValidator.NormalizeTrackingNumber(txtWaybill.Text));
        }

        private void DoTrack(string waybill)
        {
            if (waybill.Length == 0)
            {
                pnlResult.Visible = false;
                lblMsg.CssClass = UiHelper.AlertClass("error") + " mt-2";
                lblMsg.Text = "Please enter a waybill number.";
                return;
            }

            DataTable ship = Db.Query(
                @"SELECT ShipmentID, TrackingNumber, DeliveryStatus, OriginCity, DestCity, DriverID, SenderID
                  FROM dbo.[Shipment] WHERE TrackingNumber = @t", "@t", waybill);

            if (ship.Rows.Count == 0)
            {
                pnlResult.Visible = false;
                lblMsg.CssClass = UiHelper.AlertClass("warning") + " mt-2";
                lblMsg.Text = "No parcel found for waybill " + Server.HtmlEncode(waybill) + ".";
                return;
            }

            DataRow r = ship.Rows[0];
            int shipmentId = Db.ToInt(r["ShipmentID"]);
            litWaybill.Text = Server.HtmlEncode(Db.ToStr(r["TrackingNumber"]));
            litStatus.Text = UiHelper.StatusBadge(Db.ToStr(r["DeliveryStatus"]));
            litRoute.Text = "<span class='material-symbols-outlined text-brandRed mr-1'>location_on</span>" +
                            Server.HtmlEncode(Db.ToStr(r["OriginCity"])) + " &rarr; " +
                            Server.HtmlEncode(Db.ToStr(r["DestCity"]));

            DataTable hist = Db.Query(
                @"SELECT Status, Location, Remarks, [Timestamp]
                  FROM dbo.[TrackingHistory] WHERE ShipmentID = @s ORDER BY [Timestamp] DESC",
                "@s", shipmentId);
            rptTimeline.DataSource = hist;
            rptTimeline.DataBind();

            // Offer a driver rating only on the customer's OWN delivered parcel (not any
            // delivered waybill they happen to look up). btnRate re-checks this server-side.
            bool canRate = Db.ToStr(r["DeliveryStatus"]) == "Delivered"
                           && r["DriverID"] != DBNull.Value
                           && Db.ToInt(r["SenderID"]) == UserId;
            pnlRate.Visible = canRate;
            if (canRate) hfDriverId.Value = Db.ToStr(r["DriverID"]);

            // Drive the live map (TrackApi reads positions for this waybill).
            hfTrackWb.Value = Db.ToStr(r["TrackingNumber"]);

            pnlResult.Visible = true;
            lblMsg.Text = string.Empty;
        }

        protected void btnRate_Click(object sender, EventArgs e)
        {
            int score;
            if (!int.TryParse(ddlScore.SelectedValue, out score) || score < 1 || score > 5) return;

            string waybill = InputValidator.NormalizeTrackingNumber(txtWaybill.Text);

            // Owner + state re-check, server-side. Only the sender of a DELIVERED shipment may
            // rate its driver, and the driver is read from the shipment here - never taken from
            // the posted hidden field - so a rating cannot be aimed at an arbitrary driver or
            // left for a parcel that was not this customer's delivered parcel.
            DataTable own = Db.Query(
                @"SELECT DriverID FROM dbo.[Shipment]
                  WHERE TrackingNumber = @t AND SenderID = @u
                    AND DeliveryStatus = 'Delivered' AND DriverID IS NOT NULL",
                "@t", waybill, "@u", UserId);
            if (own.Rows.Count == 0)
            {
                pnlRate.Visible = false;
                lblMsg.CssClass = UiHelper.AlertClass("error") + " mt-2";
                lblMsg.Text = "You can only rate the driver of your own delivered parcel.";
                return;
            }
            int driverId = Db.ToInt(own.Rows[0]["DriverID"]);

            Db.NonQuery(
                @"INSERT INTO dbo.[Rating] (ClientID, DriverID, Comment, RatingScore)
                  VALUES (@c, @d, @m, @s)",
                "@c", UserId, "@d", driverId, "@m", txtComment.Text.Trim(), "@s", score);

            DoTrack(waybill);                  // refresh the timeline
            pnlRate.Visible = false;           // one rating per view
            lblMsg.CssClass = UiHelper.AlertClass("success") + " mt-2";
            lblMsg.Text = "Thank you for rating your delivery.";
        }
    }
}
