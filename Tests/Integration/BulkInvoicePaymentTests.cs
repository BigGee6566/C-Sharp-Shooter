using System;
using System.Collections.Generic;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ForteLogisticsApp.Tests.Integration
{
    /// <summary>Business bulk invoice payment: PaymentService.PayInvoices.</summary>
    [TestClass]
    public class BulkInvoicePaymentTests
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

        private int NewBusinessClient(int businessId)
        {
            int userId = DbTestHelper.CreateTestClient("biz", "Business", businessId);
            _cleanup.Add(() => DbTestHelper.DeleteClient(userId));
            return userId;
        }

        private int NewUnpaidInvoice(int clientId, decimal amount)
        {
            int paymentId;
            int shipmentId = DbTestHelper.CreateTestShipmentWithInvoice(clientId, amount, "NotRequired", "Unpaid", out paymentId);
            _cleanup.Add(() => DbTestHelper.DeleteShipmentAndPayment(shipmentId, paymentId));
            return paymentId;
        }

        [TestMethod]
        public void PayInvoices_SingleInvoice_Succeeds()
        {
            int business = DbTestHelper.CreateTestBusiness("Acme");
            _cleanup.Add(() => DbTestHelper.DeleteBusiness(business));
            int client = NewBusinessClient(business);
            int invoice = NewUnpaidInvoice(client, 500m);

            var outcome = PaymentService.PayInvoices(new[] { invoice }, client, "EFT", Guid.NewGuid().ToString("N"));

            Assert.IsTrue(outcome.Success, outcome.Message);
            Assert.AreEqual(500m, outcome.TotalAmount);
            Assert.AreEqual(1, outcome.InvoiceCount);
        }

        [TestMethod]
        public void PayInvoices_MultipleInvoicesAcrossOneBusiness_SumsCorrectly()
        {
            // Two different users under the same Business - this is the whole point of
            // business bulk payment: one client pays another client's invoice too.
            int business = DbTestHelper.CreateTestBusiness("Acme");
            _cleanup.Add(() => DbTestHelper.DeleteBusiness(business));
            int payer = NewBusinessClient(business);
            int colleague = NewBusinessClient(business);
            int invoiceA = NewUnpaidInvoice(payer, 300m);
            int invoiceB = NewUnpaidInvoice(colleague, 450m);

            var outcome = PaymentService.PayInvoices(new[] { invoiceA, invoiceB }, payer, "Card", Guid.NewGuid().ToString("N"));

            Assert.IsTrue(outcome.Success, outcome.Message);
            Assert.AreEqual(2, outcome.InvoiceCount);
            Assert.AreEqual(750m, outcome.TotalAmount, "The charged total must be the server-computed sum, not anything supplied by the caller.");
        }

        [TestMethod]
        public void PayInvoices_AlreadyPaidInvoice_RejectsWholeBatch()
        {
            int business = DbTestHelper.CreateTestBusiness("Acme");
            _cleanup.Add(() => DbTestHelper.DeleteBusiness(business));
            int client = NewBusinessClient(business);
            int unpaid = NewUnpaidInvoice(client, 100m);

            int paidPaymentId;
            int paidShipmentId = DbTestHelper.CreateTestShipmentWithInvoice(client, 100m, "NotRequired", "Paid", out paidPaymentId);
            _cleanup.Add(() => DbTestHelper.DeleteShipmentAndPayment(paidShipmentId, paidPaymentId));

            var outcome = PaymentService.PayInvoices(new[] { unpaid, paidPaymentId }, client, "Card", Guid.NewGuid().ToString("N"));

            Assert.IsFalse(outcome.Success, "A batch containing an already-paid invoice must be rejected entirely, not partially applied.");
            Assert.AreEqual("Unpaid",
                Db.ToStr(Db.Scalar("SELECT PaymentStatus FROM dbo.[Payment] WHERE PaymentID = @p", "@p", unpaid)),
                "The still-unpaid invoice in the rejected batch must remain untouched.");
        }

        [TestMethod]
        public void PayInvoices_InvoiceFromAnotherBusiness_IsRejected()
        {
            int businessA = DbTestHelper.CreateTestBusiness("Acme");
            int businessB = DbTestHelper.CreateTestBusiness("Globex");
            _cleanup.Add(() => DbTestHelper.DeleteBusiness(businessA));
            _cleanup.Add(() => DbTestHelper.DeleteBusiness(businessB));

            int clientA = NewBusinessClient(businessA);
            int clientB = NewBusinessClient(businessB);
            int invoiceB = NewUnpaidInvoice(clientB, 200m);

            var outcome = PaymentService.PayInvoices(new[] { invoiceB }, clientA, "Card", Guid.NewGuid().ToString("N"));

            Assert.IsFalse(outcome.Success, "A user must never be able to pay another business's invoice.");
            Assert.AreEqual("Unpaid",
                Db.ToStr(Db.Scalar("SELECT PaymentStatus FROM dbo.[Payment] WHERE PaymentID = @p", "@p", invoiceB)));
        }

        [TestMethod]
        public void PayInvoices_DuplicateBatchKey_DoesNotChargeTwice()
        {
            int business = DbTestHelper.CreateTestBusiness("Acme");
            _cleanup.Add(() => DbTestHelper.DeleteBusiness(business));
            int client = NewBusinessClient(business);
            int invoice = NewUnpaidInvoice(client, 100m);

            string key = Guid.NewGuid().ToString("N");
            var first = PaymentService.PayInvoices(new[] { invoice }, client, "Card", key);
            var second = PaymentService.PayInvoices(new[] { invoice }, client, "Card", key);

            Assert.IsTrue(first.Success);
            Assert.IsTrue(second.Success);
            Assert.AreEqual(first.ReferenceNumber, second.ReferenceNumber);
        }

        [TestMethod]
        public void PayInvoices_IndividualClient_CanOnlyPayOwnInvoice()
        {
            int individual = DbTestHelper.CreateTestClient("solo", "Individual");
            _cleanup.Add(() => DbTestHelper.DeleteClient(individual));
            int otherIndividual = DbTestHelper.CreateTestClient("solo2", "Individual");
            _cleanup.Add(() => DbTestHelper.DeleteClient(otherIndividual));

            int ownInvoice = NewUnpaidInvoice(individual, 80m);
            int othersInvoice = NewUnpaidInvoice(otherIndividual, 80m);

            var outcome = PaymentService.PayInvoices(new[] { ownInvoice, othersInvoice }, individual, "Card", Guid.NewGuid().ToString("N"));

            Assert.IsFalse(outcome.Success, "An Individual client (no Business) must not be able to include someone else's invoice.");
        }
    }
}
