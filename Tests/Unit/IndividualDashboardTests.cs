using System;
using ForteLogisticsApp.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ForteLogisticsApp.Tests.Unit
{
    [TestClass]
    public class IndividualDashboardTests
    {
        private static readonly DateTime Booked = new DateTime(2026, 11, 1, 9, 0, 0);

        [DataTestMethod]
        [DataRow(8, "Good morning, John")]
        [DataRow(11, "Good morning, John")]
        [DataRow(12, "Good afternoon, John")]
        [DataRow(16, "Good afternoon, John")]
        [DataRow(17, "Good evening, John")]
        [DataRow(23, "Good evening, John")]
        public void Greeting_FollowsTheClock_AndUsesTheFirstName(int hour, string expected)
        {
            Assert.AreEqual(expected, IndividualDashboard.Greeting("John Smith", new DateTime(2026, 11, 7, hour, 0, 0)));
        }

        [TestMethod]
        public void Greeting_WithoutAName_StillReads()
        {
            Assert.AreEqual("Good morning", IndividualDashboard.Greeting("  ", new DateTime(2026, 11, 7, 9, 0, 0)));
        }

        [DataTestMethod]
        [DataRow("Registered", "Booked")]
        [DataRow("Sorted", "At depot")]
        [DataRow("Assigned", "With driver")]
        [DataRow("InTransit", "In transit")]
        [DataRow("OutForDelivery", "Out for delivery")]
        [DataRow("Delivered", "Delivered")]
        [DataRow("Failed", "Delivery failed")]
        public void StatusLabel_IsWrittenForACustomer(string status, string expected)
        {
            Assert.AreEqual(expected, IndividualDashboard.StatusLabel(status));
        }

        [DataTestMethod]
        [DataRow("Cape Town", "Estimated delivery: 3 November")]
        [DataRow("Gqeberha", "Estimated delivery: 3 November")]
        [DataRow("East London", "Estimated delivery: 5 November")]
        public void DeliveryNote_EstimatesFromTheBookingDateAndDestination(string city, string expected)
        {
            Assert.AreEqual(expected, IndividualDashboard.DeliveryNote("OutForDelivery", Booked, null, city, Booked));
        }

        [TestMethod]
        public void DeliveryNote_OnTheEstimatedDay_IsNotLate()
        {
            string note = IndividualDashboard.DeliveryNote("Assigned", Booked, null, "Durban", new DateTime(2026, 11, 3, 23, 0, 0));

            Assert.AreEqual("Estimated delivery: 3 November", note);
        }

        [TestMethod]
        public void DeliveryNote_PastTheEstimate_SaysRunningLate()
        {
            string note = IndividualDashboard.DeliveryNote("Assigned", Booked, null, "Durban", new DateTime(2026, 11, 4, 0, 0, 0));

            Assert.AreEqual("Running late", note);
        }

        [TestMethod]
        public void DeliveryNote_ForAFinishedParcel_ReportsWhatHappened()
        {
            Assert.AreEqual("Delivered 2 November",
                IndividualDashboard.DeliveryNote("Delivered", Booked, new DateTime(2026, 11, 2), "Durban", Booked));
            Assert.AreEqual("Delivered",
                IndividualDashboard.DeliveryNote("Delivered", Booked, null, "Durban", Booked));
            Assert.AreEqual("We could not complete this delivery",
                IndividualDashboard.DeliveryNote("Failed", Booked, null, "Durban", Booked));
        }
    }
}
