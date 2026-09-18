using System;
using System.Collections.Generic;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ForteLogisticsApp.Tests.Integration
{
    /// <summary>
    /// Individual payment-before-dispatch: PaymentService.PayForShipment and the gate it
    /// applies to Shipment.PaymentGateStatus. Needs a live Logisticore database - see
    /// DbTestHelper.EnsureDatabaseAvailable.
    /// </summary>
    [TestClass]
    public class PaymentServiceTests
    {
        private readonly List<Action> _cleanup = new List<Action>();

        [TestInitialize]
        public void Setup()
        {
            DbTestHelper.EnsureDatabaseAvailable();
        }

        [TestCleanup]
        public void Cleanup()
        {
            for (int i = _cleanup.Count - 1; i >= 0; i--) _cleanup[i]();
            _cleanup.Clear();
        }

        private int NewIndividualClient()
        {
            int userId = DbTestHelper.CreateTestClient("individual", "Individual");
            _cleanup.Add(() => DbTestHelper.DeleteClient(userId));
            return userId;
        }

        [TestMethod]
        public void PayForShipment_Success_ClearsGate_AndMarksInvoicePaid()
        {
            int clientId = NewIndividualClient();
            int paymentId;
            int shipmentId = DbTestHelper.CreateTestShipmentWithInvoice(clientId, 250m, "PaymentRequired", "Unpaid", out paymentId);
            _cleanup.Add(() => DbTestHelper.DeleteShipmentAndPayment(shipmentId, paymentId));

            var outcome = PaymentService.PayForShipment(shipmentId, clientId, "Card", null, Guid.NewGuid().ToString("N"));

            Assert.IsTrue(outcome.Success, outcome.Message);
            Assert.AreEqual(250m, outcome.Amount);
            Assert.AreEqual("PaymentSuccessful",
                Db.ToStr(Db.Scalar("SELECT PaymentGateStatus FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", shipmentId)));
            Assert.AreEqual("Paid",
                Db.ToStr(Db.Scalar("SELECT PaymentStatus FROM dbo.[Payment] WHERE PaymentID = @p", "@p", paymentId)));
        }

        [TestMethod]
        public void PayForShipment_GatewayDeclines_RevertsGateSoCustomerCanRetry()
        {
            int clientId = NewIndividualClient();
            int paymentId;
            int shipmentId = DbTestHelper.CreateTestShipmentWithInvoice(clientId, 100m, "PaymentRequired", "Unpaid", out paymentId);
            _cleanup.Add(() => DbTestHelper.DeleteShipmentAndPayment(shipmentId, paymentId));

            // PaymentGateway.Charge treats "FAIL" anywhere in testNote as a forced decline.
            var outcome = PaymentService.PayForShipment(shipmentId, clientId, "Card", "FAIL-for-test", Guid.NewGuid().ToString("N"));

            Assert.IsFalse(outcome.Success);
            Assert.AreEqual("PaymentRequired",
                Db.ToStr(Db.Scalar("SELECT PaymentGateStatus FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", shipmentId)),
                "A failed payment must not leave the shipment stuck in PaymentPending.");
            Assert.AreEqual("Failed",
                Db.ToStr(Db.Scalar("SELECT PaymentStatus FROM dbo.[Payment] WHERE PaymentID = @p", "@p", paymentId)));
        }

        [TestMethod]
        public void PayForShipment_AnotherUsersShipment_IsRejected_Idor()
        {
            int owner = NewIndividualClient();
            int attacker = NewIndividualClient();
            int paymentId;
            int shipmentId = DbTestHelper.CreateTestShipmentWithInvoice(owner, 100m, "PaymentRequired", "Unpaid", out paymentId);
            _cleanup.Add(() => DbTestHelper.DeleteShipmentAndPayment(shipmentId, paymentId));

            var outcome = PaymentService.PayForShipment(shipmentId, attacker, "Card", null, Guid.NewGuid().ToString("N"));

            Assert.IsFalse(outcome.Success);
            Assert.AreEqual("PaymentRequired",
                Db.ToStr(Db.Scalar("SELECT PaymentGateStatus FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", shipmentId)),
                "An unauthorized payment attempt must not change the real owner's shipment.");
        }

        [TestMethod]
        public void PayForShipment_AlreadyPaid_IsRejected_CannotBePaidTwice()
        {
            int clientId = NewIndividualClient();
            int paymentId;
            int shipmentId = DbTestHelper.CreateTestShipmentWithInvoice(clientId, 100m, "PaymentSuccessful", "Paid", out paymentId);
            _cleanup.Add(() => DbTestHelper.DeleteShipmentAndPayment(shipmentId, paymentId));

            var outcome = PaymentService.PayForShipment(shipmentId, clientId, "Card", null, Guid.NewGuid().ToString("N"));

            Assert.IsFalse(outcome.Success);
        }

        [TestMethod]
        public void PayForShipment_DuplicateIdempotencyKey_DoesNotChargeTwice()
        {
            int clientId = NewIndividualClient();
            int paymentId;
            int shipmentId = DbTestHelper.CreateTestShipmentWithInvoice(clientId, 100m, "PaymentRequired", "Unpaid", out paymentId);
            _cleanup.Add(() => DbTestHelper.DeleteShipmentAndPayment(shipmentId, paymentId));

            string key = Guid.NewGuid().ToString("N");
            var first = PaymentService.PayForShipment(shipmentId, clientId, "Card", null, key);
            var second = PaymentService.PayForShipment(shipmentId, clientId, "Card", null, key);

            Assert.IsTrue(first.Success);
            Assert.IsTrue(second.Success);
            Assert.AreEqual(first.ReferenceNumber, second.ReferenceNumber,
                "A resubmitted request with the same idempotency key must return the original charge, not a new one.");
        }

        [TestMethod]
        public void UnpaidIndividualShipment_IsExcludedFromWarehouseQueue_UntilPaid()
        {
            // Mirrors the exact filter Ops\Warehouse.aspx.cs LoadQueue applies, since the page
            // itself needs a live HttpContext/Session to instantiate.
            int clientId = NewIndividualClient();
            int paymentId;
            int shipmentId = DbTestHelper.CreateTestShipmentWithInvoice(clientId, 100m, "PaymentRequired", "Unpaid", out paymentId);
            _cleanup.Add(() => DbTestHelper.DeleteShipmentAndPayment(shipmentId, paymentId));

            Func<int> inQueue = () => Db.ToInt(Db.Scalar(
                @"SELECT COUNT(*) FROM dbo.[Shipment]
                  WHERE ShipmentID = @s AND WarehouseStatus IN ('Pending','Received','Sorted')
                    AND PaymentGateStatus IN ('NotRequired','PaymentSuccessful')", "@s", shipmentId));

            Assert.AreEqual(0, inQueue(), "An unpaid shipment must not be eligible for warehouse sorting.");

            PaymentService.PayForShipment(shipmentId, clientId, "Card", null, Guid.NewGuid().ToString("N"));

            Assert.AreEqual(1, inQueue(), "Once paid, the shipment must become eligible for warehouse sorting.");
        }
    }
}
