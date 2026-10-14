using System;
using System.Data;

namespace ForteLogisticsApp.Data
{
    /// <summary>Thrown when a resource is not found OR does not belong to the caller - the
    /// two cases are deliberately indistinguishable so an attacker cannot probe for which
    /// IDs exist.</summary>
    public class AccessDeniedException : Exception
    {
        public AccessDeniedException(string message) : base(message) { }
    }

    /// <summary>
    /// Owner-scoped fetches. A user-facing page must never load a shipment/invoice by its
    /// primary key alone - the owner predicate (SenderID for customers, DriverID for drivers)
    /// is baked into the query here so an ID swapped in the URL or a hidden field returns
    /// "not yours", never a stranger's data.
    /// </summary>
    public static class Owned
    {
        public static DataRow RequireShipmentForCustomer(int shipmentId, int customerUserId)
        {
            DataTable dt = Db.Query(
                @"SELECT ShipmentID, TrackingNumber, DeliveryStatus, OriginCity, DestCity,
                         DriverID, SenderID, PaymentGateStatus
                  FROM dbo.[Shipment] WHERE ShipmentID = @s AND SenderID = @c",
                "@s", shipmentId, "@c", customerUserId);
            if (dt.Rows.Count == 0)
                throw new AccessDeniedException("That shipment could not be found on your account.");
            return dt.Rows[0];
        }

        public static DataRow RequireShipmentForDriver(int shipmentId, int driverUserId)
        {
            DataTable dt = Db.Query(
                @"SELECT ShipmentID, TrackingNumber, DeliveryStatus, DestCity, ReceiverName, SenderID
                  FROM dbo.[Shipment] WHERE ShipmentID = @s AND DriverID = @d",
                "@s", shipmentId, "@d", driverUserId);
            if (dt.Rows.Count == 0)
                throw new AccessDeniedException("This parcel is not assigned to you.");
            return dt.Rows[0];
        }

        public static DataRow RequireInvoiceForCustomer(int paymentId, int customerUserId)
        {
            DataTable dt = Db.Query(
                @"SELECT PaymentID, InvoiceNumber, Amount, PaymentStatus, ShipmentID, ClientID
                  FROM dbo.[Payment] WHERE PaymentID = @p AND ClientID = @c",
                "@p", paymentId, "@c", customerUserId);
            if (dt.Rows.Count == 0)
                throw new AccessDeniedException("That invoice could not be found on your account.");
            return dt.Rows[0];
        }
    }
}
