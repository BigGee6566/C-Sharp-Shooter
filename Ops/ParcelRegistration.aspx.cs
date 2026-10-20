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
        protected HiddenField hfSenderId, hfSubmitToken;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireOperationsStaff();
            if (IsPostBack) return;

            SortingService.BindCities(ddlOrigin);
            SortingService.BindCities(ddlDest, "Cape Town");
            hfSubmitToken.Value = IssueSubmitToken(TokenScope);
        }

        private const string TokenScope = "ParcelRegistration";

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
                lblSenderMatch.Text = "Walk-in (no account) &mdash; this parcel will not send notifications.";
                return;
            }

            // Match on the canonical forms, and never guess: two customers sharing a phone
            // number would otherwise put this parcel (and its recipient's details) on the
            // wrong account.
            string phone = InputValidator.NormalizePhone(query);
            DataTable dt = Db.Query(
                @"SELECT TOP 2 UserID, FullName FROM dbo.[User]
                  WHERE Role = 'Customer' AND (Email = @e OR (@p <> '' AND PhoneNumber = @p))",
                "@e", InputValidator.NormalizeEmail(query), "@p", InputValidator.IsValidPhone(phone) ? phone : string.Empty);

            if (dt.Rows.Count == 0)
            {
                lblSenderMatch.CssClass = UiHelper.AlertClass("warning") + " mt-1";
                lblSenderMatch.Text = "No account found &mdash; registering as a walk-in.";
                return;
            }
            if (dt.Rows.Count > 1)
            {
                lblSenderMatch.CssClass = UiHelper.AlertClass("warning") + " mt-1";
                lblSenderMatch.Text = "More than one customer matches &mdash; search by their email address instead.";
                return;
            }

            hfSenderId.Value = Db.ToStr(dt.Rows[0]["UserID"]);
            if (txtSender.Text.Trim().Length == 0) txtSender.Text = Db.ToStr(dt.Rows[0]["FullName"]);

            lblSenderMatch.CssClass = UiHelper.AlertClass("success") + " mt-1";
            lblSenderMatch.Text = "Matched: " + Server.HtmlEncode(Db.ToStr(dt.Rows[0]["FullName"])) +
                                  " &mdash; they will be invoiced and notified automatically.";
        }

        protected void btnRegister_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            decimal weight;
            if (!InputValidator.TryParseWeight(txtWeight.Text, out weight, IntakeService.MaxWeightKg))
            {
                // Never fall back to a default: a wrong weight means a wrong price on the invoice.
                ShowError("Please enter the parcel weight in kg (above 0 and up to " + IntakeService.MaxWeightKg.ToString("0") + ").");
                return;
            }
            if (InputValidator.NormalizeText(txtSender.Text).Length == 0)
            {
                ShowError("Please enter the sender's name.");
                return;
            }

            // A matched sender must still be a customer now - the hidden field is client data.
            // If not, stop rather than quietly registering a walk-in the customer never sees.
            int? senderId = null;
            int sid;
            if (int.TryParse(hfSenderId.Value, out sid))
            {
                if (Db.ToInt(Db.Scalar("SELECT COUNT(*) FROM dbo.[User] WHERE UserID = @u AND Role = 'Customer'", "@u", sid)) != 1)
                {
                    hfSenderId.Value = string.Empty;
                    lblSenderMatch.Text = string.Empty;
                    ShowError("The matched customer account is no longer available. Look the sender up again, or clear the search to register a walk-in.");
                    return;
                }
                senderId = sid;
            }

            var draft = new ShipmentDraft
            {
                SenderId = senderId,
                ReceiverName = txtReceiver.Text,
                PickupAddress = txtPickup.Text,
                DeliveryAddress = txtDelivery.Text,
                OriginCity = ddlOrigin.SelectedValue,
                DestCity = ddlDest.SelectedValue,
                WeightKg = weight,
                Description = txtDescription.Text,
                Cost = CourierService.CalculateQuote(ddlOrigin.SelectedValue, ddlDest.SelectedValue, weight),
                WarehouseStatus = "Received",
                PaymentGateStatus = "NotRequired",
                RegisteredByStaffId = UserId,
                TrackingLocation = ddlOrigin.SelectedValue + " Depot",
                // There is no sender-name column; keep who handed it in on the record.
                TrackingRemark = "Parcel registered at intake counter (sender: " + InputValidator.NormalizeText(txtSender.Text, 150) + ")"
            };
            IntakeService.Normalize(draft);
            string problem = IntakeService.Validate(draft);
            if (problem != null)
            {
                ShowError(problem);
                return;
            }

            // Consumed only once the details are good, so a correctable mistake keeps it live.
            if (!ConsumeSubmitToken(TokenScope, hfSubmitToken.Value))
            {
                lblStatus.CssClass = UiHelper.AlertClass("info");
                lblStatus.Text = "That parcel has already been registered. Check the list before registering it again.";
                return;
            }

            CreatedShipment created;
            try
            {
                created = IntakeService.Create(draft);
            }
            catch (ValidationFailedException ex)
            {
                hfSubmitToken.Value = IssueSubmitToken(TokenScope);
                ShowError(ex.Message);
                return;
            }
            catch (Exception ex)
            {
                // Rolled back: nothing was registered. A fresh token lets the clerk retry safely.
                System.Diagnostics.Trace.TraceError("Counter registration failed for staff " + UserId + ": " + ex);
                hfSubmitToken.Value = IssueSubmitToken(TokenScope);
                ShowError("Nothing was registered - something went wrong on our side. Please try again.");
                return;
            }
            string tracking = created.TrackingNumber;
            hfSubmitToken.Value = IssueSubmitToken(TokenScope);   // ready for the next parcel

            // A matched customer now gets the same notifications as an online booking.
            if (senderId != null)
            {
                NotificationService.TrySend(senderId.Value, created.ShipmentId,
                    "Your parcel has been registered at our counter. Waybill " + tracking + ".", "Registration");
            }

            pnlLabel.Visible = true;
            pnlNoLabel.Visible = false;
            litWaybill.Text = tracking;
            // Rendered inside a JavaScript string literal on the page.
            litBarcodeVal.Text = UiHelper.JsEnc(tracking);
            litRoute.Text = ddlOrigin.SelectedValue + " &rarr; " + ddlDest.SelectedValue + " &middot; " + weight.ToString("0.##") + " kg";

            lblStatus.CssClass = UiHelper.AlertClass("success");
            lblStatus.Text = "Parcel " + tracking + " registered successfully.";

            // Reset inputs for the next parcel.
            txtSender.Text = txtSenderLookup.Text = txtReceiver.Text = txtPickup.Text =
                txtDelivery.Text = txtDescription.Text = string.Empty;
            txtWeight.Text = "1";
            hfSenderId.Value = string.Empty;
            lblSenderMatch.Text = string.Empty;
        }

        private void ShowError(string message)
        {
            lblStatus.CssClass = UiHelper.AlertClass("error");
            lblStatus.Text = UiHelper.Enc(message);
        }
    }
}
