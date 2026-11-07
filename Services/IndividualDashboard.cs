using System;
using System.Data;
using System.Globalization;
using ForteLogisticsApp.Data;

namespace ForteLogisticsApp.Services
{
    /// <summary>
    /// What an Individual customer sees: their own parcels, in their own words. Business
    /// customers keep the analytics dashboard built by <see cref="RoleDashboards"/>.
    /// </summary>
    public static class IndividualDashboard
    {
        private const string Finished = "('Delivered','Failed','Cancelled')";
        private const string CardColumns = "ShipmentID, TrackingNumber, OriginCity, DestCity, DeliveryStatus, ShipmentDate, DeliveryDate";
        private const int MetroTransitDays = 2;
        private const int RegionalTransitDays = 4;

        public static bool IsBusinessClient(int userId)
        {
            string clientType = Db.ToStr(Db.Scalar("SELECT ClientType FROM dbo.[Client] WHERE UserID = @u", "@u", userId));
            return string.Equals(clientType, "Business", StringComparison.OrdinalIgnoreCase);
        }

        public static int ActiveCount(int userId)
        {
            return Db.ToInt(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.[Shipment] WHERE SenderID = @u AND DeliveryStatus NOT IN " + Finished,
                "@u", userId));
        }

        public static int CompletedCount(int userId)
        {
            return Db.ToInt(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.[Shipment] WHERE SenderID = @u AND DeliveryStatus = 'Delivered'",
                "@u", userId));
        }

        public static DataTable Recent(int userId, int limit)
        {
            return Db.Query(
                "SELECT TOP (@n) " + CardColumns + " FROM dbo.[Shipment] WHERE SenderID = @u ORDER BY ShipmentDate DESC",
                "@n", limit, "@u", userId);
        }

        public static DataTable Active(int userId)
        {
            return Db.Query(
                "SELECT " + CardColumns + " FROM dbo.[Shipment] WHERE SenderID = @u AND DeliveryStatus NOT IN " + Finished +
                " ORDER BY ShipmentDate DESC",
                "@u", userId);
        }

        public static DataTable History(int userId)
        {
            return Db.Query(
                "SELECT " + CardColumns + " FROM dbo.[Shipment] WHERE SenderID = @u AND DeliveryStatus IN " + Finished +
                " ORDER BY ShipmentDate DESC",
                "@u", userId);
        }

        /// <summary>Individual parcels stay out of the warehouse until paid, so these need the customer.</summary>
        public static DataTable AwaitingPayment(int userId)
        {
            return Db.Query(
                "SELECT " + CardColumns + @" FROM dbo.[Shipment]
                  WHERE SenderID = @u AND PaymentGateStatus IN ('PaymentRequired','PaymentPending')
                  ORDER BY ShipmentDate",
                "@u", userId);
        }

        public static string Greeting(string fullName, DateTime now)
        {
            string period = now.Hour < 12 ? "morning" : now.Hour < 17 ? "afternoon" : "evening";
            string[] names = (fullName ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return "Good " + period + (names.Length > 0 ? ", " + names[0] : string.Empty);
        }

        public static string StatusLabel(string status)
        {
            switch (status)
            {
                case "Registered": return "Booked";
                case "Sorted": return "At depot";
                case "Assigned": return "With driver";
                case "InTransit": return "In transit";
                case "OutForDelivery": return "Out for delivery";
                case "Failed": return "Delivery failed";
                default: return status ?? string.Empty;
            }
        }

        /// <summary>
        /// The schema stores no promised date, so the estimate is the booking date plus a fixed
        /// transit time for the destination's lane.
        /// </summary>
        public static string DeliveryNote(string status, DateTime shipmentDate, DateTime? deliveryDate, string destCity, DateTime now)
        {
            if (status == "Delivered")
                return deliveryDate.HasValue ? "Delivered " + LongDate(deliveryDate.Value) : "Delivered";
            if (status == "Failed" || status == "Cancelled")
                return "We could not complete this delivery";

            bool metro = SortingService.CategoryFor(destCity).StartsWith("Metro", StringComparison.Ordinal);
            DateTime estimate = shipmentDate.Date.AddDays(metro ? MetroTransitDays : RegionalTransitDays);
            return estimate < now.Date ? "Running late" : "Estimated delivery: " + LongDate(estimate);
        }

        private static string LongDate(DateTime date)
        {
            return date.ToString("d MMMM", CultureInfo.InvariantCulture);
        }
    }
}
