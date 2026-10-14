using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Tests.Integration
{
    /// <summary>
    /// End-to-end checks that ShipmentTransitionService actually enforces the whitelist and
    /// the concurrency guard against real tables - the "abuse" cases the review asked for
    /// alongside the service, not deferred. Each test creates isolated TEST- rows and removes
    /// them; the whole class is Inconclusive (not failed) when no database is reachable.
    ///
    /// SetDelivery below writes DeliveryStatus directly ONLY to arrange a starting state for
    /// the test - production code never does; it goes through the service under test.
    /// </summary>
    [TestClass]
    public class ShipmentTransitionTests
    {
        [TestInitialize]
        public void Init()
        {
            DbTestHelper.EnsureDatabaseAvailable();
        }

        [TestMethod]
        public void Deliver_without_pickup_is_rejected_and_state_unchanged()
        {
            int clientId = DbTestHelper.CreateTestClient("trans", "Individual");
            int paymentId;
            int shipId = DbTestHelper.CreateTestShipmentWithInvoice(clientId, 100m, "NotRequired", "Unpaid", out paymentId);
            try
            {
                SetDelivery(shipId, "Assigned");   // assigned but never collected

                Assert.ThrowsException<InvalidOperationException>(() =>
                    ShipmentTransitionService.TransitionDelivery(shipId, clientId, "Delivered", "Delivery Point", "x"));

                Assert.AreEqual("Assigned", GetDelivery(shipId),
                    "a rejected transition must leave the state unchanged");
            }
            finally
            {
                DbTestHelper.DeleteShipmentAndPayment(shipId, paymentId);
                DbTestHelper.DeleteClient(clientId);
            }
        }

        [TestMethod]
        public void Legal_delivery_moves_state_and_writes_history_and_audit()
        {
            int clientId = DbTestHelper.CreateTestClient("trans", "Individual");
            int paymentId;
            int shipId = DbTestHelper.CreateTestShipmentWithInvoice(clientId, 100m, "NotRequired", "Unpaid", out paymentId);
            try
            {
                SetDelivery(shipId, "OutForDelivery");

                ShipmentTransitionService.TransitionDelivery(shipId, clientId, "Delivered", "Delivery Point", "signed");

                Assert.AreEqual("Delivered", GetDelivery(shipId));
                Assert.IsTrue(Db.ToInt(Db.Scalar(
                    "SELECT COUNT(*) FROM dbo.[TrackingHistory] WHERE ShipmentID = @s AND Status = 'Delivered'",
                    "@s", shipId)) >= 1, "a Delivered tracking row should be written in the same transaction");
                Assert.IsTrue(Db.ToInt(Db.Scalar(
                    "SELECT COUNT(*) FROM dbo.AuditLog WHERE Entity = 'Shipment' AND EntityID = @e",
                    "@e", shipId.ToString())) >= 1, "an audit row should be written in the same transaction");
            }
            finally
            {
                DbTestHelper.DeleteShipmentAndPayment(shipId, paymentId);
                DbTestHelper.DeleteClient(clientId);
            }
        }

        [TestMethod]
        public void Repeated_pickup_cannot_double_apply()
        {
            int clientId = DbTestHelper.CreateTestClient("trans", "Individual");
            int paymentId;
            int shipId = DbTestHelper.CreateTestShipmentWithInvoice(clientId, 100m, "NotRequired", "Unpaid", out paymentId);
            try
            {
                SetDelivery(shipId, "Assigned");

                ShipmentTransitionService.TransitionDelivery(shipId, clientId, "OutForDelivery", "Collected", "pickup");
                Assert.AreEqual("OutForDelivery", GetDelivery(shipId));

                // The second identical click now starts from OutForDelivery, so it is rejected
                // rather than applied again - a double-submit cannot double-collect.
                Assert.ThrowsException<InvalidOperationException>(() =>
                    ShipmentTransitionService.TransitionDelivery(shipId, clientId, "OutForDelivery", "Collected", "pickup again"));
            }
            finally
            {
                DbTestHelper.DeleteShipmentAndPayment(shipId, paymentId);
                DbTestHelper.DeleteClient(clientId);
            }
        }

        private static void SetDelivery(int shipId, string status)
        {
            Db.NonQuery("UPDATE dbo.[Shipment] SET DeliveryStatus = @st WHERE ShipmentID = @s",
                "@st", status, "@s", shipId);
        }

        private static string GetDelivery(int shipId)
        {
            return Db.ToStr(Db.Scalar("SELECT DeliveryStatus FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", shipId));
        }
    }
}
