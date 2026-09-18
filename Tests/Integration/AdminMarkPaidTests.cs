using System;
using System.Collections.Generic;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ForteLogisticsApp.Tests.Integration
{
    /// <summary>Finance\Invoices.aspx "Mark Paid": PaymentService.MarkInvoicePaidByAdmin.</summary>
    [TestClass]
    public class AdminMarkPaidTests
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

        private int NewClient()
        {
            int userId = DbTestHelper.CreateTestClient("client", "Individual");
            _cleanup.Add(() => DbTestHelper.DeleteClient(userId));
            return userId;
        }

        [TestMethod]
        public void MarkInvoicePaidByAdmin_UnpaidInvoice_Succeeds()
        {
            int client = NewClient();
            int admin = NewClient();
            int paymentId;
            int shipmentId = DbTestHelper.CreateTestShipmentWithInvoice(client, 300m, "NotRequired", "Unpaid", out paymentId);
            _cleanup.Add(() => DbTestHelper.DeleteShipmentAndPayment(shipmentId, paymentId));

            var outcome = PaymentService.MarkInvoicePaidByAdmin(paymentId, admin, "EFT");

            Assert.IsTrue(outcome.Success, outcome.Message);
            Assert.AreEqual(300m, outcome.Amount);
            Assert.AreEqual("Paid",
                Db.ToStr(Db.Scalar("SELECT PaymentStatus FROM dbo.[Payment] WHERE PaymentID = @p", "@p", paymentId)));
        }

        [TestMethod]
        public void MarkInvoicePaidByAdmin_AlreadyPaid_IsRejected_CannotBeRecordedTwice()
        {
            int client = NewClient();
            int admin = NewClient();
            int paymentId;
            int shipmentId = DbTestHelper.CreateTestShipmentWithInvoice(client, 150m, "NotRequired", "Unpaid", out paymentId);
            _cleanup.Add(() => DbTestHelper.DeleteShipmentAndPayment(shipmentId, paymentId));

            var first = PaymentService.MarkInvoicePaidByAdmin(paymentId, admin, "EFT");
            var second = PaymentService.MarkInvoicePaidByAdmin(paymentId, admin, "EFT");

            Assert.IsTrue(first.Success);
            Assert.IsFalse(second.Success, "A second 'Mark Paid' click on an already-settled invoice must not re-record it.");
            Assert.AreEqual(first.ReferenceNumber,
                Db.ToStr(Db.Scalar("SELECT ReferenceNumber FROM dbo.[Payment] WHERE PaymentID = @p", "@p", paymentId)),
                "The stored reference must still be the first call's, untouched by the rejected second attempt.");
        }

        [TestMethod]
        public void MarkInvoicePaidByAdmin_UnknownPaymentId_IsRejected()
        {
            var outcome = PaymentService.MarkInvoicePaidByAdmin(int.MaxValue, NewClient(), "EFT");
            Assert.IsFalse(outcome.Success);
        }
    }
}
