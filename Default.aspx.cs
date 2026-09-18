using System;
using System.Data;
using System.Text;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp
{
    /// <summary>
    /// Public landing page. The two tools it offers - waybill tracking and a price quote -
    /// are deliberately available without signing in, so this derives from BasePage (which
    /// binds ViewState to the session) rather than SecurePage.
    /// </summary>
    public partial class Default : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Bound every request (not just !IsPostBack): the cards are plain HTML radios
            // whose checked state lives in hfQuoteSize rather than ViewState, exactly like
            // Customer/BookShipment.aspx step 1, which this reuses.
            rptQuoteSizes.DataSource = ParcelSize.All;
            rptQuoteSizes.DataBind();
        }

        /// <summary>Emits the checked attribute for the band currently held in hfQuoteSize.</summary>
        protected string QuoteCheckedAttr(object code)
        {
            string selected = string.IsNullOrEmpty(hfQuoteSize.Value) ? ParcelSize.Default.Code : hfQuoteSize.Value;
            return string.Equals(Convert.ToString(code), selected, StringComparison.OrdinalIgnoreCase)
                ? "checked=\"checked\""
                : string.Empty;
        }

        /// <summary>
        /// Carries the selected card into the booking wizard as a plain size code - the same
        /// code BookShipment already re-validates server-side via ParcelSize.Find and never
        /// trusts to match on its own, so nothing sensitive crosses this redirect. An
        /// anonymous visitor is bounced to Login first (SecurePage.RequireRole's normal
        /// returnUrl handling) and lands on the booking page with this selection intact.
        /// </summary>
        protected void btnContinueToBooking_Click(object sender, EventArgs e)
        {
            string code = ParcelSize.Find(hfQuoteSize.Value).Code;
            Response.Redirect("~/Customer/BookShipment.aspx?size=" + Server.UrlEncode(code));
        }

        protected void btnTrack_Click(object sender, EventArgs e)
        {
            string waybill = txtTrackingNumber.Text.Trim();

            if (string.IsNullOrEmpty(waybill))
            {
                lblTrackResult.CssClass = UiHelper.AlertClass("error") + " mt-4";
                lblTrackResult.Text = "Please enter a valid waybill or tracking number.";
                return;
            }

            DataTable shipment = Db.Query(
                @"SELECT ShipmentID, TrackingNumber, DeliveryStatus, OriginCity, DestCity,
                         DeliveryAddress, ShipmentDate, DeliveryDate
                  FROM dbo.[Shipment] WHERE TrackingNumber = @t", "@t", waybill);

            if (shipment.Rows.Count == 0)
            {
                lblTrackResult.CssClass = UiHelper.AlertClass("warning") + " mt-4";
                lblTrackResult.Text =
                    "<span class='material-symbols-outlined mr-1'>error</span> We couldn't find a parcel with waybill <strong>" +
                    Server.HtmlEncode(waybill) + "</strong>. Please check the number and try again.";
                return;
            }

            DataRow row = shipment.Rows[0];
            int shipmentId = Db.ToInt(row["ShipmentID"]);
            string status = Db.ToStr(row["DeliveryStatus"]);

            DataTable latest = Db.Query(
                @"SELECT TOP 1 Location, Status, Remarks, [Timestamp]
                  FROM dbo.[TrackingHistory] WHERE ShipmentID = @s ORDER BY [Timestamp] DESC",
                "@s", shipmentId);

            var sb = new StringBuilder();
            sb.Append("<div class='flex items-center justify-between font-label-md text-label-md text-on-surface mb-2'>")
              .Append("<span>Status for ").Append(Server.HtmlEncode(Db.ToStr(row["TrackingNumber"]))).Append("</span>")
              .Append(UiHelper.StatusBadge(status))
              .Append("</div>");

            sb.Append("<div class='font-label-sm text-label-sm text-on-surface-variant space-y-1'>");
            sb.Append("<p><strong>Route:</strong> ")
              .Append(Server.HtmlEncode(Db.ToStr(row["OriginCity"]))).Append(" &rarr; ")
              .Append(Server.HtmlEncode(Db.ToStr(row["DestCity"]))).Append("</p>");

            if (latest.Rows.Count > 0)
            {
                sb.Append("<p><strong>Latest Update:</strong> ")
                  .Append(Server.HtmlEncode(Db.ToStr(latest.Rows[0]["Status"]))).Append(" at ")
                  .Append(Server.HtmlEncode(Db.ToStr(latest.Rows[0]["Location"]))).Append("</p>");
                sb.Append("<p><strong>When:</strong> ")
                  .Append(Convert.ToDateTime(latest.Rows[0]["Timestamp"]).ToString("dd MMM yyyy HH:mm"))
                  .Append("</p>");
            }

            if (row["DeliveryDate"] != DBNull.Value)
            {
                sb.Append("<p><strong>Delivered:</strong> ")
                  .Append(Convert.ToDateTime(row["DeliveryDate"]).ToString("dd MMM yyyy")).Append("</p>");
            }
            sb.Append("</div>");

            lblTrackResult.CssClass =
                "block mt-4 p-4 bg-surface-container-lowest rounded-xl border border-outline-variant text-left";
            lblTrackResult.Text = sb.ToString();
        }

    }
}
