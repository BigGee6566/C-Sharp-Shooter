using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.CustomerPortal
{
    /// <summary>
    /// Individual-customer payment-before-dispatch page. Reached from BookShipment after an
    /// Individual client confirms a booking, or from My Invoices for a shipment still
    /// awaiting payment.
    ///
    /// All amounts, ownership and eligibility are re-derived server-side in
    /// PaymentService.PayForShipment - this page only collects the chosen payment method and
    /// displays the result. The shipment id in the query string is just a lookup key, never a
    /// trusted claim: PaymentService rejects it unless SenderID matches the logged-in user.
    /// </summary>
    public partial class Payment : SecurePage
    {
        protected HiddenField hfIdempotencyKey;
        protected HiddenField hfShipmentId;

        protected Panel pnlNotEligible;
        protected System.Web.UI.WebControls.Literal litNotEligibleMsg;

        protected Panel pnlPay;
        protected System.Web.UI.WebControls.Literal litTracking;
        protected System.Web.UI.WebControls.Literal litInvoiceNumber;
        protected System.Web.UI.WebControls.Literal litRoute;
        protected System.Web.UI.WebControls.Literal litAmount;
        protected Label lblStatus;
        protected DropDownList ddlMethod;
        protected Button btnPay;

        protected Panel pnlReceipt;
        protected System.Web.UI.WebControls.Literal litReceiptTracking;
        protected System.Web.UI.WebControls.Literal litReceiptInvoice;
        protected System.Web.UI.WebControls.Literal litReceiptReference;
        protected System.Web.UI.WebControls.Literal litReceiptAmount;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Customer");

            int shipmentId;
            if (!int.TryParse(Request.QueryString["shipmentId"], out shipmentId) || shipmentId <= 0)
            {
                ShowNotEligible("No shipment was specified.");
                return;
            }

            if (!IsPostBack)
            {
                hfShipmentId.Value = shipmentId.ToString();
                hfIdempotencyKey.Value = Guid.NewGuid().ToString("N");
                LoadShipment(shipmentId);
            }
        }

        /// <summary>
        /// Loads the shipment for display only. Ownership and PaymentGateStatus are
        /// re-checked again, independently, inside PaymentService when the customer actually
        /// pays - this lookup existing is not itself a security boundary.
        /// </summary>
        private void LoadShipment(int shipmentId)
        {
            DataTable dt = Db.Query(
                @"SELECT s.SenderID, s.TrackingNumber, s.OriginCity, s.DestCity, s.PaymentGateStatus,
                         p.InvoiceNumber, p.Amount, p.PaymentStatus
                  FROM dbo.[Shipment] s
                  LEFT JOIN dbo.[Payment] p ON p.ShipmentID = s.ShipmentID
                  WHERE s.ShipmentID = @s",
                "@s", shipmentId);

            if (dt.Rows.Count == 0 || Db.ToInt(dt.Rows[0]["SenderID"]) != UserId)
            {
                ShowNotEligible("That shipment could not be found.");
                return;
            }

            DataRow row = dt.Rows[0];
            string gateStatus = Db.ToStr(row["PaymentGateStatus"]);
            if (gateStatus == "PaymentSuccessful")
            {
                ShowNotEligible("This shipment has already been paid.");
                return;
            }
            if (gateStatus != "PaymentRequired" && gateStatus != "PaymentPending")
            {
                ShowNotEligible("This shipment does not require online payment.");
                return;
            }

            litTracking.Text = UiHelper.Enc(row["TrackingNumber"]);
            litInvoiceNumber.Text = UiHelper.Enc(row["InvoiceNumber"]);
            litRoute.Text = UiHelper.Enc(row["OriginCity"]) + " &rarr; " + UiHelper.Enc(row["DestCity"]);
            litAmount.Text = "R " + Db.ToDecimal(row["Amount"]).ToString("N2");
        }

        protected void btnPay_Click(object sender, EventArgs e)
        {
            int shipmentId;
            if (!int.TryParse(hfShipmentId.Value, out shipmentId))
            {
                ShowNotEligible("No shipment was specified.");
                return;
            }

            PaymentService.PaymentOutcome outcome = PaymentService.PayForShipment(
                shipmentId, UserId, ddlMethod.SelectedValue, null, hfIdempotencyKey.Value);

            if (outcome.Success)
            {
                NotificationService.Send(UserId, shipmentId,
                    "Payment of R " + outcome.Amount.ToString("F2") + " received for invoice " +
                    outcome.InvoiceNumber + ". Your parcel is confirmed for dispatch.", "System");

                litReceiptTracking.Text = UiHelper.Enc(Db.ToStr(Db.Scalar(
                    "SELECT TrackingNumber FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", shipmentId)));
                litReceiptInvoice.Text = UiHelper.Enc(outcome.InvoiceNumber);
                litReceiptReference.Text = UiHelper.Enc(outcome.ReferenceNumber);
                litReceiptAmount.Text = "R " + outcome.Amount.ToString("N2");

                pnlPay.Visible = false;
                pnlReceipt.Visible = true;
                return;
            }

            // Failed: mint a fresh idempotency key so a genuine Retry click is a new attempt,
            // not a resubmission of the failed one (which PaymentService would just echo back).
            hfIdempotencyKey.Value = Guid.NewGuid().ToString("N");
            lblStatus.CssClass = UiHelper.AlertClass("error") + " mt-1";
            lblStatus.Text = outcome.Message;
        }

        private void ShowNotEligible(string message)
        {
            pnlPay.Visible = false;
            pnlNotEligible.Visible = true;
            litNotEligibleMsg.Text = UiHelper.Enc(message);
        }
    }
}
