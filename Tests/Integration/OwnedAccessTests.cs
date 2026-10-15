using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ForteLogisticsApp.Data;

namespace ForteLogisticsApp.Tests.Integration
{
    /// <summary>
    /// IDOR checks against real tables: the owner-scoped fetches in <see cref="Owned"/> must
    /// return a row only to its owner and raise the indistinguishable AccessDeniedException
    /// for anyone else, so swapping an ID in a URL or hidden field can never read a stranger's
    /// shipment, invoice, or assigned parcel. Each test creates isolated TEST- rows and removes
    /// them; the class is Inconclusive (not failed) when no database is reachable.
    /// </summary>
    [TestClass]
    public class OwnedAccessTests
    {
        [TestInitialize]
        public void Init()
        {
            DbTestHelper.EnsureDatabaseAvailable();
        }

        [TestMethod]
        public void Shipment_fetch_returns_owner_row_and_denies_another_customer()
        {
            int owner = DbTestHelper.CreateTestClient("own", "Individual");
            int stranger = DbTestHelper.CreateTestClient("other", "Individual");
            int paymentId;
            int shipId = DbTestHelper.CreateTestShipmentWithInvoice(owner, 150m, "PaymentRequired", "Unpaid", out paymentId);
            try
            {
                DataRow row = Owned.RequireShipmentForCustomer(shipId, owner);
                Assert.AreEqual(shipId, Db.ToInt(row["ShipmentID"]), "the owner must get their own shipment");

                Assert.ThrowsException<AccessDeniedException>(
                    () => Owned.RequireShipmentForCustomer(shipId, stranger),
                    "a different customer must be denied, not shown the row");
            }
            finally
            {
                DbTestHelper.DeleteShipmentAndPayment(shipId, paymentId);
                DbTestHelper.DeleteClient(owner);
                DbTestHelper.DeleteClient(stranger);
            }
        }

        [TestMethod]
        public void Invoice_fetch_returns_owner_row_and_denies_another_customer()
        {
            int owner = DbTestHelper.CreateTestClient("inv", "Individual");
            int stranger = DbTestHelper.CreateTestClient("inv2", "Individual");
            int paymentId;
            int shipId = DbTestHelper.CreateTestShipmentWithInvoice(owner, 275m, "PaymentRequired", "Unpaid", out paymentId);
            try
            {
                DataRow row = Owned.RequireInvoiceForCustomer(paymentId, owner);
                Assert.AreEqual(paymentId, Db.ToInt(row["PaymentID"]), "the owner must get their own invoice");

                Assert.ThrowsException<AccessDeniedException>(
                    () => Owned.RequireInvoiceForCustomer(paymentId, stranger),
                    "a different customer must not be able to open someone else's invoice by ID");
            }
            finally
            {
                DbTestHelper.DeleteShipmentAndPayment(shipId, paymentId);
                DbTestHelper.DeleteClient(owner);
                DbTestHelper.DeleteClient(stranger);
            }
        }

        [TestMethod]
        public void Driver_fetch_returns_assigned_parcel_and_denies_an_unassigned_driver()
        {
            int client = DbTestHelper.CreateTestClient("drvcust", "Individual");
            int assignedDriver = DbTestHelper.CreateTestDriver("assigned");
            int otherDriver = DbTestHelper.CreateTestDriver("other");
            int paymentId;
            int shipId = DbTestHelper.CreateTestShipmentWithInvoice(client, 90m, "NotRequired", "Unpaid", out paymentId);
            try
            {
                Db.NonQuery("UPDATE dbo.[Shipment] SET DriverID = @d WHERE ShipmentID = @s",
                    "@d", assignedDriver, "@s", shipId);

                DataRow row = Owned.RequireShipmentForDriver(shipId, assignedDriver);
                Assert.AreEqual(shipId, Db.ToInt(row["ShipmentID"]), "the assigned driver must get the parcel");

                Assert.ThrowsException<AccessDeniedException>(
                    () => Owned.RequireShipmentForDriver(shipId, otherDriver),
                    "a driver the parcel is not assigned to must be denied");
            }
            finally
            {
                DbTestHelper.DeleteShipmentAndPayment(shipId, paymentId);   // clears Shipment.DriverID FK first
                DbTestHelper.DeleteDriver(assignedDriver);
                DbTestHelper.DeleteDriver(otherDriver);
                DbTestHelper.DeleteClient(client);
            }
        }
    }
}
