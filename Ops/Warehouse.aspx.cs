using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Ops
{
    /// <summary>
    /// Warehouse sorting desk.
    ///
    /// An operator scans a waybill, confirms or overrides the sort lane, sets the warehouse
    /// status and flags any exception. The lane is derived from the destination city by
    /// SortingService so it never has to be picked by hand, and any exception is escalated
    /// by AutomationService rather than being left for someone to notice.
    /// </summary>
    public partial class Warehouse : SecurePage
    {
        protected TextBox txtScan;
        protected Button btnScan;
        protected Button btnSave;
        protected Label lblScanMsg;
        protected Label lblSaveMsg;
        protected Panel pnlEdit;
        protected Panel pnlEmpty;
        protected Literal litParcel;
        protected Literal litRoute;
        protected Literal litSortReason;
        protected Literal litQueueCount;
        protected DropDownList ddlSort;
        protected DropDownList ddlWarehouse;
        protected DropDownList ddlException;
        protected DropDownList ddlLane;
        protected HiddenField hfShipmentId;
        protected GridView gvSortingQueue;
        protected TextBox txtSearch;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireOperationsStaff();

            if (!IsPostBack)
            {
                BindLaneFilter();
                LoadQueue();
            }
        }

        private void BindLaneFilter()
        {
            ddlLane.Items.Clear();
            ddlLane.Items.Add(new ListItem("All sort lanes", string.Empty));
            foreach (string category in SortingService.Categories)
            {
                ddlLane.Items.Add(new ListItem(category, category));
            }
        }

        private void LoadQueue()
        {
            string search = txtSearch.Text.Trim();
            string lane = ddlLane.SelectedValue;

            // Both filters are optional; a NULL/empty parameter disables its own clause so
            // the whole thing stays one parameterised statement.
            // A shipment still awaiting/mid online payment must not enter the sorting queue -
            // Individual customers pay before dispatch, and this is the queue that feeds
            // dispatch (see RoutePlanning). NotRequired (Business/counter) and
            // PaymentSuccessful are the only states eligible to be sorted.
            DataTable queue = Db.Query(
                @"SELECT ShipmentID, TrackingNumber, OriginCity, DestCity,
                         SortCategory, WarehouseStatus, ExceptionType
                  FROM dbo.[Shipment]
                  WHERE WarehouseStatus IN ('Pending','Received','Sorted')
                    AND PaymentGateStatus IN ('NotRequired','PaymentSuccessful')
                    AND (@q = '' OR TrackingNumber LIKE '%' + @q + '%' OR DestCity LIKE '%' + @q + '%')
                    AND (@lane = '' OR SortCategory = @lane)
                  ORDER BY ShipmentDate DESC",
                "@q", search, "@lane", lane);

            gvSortingQueue.DataSource = queue;
            gvSortingQueue.DataBind();

            pnlEmpty.Visible = queue.Rows.Count == 0;
            gvSortingQueue.Visible = queue.Rows.Count > 0;
            litQueueCount.Text = queue.Rows.Count.ToString();
        }

        protected void txtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadQueue();
        }

        protected void ddlLane_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadQueue();
        }

        protected void gvSortingQueue_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "LoadParcel") return;

            txtScan.Text = Convert.ToString(e.CommandArgument);
            LoadParcel(txtScan.Text.Trim());
        }

        protected void btnScan_Click(object sender, EventArgs e)
        {
            LoadParcel(txtScan.Text.Trim());
        }

        private void LoadParcel(string trackingNumber)
        {
            DataTable dt = Db.Query(
                @"SELECT ShipmentID, TrackingNumber, OriginCity, DestCity, ParcelWeight,
                         SortCategory, WarehouseStatus, ExceptionType, PaymentGateStatus
                  FROM dbo.[Shipment] WHERE TrackingNumber = @t", "@t", trackingNumber);

            if (dt.Rows.Count == 0)
            {
                pnlEdit.Visible = false;
                lblScanMsg.CssClass = UiHelper.AlertClass("error") + " mt-2";
                lblScanMsg.Text = "No parcel found for that waybill.";
                return;
            }

            DataRow row = dt.Rows[0];

            string paymentGateStatus = Db.ToStr(row["PaymentGateStatus"]);
            if (paymentGateStatus != "NotRequired" && paymentGateStatus != "PaymentSuccessful")
            {
                pnlEdit.Visible = false;
                lblScanMsg.CssClass = UiHelper.AlertClass("error") + " mt-2";
                lblScanMsg.Text = "This parcel is awaiting customer payment and cannot be sorted yet.";
                return;
            }
            hfShipmentId.Value = Db.ToStr(row["ShipmentID"]);
            litParcel.Text = Server.HtmlEncode(Db.ToStr(row["TrackingNumber"]));

            string destCity = Db.ToStr(row["DestCity"]);
            litRoute.Text = Server.HtmlEncode(
                Db.ToStr(row["OriginCity"]) + " → " + destCity + " · " +
                Db.ToDecimal(row["ParcelWeight"]).ToString("0.##") + " kg");

            string currentLane = Db.ToStr(row["SortCategory"]);
            string suggestedLane = SortingService.CategoryFor(destCity);
            SortingService.BindCategories(ddlSort, currentLane.Length > 0 ? currentLane : suggestedLane);

            litSortReason.Text = currentLane.Length > 0
                ? "Currently sorted to <strong>" + Server.HtmlEncode(currentLane) + "</strong>."
                : "Auto-sorted to <strong>" + Server.HtmlEncode(suggestedLane) +
                  "</strong> from destination " + Server.HtmlEncode(destCity) + ". Override only if needed.";

            // A parcel that is still Pending or Received is being sorted right now, so
            // default the status to where this action is about to take it.
            string warehouseStatus = Db.ToStr(row["WarehouseStatus"]);
            SortingService.SelectValue(ddlWarehouse,
                (warehouseStatus == "Pending" || warehouseStatus == "Received") ? "Sorted" : warehouseStatus);
            SortingService.SelectValue(ddlException, Db.ToStr(row["ExceptionType"]));

            pnlEdit.Visible = true;
            lblScanMsg.Text = string.Empty;
            lblScanMsg.CssClass = string.Empty;
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            int shipmentId;
            if (!int.TryParse(hfShipmentId.Value, out shipmentId)) return;

            // Re-check payment eligibility at the actual write, not just when the parcel was
            // scanned - the hidden field could otherwise carry a shipment that was still
            // unpaid when the page was first loaded and must not be walked forward on save.
            string gateStatus = Db.ToStr(Db.Scalar(
                "SELECT PaymentGateStatus FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", shipmentId));
            if (gateStatus != "NotRequired" && gateStatus != "PaymentSuccessful")
            {
                lblSaveMsg.CssClass = UiHelper.AlertClass("error") + " mt-1";
                lblSaveMsg.Text = "This parcel is awaiting customer payment and cannot be sorted yet.";
                LoadQueue();
                return;
            }

            string warehouseStatus = ddlWarehouse.SelectedValue;
            string exceptionType = ddlException.SelectedValue;

            // Warehouse status drives the customer-facing delivery status, except once the
            // parcel has reached a terminal state - that must not be walked backwards.
            string deliveryStatus =
                warehouseStatus == "Sorted" ? "Sorted" :
                warehouseStatus == "Dispatched" ? "InTransit" : "Registered";

            Db.NonQuery(
                @"UPDATE dbo.[Shipment]
                  SET SortCategory = @sc, WarehouseStatus = @ws, ExceptionType = @ex,
                      LastStatusChange = GETDATE(),
                      DeliveryStatus = CASE WHEN DeliveryStatus IN ('Delivered','Failed')
                                            THEN DeliveryStatus ELSE @ds END
                  WHERE ShipmentID = @id",
                "@sc", ddlSort.SelectedValue,
                "@ws", warehouseStatus,
                "@ex", exceptionType,
                "@ds", deliveryStatus,
                "@id", shipmentId);

            Db.NonQuery(
                @"INSERT INTO dbo.[TrackingHistory] (ShipmentID, AdminID, Location, Status, Remarks)
                  VALUES (@s, @a, 'Central Warehouse', @st, @rm)",
                "@s", shipmentId,
                "@a", UserId,
                "@st", warehouseStatus,
                "@rm", exceptionType == "None"
                    ? "Sorted to " + ddlSort.SelectedValue
                    : "Flagged as " + exceptionType);

            AutomationService.EscalateException(shipmentId, exceptionType, UserId);

            lblSaveMsg.CssClass = UiHelper.AlertClass("success") + " mt-1";
            lblSaveMsg.Text = exceptionType == "None"
                ? "Parcel updated."
                : "Parcel updated. " + exceptionType + " exception escalated to Dispatch and Management.";

            LoadQueue();
        }
    }
}
