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

        // Method-specific detail inputs. Card number / CVV have EnableViewState="false" in
        // the markup, so they are never round-tripped or persisted - only a masked summary is.
        protected TextBox txtCardName;
        protected TextBox txtCardNumber;
        protected TextBox txtCardExpiry;
        protected TextBox txtCardCvv;
        protected DropDownList ddlBank;
        protected TextBox txtEftHolder;
        protected TextBox txtEftAccount;
        protected TextBox txtEftBranch;
        protected DropDownList ddlWallet;
        protected TextBox txtWalletPhone;

        protected Panel pnlReceipt;
        protected System.Web.UI.WebControls.Literal litReceiptTracking;
        protected System.Web.UI.WebControls.Literal litReceiptInvoice;
        protected System.Web.UI.WebControls.Literal litReceiptReference;
        protected System.Web.UI.WebControls.Literal litReceiptMethod;
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

            string method = ddlMethod.SelectedValue;

            // Validate the chosen method's detail fields server-side (never trusting the
            // client-side check) and derive a masked, non-sensitive descriptor to record.
            string methodDetails;
            bool forceDecline;
            string validationError = ValidateMethod(method, out methodDetails, out forceDecline);
            if (validationError != null)
            {
                lblStatus.CssClass = UiHelper.AlertClass("error") + " mt-1";
                lblStatus.Text = validationError;
                return;
            }

            // A known sandbox "decline" test card drives the failure path; every other input
            // succeeds. Real providers would return this from the gateway instead.
            string testNote = forceDecline ? "FAIL" : null;

            PaymentService.PaymentOutcome outcome = PaymentService.PayForShipment(
                shipmentId, UserId, method, testNote, hfIdempotencyKey.Value, methodDetails);

            if (outcome.Success)
            {
                NotificationService.Send(UserId, shipmentId,
                    "Payment of R " + outcome.Amount.ToString("F2") + " received for invoice " +
                    outcome.InvoiceNumber + ". Your parcel is confirmed for dispatch.", "System");

                litReceiptTracking.Text = UiHelper.Enc(Db.ToStr(Db.Scalar(
                    "SELECT TrackingNumber FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", shipmentId)));
                litReceiptInvoice.Text = UiHelper.Enc(outcome.InvoiceNumber);
                litReceiptReference.Text = UiHelper.Enc(outcome.ReferenceNumber);
                litReceiptMethod.Text = UiHelper.Enc(methodDetails);
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

        /// <summary>
        /// Validates the detail fields for <paramref name="method"/> and produces a masked,
        /// non-sensitive <paramref name="details"/> string to record - never the raw card
        /// number or CVV. Returns null when valid, otherwise a user-facing error message.
        /// <paramref name="forceDecline"/> is set for a known sandbox decline test card so the
        /// failure path stays exercisable from the UI.
        /// </summary>
        private string ValidateMethod(string method, out string details, out bool forceDecline)
        {
            details = method;
            forceDecline = false;

            switch (method)
            {
                case "Card":
                {
                    string name = txtCardName.Text.Trim();
                    string number = DigitsOnly(txtCardNumber.Text);
                    string cvv = DigitsOnly(txtCardCvv.Text);

                    if (name.Length < 2) return "Please enter the cardholder name.";
                    if (number.Length < 13 || number.Length > 19 || !PassesLuhn(number))
                        return "Please enter a valid card number.";
                    if (!IsFutureExpiry(txtCardExpiry.Text.Trim()))
                        return "Please enter a valid, unexpired expiry date (MM/YY).";
                    if (cvv.Length < 3 || cvv.Length > 4) return "Please enter the 3 or 4 digit CVV.";

                    forceDecline = number == "4000000000000002";   // standard sandbox decline card
                    details = "Card ending " + number.Substring(number.Length - 4);
                    return null;
                }
                case "EFT":
                {
                    string holder = txtEftHolder.Text.Trim();
                    string acct = DigitsOnly(txtEftAccount.Text);
                    string branch = DigitsOnly(txtEftBranch.Text);

                    if (holder.Length < 2) return "Please enter the account holder name.";
                    if (acct.Length < 6 || acct.Length > 20) return "Please enter a valid account number.";
                    if (branch.Length < 4 || branch.Length > 8) return "Please enter a valid branch code.";

                    details = "EFT " + ddlBank.SelectedValue + " acct ****" + acct.Substring(acct.Length - 4);
                    return null;
                }
                case "Wallet":
                {
                    string phone = DigitsOnly(txtWalletPhone.Text);
                    if (phone.Length < 9 || phone.Length > 15)
                        return "Please enter the mobile number for your wallet.";
                    string masked = "***" + phone.Substring(phone.Length - 4);
                    details = ddlWallet.SelectedValue + " (" + masked + ")";
                    return null;
                }
                default:
                    return "Please choose a payment method.";
            }
        }

        private static string DigitsOnly(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char c in s) if (c >= '0' && c <= '9') sb.Append(c);
            return sb.ToString();
        }

        private static bool PassesLuhn(string digits)
        {
            int sum = 0; bool alt = false;
            for (int i = digits.Length - 1; i >= 0; i--)
            {
                int n = digits[i] - '0';
                if (alt) { n *= 2; if (n > 9) n -= 9; }
                sum += n; alt = !alt;
            }
            return digits.Length > 0 && sum % 10 == 0;
        }

        private static bool IsFutureExpiry(string mmYY)
        {
            if (string.IsNullOrEmpty(mmYY)) return false;
            string[] parts = mmYY.Split('/');
            if (parts.Length != 2) return false;
            int mm, yy;
            if (!int.TryParse(parts[0], out mm) || !int.TryParse(parts[1], out yy)) return false;
            if (mm < 1 || mm > 12) return false;
            // Card is valid through the last day of its expiry month.
            DateTime endOfMonth = new DateTime(2000 + yy, mm, 1).AddMonths(1).AddDays(-1);
            return endOfMonth >= DateTime.Today;
        }

        private void ShowNotEligible(string message)
        {
            pnlPay.Visible = false;
            pnlNotEligible.Visible = true;
            litNotEligibleMsg.Text = UiHelper.Enc(message);
        }
    }
}
