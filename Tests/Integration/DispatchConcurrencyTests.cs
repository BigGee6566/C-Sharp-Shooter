using Microsoft.VisualStudio.TestTools.UnitTesting;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;

namespace ForteLogisticsApp.Tests.Integration
{
    /// <summary>
    /// Concurrency/guard checks for dispatch, against real tables. The production code in
    /// RoutePlanning applies status-guarded UPDATEs so a duplicate click, a double-submit, or
    /// two staff acting at once cannot double-apply: the second write matches no row and is a
    /// no-op. These tests run the same guarded statements twice and assert the second does
    /// nothing. Each creates isolated TEST- rows and removes them; Inconclusive without a DB.
    /// </summary>
    [TestClass]
    public class DispatchConcurrencyTests
    {
        [TestInitialize]
        public void Init()
        {
            DbTestHelper.EnsureDatabaseAvailable();
        }

        [TestMethod]
        public void Parcel_assignment_guard_makes_a_second_assign_a_no_op()
        {
            int client = DbTestHelper.CreateTestClient("asg", "Individual");
            int routeId = DbTestHelper.CreateTestRoute("asg");
            int paymentId;
            int shipId = DbTestHelper.CreateTestShipmentWithInvoice(client, 120m, "PaymentSuccessful", "Paid", out paymentId);
            try
            {
                // Arrange the parcel as the dispatch queue would: sorted and not yet on a route.
                Db.NonQuery("UPDATE dbo.[Shipment] SET WarehouseStatus = 'Sorted', RouteID = NULL WHERE ShipmentID = @s",
                    "@s", shipId);

                // The exact guarded assignment RoutePlanning runs (driver may be null on the route).
                const string assign =
                    @"UPDATE dbo.[Shipment]
                      SET RouteID = @r, DriverID = @d, WarehouseStatus = 'Dispatched',
                          DeliveryStatus = 'Assigned', LastStatusChange = GETDATE()
                      WHERE ShipmentID = @s AND RouteID IS NULL AND WarehouseStatus = 'Sorted'";

                int first = Db.NonQuery(assign, "@r", routeId, "@d", (object)null, "@s", shipId);
                int second = Db.NonQuery(assign, "@r", routeId, "@d", (object)null, "@s", shipId);

                Assert.AreEqual(1, first, "the first assignment takes the parcel");
                Assert.AreEqual(0, second, "a repeat/concurrent assignment must be a no-op, not a re-dispatch");
                Assert.AreEqual(routeId, Db.ToInt(Db.Scalar(
                    "SELECT RouteID FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", shipId)));
            }
            finally
            {
                DbTestHelper.DeleteShipmentAndPayment(shipId, paymentId);   // clears Shipment.RouteID FK first
                DbTestHelper.DeleteRoute(routeId);
                DbTestHelper.DeleteClient(client);
            }
        }

        [TestMethod]
        public void Route_toggle_guard_makes_a_stale_toggle_a_no_op()
        {
            int routeId = DbTestHelper.CreateTestRoute("tog", "Active");
            try
            {
                // Active -> Inactive is a legal transition.
                Assert.IsTrue(StateMachine.CanTransition("RouteStatus", "Active", "Inactive"));

                const string toggle =
                    "UPDATE dbo.[Route] SET Status = @next WHERE RouteID = @r AND Status = @cur";

                int first = Db.NonQuery(toggle, "@next", "Inactive", "@r", routeId, "@cur", "Active");
                // A second click still believing the route is Active matches nothing now.
                int second = Db.NonQuery(toggle, "@next", "Inactive", "@r", routeId, "@cur", "Active");

                Assert.AreEqual(1, first, "the first toggle flips Active -> Inactive");
                Assert.AreEqual(0, second, "a stale/concurrent toggle must not re-apply on top of the new state");
                Assert.AreEqual("Inactive", Db.ToStr(Db.Scalar(
                    "SELECT Status FROM dbo.[Route] WHERE RouteID = @r", "@r", routeId)));
            }
            finally
            {
                DbTestHelper.DeleteRoute(routeId);
            }
        }

        [TestMethod]
        public void Off_duty_driver_is_not_flipped_on_route()
        {
            int driverId = DbTestHelper.CreateTestDriver("offduty", "Off Duty");
            try
            {
                // The whitelist forbids the jump...
                Assert.IsFalse(StateMachine.CanTransition("DriverAvailability", "Off Duty", "On Route"),
                    "Off Duty -> On Route is not a legal availability transition");

                // ...and the production flip is status-guarded from the validated source
                // ('Available'), so against an Off Duty driver it changes no row.
                int flipped = Db.NonQuery(
                    "UPDATE dbo.[Driver] SET AvailabilityStatus = 'On Route' WHERE UserID = @d AND AvailabilityStatus = 'Available'",
                    "@d", driverId);

                Assert.AreEqual(0, flipped, "an Off Duty driver must not be silently put On Route");
                Assert.AreEqual("Off Duty", Db.ToStr(Db.Scalar(
                    "SELECT AvailabilityStatus FROM dbo.[Driver] WHERE UserID = @d", "@d", driverId)));
            }
            finally
            {
                DbTestHelper.DeleteDriver(driverId);
            }
        }
    }
}
