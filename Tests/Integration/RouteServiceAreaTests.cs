using System;
using System.Collections.Generic;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ForteLogisticsApp.Tests.Integration
{
    /// <summary>
    /// Route <-> destination validation. Ops\RoutePlanning.aspx.cs's assignment check itself
    /// lives in a Page that needs a live HttpContext/Session to instantiate, so this exercises
    /// the same rule (SortCategory must equal the route's ServiceArea) directly against the
    /// schema it operates on: Route.ServiceArea, Shipment.SortCategory and the
    /// CK_Route_ServiceArea / SortingService.CategoryFor mapping that produces them.
    /// </summary>
    [TestClass]
    public class RouteServiceAreaTests
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

        private int CreateRoute(string serviceArea)
        {
            int routeId = Db.Insert(
                @"INSERT INTO dbo.[Route] (RouteName, Status, ServiceArea)
                  VALUES (@n, 'Active', @sa)",
                "@n", "TestRoute-" + Guid.NewGuid().ToString("N").Substring(0, 8), "@sa", serviceArea);
            _cleanup.Add(() => Db.NonQuery("DELETE FROM dbo.[Route] WHERE RouteID = @r", "@r", routeId));
            return routeId;
        }

        private int CreateSortedParcel(string destCity)
        {
            string sortCategory = SortingService.CategoryFor(destCity);
            string tracking = "RTEST-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            int shipmentId = Db.Insert(
                @"INSERT INTO dbo.[Shipment]
                    (TrackingNumber, PickupAddress, DeliveryAddress, OriginCity, DestCity,
                     ParcelWeight, DeliveryStatus, WarehouseStatus, PaymentGateStatus, SortCategory)
                  VALUES (@t, 'Pickup', 'Delivery', 'Johannesburg', @d, 1, 'Sorted', 'Sorted', 'NotRequired', @sc)",
                "@t", tracking, "@d", destCity, "@sc", sortCategory);
            _cleanup.Add(() => Db.NonQuery("DELETE FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", shipmentId));
            return shipmentId;
        }

        /// <summary>The exact predicate Ops\RoutePlanning.aspx.cs btnAssign_Click applies per parcel.</summary>
        private static bool IsCompatible(string routeServiceArea, string parcelSortCategory)
        {
            return routeServiceArea.Length == 0 ||
                   string.Equals(parcelSortCategory, routeServiceArea, StringComparison.OrdinalIgnoreCase);
        }

        [TestMethod]
        public void EastLondonParcel_IsIncompatibleWithCapeTownRoute()
        {
            int capeTownRoute = CreateRoute("Metro-CPT");
            int eastLondonParcel = CreateSortedParcel("East London");

            string routeServiceArea = Db.ToStr(Db.Scalar(
                "SELECT ServiceArea FROM dbo.[Route] WHERE RouteID = @r", "@r", capeTownRoute));
            string parcelLane = Db.ToStr(Db.Scalar(
                "SELECT SortCategory FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", eastLondonParcel));

            Assert.IsFalse(IsCompatible(routeServiceArea, parcelLane),
                "An East London parcel (Regional-Outlying) must never be assignable to a Cape Town (Metro-CPT) route.");
        }

        [TestMethod]
        public void GqeberhaParcel_IsCompatibleWithItsOwnRoute()
        {
            int peRoute = CreateRoute("Metro-PE");
            int peParcel = CreateSortedParcel("Gqeberha");

            string routeServiceArea = Db.ToStr(Db.Scalar(
                "SELECT ServiceArea FROM dbo.[Route] WHERE RouteID = @r", "@r", peRoute));
            string parcelLane = Db.ToStr(Db.Scalar(
                "SELECT SortCategory FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", peParcel));

            Assert.IsTrue(IsCompatible(routeServiceArea, parcelLane));
        }

        [TestMethod]
        public void RouteWithNoServiceArea_AcceptsAnyDestination_ForBackwardCompatibility()
        {
            // Routes created before this migration have ServiceArea = NULL and must not have
            // their existing assignments invalidated.
            int legacyRoute = CreateRoute(null);
            string routeServiceArea = Db.ToStr(Db.Scalar(
                "SELECT ServiceArea FROM dbo.[Route] WHERE RouteID = @r", "@r", legacyRoute));

            Assert.IsTrue(IsCompatible(routeServiceArea, "Regional-Outlying"));
            Assert.IsTrue(IsCompatible(routeServiceArea, "Metro-CPT"));
        }
    }
}
