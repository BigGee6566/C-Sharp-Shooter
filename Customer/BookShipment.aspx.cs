using System;
using System.Web.UI;

namespace LogistiCoreV5
{
    public partial class BookShipment : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
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
            // Populate Step 3 Order Summary
            string size = hfSelectedSize.Value;
            lblSummarySize.Text = size;
            lblSummaryPickup.Text = string.IsNullOrEmpty(txtPickupCity.Text) ? "KuGompo" : txtPickupCity.Text;
            lblSummaryDelivery.Text = string.IsNullOrEmpty(txtDeliveryCity.Text) ? "KuGompo" : txtDeliveryCity.Text;

            switch (size)
            {
                case "Medium":
                    lblSummaryTotal.Text = "R 150.00";
                    break;
                case "Large":
                    lblSummaryTotal.Text = "R 250.00";
                    break;
                case "XLarge":
                    lblSummaryTotal.Text = "R 380.00";
                    break;
                default:
                    lblSummaryTotal.Text = "R 100.00";
                    break;
            }

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
            // 1. Generate unique tracking number
            Random rnd = new Random();
            string trackingNo = "LC-" + rnd.Next(10000000, 99999999).ToString() + "ZA";
            lblWaybillTracking.Text = trackingNo;

            // 2. Set Barcode image URL (using Tec-IT Barcode API)
            imgBarcode.ImageUrl = $"https://barcode.tec-it.com/barcode.ashx?data={trackingNo}&code=Code128&translate-esc=false";

            // 3. Populate Waybill dates & details
            lblWaybillDate.Text = DateTime.Now.ToString("dd MMM yyyy, HH:mm");
            lblWaybillSize.Text = hfSelectedSize.Value;
            lblWaybillAmount.Text = lblSummaryTotal.Text;

            // 4. Populate Addresses from Step 2 Inputs
            lblWaybillPickupStreet.Text = txtPickupStreet.Text;
            lblWaybillPickupCity.Text = txtPickupCity.Text;
            lblWaybillPickupPostal.Text = txtPickupPostal.Text;

            lblWaybillDeliveryStreet.Text = txtDeliveryStreet.Text;
            lblWaybillDeliveryCity.Text = txtDeliveryCity.Text;
            lblWaybillDeliveryPostal.Text = txtDeliveryPostal.Text;

            // 5. Move to Confirmation View
            mvBookShipment.SetActiveView(vwConfirmation);
            UpdateWizardIndicators(4);
        }

        private void UpdateWizardIndicators(int activeStep)
        {
            string activeClass = "w-8 h-8 rounded-full bg-primary text-on-primary flex items-center justify-center font-bold text-sm";
            string inactiveClass = "w-8 h-8 rounded-full bg-surface-container-highest text-on-surface-variant flex items-center justify-center font-bold text-sm";

            pnlStep1Indicator.CssClass = (activeStep == 1) ? activeClass : inactiveClass;
            pnlStep2Indicator.CssClass = (activeStep == 2) ? activeClass : inactiveClass;
            pnlStep3Indicator.CssClass = (activeStep == 3) ? activeClass : inactiveClass;
            pnlStep4Indicator.CssClass = (activeStep == 4) ? activeClass : inactiveClass;
        }
    }
}