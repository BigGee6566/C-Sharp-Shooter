using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.DriverPortal
{
    public partial class DeliveryConfirmation : SecurePage
    {
        protected TextBox txtScan, txtReceivedBy, txtNotes;
        protected Button btnScan, btnConfirm;
        protected Label lblScanMsg;
        protected Panel pnlConfirm, pnlDone;
        protected Literal litParcel, litDetails, litDone;
        protected DropDownList ddlOutcome;
        protected HiddenField hfShipmentId;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Drivers are Staff. The per-shipment DriverID == UserId check below is what
            // actually stops one user confirming another's delivery.
            RequireRole("Admin", "Staff");
            if (!IsPostBack)
            {
                string wb = Request.QueryString["wb"];
                if (!string.IsNullOrEmpty(wb))
                {
                    txtScan.Text = wb;
                    LoadParcel(wb);
                }
            }
        }

        protected void btnScan_Click(object sender, EventArgs e)
        {
            LoadParcel(txtScan.Text.Trim());
        }

        private void LoadParcel(string waybill)
        {
            DataTable dt = Db.Query(
                @"SELECT ShipmentID, TrackingNumber, ReceiverName, DeliveryAddress, DestCity, DeliveryStatus, DriverID
                  FROM dbo.[Shipment] WHERE TrackingNumber = @t", "@t", waybill);

            if (dt.Rows.Count == 0)
            {
                pnlConfirm.Visible = false;
                lblScanMsg.CssClass = UiHelper.AlertClass("error") + " mt-2";
                lblScanMsg.Text = "No parcel found for that waybill.";
                return;
            }

            DataRow r = dt.Rows[0];
            if (r["DriverID"] == DBNull.Value || Db.ToInt(r["DriverID"]) != UserId)
            {
                pnlConfirm.Visible = false;
                lblScanMsg.CssClass = UiHelper.AlertClass("error") + " mt-2";
                lblScanMsg.Text = "This parcel is not assigned to you.";
                return;
            }

            string status = Db.ToStr(r["DeliveryStatus"]);
            if (status == "Delivered" || status == "Failed")
            {
                pnlConfirm.Visible = false;
                lblScanMsg.CssClass = UiHelper.AlertClass("warning") + " mt-2";
                lblScanMsg.Text = "This parcel has already been marked as " + status + ".";
                return;
            }
            if (status != "OutForDelivery")
            {
                pnlConfirm.Visible = false;
                lblScanMsg.CssClass = UiHelper.AlertClass("warning") + " mt-2";
                lblScanMsg.Text = "Please Confirm Pickup on your manifest before delivering this parcel.";
                return;
            }

            hfShipmentId.Value = Db.ToStr(r["ShipmentID"]);
            litParcel.Text = Server.HtmlEncode(Db.ToStr(r["TrackingNumber"]));
            litDetails.Text = Server.HtmlEncode("To " + Db.ToStr(r["ReceiverName"]) + " · " +
                              Db.ToStr(r["DeliveryAddress"]) + ", " + Db.ToStr(r["DestCity"]));

            // The expected recipient is already on the waybill - prefill it so the driver
            // only types something when somebody else actually signed.
            txtReceivedBy.Text = Db.ToStr(r["ReceiverName"]);
            pnlConfirm.Visible = true;
            lblScanMsg.Text = string.Empty;
        }

        protected void btnConfirm_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(hfShipmentId.Value, out int shipId)) return;

            // Re-check ownership and status at the write itself, not only when the parcel was
            // scanned into hfShipmentId - a stale postback (or a second driver's browser tab)
            // must not be able to confirm a delivery for a parcel it no longer owns, or one
            // already finalised.
            DataTable current = Db.Query(
                "SELECT DriverID, DeliveryStatus FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", shipId);
            if (current.Rows.Count == 0 ||
                current.Rows[0]["DriverID"] == DBNull.Value ||
                Db.ToInt(current.Rows[0]["DriverID"]) != UserId)
            {
                pnlConfirm.Visible = false;
                lblScanMsg.CssClass = UiHelper.AlertClass("error") + " mt-2";
                lblScanMsg.Text = "This parcel is not assigned to you.";
                return;
            }
            string currentStatus = Db.ToStr(current.Rows[0]["DeliveryStatus"]);
            if (currentStatus != "OutForDelivery")
            {
                pnlConfirm.Visible = false;
                lblScanMsg.CssClass = UiHelper.AlertClass("warning") + " mt-2";
                lblScanMsg.Text = "This parcel is no longer awaiting delivery confirmation.";
                return;
            }

            string outcome = ddlOutcome.SelectedValue;
            bool delivered = outcome == "Delivered";
            string receivedBy = txtReceivedBy.Text.Trim();
            string notes = txtNotes.Text.Trim();
            string remark = delivered
                ? "Delivered" + (receivedBy.Length > 0 ? " - received by " + receivedBy : "") + (notes.Length > 0 ? " (" + notes + ")" : "")
                : "Delivery failed" + (notes.Length > 0 ? " - " + notes : "");

            Db.NonQuery(
                @"UPDATE dbo.[Shipment]
                  SET DeliveryStatus = @st, LastStatusChange = GETDATE(),
                      DeliveryDate = CASE WHEN @dl = 1 THEN GETDATE() ELSE DeliveryDate END
                  WHERE ShipmentID = @id",
                "@st", outcome, "@dl", delivered ? 1 : 0, "@id", shipId);

            Db.NonQuery(
                @"INSERT INTO dbo.[TrackingHistory] (ShipmentID, DriverID, Location, Status, Remarks)
                  VALUES (@s, @d, 'Delivery Point', @st, @rm)",
                "@s", shipId, "@d", UserId, "@st", outcome, "@rm", remark);

            // Notify the sender.
            DataTable snd = Db.Query("SELECT SenderID, TrackingNumber FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", shipId);
            if (snd.Rows.Count > 0 && snd.Rows[0]["SenderID"] != DBNull.Value)
            {
                string tn = Db.ToStr(snd.Rows[0]["TrackingNumber"]);
                NotificationService.Send(Db.ToInt(snd.Rows[0]["SenderID"]), shipId,
                    delivered ? "Good news! Your parcel " + tn + " has been delivered."
                              : "We attempted delivery of parcel " + tn + " but it was unsuccessful. Our team will be in touch.",
                    delivered ? "Delivery" : "Delay");
            }

            // Everything that follows from this outcome - invoice, rating request, route
            // completion, driver release, or a failed-delivery re-queue - happens here so
            // nobody has to remember to do it. See Services/AutomationService.
            AutomationService.OnDeliveryOutcome(shipId, UserId, delivered, notes);

            pnlConfirm.Visible = false;
            pnlDone.Visible = true;
            litDone.Text = delivered
                ? "Delivery confirmed. The invoice has been raised automatically."
                : "Failed delivery recorded. Dispatch has been notified.";
        }
    }
}
