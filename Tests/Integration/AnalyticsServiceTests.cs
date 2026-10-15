using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Tests.Integration
{
    /// <summary>
    /// Pins every reporting metric to hand-computed answers on a small fixed dataset placed in
    /// January 2001, a month no seed or other test data touches. Covers the boundaries the
    /// team flagged: a Sunday-night booking (still week 1), a Monday-midnight booking (week 2),
    /// a booking at exactly midnight after the period (excluded), a parcel booked in January
    /// but delivered in February, a re-queued delivery attempt that only the event log
    /// remembers, invoices that are billed but not collected, a cancelled invoice that must not
    /// count, an empty previous period, a driver with no deliveries, and owner scoping.
    ///
    ///  S1  customer A  booked Mon 2001-01-01 08:00  Delivered 01-03          driver D1  Cape Town
    ///  S2  customer A  booked Sun 2001-01-07 23:30  Failed 01-09 (no event)  driver D1  Cape Town
    ///  S3  customer B  booked Mon 2001-01-08 00:00  attempt by D1 REQUEUED 01-10, Delivered 02-02  Durban
    ///  S4  customer B  booked Wed 2001-01-31 18:00  Registered               no driver  Gqeberha
    ///  S5  customer A  booked Thu 2001-02-01 00:00  Registered (first instant AFTER January)
    ///
    ///  Invoices (IssuedDate): S1 R100 Paid (01-01), S2 R50 Unpaid (01-07), S3 R200 Paid (01-08),
    ///  S4 R75 Unpaid (01-31), A no-shipment R25 Pending (01-20), B billed for S1 R40 Failed-payment
    ///  (01-22), A no-shipment R999 Cancelled (01-15).
    ///  Customers A and B created 2001-01-15; driver D1 2001-01-20; driver D2 today (idle).
    /// </summary>
    [TestClass]
    public class AnalyticsServiceTests
    {
        private static bool available;
        private static int customerA, customerB, driver1, driver2;
        private static readonly List<int> shipments = new List<int>();
        private static readonly List<int> payments = new List<int>();

        private static readonly ReportPeriod January =
            new ReportPeriod(new DateTime(2001, 1, 1), new DateTime(2001, 1, 31), Granularity.Week);

        [ClassInitialize]
        public static void Setup(TestContext context)
        {
            try
            {
                using (var conn = Db.GetConnection()) conn.Open();
                available = true;
            }
            catch
            {
                available = false;
                return;
            }

            PurgeLeftovers();
            try
            {
                customerA = DbTestHelper.CreateTestClient("anA", "Individual");
                customerB = DbTestHelper.CreateTestClient("anB", "Business");
                driver1 = DbTestHelper.CreateTestDriver("anD1");
                driver2 = DbTestHelper.CreateTestDriver("anD2");

                Db.NonQuery("UPDATE dbo.[User] SET CreatedDate = @d WHERE UserID IN (@a, @b)",
                    "@d", new DateTime(2001, 1, 15), "@a", customerA, "@b", customerB);
                Db.NonQuery("UPDATE dbo.[User] SET CreatedDate = @d WHERE UserID = @u",
                    "@d", new DateTime(2001, 1, 20), "@u", driver1);

                int s1 = AddShipment(customerA, "2001-01-01 08:00", "Delivered", "2001-01-03 10:00", null, driver1, "Cape Town");
                int s2 = AddShipment(customerA, "2001-01-07 23:30", "Failed", null, "2001-01-09 12:00", driver1, "Cape Town");
                int s3 = AddShipment(customerB, "2001-01-08 00:00", "Delivered", "2001-02-02 09:00", null, driver1, "Durban");
                int s4 = AddShipment(customerB, "2001-01-31 18:00", "Registered", null, null, null, "Gqeberha");
                AddShipment(customerA, "2001-02-01 00:00", "Registered", null, null, null, "Cape Town");

                // D1's first attempt at S3 failed and was re-queued; the shipment row no longer shows it.
                Db.NonQuery(
                    @"INSERT INTO dbo.[TrackingHistory] (ShipmentID, DriverID, Location, Status, Remarks, [Timestamp])
                      VALUES (@s, @d, 'Delivery Point', 'Requeued', 'No one home', @t)",
                    "@s", s3, "@d", driver1, "@t", At("2001-01-10 10:00"));

                AddPayment(customerA, s1, 100m, "Paid", "2001-01-01 08:05");
                AddPayment(customerA, s2, 50m, "Unpaid", "2001-01-07 23:35");
                AddPayment(customerB, s3, 200m, "Paid", "2001-01-08 00:05");
                AddPayment(customerB, s4, 75m, "Unpaid", "2001-01-31 18:05");
                AddPayment(customerA, null, 25m, "Pending", "2001-01-20 09:00");
                AddPayment(customerB, s1, 40m, "Failed", "2001-01-22 09:00");
                AddPayment(customerA, null, 999m, "Cancelled", "2001-01-15 09:00");
            }
            catch
            {
                Teardown();
                throw;
            }
        }

        [ClassCleanup]
        public static void Teardown()
        {
            if (!available) return;
            foreach (int p in payments) Db.NonQuery("DELETE FROM dbo.[Payment] WHERE PaymentID = @p", "@p", p);
            foreach (int s in shipments)
            {
                Db.NonQuery("DELETE FROM dbo.[TrackingHistory] WHERE ShipmentID = @s", "@s", s);
                Db.NonQuery("DELETE FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", s);
            }
            payments.Clear();
            shipments.Clear();
            if (driver1 > 0) DbTestHelper.DeleteDriver(driver1);
            if (driver2 > 0) DbTestHelper.DeleteDriver(driver2);
            if (customerA > 0) DbTestHelper.DeleteClient(customerA);
            if (customerB > 0) DbTestHelper.DeleteClient(customerB);
        }

        [TestInitialize]
        public void Init()
        {
            if (!available) Assert.Inconclusive("Skipped: no reachable Logisticore database (see Tests\\App.config).");
        }

        [TestMethod]
        public void Company_totals_match_the_hand_computed_answers()
        {
            PeriodTotals t = AnalyticsService.TotalsFor(AnalyticsScope.Company(), January);

            Assert.AreEqual(4m, t.Booked, "S1-S4; S5 at 2001-02-01 00:00 is outside January");
            Assert.AreEqual(1m, t.Delivered, "only S1 was delivered in January; S3 was delivered in February");
            Assert.AreEqual(2m, t.Failed, "S2 (status only) and D1's re-queued attempt at S3 (event log)");
            Assert.AreEqual(33.3m, t.SuccessRate, "1 delivery out of 3 attempts that resolved");
            Assert.AreEqual(490m, t.Invoiced, "100+50+200+75+25+40; the cancelled R999 is excluded");
            Assert.AreEqual(300m, t.Collected, "only the S1 R100 and S3 R200 invoices are paid");
            Assert.AreEqual(61.2m, t.CollectionRate);
            Assert.AreEqual(2m, t.NewCustomers);
            Assert.AreEqual(1m, t.NewDrivers, "D1 was created in January; D2 was not");
            Assert.AreEqual(1m, t.ActiveDrivers, "only D1 delivered or attempted; D2 did nothing");
        }

        [TestMethod]
        public void Weekly_buckets_split_on_monday_and_include_empty_weeks()
        {
            PerformanceSnapshot snap = AnalyticsService.Build(AnalyticsScope.Company(), January);

            CollectionAssert.AreEqual(
                new[] { "Wk 01 Jan", "Wk 08 Jan", "Wk 15 Jan", "Wk 22 Jan", "Wk 29 Jan" }, snap.Labels);
            CollectionAssert.AreEqual(new[] { 2m, 1m, 0m, 0m, 1m }, snap.Booked,
                "the Sunday 23:30 booking stays in week 1; the Monday 00:00 booking starts week 2");
            CollectionAssert.AreEqual(new[] { 150m, 200m, 25m, 40m, 75m }, snap.Invoiced);
            CollectionAssert.AreEqual(new[] { 0m, 2m, 0m, 0m, 0m }, snap.Failed,
                "S2 failed 01-09 and the S3 attempt was 01-10 - both in the week of 08 Jan");
        }

        [TestMethod]
        public void An_empty_previous_period_reads_as_new_not_as_an_error()
        {
            PerformanceSnapshot snap = AnalyticsService.Build(AnalyticsScope.Company(), January);

            Assert.AreEqual(new DateTime(2000, 12, 1), snap.PreviousPeriod.From);
            Assert.AreEqual(0m, snap.Previous.Booked);
            Assert.IsNull(snap.Previous.SuccessRate, "nothing was resolved in December 2000");
            Assert.AreEqual("new this period (none previously)", Delta.Describe(snap.Current.Booked, snap.Previous.Booked));
        }

        [TestMethod]
        public void Events_count_in_the_period_they_happened()
        {
            var february = new ReportPeriod(new DateTime(2001, 2, 1), new DateTime(2001, 2, 28), Granularity.Week);
            PeriodTotals t = AnalyticsService.TotalsFor(AnalyticsScope.Company(), february);

            Assert.AreEqual(1m, t.Booked, "S5 was booked at the first instant of February");
            Assert.AreEqual(1m, t.Delivered, "S3, booked in January, was delivered in February");
            Assert.AreEqual(0m, t.Failed);
        }

        [TestMethod]
        public void Cohort_and_pipeline_follow_the_booked_shipments()
        {
            PerformanceSnapshot snap = AnalyticsService.Build(AnalyticsScope.Company(), January);

            Assert.AreEqual(4, snap.Cohort.Total);
            Assert.AreEqual(2, snap.Cohort.Delivered, "S1 and S3 - the cohort follows the parcels booked in January");
            Assert.AreEqual(1, snap.Cohort.Failed);
            Assert.AreEqual(1, snap.Cohort.InProgress, "S4 is still registered");

            CollectionAssert.AreEqual(new[] { 4, 3, 3, 3, 2 }, new[]
            {
                snap.Pipeline[0].Value, snap.Pipeline[1].Value, snap.Pipeline[2].Value,
                snap.Pipeline[3].Value, snap.Pipeline[4].Value
            });

            Assert.AreEqual("Cape Town", snap.TopDestinations[0].Name);
            Assert.AreEqual(2m, snap.TopDestinations[0].Value, "S5 is a Cape Town booking but falls in February");
        }

        [TestMethod]
        public void Customer_scope_sees_only_their_own_shipments_and_invoices()
        {
            PeriodTotals a = AnalyticsService.TotalsFor(AnalyticsScope.ForCustomer(customerA), January);
            Assert.AreEqual(2m, a.Booked);
            Assert.AreEqual(1m, a.Delivered);
            Assert.AreEqual(1m, a.Failed);
            Assert.AreEqual(175m, a.Invoiced, "100 + 50 + the R25 pending invoice; the cancelled one is excluded");
            Assert.AreEqual(100m, a.Collected);
            Assert.AreEqual(0m, a.NewCustomers, "headcounts are a company-only figure");

            PeriodTotals b = AnalyticsService.TotalsFor(AnalyticsScope.ForCustomer(customerB), January);
            Assert.AreEqual(2m, b.Booked);
            Assert.AreEqual(0m, b.Delivered, "B's only delivery happened in February");
            Assert.AreEqual(1m, b.Failed, "the re-queued attempt at B's parcel S3");
            Assert.AreEqual(0.0m, b.SuccessRate);
            Assert.AreEqual(315m, b.Invoiced, "200 + 75 + the R40 invoice billed to B for A's parcel S1");
            Assert.AreEqual(200m, b.Collected);
        }

        [TestMethod]
        public void Driver_scope_sees_only_their_own_work_and_no_revenue()
        {
            PeriodTotals d1 = AnalyticsService.TotalsFor(AnalyticsScope.ForDriver(driver1), January);
            Assert.AreEqual(3m, d1.Booked, "S1, S2 and S3 are assigned to D1");
            Assert.AreEqual(1m, d1.Delivered);
            Assert.AreEqual(2m, d1.Failed, "S2, plus D1's own re-queued attempt at S3");
            Assert.AreEqual(1m, d1.ActiveDrivers);
            Assert.AreEqual(0m, d1.Invoiced, "drivers never see billing amounts");
            Assert.AreEqual(0m, d1.Collected);

            PeriodTotals d2 = AnalyticsService.TotalsFor(AnalyticsScope.ForDriver(driver2), January);
            Assert.AreEqual(0m, d2.Booked, "a driver with no assignments sees nothing");
            Assert.AreEqual(0m, d2.Delivered);
            Assert.AreEqual(0m, d2.Failed);
            Assert.AreEqual(0m, d2.ActiveDrivers);
        }

        [TestMethod]
        public void Driver_standings_rank_by_deliveries_and_include_idle_drivers()
        {
            IList<DriverStanding> standings =
                AnalyticsService.DriverStandings(AnalyticsScope.Company(), January.From, January.ToExclusive);
            DriverStanding d1 = Find(standings, driver1);
            DriverStanding d2 = Find(standings, driver2);

            Assert.IsNotNull(d1);
            Assert.AreEqual(1, d1.Delivered);
            Assert.AreEqual(2, d1.Failed);
            Assert.AreEqual(33.3m, d1.SuccessRate);
            Assert.IsNotNull(d2, "a driver with no deliveries still appears, with zeros");
            Assert.AreEqual(0, d2.Delivered);
            Assert.AreEqual(0, d2.Failed);
            Assert.IsNull(d2.SuccessRate);
        }

        [TestMethod]
        public void Average_delivery_time_and_revenue_per_shipment_follow_the_fixture()
        {
            PeriodTotals t = AnalyticsService.TotalsFor(AnalyticsScope.Company(), January);
            Assert.AreEqual(50.0m, t.AvgDeliveryHours, "S1 was booked 01-01 08:00 and delivered 01-03 10:00");
            Assert.AreEqual(122.50m, t.RevenuePerShipment, "R490 invoiced over 4 shipments");

            var february = new ReportPeriod(new DateTime(2001, 2, 1), new DateTime(2001, 2, 28), Granularity.Week);
            Assert.AreEqual(25.0m * 24m + 9m, AnalyticsService.TotalsFor(AnalyticsScope.Company(), february).AvgDeliveryHours,
                "S3: booked 01-08 00:00, delivered 02-02 09:00 = 25 days 9 hours");
        }

        [TestMethod]
        public void Receivables_split_overdue_from_not_yet_due()
        {
            DateTime asOf = At("2001-02-01 00:00");
            Receivables company = AnalyticsService.ReceivablesFor(AnalyticsScope.Company(), January.From, January.ToExclusive, asOf);
            Assert.AreEqual(190m, company.Outstanding, "50 + 75 + 25 pending + 40 failed-payment; paid and cancelled excluded");
            Assert.AreEqual(115m, company.Overdue, "S2 (due 01-14), the pending (due 01-27) and the failed-payment (due 01-29)");
            Assert.AreEqual(3, company.OverdueInvoices);
            Assert.AreEqual(18, company.OldestOverdueDays, "S2 due 01-14, counted to 02-01");
            Assert.AreEqual(75m, company.NotYetDue, "S4 is not due until 02-07");

            Receivables a = AnalyticsService.ReceivablesFor(AnalyticsScope.ForCustomer(customerA), January.From, January.ToExclusive, asOf);
            Assert.AreEqual(75m, a.Outstanding, "A's own unpaid S2 invoice and the pending one");

            Receivables d = AnalyticsService.ReceivablesFor(AnalyticsScope.ForDriver(driver1), January.From, January.ToExclusive, asOf);
            Assert.AreEqual(0m, d.Outstanding, "drivers never see money");
        }

        [TestMethod]
        public void Stuck_parcels_are_those_that_have_not_moved_for_long_enough()
        {
            IList<StuckStage> stuck = AnalyticsService.StuckParcels(AnalyticsScope.ForCustomer(customerB), At("2001-02-10 12:00"), 3);
            Assert.AreEqual(1, stuck.Count);
            Assert.AreEqual("Registered", stuck[0].Status);
            Assert.AreEqual(1, stuck[0].Count, "S4 only - B's other parcel was delivered");
            Assert.AreEqual(10, stuck[0].OldestDays, "booked 01-31, checked 02-10");

            IList<StuckStage> notYet = AnalyticsService.StuckParcels(AnalyticsScope.ForCustomer(customerB), At("2001-02-02 12:00"), 3);
            Assert.AreEqual(0, notYet.Count, "two days without moving is not yet stuck");
        }

        [TestMethod]
        public void Top_customers_rank_by_money_invoiced()
        {
            IList<NamedValue> top = AnalyticsService.TopCustomers(AnalyticsScope.Company(), January.From, January.ToExclusive, 5);
            Assert.AreEqual("Test anB", top[0].Name);
            Assert.AreEqual(315m, top[0].Value, "200 + 75 + 40");
            Assert.AreEqual("Test anA", top[1].Name);
            Assert.AreEqual(175m, top[1].Value, "100 + 50 + 25; the cancelled R999 is excluded");

            Assert.ThrowsException<UnauthorizedAccessException>(() =>
                AnalyticsService.TopCustomers(AnalyticsScope.ForCustomer(customerA), January.From, January.ToExclusive, 5));
        }

        [TestMethod]
        public void Breakdown_by_customer_type_service_area_and_driver_matches_the_fixture()
        {
            AnalyticsScope company = AnalyticsScope.Company();

            IList<GroupFigures> byType = AnalyticsService.BreakdownBy(company, CompareDimension.ClientType, January.From, January.ToExclusive);
            GroupFigures individual = Group(byType, "Individual"), business = Group(byType, "Business");
            Assert.AreEqual(2, individual.Booked, "A booked S1 and S2");
            Assert.AreEqual(1, individual.Delivered);
            Assert.AreEqual(1, individual.Failed, "S2");
            Assert.AreEqual(175m, individual.Invoiced, "100 + 50 + 25; the cancelled one excluded");
            Assert.AreEqual(2, business.Booked);
            Assert.AreEqual(0, business.Delivered);
            Assert.AreEqual(1, business.Failed, "the re-queued attempt at B's S3");
            Assert.AreEqual(315m, business.Invoiced, "200 + 75 + 40");

            IList<GroupFigures> byArea = AnalyticsService.BreakdownBy(company, CompareDimension.ServiceArea, January.From, January.ToExclusive);
            GroupFigures unsorted = Group(byArea, "Not yet sorted");
            Assert.AreEqual(4, unsorted.Booked, "no fixture parcel has a sort lane");
            Assert.AreEqual(465m, unsorted.Invoiced, "every invoice tied to a shipment");
            Assert.AreEqual(25m, Group(byArea, "No shipment").Invoiced, "the pending invoice has no shipment");

            IList<GroupFigures> byDriver = AnalyticsService.BreakdownBy(company, CompareDimension.Driver, January.From, January.ToExclusive);
            GroupFigures d1 = Group(byDriver, "Test Driver anD1");
            Assert.AreEqual(3, d1.Booked);
            Assert.AreEqual(1, d1.Delivered);
            Assert.AreEqual(2, d1.Failed, "S2 plus D1's own re-queued attempt");
            Assert.IsNull(d1.Invoiced, "no revenue split by driver");
            Assert.AreEqual(1, Group(byDriver, "Unassigned").Booked, "S4 has no driver");
            Assert.IsTrue(Group(byDriver, "Unassigned").Placeholder, "catch-all groups are flagged");
            Assert.IsFalse(d1.Placeholder);
            StringAssert.StartsWith(d1.Key, driver1.ToString(System.Globalization.CultureInfo.InvariantCulture) + (char)31,
                "drivers are keyed by ID, so two with the same name stay apart");
            Assert.IsTrue(unsorted.Placeholder);

            Assert.ThrowsException<UnauthorizedAccessException>(() =>
                AnalyticsService.BreakdownBy(AnalyticsScope.ForCustomer(customerA), CompareDimension.Driver, January.From, January.ToExclusive));
        }

        private static GroupFigures Group(IList<GroupFigures> groups, string name)
        {
            foreach (GroupFigures g in groups) if (g.Name == name) return g;
            Assert.Fail("missing group " + name);
            return null;
        }

        private static DriverStanding Find(IList<DriverStanding> list, int id)
        {
            foreach (DriverStanding s in list) if (s.DriverId == id) return s;
            return null;
        }

        private static DateTime At(string value)
        {
            return DateTime.ParseExact(value, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Removes any rows a previously killed run left behind, so the company-wide January 2001
        /// assertions only ever see this run's fixture.
        /// </summary>
        private static void PurgeLeftovers()
        {
            Db.NonQuery("DELETE FROM dbo.[Payment] WHERE InvoiceNumber LIKE 'TEST-AN-%'");
            Db.NonQuery(
                @"DELETE th FROM dbo.[TrackingHistory] th
                  JOIN dbo.[Shipment] s ON s.ShipmentID = th.ShipmentID
                  WHERE s.TrackingNumber LIKE 'TEST-AN-%'");
            Db.NonQuery("DELETE FROM dbo.[Shipment] WHERE TrackingNumber LIKE 'TEST-AN-%'");
            Db.NonQuery(
                @"DECLARE @u TABLE (id INT);
                  INSERT INTO @u SELECT UserID FROM dbo.[User] WHERE Email LIKE 'an[ABD]%@test.invalid';
                  DELETE FROM dbo.AuditLog WHERE UserID IN (SELECT id FROM @u);
                  DELETE FROM dbo.PaymentAllocation WHERE AppliedBy IN (SELECT id FROM @u);
                  DELETE FROM dbo.[Client] WHERE UserID IN (SELECT id FROM @u);
                  DELETE FROM dbo.[Driver] WHERE UserID IN (SELECT id FROM @u);
                  DELETE FROM dbo.[User] WHERE UserID IN (SELECT id FROM @u);");
        }

        private static int AddShipment(int sender, string booked, string status, string deliveredOn,
            string lastChange, int? driverId, string destCity)
        {
            int id = Db.Insert(
                @"INSERT INTO dbo.[Shipment]
                    (TrackingNumber, SenderID, DriverID, PickupAddress, DeliveryAddress, OriginCity, DestCity,
                     ParcelWeight, Cost, ShipmentDate, DeliveryDate, LastStatusChange, DeliveryStatus,
                     WarehouseStatus, PaymentGateStatus)
                  VALUES (@t, @s, @d, 'Test pickup', 'Test delivery', 'Johannesburg', @dc,
                          2, 100, @sd, @dd, @lc, @st, 'Pending', 'NotRequired')",
                "@t", "TEST-AN-" + Guid.NewGuid().ToString("N").Substring(0, 10).ToUpperInvariant(),
                "@s", sender, "@d", (object)driverId, "@dc", destCity,
                "@sd", At(booked),
                "@dd", deliveredOn == null ? (object)null : At(deliveredOn),
                "@lc", lastChange == null ? (object)null : At(lastChange),
                "@st", status);
            shipments.Add(id);
            return id;
        }

        private static void AddPayment(int client, int? shipmentId, decimal amount, string status, string issued)
        {
            int id = Db.Insert(
                @"INSERT INTO dbo.[Payment] (ClientID, ShipmentID, InvoiceNumber, Amount, PaymentStatus, IssuedDate)
                  VALUES (@c, @s, @inv, @a, @ps, @i)",
                "@c", client, "@s", (object)shipmentId,
                "@inv", "TEST-AN-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                "@a", amount, "@ps", status, "@i", At(issued));
            payments.Add(id);
        }
    }
}
