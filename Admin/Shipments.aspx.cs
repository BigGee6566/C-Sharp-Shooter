using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.AdminPortal
{
    public partial class Shipments : SecurePage
    {
        protected Panel pnlList, pnlDetail, pnlEmpty;
        protected TextBox txtSearch;
        protected Button btnSearch;
        protected Repeater rptShipments, rptTimeline;
        protected Literal litWaybill, litDelStatus, litWhStatus, litException,
                          litSender, litReceiver, litPickup, litDelivery, litRoute, litWeight, litDimensions,
                          litDesc, litSort, litDriver, litRouteName, litVehicle, litCost, litShipDate,
                          litDelDate, litBarcode, litPayment;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Admin");
            if (!IsPostBack)
            {
                int id;
                if (int.TryParse(Request.QueryString["id"], out id))
                    ShowDetail(id);
                else
                    BindList(null);
            }
        }

        /// <summary>
        /// The only WHERE clause in the app chosen at run time. It is a compile-time constant:
        /// the search term itself never touches the SQL text, it is bound to @q. A term such as
        /// <c>' OR 1=1--</c> is matched as a literal string and simply returns no rows.
        /// </summary>
        private const string SearchWhere =
            @"WHERE s.TrackingNumber LIKE @q OR s.OriginCity LIKE @q OR s.DestCity LIKE @q
              OR s.DeliveryStatus LIKE @q OR ISNULL(su.FullName,'') LIKE @q";

        private void BindList(string search)
        {
            string where = "";
            object[] p = new object[0];
            if (!string.IsNullOrEmpty(search))
            {
                where = SearchWhere;
                // Escape LIKE wildcards so a term of "%" cannot scan the whole table.
                string term = search.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
                p = new object[] { "@q", "%" + term + "%" };
            }

            DataTable dt = Db.Query(
                @"SELECT s.ShipmentID, s.TrackingNumber, s.OriginCity, s.DestCity, s.Cost,
                         s.WarehouseStatus, s.DeliveryStatus,
                         ISNULL(su.FullName, 'Walk-in') AS SenderName,
                         ISNULL(du.FullName, '-') AS DriverName
                  FROM dbo.[Shipment] s
                  LEFT JOIN dbo.[User] su ON su.UserID = s.SenderID
                  LEFT JOIN dbo.[User] du ON du.UserID = s.DriverID
                  " + where + @"
                  ORDER BY s.ShipmentDate DESC", p);

            rptShipments.DataSource = dt;
            rptShipments.DataBind();
            pnlEmpty.Visible = dt.Rows.Count == 0;
            pnlList.Visible = true;
            pnlDetail.Visible = false;
        }

        protected void btnSearch_Click(object sender, EventArgs e)
        {
            BindList(txtSearch.Text.Trim());
        }

        private void ShowDetail(int shipmentId)
        {
            DataTable dt = Db.Query(
                @"SELECT s.*, su.FullName AS SenderName, du.FullName AS DriverName,
                         r.RouteName, v.RegistrationNumber AS VehicleReg
                  FROM dbo.[Shipment] s
                  LEFT JOIN dbo.[User] su ON su.UserID = s.SenderID
                  LEFT JOIN dbo.[User] du ON du.UserID = s.DriverID
                  LEFT JOIN dbo.[Route] r ON r.RouteID = s.RouteID
                  LEFT JOIN dbo.[Vehicle] v ON v.VehicleID = r.VehicleID
                  WHERE s.ShipmentID = @id", "@id", shipmentId);

            if (dt.Rows.Count == 0) { BindList(null); return; }
            DataRow r0 = dt.Rows[0];

            pnlList.Visible = false;
            pnlDetail.Visible = true;

            litWaybill.Text = Server.HtmlEncode(Db.ToStr(r0["TrackingNumber"]));
            litDelStatus.Text = UiHelper.StatusBadge(Db.ToStr(r0["DeliveryStatus"]));
            litWhStatus.Text = UiHelper.StatusBadge(Db.ToStr(r0["WarehouseStatus"]));
            string exc = Db.ToStr(r0["ExceptionType"]);
            litException.Text = exc == "None" ? "" : UiHelper.StatusBadge(exc);

            litSender.Text = Enc(Db.ToStr(r0["SenderName"]), "Walk-in");
            litReceiver.Text = Enc(Db.ToStr(r0["ReceiverName"]) +
                (Db.ToStr(r0["ReceiverPhone"]).Length > 0 ? " (" + Db.ToStr(r0["ReceiverPhone"]) + ")" : ""), "-");
            litPickup.Text = Enc(Db.ToStr(r0["PickupAddress"]), "-");
            litDelivery.Text = Enc(Db.ToStr(r0["DeliveryAddress"]), "-");
            litRoute.Text = Server.HtmlEncode(Db.ToStr(r0["OriginCity"]) + " → " + Db.ToStr(r0["DestCity"]));
            litWeight.Text = Db.ToDecimal(r0["ParcelWeight"]).ToString("0.##") + " kg";
            // Shows a dash for counter-registered shipments, which have no size band.
            litDimensions.Text = ParcelSize.Describe(
                r0["ParcelLengthCm"], r0["ParcelWidthCm"], r0["ParcelHeightCm"]);
            litDesc.Text = Enc(Db.ToStr(r0["ParcelDescription"]), "-");
            litSort.Text = Enc(Db.ToStr(r0["SortCategory"]), "-");
            litDriver.Text = Enc(Db.ToStr(r0["DriverName"]), "Unassigned");
            litRouteName.Text = Enc(Db.ToStr(r0["RouteName"]), "-");
            litVehicle.Text = Enc(Db.ToStr(r0["VehicleReg"]), "-");
            litCost.Text = Db.ToDecimal(r0["Cost"]).ToString("N2");
            litShipDate.Text = FmtDate(r0["ShipmentDate"]);
            litDelDate.Text = FmtDate(r0["DeliveryDate"]);
            litBarcode.Text = Enc(Db.ToStr(r0["Barcode"]), "-");

            // Tracking history
            DataTable hist = Db.Query(
                @"SELECT Status, Location, Remarks, [Timestamp]
                  FROM dbo.[TrackingHistory] WHERE ShipmentID = @s ORDER BY [Timestamp] DESC",
                "@s", shipmentId);
            rptTimeline.DataSource = hist;
            rptTimeline.DataBind();

            // Payment
            DataTable pay = Db.Query(
                @"SELECT InvoiceNumber, Amount, PaymentStatus, PaymentMethod, ReferenceNumber, IssuedDate
                  FROM dbo.[Payment] WHERE ShipmentID = @s", "@s", shipmentId);
            if (pay.Rows.Count == 0)
            {
                litPayment.Text = "<p class='text-sm text-gray-500'>No invoice generated for this shipment yet.</p>";
            }
            else
            {
                DataRow pr = pay.Rows[0];
                litPayment.Text =
                    "<div class='text-sm space-y-1'>" +
                    "<div><span class='text-gray-500'>Invoice:</span> <span class='font-semibold text-brandDark'>" + Server.HtmlEncode(Db.ToStr(pr["InvoiceNumber"])) + "</span></div>" +
                    "<div><span class='text-gray-500'>Amount:</span> R " + Db.ToDecimal(pr["Amount"]).ToString("N2") + "</div>" +
                    "<div><span class='text-gray-500'>Status:</span> " + UiHelper.StatusBadge(Db.ToStr(pr["PaymentStatus"])) + "</div>" +
                    (Db.ToStr(pr["PaymentMethod"]).Length > 0 ? "<div><span class='text-gray-500'>Method:</span> " + Server.HtmlEncode(Db.ToStr(pr["PaymentMethod"])) + "</div>" : "") +
                    "</div>";
            }
        }

        private string Enc(string value, string fallback)
        {
            return Server.HtmlEncode(string.IsNullOrWhiteSpace(value) ? fallback : value);
        }

        private static string FmtDate(object o)
        {
            return (o == null || o == DBNull.Value) ? "-" : Convert.ToDateTime(o).ToString("dd MMM yyyy HH:mm");
        }
    }
}
