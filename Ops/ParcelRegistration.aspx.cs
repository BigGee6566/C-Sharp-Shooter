using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Ops
{
    public partial class ParcelRegistration : SecurePage
    {
        protected TextBox txtSender, txtSenderLookup, txtReceiver, txtPickup, txtDelivery, txtWeight, txtDescription;
        protected DropDownList ddlOrigin, ddlDest;
        protected Button btnRegister, btnLookupSender;
        protected Label lblStatus, lblSenderMatch;
        protected Panel pnlLabel, pnlNoLabel;
        protected Literal litWaybill, litRoute, litBarcodeVal;
        protected HiddenField hfSenderId;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Admin", "Staff");
            if (IsPostBack) return;

            SortingService.BindCities(ddlOrigin);
            SortingService.BindCities(ddlDest, "Cape Town");
        }

        private decimal Weight { get { return CourierService.ParseWeight(txtWeight.Text); } }

        /// <summary>
        /// Matches a walk-in sender to an existing customer account by email or phone.
        /// Linking the account is what lets the automatic invoice and every status
        /// notification reach them, exactly as they do for an online booking.
        /// </summary>
        protected void btnLookupSender_Click(object sender, EventArgs e)
        {
            string query = txtSenderLookup.Text.Trim();
            hfSenderId.Value = string.Empty;

            if (query.Length == 0)
            {
                lblSenderMatch.CssClass = "block text-xs mt-1 text-gray-500";
                lblSenderMatch.Text = "Walk-in (no account) — this parcel will not send notifications.";
                return;
            }

            DataTable dt = Db.Query(
                @"SELECT TOP 1 UserID, FullName FROM dbo.[User]
                  WHERE Role = 'Customer' AND (Email = @q OR PhoneNumber = @q)", "@q", query);

            if (dt.Rows.Count == 0)
            {
                lblSenderMatch.CssClass = UiHelper.AlertClass("warning") + " mt-1";
                lblSenderMatch.Text = "No account found — registering as a walk-in.";
                return;
            }

            hfSenderId.Value = Db.ToStr(dt.Rows[0]["UserID"]);
            if (txtSender.Text.Trim().Length == 0) txtSender.Text = Db.ToStr(dt.Rows[0]["FullName"]);

            lblSenderMatch.CssClass = UiHelper.AlertClass("success") + " mt-1";
            lblSenderMatch.Text = "Matched: " + Server.HtmlEncode(Db.ToStr(dt.Rows[0]["FullName"])) +
                                  " — they will be invoiced and notified automatically.";
        }

        protected void btnRegister_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            if (txtSender.Text.Trim().Length == 0 || txtReceiver.Text.Trim().Length == 0 ||
                txtDelivery.Text.Trim().Length == 0)
            {
                lblStatus.CssClass = UiHelper.AlertClass("error");
                lblStatus.Text = "Please complete the sender, recipient and delivery address.";
                return;
            }

            string tracking = CourierService.GenerateTrackingNumber();
            decimal cost = CourierService.CalculateQuote(ddlOrigin.SelectedValue, ddlDest.SelectedValue, Weight);
            object senderId = int.TryParse(hfSenderId.Value, out int sid) ? (object)sid : DBNull.Value;

            int shipId = Db.Insert(
                @"INSERT INTO dbo.[Shipment]
                    (TrackingNumber, Barcode, SenderID, ReceiverName, PickupAddress, DeliveryAddress,
                     OriginCity, DestCity, ParcelWeight, ParcelDescription, Cost,
                     DeliveryStatus, WarehouseStatus, LastStatusChange)
                  VALUES (@t, @t, @sid, @rn, @pu, @da, @o, @d, @w, @desc, @c, 'Registered', 'Received', GETDATE())",
                "@t", tracking, "@sid", senderId, "@rn", txtReceiver.Text.Trim(),
                "@pu", txtPickup.Text.Trim(), "@da", txtDelivery.Text.Trim(),
                "@o", ddlOrigin.SelectedValue, "@d", ddlDest.SelectedValue,
                "@w", Weight, "@desc", txtDescription.Text.Trim(), "@c", cost);

            // A matched customer now gets the same notifications as an online booking.
            if (senderId != DBNull.Value)
            {
                NotificationService.Send((int)senderId, shipId,
                    "Your parcel has been registered at our counter. Waybill " + tracking + ".", "Registration");
            }

            Db.NonQuery(
                @"INSERT INTO dbo.[TrackingHistory] (ShipmentID, AdminID, Location, Status, Remarks)
                  VALUES (@s, @a, @loc, 'Registered', 'Parcel registered at intake counter')",
                "@s", shipId, "@a", UserId, "@loc", ddlOrigin.SelectedValue + " Depot");

            pnlLabel.Visible = true;
            pnlNoLabel.Visible = false;
            litWaybill.Text = tracking;
            // Rendered inside a JavaScript string literal on the page.
            litBarcodeVal.Text = UiHelper.JsEnc(tracking);
            litRoute.Text = ddlOrigin.SelectedValue + " → " + ddlDest.SelectedValue + " · " + Weight.ToString("0.##") + " kg";

            lblStatus.CssClass = UiHelper.AlertClass("success");
            lblStatus.Text = "Parcel " + tracking + " registered successfully.";

            // Reset inputs for the next parcel.
            txtSender.Text = txtSenderLookup.Text = txtReceiver.Text = txtPickup.Text =
                txtDelivery.Text = txtDescription.Text = string.Empty;
            txtWeight.Text = "1";
            hfSenderId.Value = string.Empty;
            lblSenderMatch.Text = string.Empty;
        }
    }
}
