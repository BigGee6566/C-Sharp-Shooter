using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.CustomerPortal
{
    /// <summary>
    /// Customer landing page: what the signed-in customer has in flight, what has been
    /// delivered, what they owe, and their most recent notifications.
    ///
    /// Every figure here is read from the database for the current UserID. The page
    /// deliberately shows no metric the schema cannot support.
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

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Customer");

            if (!IsPostBack)
            {
                litName.Text = Server.HtmlEncode(FullName);
                litTitle.Text = Server.HtmlEncode(DashboardTitle());
                LoadData();
            }
        }

        /// <summary>
        /// Business and Individual customers get a differently-titled dashboard. The
        /// distinction is Client.ClientType in the database - not a client-side toggle.
        /// </summary>
        private string DashboardTitle()
        {
            string clientType = Db.ToStr(Db.Scalar(
                "SELECT ClientType FROM dbo.[Client] WHERE UserID = @u", "@u", UserId));

            return string.Equals(clientType, "Business", StringComparison.OrdinalIgnoreCase)
                ? "Business Dashboard"
                : "My Dashboard";
        }

        private void LoadData()
        {
            DataTable shipments = Db.Query(
                @"SELECT TrackingNumber, DestCity, DeliveryStatus, ShipmentDate
                  FROM dbo.[Shipment] WHERE SenderID = @u ORDER BY ShipmentDate DESC",
                "@u", UserId);

            rptShipments.DataSource = shipments;
            rptShipments.DataBind();
            pnlNoShipments.Visible = shipments.Rows.Count == 0;

            litTotal.Text = shipments.Rows.Count.ToString();

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

            // Only raise the payment callout when there is genuinely something to pay.
            pnlBalanceDue.Visible = outstanding > 0m;
            litBalanceDue.Text = outstanding.ToString("N2");
            litUnpaidCount.Text = unpaidCount.ToString();

            DataTable notifications = Db.Query(
                @"SELECT TOP 5 Message, Type, CreatedDate FROM dbo.[Notification]
                  WHERE UserID = @u ORDER BY CreatedDate DESC", "@u", UserId);

            rptNotifications.DataSource = notifications;
            rptNotifications.DataBind();
            pnlNoNotifs.Visible = notifications.Rows.Count == 0;

            // Scoped to this customer's own shipments and invoices.
            ChartAssets.Register(this);
            litSummary.Text = RoleDashboards.Render(() => RoleDashboards.Customer(UserId, DateTime.Now, ResolveUrl), null);
        }

        protected void btnHeaderCreateOrder_Click(object sender, EventArgs e)
        {
            Response.Redirect(ResolveUrl("~/Customer/BookShipment.aspx"));
        }
    }
}
