using System;
using ForteLogisticsApp.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ForteLogisticsApp.Tests.Integration
{
    /// <summary>
    /// Shared setup for tests that need a real Logisticore database (see Tests\App.config for
    /// the connection string). These exercise PaymentService/RoutePlanning end to end against
    /// actual tables rather than mocking Db, since Db is a thin, already-parameterised ADO.NET
    /// wrapper with nothing worth mocking - the behaviour worth testing is what the SQL and
    /// the surrounding transaction actually do.
    ///
    /// EnsureDatabaseAvailable marks a test Inconclusive (not failed) when no SQL Server is
    /// reachable, so the suite stays green in an environment without one (e.g. a CI runner or
    /// sandbox with no local SQLEXPRESS instance) while still running for real wherever a dev
    /// database is present.
    /// </summary>
    public static class DbTestHelper
    {
        public static void EnsureDatabaseAvailable()
        {
            try
            {
                using (var conn = Db.GetConnection())
                {
                    conn.Open();
                }
            }
            catch (Exception ex)
            {
                Assert.Inconclusive(
                    "Skipped: no reachable Logisticore database (see Tests\\App.config). " + ex.Message);
            }
        }

        /// <summary>Creates a bare User + Client row and returns the new UserID.</summary>
        public static int CreateTestClient(string emailPrefix, string clientType, int? businessId = null)
        {
            string email = emailPrefix + "-" + Guid.NewGuid().ToString("N").Substring(0, 8) + "@test.invalid";
            int userId = Db.Insert(
                @"INSERT INTO dbo.[User] (FullName, Password, Email, Role)
                  VALUES (@n, 'x', @e, 'Customer')",
                "@n", "Test " + emailPrefix, "@e", email);

            Db.NonQuery(
                "INSERT INTO dbo.[Client] (UserID, ClientType, BusinessID) VALUES (@u, @t, @b)",
                "@u", userId, "@t", clientType, "@b", businessId);

            return userId;
        }

        public static int CreateTestBusiness(string name)
        {
            return Db.Insert(
                "INSERT INTO dbo.Business (BusinessName) VALUES (@n)",
                "@n", name + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
        }

        /// <summary>Creates a Shipment + its Payment row, mirroring what BookShipment.aspx.cs writes.</summary>
        public static int CreateTestShipmentWithInvoice(
            int senderId, decimal amount, string paymentGateStatus, string paymentStatus, out int paymentId)
        {
            string tracking = "TEST-" + Guid.NewGuid().ToString("N").Substring(0, 10).ToUpperInvariant();
            int shipmentId = Db.Insert(
                @"INSERT INTO dbo.[Shipment]
                    (TrackingNumber, SenderID, PickupAddress, DeliveryAddress, OriginCity, DestCity,
                     ParcelWeight, Cost, DeliveryStatus, WarehouseStatus, PaymentGateStatus)
                  VALUES (@t, @s, 'Test pickup', 'Test delivery', 'Johannesburg', 'Cape Town',
                          2, @c, 'Registered', 'Pending', @pgs)",
                "@t", tracking, "@s", senderId, "@c", amount, "@pgs", paymentGateStatus);

            paymentId = Db.Insert(
                @"INSERT INTO dbo.[Payment] (ClientID, ShipmentID, InvoiceNumber, Amount, PaymentStatus)
                  VALUES (@c, @s, @inv, @a, @ps)",
                "@c", senderId, "@s", shipmentId,
                "@inv", "TEST-" + Guid.NewGuid().ToString("N").Substring(0, 8), "@a", amount, "@ps", paymentStatus);

            return shipmentId;
        }

        public static void DeleteShipmentAndPayment(int shipmentId, int paymentId)
        {
            Db.NonQuery("DELETE FROM dbo.PaymentAllocation WHERE PaymentID = @p", "@p", paymentId);
            Db.NonQuery("DELETE FROM dbo.[TrackingHistory] WHERE ShipmentID = @s", "@s", shipmentId);
            Db.NonQuery("DELETE FROM dbo.AuditLog WHERE EntityID = @e", "@e", shipmentId.ToString());
            Db.NonQuery("DELETE FROM dbo.[Payment] WHERE PaymentID = @p", "@p", paymentId);
            Db.NonQuery("DELETE FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", shipmentId);
        }

        public static void DeleteClient(int userId)
        {
            // PayInvoices logs an AuditLog row (UserID = the payer) and a PaymentAllocation
            // row (AppliedBy = the payer) - both FK back to User, so both must go first.
            Db.NonQuery("DELETE FROM dbo.AuditLog WHERE UserID = @u", "@u", userId);
            Db.NonQuery("DELETE FROM dbo.PaymentAllocation WHERE AppliedBy = @u", "@u", userId);
            Db.NonQuery("DELETE FROM dbo.[Client] WHERE UserID = @u", "@u", userId);
            Db.NonQuery("DELETE FROM dbo.[User] WHERE UserID = @u", "@u", userId);
        }

        public static void DeleteBusiness(int businessId)
        {
            Db.NonQuery("DELETE FROM dbo.Business WHERE BusinessID = @b", "@b", businessId);
        }
    }
}
