using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Ops
{
    public partial class RoutePlanning : SecurePage
    {
        protected TextBox txtRouteName;
        protected DropDownList ddlDriver, ddlVehicle, ddlRoute, ddlFilterCategory;
        protected Button btnCreateRoute, btnAssign;
        protected LinkButton btnSelectAll;
        protected Label lblRouteMsg, lblAssignMsg;
        protected CheckBoxList cblParcels;
        protected Panel pnlNoParcels, pnlNoRoutes;
        protected Repeater rptRoutes;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Admin", "Staff");
            if (!IsPostBack) LoadAll();
        }

        private void LoadAll()
        {
            LoadDrivers();
            LoadVehicles();
            LoadRoutesDropdown();
            LoadLaneFilter();
            LoadParcels();
            LoadRoutesTable();
            SuggestRouteName();
        }

        private void LoadLaneFilter()
        {
            ddlFilterCategory.Items.Clear();
            ddlFilterCategory.Items.Add(new ListItem("All sort lanes", ""));
            foreach (string category in SortingService.Categories)
            {
                ddlFilterCategory.Items.Add(new ListItem(category, category));
            }
        }

        /// <summary>
        /// Lists drivers best-first - available before on-route, then lightest load - and
        /// shows why, so the dispatcher can accept the top entry instead of working it out.
        /// </summary>
        private void LoadDrivers()
        {
            DataTable dt = Db.Query(
                @"SELECT d.UserID AS DriverID,
                         u.FullName + ' — ' + d.AvailabilityStatus + ' (' +
                             CAST((SELECT COUNT(*) FROM dbo.[Shipment] s
                                   WHERE s.DriverID = d.UserID
                                     AND s.DeliveryStatus IN ('Assigned','OutForDelivery')) AS NVARCHAR(5))
                             + ' open)' AS Label
                  FROM dbo.[Driver] d
                  JOIN dbo.[User] u ON u.UserID = d.UserID
                  ORDER BY CASE d.AvailabilityStatus
                               WHEN 'Available' THEN 0 WHEN 'On Route' THEN 1 ELSE 2 END,
                           (SELECT COUNT(*) FROM dbo.[Shipment] s
                            WHERE s.DriverID = d.UserID
                              AND s.DeliveryStatus IN ('Assigned','OutForDelivery')),
                           u.FullName");
            ddlDriver.DataSource = dt;
            ddlDriver.DataTextField = "Label";
            ddlDriver.DataValueField = "DriverID";
            ddlDriver.DataBind();
        }

        /// <summary>Proposes a route name from the busiest waiting sort lane and today's run number.</summary>
        private void SuggestRouteName()
        {
            if (txtRouteName.Text.Trim().Length > 0) return;

            string category = Db.ToStr(Db.Scalar(
                @"SELECT TOP 1 SortCategory FROM dbo.[Shipment]
                  WHERE RouteID IS NULL AND WarehouseStatus = 'Sorted' AND SortCategory IS NOT NULL
                  GROUP BY SortCategory ORDER BY COUNT(*) DESC"));
            int runsToday = Db.ToInt(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.[Route] WHERE RouteDate = CAST(GETDATE() AS DATE)"));

            txtRouteName.Text = SortingService.SuggestRouteName(category, DateTime.Today, runsToday + 1);
        }

        private void LoadVehicles()
        {
            DataTable dt = Db.Query("SELECT VehicleID, RegistrationNumber + ' (' + VehicleType + ')' AS Label FROM dbo.[Vehicle] ORDER BY RegistrationNumber");
            ddlVehicle.DataSource = dt;
            ddlVehicle.DataTextField = "Label";
            ddlVehicle.DataValueField = "VehicleID";
            ddlVehicle.DataBind();
        }

        private void LoadRoutesDropdown()
        {
            DataTable dt = Db.Query("SELECT RouteID, RouteName FROM dbo.[Route] WHERE Status <> 'Completed' ORDER BY RouteDate DESC");
            ddlRoute.DataSource = dt;
            ddlRoute.DataTextField = "RouteName";
            ddlRoute.DataValueField = "RouteID";
            ddlRoute.DataBind();
        }

        private void LoadParcels()
        {
            string lane = ddlFilterCategory.SelectedValue ?? string.Empty;
            // Belt-and-braces alongside Warehouse's own gate: a parcel can only reach
            // WarehouseStatus = 'Sorted' once it is cleared for payment, but dispatch is the
            // point of no return, so it is re-checked here as well.
            DataTable dt = Db.Query(
                @"SELECT ShipmentID, TrackingNumber, DestCity, SortCategory
                  FROM dbo.[Shipment]
                  WHERE RouteID IS NULL AND WarehouseStatus = 'Sorted'
                    AND DeliveryStatus NOT IN ('Delivered','Failed')
                    AND PaymentGateStatus IN ('NotRequired','PaymentSuccessful')
                    AND (@lane = '' OR SortCategory = @lane)
                  ORDER BY ShipmentDate", "@lane", lane);
            cblParcels.Items.Clear();
            foreach (DataRow r in dt.Rows)
            {
                cblParcels.Items.Add(new ListItem(
                    Db.ToStr(r["TrackingNumber"]) + " → " + Db.ToStr(r["DestCity"]) + " (" + Db.ToStr(r["SortCategory"]) + ")",
                    Db.ToStr(r["ShipmentID"])));
            }
            pnlNoParcels.Visible = dt.Rows.Count == 0;
        }

        private void LoadRoutesTable()
        {
            DataTable dt = Db.Query(
                @"SELECT r.RouteName, r.RouteDate, r.Status,
                         ISNULL(u.FullName, '-') AS DriverName,
                         ISNULL(v.RegistrationNumber, '-') AS Vehicle,
                         (SELECT COUNT(*) FROM dbo.[Shipment] s WHERE s.RouteID = r.RouteID) AS ParcelCount
                  FROM dbo.[Route] r
                  LEFT JOIN dbo.[User] u ON u.UserID = r.DriverID
                  LEFT JOIN dbo.[Vehicle] v ON v.VehicleID = r.VehicleID
                  ORDER BY r.RouteDate DESC");
            rptRoutes.DataSource = dt;
            rptRoutes.DataBind();
            pnlNoRoutes.Visible = dt.Rows.Count == 0;
        }

        /// <summary>Narrows the waiting parcels to one sort lane - one lane, one route.</summary>
        protected void ddlFilterCategory_Changed(object sender, EventArgs e)
        {
            LoadParcels();
        }

        /// <summary>Ticks every parcel currently listed. The dispatcher still confirms with Assign.</summary>
        protected void btnSelectAll_Click(object sender, EventArgs e)
        {
            foreach (ListItem item in cblParcels.Items) item.Selected = true;
        }

        protected void btnCreateRoute_Click(object sender, EventArgs e)
        {
            if (txtRouteName.Text.Trim().Length == 0)
            {
                lblRouteMsg.CssClass = UiHelper.AlertClass("error") + " mt-1";
                lblRouteMsg.Text = "Please enter a route name.";
                return;
            }
            if (ddlDriver.Items.Count == 0 || ddlVehicle.Items.Count == 0)
            {
                lblRouteMsg.CssClass = UiHelper.AlertClass("error") + " mt-1";
                lblRouteMsg.Text = "Add drivers and vehicles under Fleet first.";
                return;
            }

            int driverId = int.Parse(ddlDriver.SelectedValue);
            int vehicleId = int.Parse(ddlVehicle.SelectedValue);

            Db.NonQuery(
                @"INSERT INTO dbo.[Route] (RouteName, DriverID, VehicleID, Status)
                  VALUES (@n, @d, @v, 'Active')",
                "@n", txtRouteName.Text.Trim(), "@d", driverId, "@v", vehicleId);

            Db.NonQuery("UPDATE dbo.[Driver] SET AvailabilityStatus = 'On Route' WHERE UserID = @d", "@d", driverId);

            lblRouteMsg.CssClass = UiHelper.AlertClass("success") + " mt-1";
            lblRouteMsg.Text = "Route created.";
            txtRouteName.Text = string.Empty;
            LoadDrivers();          // the driver is now 'On Route', so re-rank the list
            LoadRoutesDropdown();
            LoadRoutesTable();
            SuggestRouteName();
        }

        protected void btnAssign_Click(object sender, EventArgs e)
        {
            if (ddlRoute.Items.Count == 0)
            {
                lblAssignMsg.CssClass = UiHelper.AlertClass("error") + " mt-1";
                lblAssignMsg.Text = "Create a route first.";
                return;
            }

            int routeId = int.Parse(ddlRoute.SelectedValue);
            DataTable route = Db.Query("SELECT DriverID, VehicleID FROM dbo.[Route] WHERE RouteID = @r", "@r", routeId);
            if (route.Rows.Count == 0) return;
            int? driverId = route.Rows[0]["DriverID"] == DBNull.Value ? (int?)null : Db.ToInt(route.Rows[0]["DriverID"]);
            int? vehicleId = route.Rows[0]["VehicleID"] == DBNull.Value ? (int?)null : Db.ToInt(route.Rows[0]["VehicleID"]);

            int assigned = 0;
            int blocked = 0;
            foreach (ListItem item in cblParcels.Items)
            {
                if (!item.Selected) continue;
                int shipId = int.Parse(item.Value);

                // Re-verify at the write itself - the posted checkbox value is not trusted
                // just because the list was filtered when it was rendered.
                string gateStatus = Db.ToStr(Db.Scalar(
                    "SELECT PaymentGateStatus FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", shipId));
                if (gateStatus != "NotRequired" && gateStatus != "PaymentSuccessful")
                {
                    blocked++;
                    continue;
                }

                // Assigned to a driver, awaiting collection (not yet in the driver's hands).
                Db.NonQuery(
                    @"UPDATE dbo.[Shipment]
                      SET RouteID = @r, DriverID = @d, WarehouseStatus = 'Dispatched',
                          DeliveryStatus = 'Assigned', LastStatusChange = GETDATE()
                      WHERE ShipmentID = @s",
                    "@r", routeId, "@d", (object)driverId, "@s", shipId);

                // Record which driver AND vehicle the parcel was assigned to.
                Db.NonQuery(
                    @"INSERT INTO dbo.[TrackingHistory] (ShipmentID, DriverID, VehicleID, AdminID, Location, Status, Remarks)
                      VALUES (@s, @d, @v, @a, 'Dispatch', 'Assigned', 'Allocated to delivery route and driver')",
                    "@s", shipId, "@d", (object)driverId, "@v", (object)vehicleId, "@a", UserId);

                // Notify the sender (if a registered customer).
                DataTable snd = Db.Query("SELECT SenderID, TrackingNumber FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", shipId);
                if (snd.Rows.Count > 0 && snd.Rows[0]["SenderID"] != DBNull.Value)
                {
                    NotificationService.Send(Db.ToInt(snd.Rows[0]["SenderID"]), shipId,
                        "Your parcel " + Db.ToStr(snd.Rows[0]["TrackingNumber"]) + " has been assigned to a driver for collection.",
                        "Dispatch");
                }
                assigned++;
            }

            lblAssignMsg.CssClass = UiHelper.AlertClass(assigned > 0) + " mt-1";
            lblAssignMsg.Text = assigned > 0
                ? assigned + " parcel(s) assigned to the driver for collection." +
                  (blocked > 0 ? " " + blocked + " skipped - awaiting customer payment." : "")
                : (blocked > 0
                    ? blocked + " parcel(s) are awaiting customer payment and were not assigned."
                    : "Select at least one parcel to assign.");
            LoadParcels();
            LoadRoutesTable();
        }
    }
}
