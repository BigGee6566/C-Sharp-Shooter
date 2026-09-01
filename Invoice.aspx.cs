using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp
{
    public partial class InvoicePage : SecurePage
    {
        protected Panel pnlInvoice, pnlNotFound;
        protected Literal litInvoiceNo, litDate, litClient, litClientEmail, litStatus,
                          litDesc, litWaybill, litAmount, litSubtotal, litTotal;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Customers reach their own invoice; the ownership check below scopes them to it.
            RequireRole("Admin", "Customer");
            if (IsPostBack) return;

            int id;
            if (!int.TryParse(Request.QueryString["id"], out id)) { ShowNotFound(); return; }

            DataTable dt = Db.Query(
                @"SELECT p.InvoiceNumber, p.Amount, p.PaymentStatus, p.IssuedDate, p.ClientID,
                         s.TrackingNumber, s.ParcelDescription, s.OriginCity, s.DestCity,
                         u.FullName, u.Email
                  FROM dbo.[Payment] p
                  LEFT JOIN dbo.[Shipment] s ON s.ShipmentID = p.ShipmentID
                  LEFT JOIN dbo.[User] u ON u.UserID = p.ClientID
                  WHERE p.PaymentID = @id", "@id", id);

            if (dt.Rows.Count == 0) { ShowNotFound(); return; }
            DataRow r = dt.Rows[0];

            // Customers may only view their own invoices.
            if (Role == "Customer" && Db.ToInt(r["ClientID"]) != UserId) { ShowNotFound(); return; }

            litInvoiceNo.Text = Db.ToStr(r["InvoiceNumber"]);
            litDate.Text = Convert.ToDateTime(r["IssuedDate"]).ToString("dd MMMM yyyy");
            litClient.Text = Server.HtmlEncode(Db.ToStr(r["FullName"]));
            litClientEmail.Text = Server.HtmlEncode(Db.ToStr(r["Email"]));
            litStatus.Text = UiHelper.StatusBadge(Db.ToStr(r["PaymentStatus"]));

            string desc = Db.ToStr(r["ParcelDescription"]);
            if (desc.Length == 0) desc = "Courier delivery service";
            litDesc.Text = Server.HtmlEncode(desc + " (" + Db.ToStr(r["OriginCity"]) + " to " + Db.ToStr(r["DestCity"]) + ")");
            litWaybill.Text = Server.HtmlEncode(Db.ToStr(r["TrackingNumber"]));

            decimal amount = Db.ToDecimal(r["Amount"]);
            litAmount.Text = amount.ToString("N2");
            litSubtotal.Text = amount.ToString("N2");
            litTotal.Text = amount.ToString("N2");
        }

        private void ShowNotFound()
        {
            pnlInvoice.Visible = false;
            pnlNotFound.Visible = true;
        }
    }
}
