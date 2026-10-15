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
        protected DropDownList ddlDriver, ddlVehicle, ddlRoute, ddlFilterCategory, ddlServiceArea;
        protected Button btnCreateRoute, btnAssign;
        protected LinkButton btnSelectAll;
        protected Label lblRouteMsg, lblAssignMsg;
        protected CheckBoxList cblParcels;
        protected Panel pnlNoParcels, pnlNoRoutes;
        protected Repeater rptRoutes;
        protected Literal litRouteServiceArea;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireOperationsStaff();
            if (!IsPostBack) LoadAll();
        }

        private void LoadAll()
        {
            LoadDrivers();
            LoadVehicles();
            LoadServiceAreaFilter(ddlServiceArea, false);
            LoadRoutesDropdown();
            LoadLaneFilter();
            LoadParcels();
            LoadRoutesTable();
            SuggestRouteName();
            ShowRouteServiceArea();
        }

        private void LoadLaneFilter()
        {
            LoadServiceAreaFilter(ddlFilterCategory, true);
        }

        /// <summary>
        /// Binds the fixed set of sort lanes/service areas. <paramref name="includeAny"/>
        /// adds an "All" option, used for the parcel-picker filter but not for the
        /// route-creation dropdown, which must always pin a route to exactly one area.
        /// </summary>
        private static void LoadServiceAreaFilter(DropDownList ddl, bool includeAny)
        {
            ddl.Items.Clear();
            if (includeAny) ddl.Items.Add(new ListItem("All sort lanes", ""));
            foreach (string category in SortingService.Categories)
            {
                ddl.Items.Add(new ListItem(category, category));
            }
        }

        /// <summary>Shows the destination lane the currently-selected route is pinned to.</summary>
        private void ShowRouteServiceArea()
        {
            if (ddlRoute.Items.Count == 0)
            {
                litRouteServiceArea.Text = string.Empty;
                return;
            }
            int routeId = int.Parse(ddlRoute.SelectedValue);
            string area = Db.ToStr(Db.Scalar("SELECT ServiceArea FROM dbo.[Route] WHERE RouteID = @r", "@r", routeId));
            litRouteServiceArea.Text = area.Length > 0
                ? "This route only accepts parcels destined for <strong>" + Server.HtmlEncode(area) + "</strong>."
                : "This route has no service area set (created before this restriction was added) - any parcel may be assigned.";
        }

        protected void ddlRoute_Changed(object sender, EventArgs e)
        {
            ShowRouteServiceArea();
        }

        /// <summary>
        /// Lists drivers best-first - available before on-route, then lightest load - and
        /// shows why, so the dispatcher can accept the top entry instead of working it out.
        /// </summary>
        private void LoadDrivers()
        {
            DataTable dt = Db.Query(
                @"SELECT d.UserID AS DriverID,
                         u.FullName + ' - ' + d.AvailabilityStatus + ' (' +
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
            DataTable dt = Db.Query("SELECT RouteID, RouteName FROM dbo.[Route] WHERE Status NOT IN ('Completed', 'Inactive') ORDER BY RouteDate DESC");
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
                    Db.ToStr(r["TrackingNumber"]) + " -> " + Db.ToStr(r["DestCity"]) + " (" + Db.ToStr(r["SortCategory"]) + ")",
                    Db.ToStr(r["ShipmentID"])));
            }
            pnlNoParcels.Visible = dt.Rows.Count == 0;
        }

        private void LoadRoutesTable()
        {
            DataTable dt = Db.Query(
                @"SELECT r.RouteID, r.RouteName, r.RouteDate, r.Status, ISNULL(r.ServiceArea, '(none)') AS ServiceArea,
                         ISNULL(u.FullName, '-') AS DriverName,
                         ISNULL(v.RegistrationNumber, '-') AS Vehicle,
                         (SELECT COUNT(*) FROM dbo.[Shipment] s WHERE s.RouteID = r.RouteID) AS ParcelCount,
                         (SELECT COUNT(*) FROM dbo.[Shipment] s WHERE s.RouteID = r.RouteID
                            AND s.DeliveryStatus IN ('Assigned','OutForDelivery')) AS ActiveCount
                  FROM dbo.[Route] r
                  LEFT JOIN dbo.[User] u ON u.UserID = r.DriverID
                  LEFT JOIN dbo.[Vehicle] v ON v.VehicleID = r.VehicleID
                  ORDER BY r.RouteDate DESC");
            rptRoutes.DataSource = dt;
            rptRoutes.DataBind();
            pnlNoRoutes.Visible = dt.Rows.Count == 0;
        }

        protected void rptRoutes_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            int routeId;
            if (!int.TryParse((string)e.CommandArgument, out routeId)) return;

            if (e.CommandName == "toggle")
            {
                string current = Db.ToStr(Db.Scalar("SELECT Status FROM dbo.[Route] WHERE RouteID = @r", "@r", routeId));
                string next = current == "Inactive" ? "Active" : "Inactive";

                // Whitelist the route status change: a Completed route is terminal and cannot be
                // reactivated or deactivated, and no other illegal jump is permitted either.
                if (!StateMachine.CanTransition("RouteStatus", current, next))
                {
                    lblRouteMsg.CssClass = UiHelper.AlertClass("error") + " mt-1";
                    lblRouteMsg.Text = current == "Completed"
                        ? "A completed route cannot be reactivated or deactivated."
                        : "This route cannot be changed from " + current + " to " + next + ".";
                    LoadRoutesTable();
                    return;
                }

                // Status-guarded UPDATE: a concurrent toggle that already moved the route makes
                // this a no-op (rows = 0) instead of applying a stale change on top of it.
                int toggled = Db.NonQuery(
                    "UPDATE dbo.[Route] SET Status = @s, UpdatedBy = @u, UpdatedDate = GETDATE() WHERE RouteID = @r AND Status = @cur",
                    "@s", next, "@u", UserId, "@r", routeId, "@cur", current);
                if (toggled != 1)
                {
                    lblRouteMsg.CssClass = UiHelper.AlertClass("error") + " mt-1";
                    lblRouteMsg.Text = "This route's status changed meanwhile. Refresh and try again.";
                    LoadRoutesTable();
                    return;
                }

                Db.NonQuery(
                    @"INSERT INTO dbo.AuditLog (UserID, Action, Entity, EntityID, Details)
                      VALUES (@u, 'Route.StatusChange', 'Route', @e, @d)",
                    "@u", UserId, "@e", routeId.ToString(), "@d", "Status set to " + next);

                lblRouteMsg.CssClass = UiHelper.AlertClass("success") + " mt-1";
                lblRouteMsg.Text = "Route " + (next == "Active" ? "activated." : "deactivated - it no longer accepts new parcel assignments.");
                LoadDrivers();
                LoadRoutesDropdown();
            }
            else if (e.CommandName == "delete")
            {
                // The FK from Shipment.RouteID would refuse the DELETE anyway, but check first
                // so the dispatcher gets a clear message instead of a raw SQL error, and so an
                // in-progress (active) assignment is called out distinctly from mere history.
                int activeCount = Db.ToInt(Db.Scalar(
                    @"SELECT COUNT(*) FROM dbo.[Shipment]
                      WHERE RouteID = @r AND DeliveryStatus IN ('Assigned','OutForDelivery')", "@r", routeId));
                int totalCount = Db.ToInt(Db.Scalar(
                    "SELECT COUNT(*) FROM dbo.[Shipment] WHERE RouteID = @r", "@r", routeId));

                if (activeCount > 0)
                {
                    lblRouteMsg.CssClass = UiHelper.AlertClass("error") + " mt-1";
                    lblRouteMsg.Text = activeCount + " parcel(s) on this route are still active. Reassign them to another route before deleting.";
                }
                else if (totalCount > 0)
                {
                    lblRouteMsg.CssClass = UiHelper.AlertClass("error") + " mt-1";
                    lblRouteMsg.Text = "This route has shipment history and cannot be deleted. Deactivate it instead.";
                }
                else
                {
                    Db.NonQuery("DELETE FROM dbo.[Route] WHERE RouteID = @r", "@r", routeId);
                    Db.NonQuery(
                        @"INSERT INTO dbo.AuditLog (UserID, Action, Entity, EntityID, Details)
                          VALUES (@u, 'Route.Deleted', 'Route', @e, 'Deleted - no parcels were ever assigned')",
                        "@u", UserId, "@e", routeId.ToString());
                    lblRouteMsg.CssClass = UiHelper.AlertClass("success") + " mt-1";
                    lblRouteMsg.Text = "Route deleted.";
                    LoadRoutesDropdown();
                }
            }

            LoadRoutesTable();
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
            if (ddlServiceArea.SelectedValue.Length == 0)
            {
                lblRouteMsg.CssClass = UiHelper.AlertClass("error") + " mt-1";
                lblRouteMsg.Text = "Please choose the service area this route covers.";
                return;
            }

            int driverId = int.Parse(ddlDriver.SelectedValue);
            int vehicleId = int.Parse(ddlVehicle.SelectedValue);
            string serviceArea = ddlServiceArea.SelectedValue;

            if (Db.ToInt(Db.Scalar(
                    "SELECT COUNT(*) FROM dbo.[Route] WHERE RouteName = @n AND Status <> 'Inactive'",
                    "@n", txtRouteName.Text.Trim())) > 0)
            {
                lblRouteMsg.CssClass = UiHelper.AlertClass("error") + " mt-1";
                lblRouteMsg.Text = "A route with that name already exists.";
                return;
            }

            // Create the route and put its driver On Route atomically, with the driver's
            // availability transition whitelisted: a route must never be left without its
            // driver flipped (or vice versa), and an Off Duty driver must not be silently
            // jumped straight to On Route - that illegal transition is rejected here so the
            // whole creation rolls back and nothing is written.
            string createError = null;
            Db.RunInTransaction((conn, tx) =>
            {
                string driverAvail = Db.ToStr(Db.Scalar(conn, tx,
                    "SELECT AvailabilityStatus FROM dbo.[Driver] WITH (UPDLOCK, ROWLOCK) WHERE UserID = @d",
                    "@d", driverId));

                bool alreadyOnRoute = string.Equals(driverAvail, "On Route", StringComparison.OrdinalIgnoreCase);
                if (!alreadyOnRoute && !StateMachine.CanTransition("DriverAvailability", driverAvail, "On Route"))
                {
                    createError = "This driver is " + driverAvail +
                                  " and cannot be put On Route. Set them Available under Fleet first.";
                    return;   // nothing written yet; the transaction rolls back
                }

                Db.NonQuery(conn, tx,
                    @"INSERT INTO dbo.[Route] (RouteName, DriverID, VehicleID, Status, ServiceArea, CreatedBy)
                      VALUES (@n, @d, @v, 'Active', @sa, @u)",
                    "@n", txtRouteName.Text.Trim(), "@d", driverId, "@v", vehicleId, "@sa", serviceArea, "@u", UserId);

                if (!alreadyOnRoute)
                {
                    // Status-guarded so the flip only lands from the state we validated.
                    Db.NonQuery(conn, tx,
                        "UPDATE dbo.[Driver] SET AvailabilityStatus = 'On Route' WHERE UserID = @d AND AvailabilityStatus = @from",
                        "@d", driverId, "@from", driverAvail);
                }
            });

            if (createError != null)
            {
                lblRouteMsg.CssClass = UiHelper.AlertClass("error") + " mt-1";
                lblRouteMsg.Text = createError;
                LoadDrivers();
                return;
            }

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
            DataTable route = Db.Query("SELECT DriverID, VehicleID, ServiceArea FROM dbo.[Route] WHERE RouteID = @r", "@r", routeId);
            if (route.Rows.Count == 0) return;
            int? driverId = route.Rows[0]["DriverID"] == DBNull.Value ? (int?)null : Db.ToInt(route.Rows[0]["DriverID"]);
            int? vehicleId = route.Rows[0]["VehicleID"] == DBNull.Value ? (int?)null : Db.ToInt(route.Rows[0]["VehicleID"]);
            string routeServiceArea = Db.ToStr(route.Rows[0]["ServiceArea"]);

            int assigned = 0;
            int blocked = 0;
            int mismatched = 0;
            int taken = 0;
            foreach (ListItem item in cblParcels.Items)
            {
                if (!item.Selected) continue;
                int shipId = int.Parse(item.Value);

                // Re-verify at the write itself - the posted checkbox value is not trusted
                // just because the list was filtered when it was rendered.
                DataTable ship = Db.Query(
                    "SELECT PaymentGateStatus, SortCategory, DestCity FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", shipId);
                if (ship.Rows.Count == 0) continue;

                string gateStatus = Db.ToStr(ship.Rows[0]["PaymentGateStatus"]);
                if (gateStatus != "NotRequired" && gateStatus != "PaymentSuccessful")
                {
                    blocked++;
                    continue;
                }

                // A route pinned to a service area (every route created since this check was
                // added) may only carry parcels destined for that same lane - an East London
                // parcel (SortCategory "Regional-Outlying") must never land on a Cape Town
                // ("Metro-CPT") route. Routes with no ServiceArea predate this restriction and
                // are left unrestricted rather than retroactively blocking their assignments.
                string parcelLane = Db.ToStr(ship.Rows[0]["SortCategory"]);
                if (routeServiceArea.Length > 0 && !string.Equals(parcelLane, routeServiceArea, StringComparison.OrdinalIgnoreCase))
                {
                    mismatched++;
                    continue;
                }

                // Assigned to a driver, awaiting collection. The guard makes a concurrent or
                // repeated assignment a no-op (rows = 0) instead of silently overwriting another
                // staff member's assignment or re-dispatching an already-dispatched parcel: only
                // a still-unassigned, still-sorted parcel is taken.
                int rows = Db.NonQuery(
                    @"UPDATE dbo.[Shipment]
                      SET RouteID = @r, DriverID = @d, WarehouseStatus = 'Dispatched',
                          DeliveryStatus = 'Assigned', LastStatusChange = GETDATE()
                      WHERE ShipmentID = @s AND RouteID IS NULL AND WarehouseStatus = 'Sorted'",
                    "@r", routeId, "@d", (object)driverId, "@s", shipId);
                if (rows != 1) { taken++; continue; }

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

            var notes = new System.Collections.Generic.List<string>();
            if (blocked > 0) notes.Add(blocked + " skipped - awaiting customer payment");
            if (mismatched > 0) notes.Add(mismatched + " skipped - destination does not match this route's service area (" + routeServiceArea + ")");
            if (taken > 0) notes.Add(taken + " skipped - already assigned to another route");
            string noteText = notes.Count > 0 ? " " + string.Join("; ", notes) + "." : "";

            lblAssignMsg.CssClass = UiHelper.AlertClass(assigned > 0) + " mt-1";
            lblAssignMsg.Text = assigned > 0
                ? assigned + " parcel(s) assigned to the driver for collection." + noteText
                : (notes.Count > 0
                    ? "No parcels were assigned." + noteText
                    : "Select at least one parcel to assign.");
            LoadParcels();
            LoadRoutesTable();
        }
    }
}
