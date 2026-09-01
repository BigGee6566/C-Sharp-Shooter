using System;
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
    /// Confirming does not take a card payment: it books the shipment and raises an unpaid
    /// invoice in dbo.[Payment], which the customer settles under My Invoices.
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

        protected TextBox txtPickupStreet;
        protected DropDownList ddlPickupCity;
        protected TextBox txtPickupPostal;
        protected TextBox txtDeliveryStreet;
        protected DropDownList ddlDeliveryCity;
        protected TextBox txtDeliveryPostal;

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
                    ddlPickupCity.SelectedValue, ddlDeliveryCity.SelectedValue, BilledWeight);
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

            if (!IsPostBack)
            {
                SortingService.BindCities(ddlPickupCity);
                SortingService.BindCities(ddlDeliveryCity, "Cape Town");

                // Pre-fill collection from the address already on the account.
                txtPickupStreet.Text = Db.ToStr(Db.Scalar(
                    "SELECT Address FROM dbo.[User] WHERE UserID = @u", "@u", UserId));

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
            lblSummarySize.Text = SelectedSize.Name + " (" + SelectedSize.MaxWeightText.ToLowerInvariant() +
                                  ", " + SelectedSize.DimensionsText + ")";
            lblSummaryPickup.Text = ddlPickupCity.SelectedValue;
            lblSummaryDelivery.Text = ddlDeliveryCity.SelectedValue;
            lblSummaryTotal.Text = "R " + Quote.ToString("N2");

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

            if (txtPickupStreet.Text.Trim().Length == 0 ||
                txtDeliveryStreet.Text.Trim().Length == 0 ||
                txtReceiver.Text.Trim().Length == 0)
            {
                lblStatus.CssClass = UiHelper.AlertClass("error") + " mt-4";
                lblStatus.Text = "Please complete the collection, delivery and recipient details.";
                return;
            }

            string trackingNumber = CourierService.GenerateTrackingNumber();
            decimal cost = Quote;

            string pickupAddress = Join(txtPickupStreet.Text, ddlPickupCity.SelectedValue, txtPickupPostal.Text);
            string deliveryAddress = Join(txtDeliveryStreet.Text, ddlDeliveryCity.SelectedValue, txtDeliveryPostal.Text);

            ParcelSize size = SelectedSize;

            int shipmentId = Db.Insert(
                @"INSERT INTO dbo.[Shipment]
                    (TrackingNumber, Barcode, SenderID, ReceiverName, ReceiverPhone,
                     PickupAddress, DeliveryAddress, OriginCity, DestCity, ParcelWeight,
                     ParcelLengthCm, ParcelWidthCm, ParcelHeightCm,
                     ParcelDescription, Cost, DeliveryStatus, WarehouseStatus, LastStatusChange)
                  VALUES (@t, @t, @s, @rn, @rp, @pu, @da, @o, @d, @w,
                          @len, @wid, @hgt, @desc, @c,
                          'Registered', 'Pending', GETDATE())",
                "@t", trackingNumber,
                "@s", UserId,
                "@rn", txtReceiver.Text.Trim(),
                "@rp", txtReceiverPhone.Text.Trim(),
                "@pu", pickupAddress,
                "@da", deliveryAddress,
                "@o", ddlPickupCity.SelectedValue,
                "@d", ddlDeliveryCity.SelectedValue,
                "@w", size.MaxWeightKg,
                "@len", size.LengthCm,
                "@wid", size.WidthCm,
                "@hgt", size.HeightCm,
                "@desc", txtDescription.Text.Trim(),
                "@c", cost);

            Db.NonQuery(
                @"INSERT INTO dbo.[TrackingHistory] (ShipmentID, Location, Status, Remarks)
                  VALUES (@s, @loc, 'Registered', 'Collection booked online by customer')",
                "@s", shipmentId, "@loc", ddlPickupCity.SelectedValue + " (Booking)");

            string invoiceNumber = CourierService.GenerateInvoiceNumber();
            Db.NonQuery(
                @"INSERT INTO dbo.[Payment] (ClientID, ShipmentID, InvoiceNumber, Amount, PaymentStatus)
                  VALUES (@c, @s, @inv, @a, 'Unpaid')",
                "@c", UserId, "@s", shipmentId, "@inv", invoiceNumber, "@a", cost);

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
            lblWaybillPickupCity.Text = ddlPickupCity.SelectedValue;
            lblWaybillPickupPostal.Text = txtPickupPostal.Text.Trim();

            lblWaybillDeliveryStreet.Text = txtDeliveryStreet.Text.Trim();
            lblWaybillDeliveryCity.Text = ddlDeliveryCity.SelectedValue;
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
