using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Finance
{
    public partial class Invoices : SecurePage
    {
        protected Label lblMsg;
        protected Button btnInvoiceAll;
        protected Repeater rptUninvoiced, rptInvoices;
        protected Panel pnlNoUninvoiced, pnlNoInvoices;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Admin");
            if (!IsPostBack) LoadAll();
        }

        private void LoadAll()
        {
            LoadUninvoiced();
            LoadInvoices();
        }

        private void LoadUninvoiced()
        {
            DataTable dt = Db.Query(
                @"SELECT s.ShipmentID, s.TrackingNumber, s.OriginCity, s.DestCity, s.Cost,
                         ISNULL(u.FullName, 'Walk-in') AS ClientName
                  FROM dbo.[Shipment] s
                  LEFT JOIN dbo.[User] u ON u.UserID = s.SenderID
                  WHERE s.DeliveryStatus = 'Delivered'
                    AND NOT EXISTS (SELECT 1 FROM dbo.[Payment] p WHERE p.ShipmentID = s.ShipmentID)
                  ORDER BY s.DeliveryDate DESC");
            rptUninvoiced.DataSource = dt;
            rptUninvoiced.DataBind();
            pnlNoUninvoiced.Visible = dt.Rows.Count == 0;
        }

        private void LoadInvoices()
        {
            DataTable dt = Db.Query(
                @"SELECT p.PaymentID, p.InvoiceNumber, p.Amount, p.PaymentStatus, p.IssuedDate,
                         ISNULL(u.FullName, 'Walk-in') AS ClientName
                  FROM dbo.[Payment] p
                  LEFT JOIN dbo.[User] u ON u.UserID = p.ClientID
                  ORDER BY p.IssuedDate DESC");
            rptInvoices.DataSource = dt;
            rptInvoices.DataBind();
            pnlNoInvoices.Visible = dt.Rows.Count == 0;
        }

        protected void rptUninvoiced_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "gen")
            {
                // Invoices are normally raised automatically the moment a delivery is
                // confirmed; this button remains as a manual fallback. AutomationService
                // is idempotent, so pressing it on an invoiced parcel does nothing.
                string invoiceNo = AutomationService.EnsureInvoice(int.Parse((string)e.CommandArgument));
                SetMsg(invoiceNo != null
                        ? "Invoice " + invoiceNo + " generated."
                        : "That shipment is already invoiced or is not yet delivered.",
                       invoiceNo != null);
                LoadAll();
            }
        }

        protected void btnInvoiceAll_Click(object sender, EventArgs e)
        {
            int count = AutomationService.AutoInvoiceDelivered();
            SetMsg(count > 0
                    ? count + " invoice(s) generated."
                    : "Nothing to invoice - every delivered parcel already has an invoice.",
                   true);
            LoadAll();
        }

        protected void rptInvoices_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "paid")
            {
                int paymentId = int.Parse((string)e.CommandArgument);
                Db.NonQuery(
                    @"UPDATE dbo.[Payment]
                      SET PaymentStatus = 'Paid', PaymentMethod = 'EFT', ReferenceNumber = @ref
                      WHERE PaymentID = @id",
                    "@ref", "PAY" + DateTime.Now.ToString("yyMMddHHmmss"), "@id", paymentId);
                SetMsg("Payment recorded.", true);
                LoadInvoices();
            }
        }

        private void SetMsg(string text, bool ok)
        {
            lblMsg.CssClass = UiHelper.AlertClass(ok) + " mb-4";
            lblMsg.Text = text;
        }
    }
}
