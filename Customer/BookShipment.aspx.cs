using System;
using System.Text;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.CustomerPortal
{
    /// <summary>
    /// Four-step booking wizard: parcel size, addresses, recipient, waybill.
    ///
    /// Pricing is not a fixed per-size table. The size band selected in step 1 sets the
    /// billed weight, and CourierService.CalculateQuote prices that weight over the chosen
    /// route - the same calculation the public quote tool on the home page uses, so a
    /// customer is never quoted two different prices for the same shipment.
    ///
    /// Confirming books the shipment and raises an unpaid invoice in dbo.[Payment]. An
    /// Individual client (Client.ClientType = 'Individual') is then sent straight to
    /// Customer\Payment.aspx to pay before the parcel is dispatchable - see
    /// Shipment.PaymentGateStatus and Ops\Warehouse.aspx.cs, which will not admit an unpaid
    /// Individual shipment into the sorting queue. A Business client is invoiced instead and
    /// settles later under My Invoices, so their shipment is not gated.
    /// </summary>
    public partial class BookShipment : SecurePage
    {
        protected MultiView mvBookShipment;
        protected View vwParcelDetails;
        protected View vwAddresses;
        protected View vwPayment;
        protected View vwConfirmation;

        protected Panel pnlStep1Indicator;
        protected Panel pnlStep2Indicator;
        protected Panel pnlStep3Indicator;
        protected Panel pnlStep4Indicator;

        protected HiddenField hfSelectedSize;
        protected HiddenField hfSubmitToken;

        protected TextBox txtPickupStreet;
        protected TextBox txtPickupCity;
        protected TextBox txtPickupPostal;
        protected TextBox txtDeliveryStreet;
        protected TextBox txtDeliveryCity;
        protected TextBox txtDeliveryPostal;
        protected Literal litCityOptions;
        protected Label lblAddrStatus;

        protected TextBox txtReceiver;
        protected TextBox txtReceiverPhone;
        protected TextBox txtDescription;
        protected Label lblStatus;

        protected Label lblSummarySize;
        protected Label lblSummaryPickup;
        protected Label lblSummaryDelivery;
        protected Label lblSummaryTotal;

        protected Repeater rptParcelSizes;

        protected Label lblWaybillTracking;
        protected Label lblWaybillDate;
        protected Label lblWaybillSize;
        protected Label lblWaybillDimensions;
        protected Label lblWaybillAmount;
        protected Label lblWaybillSenderName;
        protected Label lblWaybillReceiverName;
        protected Label lblWaybillPickupStreet;
        protected Label lblWaybillPickupCity;
        protected Label lblWaybillPickupPostal;
        protected Label lblWaybillDeliveryStreet;
        protected Label lblWaybillDeliveryCity;
        protected Label lblWaybillDeliveryPostal;

        /// <summary>The band the customer picked in step 1; never null.</summary>
        private ParcelSize SelectedSize
        {
            get { return ParcelSize.Find(hfSelectedSize.Value); }
        }

        /// <summary>
        /// Billed weight is the band maximum, so the price quoted on the card in step 1 is
        /// exactly the price the band costs. There is no separate entered weight to drift
        /// from it.
        /// </summary>
        private decimal BilledWeight
        {
            get { return SelectedSize.MaxWeightKg; }
        }

        private decimal Quote
        {
            get
            {
                return CourierService.CalculateQuote(
                    txtPickupCity.Text.Trim(), txtDeliveryCity.Text.Trim(), BilledWeight);
            }
        }

        /// <summary>
        /// Emits the checked attribute for the band currently held in the hidden field.
        ///
        /// The radios are plain HTML inputs, so their checked state is not carried in
        /// ViewState. Without this, returning from step 2 always re-rendered Small as
        /// selected while the hidden field still said Large, and the card disagreed with
        /// the order summary.
        /// </summary>
        protected string CheckedAttr(object code)
        {
            return string.Equals(Convert.ToString(code), SelectedSize.Code, StringComparison.OrdinalIgnoreCase)
                ? "checked=\"checked\""
                : string.Empty;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Customer");

            // Bound on every request, not just the first: the cards are plain HTML radios
            // whose checked state lives in the hidden field rather than ViewState, so the
            // highlight is re-derived each time the step is rendered.
            rptParcelSizes.DataSource = ParcelSize.All;
            rptParcelSizes.DataBind();

            // The city fields are now type-or-pick: a <datalist> offers the known service
            // cities as suggestions while still letting the customer type any other town we
            // deliver to. Rebuilt on every load so the options survive a postback.
            litCityOptions.Text = BuildCityOptions();

            if (!IsPostBack)
            {
                // Pre-fill collection from the address already on the account.
                txtPickupStreet.Text = Db.ToStr(Db.Scalar(
                    "SELECT Address FROM dbo.[User] WHERE UserID = @u", "@u", UserId));

                // A size selected on the landing page's quotation cards arrives here as a
                // plain code, e.g. ?size=Large. ParcelSize.Find falls back to Default for
                // anything unrecognised, so this is never trusted to be a real band on its
                // own - it only decides which card starts checked.
                string requestedSize = Request.QueryString["size"];
                if (!string.IsNullOrEmpty(requestedSize))
                {
                    hfSelectedSize.Value = ParcelSize.Find(requestedSize).Code;
                }

                UpdateWizardIndicators(1);
            }
        }

        protected void btnNextAddress_Click(object sender, EventArgs e)
        {
            mvBookShipment.SetActiveView(vwAddresses);
            UpdateWizardIndicators(2);
        }

        protected void btnBackToParcel_Click(object sender, EventArgs e)
        {
            mvBookShipment.SetActiveView(vwParcelDetails);
            UpdateWizardIndicators(1);
        }

        protected void btnNextPayment_Click(object sender, EventArgs e)
        {
            string pickupCity = txtPickupCity.Text.Trim();
            string deliveryCity = txtDeliveryCity.Text.Trim();

            // A blank city cannot be priced or sorted, and because the quote treats two equal
            // cities (here, two empty strings) as a local delivery, an empty box would
            // silently quote the cheaper local rate. Require both before moving on.
            if (pickupCity.Length == 0 || deliveryCity.Length == 0)
            {
                lblAddrStatus.CssClass = UiHelper.AlertClass("error") + " block mb-3";
                lblAddrStatus.Text = "Please enter both the collection and delivery city.";
                return;
            }
            lblAddrStatus.Text = string.Empty;
            lblAddrStatus.CssClass = "block mb-3";

            lblSummarySize.Text = SelectedSize.Name + " (" + SelectedSize.MaxWeightText.ToLowerInvariant() +
                                  ", " + SelectedSize.DimensionsText + ")";
            lblSummaryPickup.Text = pickupCity;
            lblSummaryDelivery.Text = deliveryCity;
            lblSummaryTotal.Text = "R " + Quote.ToString("N2");

            // Entering the confirm step: mint a one-time token so the Confirm POST can be
            // accepted exactly once. Re-issued each time the step is shown (e.g. after going
            // back to addresses and forward again) so the latest render always holds a live one.
            hfSubmitToken.Value = IssueSubmitToken("BookShipment");

            mvBookShipment.SetActiveView(vwPayment);
            UpdateWizardIndicators(3);
        }

        protected void btnBackToAddresses_Click(object sender, EventArgs e)
        {
            mvBookShipment.SetActiveView(vwAddresses);
            UpdateWizardIndicators(2);
        }

        protected void btnConfirmPayment_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            string pickupCity = txtPickupCity.Text.Trim();
            string deliveryCity = txtDeliveryCity.Text.Trim();

            if (txtPickupStreet.Text.Trim().Length == 0 ||
                txtDeliveryStreet.Text.Trim().Length == 0 ||
                pickupCity.Length == 0 || deliveryCity.Length == 0 ||
                txtReceiver.Text.Trim().Length == 0)
            {
                lblStatus.CssClass = UiHelper.AlertClass("error") + " mt-4";
                lblStatus.Text = "Please complete the collection, delivery and recipient details.";
                return;
            }

            // Consume the one-time token only after the details validate, so a correctable
            // error above leaves the token live for a genuine retry. A double-click or an
            // F5-resubmit arrives here carrying a spent token and is turned away before the
            // INSERT, so one Confirm can create at most one shipment + invoice.
            if (!ConsumeSubmitToken("BookShipment", hfSubmitToken.Value))
            {
                lblStatus.CssClass = UiHelper.AlertClass("info") + " mt-4";
                lblStatus.Text = "This booking has already been submitted. Check My Shipments before booking again.";
                return;
            }

            string trackingNumber = CourierService.GenerateTrackingNumber();
            decimal cost = Quote;

            string pickupAddress = Join(txtPickupStreet.Text, pickupCity, txtPickupPostal.Text);
            string deliveryAddress = Join(txtDeliveryStreet.Text, deliveryCity, txtDeliveryPostal.Text);

            ParcelSize size = SelectedSize;

            // Individual clients must pay before their parcel is dispatchable; Business
            // clients are invoiced and settle later under My Invoices, so their shipment is
            // never gated. Client.ClientType is looked up server-side - it is never taken
            // from the booking form.
            string clientType = Db.ToStr(Db.Scalar(
                "SELECT ClientType FROM dbo.[Client] WHERE UserID = @u", "@u", UserId));
            bool paymentRequired = !string.Equals(clientType, "Business", StringComparison.OrdinalIgnoreCase);
            string paymentGateStatus = paymentRequired ? "PaymentRequired" : "NotRequired";

            int shipmentId = Db.Insert(
                @"INSERT INTO dbo.[Shipment]
                    (TrackingNumber, Barcode, SenderID, ReceiverName, ReceiverPhone,
                     PickupAddress, DeliveryAddress, OriginCity, DestCity, ParcelWeight,
                     ParcelLengthCm, ParcelWidthCm, ParcelHeightCm,
                     ParcelDescription, Cost, DeliveryStatus, WarehouseStatus, PaymentGateStatus, LastStatusChange)
                  VALUES (@t, @t, @s, @rn, @rp, @pu, @da, @o, @d, @w,
                          @len, @wid, @hgt, @desc, @c,
                          'Registered', 'Pending', @pgs, GETDATE())",
                "@t", trackingNumber,
                "@s", UserId,
                "@rn", txtReceiver.Text.Trim(),
                "@rp", txtReceiverPhone.Text.Trim(),
                "@pu", pickupAddress,
                "@da", deliveryAddress,
                "@o", pickupCity,
                "@d", deliveryCity,
                "@w", size.MaxWeightKg,
                "@len", size.LengthCm,
                "@wid", size.WidthCm,
                "@hgt", size.HeightCm,
                "@desc", txtDescription.Text.Trim(),
                "@c", cost,
                "@pgs", paymentGateStatus);

            Db.NonQuery(
                @"INSERT INTO dbo.[TrackingHistory] (ShipmentID, Location, Status, Remarks)
                  VALUES (@s, @loc, 'Registered', 'Collection booked online by customer')",
                "@s", shipmentId, "@loc", pickupCity + " (Booking)");

            string invoiceNumber = CourierService.GenerateInvoiceNumber();
            Db.NonQuery(
                @"INSERT INTO dbo.[Payment] (ClientID, ShipmentID, InvoiceNumber, Amount, PaymentStatus)
                  VALUES (@c, @s, @inv, @a, 'Unpaid')",
                "@c", UserId, "@s", shipmentId, "@inv", invoiceNumber, "@a", cost);

            if (paymentRequired)
            {
                NotificationService.Send(UserId, shipmentId,
                    "Your collection has been booked. Waybill " + trackingNumber + ". Pay invoice " +
                    invoiceNumber + " (R " + cost.ToString("F2") + ") now to confirm dispatch.",
                    "Registration");

                // Individual bookings are not shippable until paid - go straight to payment
                // rather than showing a waybill that implies the parcel will be collected.
                Response.Redirect("~/Customer/Payment.aspx?shipmentId=" + shipmentId);
                return;
            }

            NotificationService.Send(UserId, shipmentId,
                "Your collection has been booked. Waybill " + trackingNumber + ". Invoice " +
                invoiceNumber + " for R " + cost.ToString("F2") + " is ready to pay in My Invoices.",
                "Registration");

            lblStatus.Text = string.Empty;
            lblStatus.CssClass = string.Empty;

            lblWaybillTracking.Text = trackingNumber;
            lblWaybillDate.Text = DateTime.Now.ToString("dd MMM yyyy, HH:mm");
            lblWaybillSize.Text = size.Name + " (" + size.MaxWeightText.ToLowerInvariant() + ")";
            lblWaybillDimensions.Text = size.DimensionsText;
            lblWaybillAmount.Text = "R " + cost.ToString("N2");
            lblWaybillSenderName.Text = FullName;
            lblWaybillReceiverName.Text = txtReceiver.Text.Trim();

            lblWaybillPickupStreet.Text = txtPickupStreet.Text.Trim();
            lblWaybillPickupCity.Text = pickupCity;
            lblWaybillPickupPostal.Text = txtPickupPostal.Text.Trim();

            lblWaybillDeliveryStreet.Text = txtDeliveryStreet.Text.Trim();
            lblWaybillDeliveryCity.Text = deliveryCity;
            lblWaybillDeliveryPostal.Text = txtDeliveryPostal.Text.Trim();

            // JsBarcode (loaded by the master) renders the waybill locally, so the number
            // is never sent to a third-party barcode service.
            ClientScript.RegisterStartupScript(GetType(), "waybillBarcode",
                "if (window.JsBarcode) { JsBarcode('#waybillBarcode', '" +
                UiHelper.JsEnc(trackingNumber) + "', { format: 'CODE128', displayValue: false, height: 60 }); }",
                true);

            mvBookShipment.SetActiveView(vwConfirmation);
            UpdateWizardIndicators(4);
        }

        /// <summary>
        /// Builds the &lt;option&gt; list for the city datalist from the single source of
        /// truth in SortingService. These are suggestions only - the customer may type any
        /// other town - so the list is never used to validate or constrain the entry.
        /// </summary>
        private static string BuildCityOptions()
        {
            var sb = new StringBuilder();
            foreach (string city in SortingService.Cities)
            {
                sb.Append("<option value=\"")
                  .Append(UiHelper.Enc(city))
                  .Append("\"></option>");
            }
            return sb.ToString();
        }

        private static string Join(string street, string city, string postal)
        {
            string result = street.Trim();
            if (city.Length > 0) result += ", " + city;
            if (postal.Trim().Length > 0) result += ", " + postal.Trim();
            return result;
        }

        private void UpdateWizardIndicators(int activeStep)
        {
            const string active =
                "w-8 h-8 rounded-full bg-primary text-on-primary flex items-center justify-center font-bold text-label-md";
            const string inactive =
                "w-8 h-8 rounded-full bg-surface-container-highest text-on-surface-variant flex items-center justify-center font-bold text-label-md";

            pnlStep1Indicator.CssClass = activeStep == 1 ? active : inactive;
            pnlStep2Indicator.CssClass = activeStep == 2 ? active : inactive;
            pnlStep3Indicator.CssClass = activeStep == 3 ? active : inactive;
            pnlStep4Indicator.CssClass = activeStep == 4 ? active : inactive;
        }
    }
}
