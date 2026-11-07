using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Controls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.CustomerPortal
{
    /// <summary>
    /// Customer landing page. Client.ClientType in the database, not a client-side toggle,
    /// decides which of two dashboards is shown. A Business customer sees what is in flight,
    /// what has been delivered, what they owe, analytics and notifications. An Individual
    /// customer sees a personal view built on <see cref="IndividualDashboard"/>.
    ///
    /// Every figure here is read from the database for the current UserID.
    /// </summary>
    public partial class Dashboard : SecurePage
    {
        protected Literal litTitle;
        protected Literal litName;
        protected Literal litActiveShipments;
        protected Literal litDelivered;
        protected Literal litTotal;
        protected Literal litOutstanding;
        protected Repeater rptShipments;
        protected Panel pnlNoShipments;
        protected Repeater rptNotifications;
        protected Panel pnlNoNotifs;
        protected Panel pnlBalanceDue;
        protected Literal litBalanceDue;
        protected Literal litUnpaidCount;
        protected Button btnHeaderCreateOrder;
        protected Literal litSummary;
        protected Panel pnlIndividual, pnlBusiness, pnlPaymentNeeded;
        protected Literal litGreeting, litActiveCount, litCompletedCount, litPaymentNeeded;
        protected HyperLink lnkPayNow;
        protected TextBox txtQuickTrack;
        protected LinkButton btnQuickTrack;
        protected DeliveryCards cardsRecent;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Customer");

            if (IsPostBack) return;

            if (IndividualDashboard.IsBusinessClient(UserId))
            {
                pnlBusiness.Visible = true;
                litName.Text = Server.HtmlEncode(FullName);
                litTitle.Text = "Business Dashboard";
                LoadBusinessDashboard();
            }
            else
            {
                pnlIndividual.Visible = true;
                LoadIndividualDashboard();
            }
        }

        private void LoadIndividualDashboard()
        {
            litGreeting.Text = Server.HtmlEncode(IndividualDashboard.Greeting(FullName, DateTime.Now));
            litActiveCount.Text = IndividualDashboard.ActiveCount(UserId).ToString("00");
            litCompletedCount.Text = IndividualDashboard.CompletedCount(UserId).ToString("00");
            cardsRecent.Show(IndividualDashboard.Recent(UserId, 3), "You haven't booked a delivery yet.");

            DataTable unpaid = IndividualDashboard.AwaitingPayment(UserId);
            pnlPaymentNeeded.Visible = unpaid.Rows.Count > 0;
            if (unpaid.Rows.Count > 0)
            {
                litPaymentNeeded.Text = unpaid.Rows.Count == 1
                    ? "Parcel " + Server.HtmlEncode(Db.ToStr(unpaid.Rows[0]["TrackingNumber"])) + " is waiting for payment before it can be collected."
                    : unpaid.Rows.Count + " parcels are waiting for payment before they can be collected.";
                lnkPayNow.NavigateUrl = "~/Customer/Payment.aspx?shipmentId=" + Db.ToInt(unpaid.Rows[0]["ShipmentID"]);
            }
        }

        protected void btnQuickTrack_Click(object sender, EventArgs e)
        {
            string waybill = InputValidator.NormalizeTrackingNumber(txtQuickTrack.Text);
            Response.Redirect(ResolveUrl("~/Customer/Track.aspx") +
                (waybill.Length > 0 ? "?wb=" + Server.UrlEncode(waybill) : string.Empty));
        }

        private void LoadBusinessDashboard()
        {
            DataTable shipments = Db.Query(
                @"SELECT TOP 5 TrackingNumber, DestCity, DeliveryStatus, ShipmentDate
                  FROM dbo.[Shipment] WHERE SenderID = @u ORDER BY ShipmentDate DESC",
                "@u", UserId);

            rptShipments.DataSource = shipments;
            rptShipments.DataBind();
            pnlNoShipments.Visible = shipments.Rows.Count == 0;

            litTotal.Text = Db.ToStr(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.[Shipment] WHERE SenderID = @u", "@u", UserId));

            litActiveShipments.Text = Db.ToStr(Db.Scalar(
                @"SELECT COUNT(*) FROM dbo.[Shipment]
                  WHERE SenderID = @u AND DeliveryStatus IN ('Assigned','InTransit','OutForDelivery')",
                "@u", UserId));

            litDelivered.Text = Db.ToStr(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.[Shipment] WHERE SenderID = @u AND DeliveryStatus = 'Delivered'",
                "@u", UserId));

            decimal outstanding = Db.ToDecimal(Db.Scalar(
                "SELECT ISNULL(SUM(Amount),0) FROM dbo.[Payment] WHERE ClientID = @u AND PaymentStatus = 'Unpaid'",
                "@u", UserId));
            int unpaidCount = Db.ToInt(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.[Payment] WHERE ClientID = @u AND PaymentStatus = 'Unpaid'",
                "@u", UserId));

            litOutstanding.Text = outstanding.ToString("N2");

            
            pnlBalanceDue.Visible = outstanding > 0m;
            litBalanceDue.Text = outstanding.ToString("N2");
            litUnpaidCount.Text = unpaidCount.ToString();

            DataTable notifications = Db.Query(
                @"SELECT TOP 5 Message, Type, CreatedDate FROM dbo.[Notification]
                  WHERE UserID = @u ORDER BY CreatedDate DESC", "@u", UserId);

            rptNotifications.DataSource = notifications;
            rptNotifications.DataBind();
            pnlNoNotifs.Visible = notifications.Rows.Count == 0;

            
            ChartAssets.Register(this);
            litSummary.Text = RoleDashboards.Render(() => RoleDashboards.Customer(UserId, DateTime.Now, ResolveUrl), null);
        }

        protected void btnHeaderCreateOrder_Click(object sender, EventArgs e)
        {
            Response.Redirect(ResolveUrl("~/Customer/BookShipment.aspx"));
        }
    }
}
