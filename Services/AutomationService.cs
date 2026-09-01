using System;
using System.Data;
using ForteLogisticsApp.Data;

namespace ForteLogisticsApp.Services
{
    /// <summary>
    /// Event-driven automation. These run off actions staff already perform, so the
    /// system advances its own state instead of waiting for someone to remember.
    ///
    /// Every method is idempotent - calling it twice does nothing the second time.
    /// That is what lets the manual Finance buttons and the automatic hooks share one
    /// implementation without ever double-invoicing.
    ///
    /// Deliberately NOT automated (each stays a human decision):
    ///   - marking a parcel Delivered  (proof of delivery is a contractual event)
    ///   - declaring a parcel Lost/Damaged  (a liability claim)
    ///   - taking payment or marking an invoice Paid
    ///   - assigning parcels to a driver without a dispatcher confirming
    /// </summary>
    public static class AutomationService
    {
        // ---------------------------------------------------------------- billing

        /// <summary>
        /// Raises the invoice for a delivered shipment if one does not already exist.
        /// Returns the new invoice number, or null if nothing was done.
        /// </summary>
        public static string EnsureInvoice(int shipmentId)
        {
            if (shipmentId <= 0) return null;

            DataTable dt = Db.Query(
                @"SELECT SenderID, Cost, TrackingNumber, DeliveryStatus
                  FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", shipmentId);
            if (dt.Rows.Count == 0) return null;

            DataRow r = dt.Rows[0];
            if (Db.ToStr(r["DeliveryStatus"]) != "Delivered") return null;

            // Already invoiced - nothing to do.
            if (Db.ToInt(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.[Payment] WHERE ShipmentID = @s", "@s", shipmentId)) > 0) return null;

            string invoiceNo = CourierService.GenerateInvoiceNumber();
            object clientId = r["SenderID"] == DBNull.Value ? (object)DBNull.Value : Db.ToInt(r["SenderID"]);
            decimal amount = Db.ToDecimal(r["Cost"]);

            Db.NonQuery(
                @"INSERT INTO dbo.[Payment] (InvoiceNumber, ClientID, ShipmentID, Amount, PaymentStatus, DueDate)
                  VALUES (@inv, @c, @s, @a, 'Unpaid', DATEADD(day, 7, GETDATE()))",
                "@inv", invoiceNo, "@c", clientId, "@s", shipmentId, "@a", amount);

            if (clientId != DBNull.Value)
            {
                NotificationService.Send(Db.ToInt(clientId), shipmentId,
                    "Invoice " + invoiceNo + " for parcel " + Db.ToStr(r["TrackingNumber"]) +
                    " (R " + amount.ToString("N2") + ") has been issued and is due in 7 days.", "System");
            }
            return invoiceNo;
        }

        /// <summary>Invoices every delivered shipment that has no payment record. Returns the count.</summary>
        public static int AutoInvoiceDelivered()
        {
            DataTable dt = Db.Query(
                @"SELECT s.ShipmentID FROM dbo.[Shipment] s
                  WHERE s.DeliveryStatus = 'Delivered'
                    AND NOT EXISTS (SELECT 1 FROM dbo.[Payment] p WHERE p.ShipmentID = s.ShipmentID)");

            int count = 0;
            foreach (DataRow r in dt.Rows)
            {
                if (EnsureInvoice(Db.ToInt(r["ShipmentID"])) != null) count++;
            }
            return count;
        }

        // ------------------------------------------------- route / driver lifecycle

        /// <summary>
        /// Closes any active route whose parcels have all reached a final state.
        /// Returns the number of routes completed.
        /// </summary>
        public static int CompleteFinishedRoutes()
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
        /// Returns drivers to 'Available' once they hold no active route and no open parcels.
        /// 'Off Duty' is excluded on purpose so automation never overrides a manual flag.
        /// </summary>
        public static int ReleaseIdleDrivers()
        {
            return Db.NonQuery(
                @"UPDATE d SET AvailabilityStatus = 'Available'
                  FROM dbo.[Driver] d
                  WHERE d.AvailabilityStatus = 'On Route'
                    AND NOT EXISTS (SELECT 1 FROM dbo.[Route] r
                                    WHERE r.DriverID = d.UserID AND r.Status = 'Active')
                    AND NOT EXISTS (SELECT 1 FROM dbo.[Shipment] s
                                    WHERE s.DriverID = d.UserID
                                      AND s.DeliveryStatus IN ('Assigned','OutForDelivery'))");
        }

        // ------------------------------------------------------------- escalation

        /// <summary>
        /// Routes a warehouse exception to the people who can act on it. Automation only
        /// ever escalates an exception a human declared - it never declares one itself.
        /// </summary>
        public static void EscalateException(int shipmentId, string exceptionType, int actorUserId)
        {
            if (shipmentId <= 0 || string.IsNullOrEmpty(exceptionType) || exceptionType == "None") return;

            DataTable dt = Db.Query(
                "SELECT TrackingNumber, SenderID FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", shipmentId);
            if (dt.Rows.Count == 0) return;

            string tracking = Db.ToStr(dt.Rows[0]["TrackingNumber"]);
            string key = "EXC:" + shipmentId + ":" + exceptionType;
            string staffMsg = "EXCEPTION: parcel " + tracking + " has been flagged as " + exceptionType + ".";

            // Dispatch and Manager were separate roles; both are Staff now, so this is one call.
            SendOnceToRole("Staff", shipmentId, staffMsg, "Delay", key);

            if (exceptionType == "Lost")
            {
                // Claims and credits are an Admin concern now that Finance has been folded in.
                SendOnceToRole("Admin", shipmentId,
                    "Parcel " + tracking + " has been reported LOST - a claim or credit may be required.",
                    "Delay", key);
            }

            // The customer gets a calmer version - no internal exception vocabulary.
            if (dt.Rows[0]["SenderID"] != DBNull.Value)
            {
                NotificationService.SendOnce(Db.ToInt(dt.Rows[0]["SenderID"]), shipmentId,
                    "There is a delay with parcel " + tracking +
                    ". Our team is investigating and will be in touch.", "Delay", key);
            }
        }

        /// <summary>
        /// Handles a failed delivery. Below the attempt cap the parcel is returned to the
        /// dispatch pool so it reappears on Route Planning automatically; at the cap it
        /// stops and goes to a manager for a human decision.
        /// </summary>
        public const int MaxDeliveryAttempts = 3;

        public static void EscalateFailedDelivery(int shipmentId, int driverId, string notes)
        {
            if (shipmentId <= 0) return;

            DataTable dt = Db.Query(
                "SELECT TrackingNumber, AttemptCount, SenderID FROM dbo.[Shipment] WHERE ShipmentID = @s",
                "@s", shipmentId);
            if (dt.Rows.Count == 0) return;

            string tracking = Db.ToStr(dt.Rows[0]["TrackingNumber"]);
            int attempts = Db.ToInt(dt.Rows[0]["AttemptCount"]) + 1;
            bool retry = attempts < MaxDeliveryAttempts;

            Db.NonQuery(
                @"UPDATE dbo.[Shipment]
                  SET AttemptCount     = @n,
                      LastStatusChange = GETDATE(),
                      DeliveryStatus   = CASE WHEN @retry = 1 THEN 'Sorted'  ELSE 'Failed' END,
                      WarehouseStatus  = CASE WHEN @retry = 1 THEN 'Sorted'  ELSE WarehouseStatus END,
                      RouteID          = CASE WHEN @retry = 1 THEN NULL      ELSE RouteID END,
                      DriverID         = CASE WHEN @retry = 1 THEN NULL      ELSE DriverID END
                  WHERE ShipmentID = @s",
                "@n", attempts, "@retry", retry ? 1 : 0, "@s", shipmentId);

            Db.NonQuery(
                @"INSERT INTO dbo.[TrackingHistory] (ShipmentID, DriverID, Location, Status, Remarks)
                  VALUES (@s, @d, 'Depot', @st, @rm)",
                "@s", shipmentId, "@d", driverId > 0 ? (object)driverId : DBNull.Value,
                "@st", retry ? "Requeued" : "Failed",
                "@rm", retry
                    ? "Delivery attempt " + attempts + " failed - returned to the dispatch pool for re-delivery."
                    : "Delivery failed after " + attempts + " attempts - escalated for a manual decision.");

            if (retry)
            {
                NotificationService.SendToRole("Staff", shipmentId,
                    "Parcel " + tracking + " failed delivery (attempt " + attempts + " of " +
                    MaxDeliveryAttempts + ") and is back in the pool for re-assignment.", "Delay");
            }
            else
            {
                // Previously two sends, to Manager and to Dispatch. Both are Staff now, and
                // SendToRole does not deduplicate, so keep this as a single combined message -
                // otherwise every staff user is notified twice for one event.
                NotificationService.SendToRole("Staff", shipmentId,
                    "Parcel " + tracking + " has exhausted its delivery attempts (" + attempts +
                    ") and needs a decision.", "Delay");
            }
        }

        // ------------------------------------------------------------ composite hook

        /// <summary>
        /// Everything that should follow a driver confirming an outcome. Called from
        /// Driver/DeliveryConfirmation so that page stays about capturing the outcome.
        /// </summary>
        public static void OnDeliveryOutcome(int shipmentId, int driverId, bool delivered, string notes)
        {
            if (delivered)
            {
                EnsureInvoice(shipmentId);
                RequestRating(shipmentId);
            }
            else
            {
                EscalateFailedDelivery(shipmentId, driverId, notes);
            }

            // The parcel just reached a final state, so a route may now be finished
            // and its driver free again.
            if (CompleteFinishedRoutes() > 0)
            {
                NotificationService.SendToRole("Staff", null,
                    "A delivery route has been completed and its driver is available again.", "Dispatch");
            }
            ReleaseIdleDrivers();
        }

        /// <summary>Invites the customer to rate the driver instead of waiting for them to visit Track.</summary>
        private static void RequestRating(int shipmentId)
        {
            DataTable dt = Db.Query(
                @"SELECT SenderID, DriverID, TrackingNumber
                  FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", shipmentId);
            if (dt.Rows.Count == 0) return;
            if (dt.Rows[0]["SenderID"] == DBNull.Value || dt.Rows[0]["DriverID"] == DBNull.Value) return;

            NotificationService.SendOnce(Db.ToInt(dt.Rows[0]["SenderID"]), shipmentId,
                "Your parcel " + Db.ToStr(dt.Rows[0]["TrackingNumber"]) +
                " was delivered. Open Track to rate your driver.", "Delivery", "RATE:" + shipmentId);
        }

        private static void SendOnceToRole(string role, int? shipmentId, string message, string type, string key)
        {
            DataTable users = Db.Query("SELECT UserID FROM dbo.[User] WHERE Role = @r", "@r", role);
            foreach (DataRow u in users.Rows)
            {
                NotificationService.SendOnce(Db.ToInt(u["UserID"]), shipmentId, message, type, key);
            }
        }
    }
}
