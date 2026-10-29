using System;
using System.Collections.Generic;
using System.Data;
using System.Web.UI;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.CustomerPortal
{
    /// <summary>
    /// My Invoices - billing page for both Individual and Business customers.
    ///
    /// A Business client (Client.BusinessID set) sees and can bulk-pay every unpaid invoice
    /// belonging to any client of that Business, not only their own; an Individual client
    /// sees only their own. That scope is derived server-side in LoadInvoices/PaymentService
    /// from the logged-in user's own Client row - the page never trusts anything about scope
    /// from the browser, only which of the already-scoped invoices were ticked.
    /// </summary>
    public partial class Invoices : SecurePage
    {
        protected Repeater rptInvoices;
        protected Panel pnlNone;
        protected Panel pnlPayBar;
        protected Panel pnlReceipt;
        protected Literal litReceipt;
        protected Literal litScopeHint;
        protected Label lblMsg;
        protected DropDownList ddlMethod;
        protected HiddenField hfIdempotencyKey;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Customer");

            if (!IsPostBack)
            {
                hfIdempotencyKey.Value = Guid.NewGuid().ToString("N");
                LoadInvoices();
            }
        }

        /// <summary>The caller's own BusinessID, or null if they are not part of a Business.</summary>
        private object CurrentBusinessId()
        {
            return Db.Scalar("SELECT BusinessID FROM dbo.[Client] WHERE UserID = @u", "@u", UserId);
        }

        private void LoadInvoices()
        {
            object businessId = CurrentBusinessId();
            bool isBusiness = businessId != null && businessId != DBNull.Value;

            litScopeHint.Text = isBusiness
                ? "Showing every invoice for your business. Select one, several, or all unpaid invoices to pay together."
                : "View and pay invoices for your own shipments.";

            DataTable dt = Db.Query(
                @"SELECT p.PaymentID, p.InvoiceNumber, p.Amount, p.PaymentStatus, p.IssuedDate,
                         s.TrackingNumber, u.FullName AS ClientName
                  FROM dbo.[Payment] p
                  JOIN dbo.[Client] c ON c.UserID = p.ClientID
                  LEFT JOIN dbo.[Shipment] s ON s.ShipmentID = p.ShipmentID
                  LEFT JOIN dbo.[User] u ON u.UserID = p.ClientID
                  WHERE (@b IS NOT NULL AND c.BusinessID = @b) OR (@b IS NULL AND p.ClientID = @u)
                  ORDER BY p.IssuedDate DESC",
                "@b", businessId, "@u", UserId);

            rptInvoices.DataSource = dt;
            rptInvoices.DataBind();
            pnlNone.Visible = dt.Rows.Count == 0;
            pnlPayBar.Visible = dt.Rows.Count > 0;
        }

        protected void rptInvoices_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem) return;

            var chk = (CheckBox)e.Item.FindControl("chkSelect");
            if (chk == null) return;

            decimal amount = Db.ToDecimal(((DataRowView)e.Item.DataItem)["Amount"]);
            string amtFormatted = amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            chk.Attributes["data-amount"] = amtFormatted;
            chk.InputAttributes["data-amount"] = amtFormatted;
        }

        /// <summary>Collects the PaymentIDs of every ticked, enabled checkbox in the grid.</summary>
        private int[] SelectedPaymentIds()
        {
            var ids = new List<int>();
            foreach (RepeaterItem item in rptInvoices.Items)
            {
                var chk = (CheckBox)item.FindControl("chkSelect");
                var hf = (HiddenField)item.FindControl("hfPaymentId");
                if (chk != null && hf != null && chk.Enabled)
                {
                    bool isChecked = chk.Checked || (!string.IsNullOrEmpty(chk.UniqueID) && Request.Form[chk.UniqueID] != null);
                    if (isChecked)
                    {
                        int id;
                        if (int.TryParse(hf.Value, out id)) ids.Add(id);
                    }
                }
            }
            return ids.ToArray();
        }

        /// <summary>Every currently-listed invoice whose status is Unpaid, regardless of ticks.</summary>
        private int[] AllUnpaidPaymentIds()
        {
            var ids = new List<int>();
            foreach (RepeaterItem item in rptInvoices.Items)
            {
                var chk = (CheckBox)item.FindControl("chkSelect");
                var hf = (HiddenField)item.FindControl("hfPaymentId");
                if (chk != null && chk.Enabled)
                {
                    int id;
                    if (int.TryParse(hf.Value, out id)) ids.Add(id);
                }
            }
            return ids.ToArray();
        }

        protected void btnPaySelected_Click(object sender, EventArgs e)
        {
            ProcessPayment(SelectedPaymentIds());
        }

        protected void btnPayAllUnpaid_Click(object sender, EventArgs e)
        {
            ProcessPayment(AllUnpaidPaymentIds());
        }

        private void ProcessPayment(int[] paymentIds)
        {
            if (paymentIds.Length == 0)
            {
                SetMsg("Select at least one unpaid invoice.", false);
                LoadInvoices();
                return;
            }

            PaymentService.BulkPaymentOutcome outcome =
                PaymentService.PayInvoices(paymentIds, UserId, ddlMethod.SelectedValue, hfIdempotencyKey.Value);

            if (outcome.Success)
            {
                NotificationService.Send(UserId, null,
                    "Payment received: " + outcome.InvoiceCount + " invoice(s) totalling R " +
                    outcome.TotalAmount.ToString("F2") + (outcome.ReferenceNumber != null ? " (ref " + outcome.ReferenceNumber + ")" : "") + ".",
                    "System");

                pnlReceipt.Visible = true;
                litReceipt.Text = "Payment successful - " + outcome.InvoiceCount + " invoice(s), total R " +
                                   outcome.TotalAmount.ToString("N2") +
                                   (outcome.ReferenceNumber != null ? ". Reference " + UiHelper.Enc(outcome.ReferenceNumber) + "." : ".");
                hfIdempotencyKey.Value = Guid.NewGuid().ToString("N");
                LoadInvoices();
                return;
            }

            // Failed: mint a fresh idempotency key so a genuine retry is a new attempt rather
            // than a resubmission that PaymentService would just echo back unpaid.
            hfIdempotencyKey.Value = Guid.NewGuid().ToString("N");
            SetMsg(outcome.Message, false);
            LoadInvoices();
        }

        private void SetMsg(string text, bool ok)
        {
            lblMsg.CssClass = UiHelper.AlertClass(ok) + " mb-3";
            lblMsg.Text = text;
        }
    }
}
