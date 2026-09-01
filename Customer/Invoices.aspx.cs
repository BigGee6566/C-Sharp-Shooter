using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.CustomerPortal
{
    public partial class Invoices : SecurePage
    {
        protected Repeater rptInvoices;
        protected Panel pnlNone;
        protected Label lblMsg;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Customer");
            if (!IsPostBack) LoadInvoices();
        }

        private void LoadInvoices()
        {
            DataTable dt = Db.Query(
                @"SELECT p.PaymentID, p.InvoiceNumber, p.Amount, p.PaymentStatus, p.IssuedDate, s.TrackingNumber
                  FROM dbo.[Payment] p
                  LEFT JOIN dbo.[Shipment] s ON s.ShipmentID = p.ShipmentID
                  WHERE p.ClientID = @u ORDER BY p.IssuedDate DESC", "@u", UserId);
            rptInvoices.DataSource = dt;
            rptInvoices.DataBind();
            pnlNone.Visible = dt.Rows.Count == 0;
        }

        protected void rptInvoices_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "pay") return;

            int paymentId = int.Parse((string)e.CommandArgument);
            // Only allow paying the customer's own unpaid invoice.
            DataTable dt = Db.Query(
                @"SELECT InvoiceNumber, Amount, ShipmentID FROM dbo.[Payment]
                  WHERE PaymentID = @id AND ClientID = @u AND PaymentStatus = 'Unpaid'",
                "@id", paymentId, "@u", UserId);
            if (dt.Rows.Count == 0)
            {
                SetMsg("That invoice could not be paid (already paid or not found).", false);
                LoadInvoices();
                return;
            }

            DataRow r = dt.Rows[0];
            Db.NonQuery(
                @"UPDATE dbo.[Payment]
                  SET PaymentStatus = 'Paid', PaymentMethod = 'Card (simulated)', ReferenceNumber = @ref
                  WHERE PaymentID = @id",
                "@ref", "PAY" + DateTime.Now.ToString("yyMMddHHmmss"), "@id", paymentId);

            NotificationService.Send(UserId,
                dt.Rows[0]["ShipmentID"] == DBNull.Value ? (int?)null : Db.ToInt(r["ShipmentID"]),
                "Payment received for invoice " + Db.ToStr(r["InvoiceNumber"]) +
                " (R " + Db.ToDecimal(r["Amount"]).ToString("N2") + "). Thank you.", "System");

            SetMsg("Payment successful. Thank you!", true);
            LoadInvoices();
        }

        private void SetMsg(string text, bool ok)
        {
            lblMsg.CssClass = UiHelper.AlertClass(ok) + " mb-3";
            lblMsg.Text = text;
        }
    }
}
