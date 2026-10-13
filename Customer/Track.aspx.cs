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
                string wb = Request.QueryString["wb"];
                if (!string.IsNullOrEmpty(wb))
                {
                    txtWaybill.Text = wb;
                    DoTrack(wb.Trim());
                }
            }
        }

        private void LoadMine()
        {
            DataTable dt = Db.Query(
                @"SELECT TrackingNumber, DestCity, DeliveryStatus
                  FROM dbo.[Shipment] WHERE SenderID = @u ORDER BY ShipmentDate DESC", "@u", UserId);
            rptMine.DataSource = dt;
            rptMine.DataBind();
            pnlNoneMine.Visible = dt.Rows.Count == 0;
        }

        protected void btnTrack_Click(object sender, EventArgs e)
        {
            DoTrack(txtWaybill.Text.Trim());
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
                @"SELECT ShipmentID, TrackingNumber, DeliveryStatus, OriginCity, DestCity, DriverID
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

            // Offer a driver rating once the parcel is delivered and a driver is known.
            bool delivered = Db.ToStr(r["DeliveryStatus"]) == "Delivered" && r["DriverID"] != DBNull.Value;
            pnlRate.Visible = delivered;
            if (delivered) hfDriverId.Value = Db.ToStr(r["DriverID"]);

            // Drive the live map (TrackApi reads positions for this waybill).
            hfTrackWb.Value = Db.ToStr(r["TrackingNumber"]);

            pnlResult.Visible = true;
            lblMsg.Text = string.Empty;
        }

        protected void btnRate_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(hfDriverId.Value, out int driverId)) return;
            int score = int.Parse(ddlScore.SelectedValue);

            Db.NonQuery(
                @"INSERT INTO dbo.[Rating] (ClientID, DriverID, Comment, RatingScore)
                  VALUES (@c, @d, @m, @s)",
                "@c", UserId, "@d", driverId, "@m", txtComment.Text.Trim(), "@s", score);

            DoTrack(txtWaybill.Text.Trim());   // refresh the timeline
            pnlRate.Visible = false;           // one rating per view
            lblMsg.CssClass = UiHelper.AlertClass("success") + " mt-2";
            lblMsg.Text = "Thank you for rating your delivery.";
        }
    }
}
