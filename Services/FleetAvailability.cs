using System;
using System.Data;
using System.Data.SqlClient;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;

namespace ForteLogisticsApp.Services
{
    /// <summary>
    /// The one definition of who and what is free to take a route. Availability is worked out
    /// from a driver's real parcels and routes; Driver.AvailabilityStatus is a display copy of
    /// that answer, kept true by <see cref="Sync"/>, except for 'Off Duty', which is a manual
    /// flag that automation never overrides.
    /// </summary>
    public static class FleetAvailability
    {
        private const string LiveRoute = "r.Status IN ('Active','Planned')";
        private const string OpenParcel = "s.DeliveryStatus IN ('Assigned','OutForDelivery')";

        private const string DriverIsBusy =
            "(EXISTS (SELECT 1 FROM dbo.[Shipment] s WHERE s.DriverID = d.UserID AND " + OpenParcel + ")" +
            " OR EXISTS (SELECT 1 FROM dbo.[Route] r WHERE r.DriverID = d.UserID AND " + LiveRoute + "))";

        public static DataTable AvailableDrivers()
        {
            return Db.Query(
                @"SELECT d.UserID AS DriverID, u.FullName + ' - Available' AS Label
                  FROM dbo.[Driver] d
                  JOIN dbo.[User] u ON u.UserID = d.UserID
                  WHERE d.AvailabilityStatus <> 'Off Duty' AND NOT " + DriverIsBusy + @"
                  ORDER BY u.FullName");
        }

        public static DataTable AvailableVehicles()
        {
            return Db.Query(
                @"SELECT v.VehicleID, v.RegistrationNumber + ' (' + ISNULL(v.VehicleType, 'Vehicle') + ')' AS Label
                  FROM dbo.[Vehicle] v
                  WHERE NOT EXISTS (SELECT 1 FROM dbo.[Route] r WHERE r.VehicleID = v.VehicleID AND " + LiveRoute + @")
                  ORDER BY v.RegistrationNumber");
        }

        /// <summary>
        /// Brings routes and driver statuses into line with the parcels. Safe to run at any
        /// time. Returns the number of routes it completed.
        /// </summary>
        public static int Sync()
        {
            int completed = CompleteFinishedRoutes();
            DeactivateAbandonedRoutes();
            ReleaseIdleDrivers();
            MarkBusyDrivers();
            return completed;
        }

        private static int CompleteFinishedRoutes()
        {
            return Db.NonQuery(
                @"UPDATE r SET Status = 'Completed', CompletedDate = GETDATE()
                  FROM dbo.[Route] r
                  WHERE r.Status = 'Active'
                    AND EXISTS (SELECT 1 FROM dbo.[Shipment] s WHERE s.RouteID = r.RouteID)
                    AND NOT EXISTS (SELECT 1 FROM dbo.[Shipment] s
                                    WHERE s.RouteID = r.RouteID
                                      AND s.DeliveryStatus NOT IN ('Delivered','Failed'))");
        }

        /// <summary>
        /// A route created today may be waiting for its parcels, so it keeps its driver and
        /// vehicle. One from an earlier day that never received a parcel never will.
        /// </summary>
        private static void DeactivateAbandonedRoutes()
        {
            Db.NonQuery(
                @"UPDATE r SET Status = 'Inactive', UpdatedDate = GETDATE()
                  FROM dbo.[Route] r
                  WHERE " + LiveRoute + @"
                    AND r.RouteDate < CAST(GETDATE() AS DATE)
                    AND NOT EXISTS (SELECT 1 FROM dbo.[Shipment] s WHERE s.RouteID = r.RouteID)");
        }

        private static void ReleaseIdleDrivers()
        {
            Db.NonQuery(
                @"UPDATE d SET AvailabilityStatus = 'Available'
                  FROM dbo.[Driver] d
                  WHERE d.AvailabilityStatus = 'On Route' AND NOT " + DriverIsBusy);
        }

        private static void MarkBusyDrivers()
        {
            Db.NonQuery(
                @"UPDATE d SET AvailabilityStatus = 'On Route'
                  FROM dbo.[Driver] d
                  WHERE d.AvailabilityStatus = 'Available' AND " + DriverIsBusy);
        }

        /// <summary>
        /// Creates a route and puts its driver On Route in one transaction. The driver and
        /// vehicle rows are locked before the checks, so two dispatchers holding the same stale
        /// dropdown cannot both book them. Returns a user-safe error, or null on success.
        /// </summary>
        public static string CreateRoute(string routeName, int driverId, int vehicleId, string serviceArea, int actorUserId)
        {
            string error = null;
            Db.RunInTransaction((conn, tx) =>
            {
                string availability = Db.ToStr(Db.Scalar(conn, tx,
                    "SELECT AvailabilityStatus FROM dbo.[Driver] WITH (UPDLOCK, ROWLOCK) WHERE UserID = @d",
                    "@d", driverId));
                bool flaggedOnRoute = string.Equals(availability, "On Route", StringComparison.OrdinalIgnoreCase);
                if (!flaggedOnRoute && !StateMachine.CanTransition("DriverAvailability", availability, "On Route"))
                {
                    error = "This driver is " + availability + " and cannot be put On Route. Set them Available under Fleet first.";
                    return;
                }

                Db.Scalar(conn, tx, "SELECT VehicleID FROM dbo.[Vehicle] WITH (UPDLOCK, ROWLOCK) WHERE VehicleID = @v", "@v", vehicleId);

                error = Conflict(conn, tx, driverId, vehicleId, null);
                if (error != null) return;

                Db.NonQuery(conn, tx,
                    @"INSERT INTO dbo.[Route] (RouteName, DriverID, VehicleID, Status, ServiceArea, CreatedBy)
                      VALUES (@n, @d, @v, 'Active', @sa, @u)",
                    "@n", routeName, "@d", driverId, "@v", vehicleId, "@sa", serviceArea, "@u", actorUserId);

                Db.NonQuery(conn, tx,
                    "UPDATE dbo.[Driver] SET AvailabilityStatus = 'On Route' WHERE UserID = @d", "@d", driverId);
            });
            return error;
        }

        /// <summary>
        /// Why an Inactive route cannot go back on the road, or null if it can: while it was
        /// parked, its driver or vehicle may have been given to another route.
        /// </summary>
        public static string ReactivationConflict(int routeId)
        {
            DataTable route = Db.Query("SELECT DriverID, VehicleID FROM dbo.[Route] WHERE RouteID = @r", "@r", routeId);
            if (route.Rows.Count == 0) return null;

            int? driverId = route.Rows[0]["DriverID"] == DBNull.Value ? (int?)null : Db.ToInt(route.Rows[0]["DriverID"]);
            int? vehicleId = route.Rows[0]["VehicleID"] == DBNull.Value ? (int?)null : Db.ToInt(route.Rows[0]["VehicleID"]);

            string conflict = null;
            Db.RunInTransaction((conn, tx) => conflict = Conflict(conn, tx, driverId, vehicleId, routeId));
            return conflict;
        }

        private static string Conflict(SqlConnection conn, SqlTransaction tx, int? driverId, int? vehicleId, int? ignoredRouteId)
        {
            int ignored = ignoredRouteId ?? 0;

            if (driverId.HasValue)
            {
                int driverRoutes = Db.ToInt(Db.Scalar(conn, tx,
                    "SELECT COUNT(*) FROM dbo.[Route] r WHERE r.DriverID = @d AND r.RouteID <> @self AND " + LiveRoute,
                    "@d", driverId.Value, "@self", ignored));
                int driverParcels = Db.ToInt(Db.Scalar(conn, tx,
                    "SELECT COUNT(*) FROM dbo.[Shipment] s WHERE s.DriverID = @d AND (s.RouteID IS NULL OR s.RouteID <> @self) AND " + OpenParcel,
                    "@d", driverId.Value, "@self", ignored));
                if (driverRoutes + driverParcels > 0)
                    return "This driver is already busy with another route or parcels. Choose a driver who is free.";
            }

            if (vehicleId.HasValue)
            {
                int vehicleRoutes = Db.ToInt(Db.Scalar(conn, tx,
                    "SELECT COUNT(*) FROM dbo.[Route] r WHERE r.VehicleID = @v AND r.RouteID <> @self AND " + LiveRoute,
                    "@v", vehicleId.Value, "@self", ignored));
                if (vehicleRoutes > 0)
                    return "This vehicle is already on another route. Choose a vehicle that is free.";
            }

            return null;
        }
    }
}
