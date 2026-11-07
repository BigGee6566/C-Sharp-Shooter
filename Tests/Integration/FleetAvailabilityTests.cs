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
    public class FleetAvailabilityTests
    {
        private const string ServiceArea = "Metro-CPT";
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

        private int NewDriver(string availability = "Available")
        {
            int id = DbTestHelper.CreateTestDriver("fleet", availability);
            _cleanup.Add(() => DbTestHelper.DeleteDriver(id));
            return id;
        }

        private int NewVehicle()
        {
            int id = Db.Insert(
                "INSERT INTO dbo.[Vehicle] (VehicleType, RegistrationNumber) VALUES ('Panel Van', @r)",
                "@r", "T" + Guid.NewGuid().ToString("N").Substring(0, 10));
            _cleanup.Add(() => Db.NonQuery("DELETE FROM dbo.[Vehicle] WHERE VehicleID = @v", "@v", id));
            return id;
        }

        private int NewRoute(int? driverId, int? vehicleId, string status = "Active", int daysOld = 0)
        {
            int id = Db.Insert(
                @"INSERT INTO dbo.[Route] (RouteName, DriverID, VehicleID, Status, ServiceArea, RouteDate)
                  VALUES (@n, @d, @v, @s, @a, DATEADD(day, -@age, CAST(GETDATE() AS DATE)))",
                "@n", "FleetTest-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                "@d", driverId, "@v", vehicleId, "@s", status, "@a", ServiceArea, "@age", daysOld);
            _cleanup.Add(() => DbTestHelper.DeleteRoute(id));
            return id;
        }

        private int NewParcel(int driverId, int? routeId, string deliveryStatus)
        {
            int id = Db.Insert(
                @"INSERT INTO dbo.[Shipment]
                    (TrackingNumber, DriverID, RouteID, OriginCity, DestCity, DeliveryStatus,
                     WarehouseStatus, PaymentGateStatus)
                  VALUES (@t, @d, @r, 'Johannesburg', 'Cape Town', @s, 'Dispatched', 'NotRequired')",
                "@t", "FLT-" + Guid.NewGuid().ToString("N").Substring(0, 10).ToUpperInvariant(),
                "@d", driverId, "@r", routeId, "@s", deliveryStatus);
            _cleanup.Add(() => Db.NonQuery("DELETE FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", id));
            return id;
        }

        private static void SetParcelStatus(int shipmentId, string status)
        {
            Db.NonQuery("UPDATE dbo.[Shipment] SET DeliveryStatus = @st WHERE ShipmentID = @s", "@st", status, "@s", shipmentId);
        }

        private static void SetRouteStatus(int routeId, string status)
        {
            Db.NonQuery("UPDATE dbo.[Route] SET Status = @st WHERE RouteID = @r", "@st", status, "@r", routeId);
        }

        private static string DriverStatus(int driverId)
        {
            return Db.ToStr(Db.Scalar("SELECT AvailabilityStatus FROM dbo.[Driver] WHERE UserID = @d", "@d", driverId));
        }

        private static string RouteStatus(int routeId)
        {
            return Db.ToStr(Db.Scalar("SELECT Status FROM dbo.[Route] WHERE RouteID = @r", "@r", routeId));
        }

        private static bool DriverListed(int driverId)
        {
            return FleetAvailability.AvailableDrivers().Rows.Cast<DataRow>().Any(r => Db.ToInt(r["DriverID"]) == driverId);
        }

        private static bool VehicleListed(int vehicleId)
        {
            return FleetAvailability.AvailableVehicles().Rows.Cast<DataRow>().Any(r => Db.ToInt(r["VehicleID"]) == vehicleId);
        }

        [TestMethod]
        public void IdleDriver_IsOffered()
        {
            Assert.IsTrue(DriverListed(NewDriver()));
        }

        [TestMethod]
        public void DriverWithOpenParcels_IsNotOffered_EvenWhenFlaggedAvailable()
        {
            int driver = NewDriver("Available");
            NewParcel(driver, null, "Assigned");

            Assert.IsFalse(DriverListed(driver));
        }

        [TestMethod]
        public void OutForDeliveryDriver_IsNotOffered()
        {
            int driver = NewDriver("On Route");
            NewParcel(driver, NewRoute(driver, null), "OutForDelivery");

            Assert.IsFalse(DriverListed(driver));
        }

        [TestMethod]
        public void OffDutyDriver_IsNotOffered_AndSyncLeavesThemOffDuty()
        {
            int driver = NewDriver("Off Duty");

            FleetAvailability.Sync();

            Assert.IsFalse(DriverListed(driver));
            Assert.AreEqual("Off Duty", DriverStatus(driver));
        }

        [TestMethod]
        public void Sync_MarksAvailableDriverOnRoute_WhenTheyHoldParcels()
        {
            int driver = NewDriver("Available");
            NewParcel(driver, null, "Assigned");

            FleetAvailability.Sync();

            Assert.AreEqual("On Route", DriverStatus(driver));
        }

        [TestMethod]
        public void FinishingEveryParcel_CompletesRoute_FreesDriverAndVehicle()
        {
            int driver = NewDriver("On Route");
            int vehicle = NewVehicle();
            int route = NewRoute(driver, vehicle);
            int delivered = NewParcel(driver, route, "OutForDelivery");
            int failed = NewParcel(driver, route, "OutForDelivery");

            FleetAvailability.Sync();
            Assert.AreEqual("On Route", DriverStatus(driver), "parcels are still out");
            Assert.IsFalse(VehicleListed(vehicle));

            SetParcelStatus(delivered, "Delivered");
            SetParcelStatus(failed, "Failed");
            FleetAvailability.Sync();

            Assert.AreEqual("Completed", RouteStatus(route));
            Assert.AreEqual("Available", DriverStatus(driver));
            Assert.IsTrue(DriverListed(driver));
            Assert.IsTrue(VehicleListed(vehicle));
        }

        [TestMethod]
        public void EmptyRouteCreatedToday_HoldsDriverAndVehicle()
        {
            int driver = NewDriver("On Route");
            int vehicle = NewVehicle();
            int route = NewRoute(driver, vehicle);

            FleetAvailability.Sync();

            Assert.AreEqual("Active", RouteStatus(route));
            Assert.AreEqual("On Route", DriverStatus(driver));
            Assert.IsFalse(DriverListed(driver));
            Assert.IsFalse(VehicleListed(vehicle));
        }

        [TestMethod]
        public void EmptyRouteFromEarlierDay_IsDeactivated_AndFreesDriverAndVehicle()
        {
            int driver = NewDriver("On Route");
            int vehicle = NewVehicle();
            int route = NewRoute(driver, vehicle, daysOld: 1);

            FleetAvailability.Sync();

            Assert.AreEqual("Inactive", RouteStatus(route));
            Assert.AreEqual("Available", DriverStatus(driver));
            Assert.IsTrue(DriverListed(driver));
            Assert.IsTrue(VehicleListed(vehicle));
        }

        [TestMethod]
        public void DeactivatingAnEmptyRoute_FreesDriverAndVehicleOnNextSync()
        {
            int driver = NewDriver("On Route");
            int vehicle = NewVehicle();
            int route = NewRoute(driver, vehicle);

            SetRouteStatus(route, "Inactive");
            FleetAvailability.Sync();

            Assert.AreEqual("Available", DriverStatus(driver));
            Assert.IsTrue(VehicleListed(vehicle));
        }

        [TestMethod]
        public void CreateRoute_PutsDriverOnRoute_AndTakesBothOffTheLists()
        {
            int driver = NewDriver();
            int vehicle = NewVehicle();
            string name = "FleetTest-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            _cleanup.Add(() => Db.NonQuery("DELETE FROM dbo.[Route] WHERE RouteName = @n", "@n", name));

            string error = FleetAvailability.CreateRoute(name, driver, vehicle, ServiceArea, driver);

            Assert.IsNull(error, error);
            Assert.AreEqual("On Route", DriverStatus(driver));
            Assert.IsFalse(DriverListed(driver));
            Assert.IsFalse(VehicleListed(vehicle));
        }

        [TestMethod]
        public void CreateRoute_RefusesVehicleAlreadyOnALiveRoute()
        {
            int vehicle = NewVehicle();
            NewRoute(NewDriver("On Route"), vehicle);
            int secondDriver = NewDriver();
            string name = "FleetTest-" + Guid.NewGuid().ToString("N").Substring(0, 8);

            string error = FleetAvailability.CreateRoute(name, secondDriver, vehicle, ServiceArea, secondDriver);

            Assert.IsNotNull(error);
            Assert.AreEqual(0, Db.ToInt(Db.Scalar("SELECT COUNT(*) FROM dbo.[Route] WHERE RouteName = @n", "@n", name)));
            Assert.AreEqual("Available", DriverStatus(secondDriver), "a refused route must not touch the driver");
        }

        [TestMethod]
        public void CreateRoute_RefusesDriverWithOpenParcels()
        {
            int driver = NewDriver("Available");
            NewParcel(driver, null, "Assigned");
            string name = "FleetTest-" + Guid.NewGuid().ToString("N").Substring(0, 8);

            string error = FleetAvailability.CreateRoute(name, driver, NewVehicle(), ServiceArea, driver);

            Assert.IsNotNull(error);
            Assert.AreEqual(0, Db.ToInt(Db.Scalar("SELECT COUNT(*) FROM dbo.[Route] WHERE RouteName = @n", "@n", name)));
        }

        [TestMethod]
        public void CreateRoute_RefusesOffDutyDriver()
        {
            int driver = NewDriver("Off Duty");
            string name = "FleetTest-" + Guid.NewGuid().ToString("N").Substring(0, 8);

            Assert.IsNotNull(FleetAvailability.CreateRoute(name, driver, NewVehicle(), ServiceArea, driver));
            Assert.AreEqual("Off Duty", DriverStatus(driver));
        }

        [TestMethod]
        public void ReactivationConflict_IsSet_WhenVehicleWentToAnotherRoute()
        {
            int vehicle = NewVehicle();
            int parked = NewRoute(NewDriver(), vehicle, "Inactive");
            NewRoute(NewDriver("On Route"), vehicle);

            Assert.IsNotNull(FleetAvailability.ReactivationConflict(parked));
        }

        [TestMethod]
        public void ReactivationConflict_IsSet_WhenDriverWentToAnotherRoute()
        {
            int driver = NewDriver("On Route");
            int parked = NewRoute(driver, NewVehicle(), "Inactive");
            NewRoute(driver, NewVehicle());

            Assert.IsNotNull(FleetAvailability.ReactivationConflict(parked));
        }

        [TestMethod]
        public void ReactivationConflict_IsNull_WhenDriverAndVehicleAreFree()
        {
            Assert.IsNull(FleetAvailability.ReactivationConflict(NewRoute(NewDriver(), NewVehicle(), "Inactive")));
        }

        [TestMethod]
        public void ReactivationConflict_IgnoresTheRoutesOwnParcels()
        {
            int driver = NewDriver("On Route");
            int parked = NewRoute(driver, NewVehicle(), "Inactive");
            NewParcel(driver, parked, "Assigned");

            Assert.IsNull(FleetAvailability.ReactivationConflict(parked));
        }
    }
}
