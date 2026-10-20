using System;
using System.Data;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;

namespace ForteLogisticsApp.Services
{
    /// <summary>
    /// The single, transactional path for changing a shipment's DeliveryStatus. Every status
    /// change must route through here - grep should find no inline "UPDATE Shipment SET
    /// DeliveryStatus" anywhere else.
    ///
    /// It applies the same discipline the payment flow proved: one transaction, an UPDLOCK'd
    /// read, a whitelist check (<see cref="StateMachine"/>), a status-guarded UPDATE whose
    /// @@ROWCOUNT must be exactly 1 (so a concurrent change loses rather than silently
    /// overwrites), and a TrackingHistory + AuditLog entry written in the same transaction.
    /// Ownership is the caller's responsibility (use <see cref="Owned"/>); this guards the
    /// state transition itself.
    /// </summary>
    public static class ShipmentTransitionService
    {
        /// <summary>
        /// Moves one shipment to <paramref name="newStatus"/>. Throws
        /// <see cref="InvalidOperationException"/> (with a user-safe message) when the shipment
        /// is gone, the transition is not whitelisted, or another request changed the row first.
        /// </summary>
        public static void TransitionDelivery(
            int shipmentId, int actorUserId, string newStatus, string location, string remark)
        {
            if (!StateMachine.IsKnownState("DeliveryStatus", newStatus))
                throw new InvalidOperationException("Unknown delivery status.");

            Db.RunInTransaction((conn, tx) =>
            {
                DataTable cur = Db.Query(conn, tx,
                    "SELECT DeliveryStatus FROM dbo.[Shipment] WITH (UPDLOCK, ROWLOCK) WHERE ShipmentID = @s",
                    "@s", shipmentId);
                if (cur.Rows.Count == 0)
                    throw new InvalidOperationException("That parcel could not be found.");

                string from = Db.ToStr(cur.Rows[0]["DeliveryStatus"]);
                if (!StateMachine.CanTransition("DeliveryStatus", from, newStatus))
                    throw new InvalidOperationException(FriendlyReject(from, newStatus));

                // Side-effect columns are chosen from a fixed whitelist of target statuses,
                // never concatenated from user input, so this cannot inject SQL. AttemptCount
                // on a failed delivery is owned by AutomationService.OnDeliveryOutcome (called
                // after this commit), so it is deliberately NOT touched here.
                string extra = string.Empty;
                if (newStatus == "OutForDelivery") extra = ", CollectedDate = GETDATE()";
                else if (newStatus == "Delivered") extra = ", DeliveryDate = GETDATE()";

                int rows = Db.NonQuery(conn, tx,
                    "UPDATE dbo.[Shipment] SET DeliveryStatus = @to, LastStatusChange = GETDATE()" + extra +
                    " WHERE ShipmentID = @s AND DeliveryStatus = @from",
                    "@to", newStatus, "@s", shipmentId, "@from", from);
                if (rows != 1)
                    throw new InvalidOperationException("This parcel was just updated by someone else. Please reload and try again.");

                Db.NonQuery(conn, tx,
                    @"INSERT INTO dbo.[TrackingHistory] (ShipmentID, DriverID, Location, Status, Remarks)
                      VALUES (@s, @d, @loc, @st, @rm)",
                    "@s", shipmentId, "@d", actorUserId, "@loc", location ?? string.Empty,
                    "@st", newStatus, "@rm", remark ?? string.Empty);

                Db.NonQuery(conn, tx,
                    @"INSERT INTO dbo.AuditLog (UserID, Action, Entity, EntityID, Details)
                      VALUES (@u, 'Shipment.Transition', 'Shipment', @e, @d)",
                    "@u", actorUserId, "@e", shipmentId.ToString(),
                    "@d", from + " -> " + newStatus + (string.IsNullOrEmpty(remark) ? string.Empty : " (" + remark + ")"));

                // Live positions are personal data with a purpose: once the parcel is no longer
                // with a driver the driver's position goes, and once it is finished, all of them.
                if (newStatus == "Delivered" || newStatus == "Failed")
                {
                    Db.NonQuery(conn, tx, "DELETE FROM dbo.LocationPing WHERE ShipmentID = @s", "@s", shipmentId);
                }
                else if (newStatus != "Assigned" && newStatus != "OutForDelivery")
                {
                    Db.NonQuery(conn, tx, "DELETE FROM dbo.LocationPing WHERE ShipmentID = @s AND Role = 'Driver'", "@s", shipmentId);
                }
            });
        }

        private static string FriendlyReject(string from, string to)
        {
            if (to == "Delivered" && (from == "Assigned" || from == "Sorted" || from == "Registered"))
                return "This parcel has not been collected yet, so it cannot be marked delivered.";
            if (to == "OutForDelivery" && from != "Assigned")
                return "This parcel is not awaiting collection.";
            if (from == "Delivered")
                return "This parcel has already been delivered.";
            return "This parcel cannot move from " + from + " to " + to + " right now.";
        }
    }
}
