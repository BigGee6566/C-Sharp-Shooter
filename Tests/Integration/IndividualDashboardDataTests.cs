using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ForteLogisticsApp.Tests.Integration
{
    [TestClass]
    public class IndividualDashboardDataTests
    {
        private readonly List<Action> _cleanup = new List<Action>();
        private int _customer;

        [TestInitialize]
        public void Setup()
        {
            DbTestHelper.EnsureDatabaseAvailable();
            _customer = NewClient("Individual");
        }

        [TestCleanup]
        public void Cleanup()
        {
            for (int i = _cleanup.Count - 1; i >= 0; i--) _cleanup[i]();
            _cleanup.Clear();
        }

        private int NewClient(string clientType)
        {
            int id = DbTestHelper.CreateTestClient("dash", clientType);
            _cleanup.Add(() => DbTestHelper.DeleteClient(id));
            return id;
        }

        private string NewParcel(int sender, string deliveryStatus, string gate = "NotRequired", int daysAgo = 0)
        {
            string tracking = "DSH-" + Guid.NewGuid().ToString("N").Substring(0, 10).ToUpperInvariant();
            int id = Db.Insert(
                @"INSERT INTO dbo.[Shipment]
                    (TrackingNumber, SenderID, OriginCity, DestCity, DeliveryStatus, WarehouseStatus,
                     PaymentGateStatus, ShipmentDate)
                  VALUES (@t, @s, 'Johannesburg', 'Durban', @st, 'Pending', @g, DATEADD(day, -@age, GETDATE()))",
                "@t", tracking, "@s", sender, "@st", deliveryStatus, "@g", gate, "@age", daysAgo);
            _cleanup.Add(() => Db.NonQuery("DELETE FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", id));
            return tracking;
        }

        private static List<string> Waybills(DataTable rows)
        {
            return rows.Rows.Cast<DataRow>().Select(r => Db.ToStr(r["TrackingNumber"])).ToList();
        }

        [TestMethod]
        public void IsBusinessClient_ReadsTheClientRecord()
        {
            Assert.IsFalse(IndividualDashboard.IsBusinessClient(_customer));
            Assert.IsTrue(IndividualDashboard.IsBusinessClient(NewClient("Business")));
        }

        [TestMethod]
        public void ActiveAndHistory_SplitParcelsWithoutOverlap_AndCountsAgree()
        {
            string booked = NewParcel(_customer, "Registered");
            string moving = NewParcel(_customer, "OutForDelivery");
            string delivered = NewParcel(_customer, "Delivered");
            string failed = NewParcel(_customer, "Failed");

            CollectionAssert.AreEquivalent(new[] { booked, moving }, Waybills(IndividualDashboard.Active(_customer)));
            CollectionAssert.AreEquivalent(new[] { delivered, failed }, Waybills(IndividualDashboard.History(_customer)));
            Assert.AreEqual(2, IndividualDashboard.ActiveCount(_customer));
            Assert.AreEqual(1, IndividualDashboard.CompletedCount(_customer), "a failed delivery is not a completed one");
        }

        [TestMethod]
        public void Recent_ReturnsTheNewestParcelsFirst_UpToTheLimit()
        {
            NewParcel(_customer, "Delivered", daysAgo: 9);
            string middle = NewParcel(_customer, "Delivered", daysAgo: 5);
            string newest = NewParcel(_customer, "Registered", daysAgo: 1);

            CollectionAssert.AreEqual(new[] { newest, middle }, Waybills(IndividualDashboard.Recent(_customer, 2)));
        }

        [TestMethod]
        public void ParcelsOfAnotherCustomer_NeverAppear()
        {
            int other = NewClient("Individual");
            NewParcel(other, "OutForDelivery");
            NewParcel(other, "Delivered");
            NewParcel(other, "Registered", "PaymentRequired");

            Assert.AreEqual(0, IndividualDashboard.Active(_customer).Rows.Count);
            Assert.AreEqual(0, IndividualDashboard.History(_customer).Rows.Count);
            Assert.AreEqual(0, IndividualDashboard.Recent(_customer, 3).Rows.Count);
            Assert.AreEqual(0, IndividualDashboard.AwaitingPayment(_customer).Rows.Count);
        }

        [TestMethod]
        public void AwaitingPayment_ListsOnlyParcelsStillHeldForPayment_OldestFirst()
        {
            NewParcel(_customer, "Registered", "NotRequired");
            NewParcel(_customer, "Registered", "PaymentSuccessful");
            string younger = NewParcel(_customer, "Registered", "PaymentRequired", daysAgo: 1);
            string older = NewParcel(_customer, "Registered", "PaymentPending", daysAgo: 3);

            CollectionAssert.AreEqual(new[] { older, younger }, Waybills(IndividualDashboard.AwaitingPayment(_customer)));
        }
    }
}
