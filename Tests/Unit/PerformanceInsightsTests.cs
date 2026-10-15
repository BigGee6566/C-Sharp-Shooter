using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Tests.Unit
{
    /// <summary>
    /// The "Needs attention" rules must be quiet on healthy, routine data (so the list stays
    /// actionable) and loud, ordered and linked when something is genuinely wrong.
    /// </summary>
    [TestClass]
    public class PerformanceInsightsTests
    {
        private static PerformanceSnapshot Snap(decimal[] delivered, decimal[] failed,
            decimal currentFailed = 0, decimal previousFailed = 0, int lost = 0, int damaged = 0)
        {
            var labels = new string[delivered.Length];
            for (int i = 0; i < labels.Length; i++) labels[i] = "B" + (i + 1);
            return new PerformanceSnapshot
            {
                Labels = labels,
                Delivered = delivered,
                Failed = failed,
                Current = new PeriodTotals { Failed = currentFailed },
                Previous = new PeriodTotals { Failed = previousFailed },
                Cohort = new CohortSummary { Lost = lost, Damaged = damaged }
            };
        }

        private static PerformanceSnapshot Healthy()
        {
            return Snap(new[] { 20m, 30m }, new[] { 1m, 0m });
        }

        [TestMethod]
        public void A_healthy_period_with_unpaid_but_not_overdue_invoices_raises_nothing()
        {
            var notYetDue = new Receivables { Outstanding = 900m, Overdue = 0m };
            List<AttentionItem> items = PerformanceInsights.Build(Healthy(), new List<DriverStanding>(), notYetDue,
                new List<StuckStage>(), "week");
            Assert.AreEqual(0, items.Count, "invoices that are simply not due yet are not a problem");
        }

        [TestMethod]
        public void Overdue_money_comes_first_with_a_link_to_billing()
        {
            var overdue = new Receivables { Outstanding = 500m, Overdue = 300m, OverdueInvoices = 2, OldestOverdueDays = 9 };
            var weakDriver = new List<DriverStanding> { new DriverStanding { Name = "Zed", Delivered = 1, Failed = 3 } };
            List<AttentionItem> items = PerformanceInsights.Build(Healthy(), weakDriver, overdue, null, "week");

            Assert.AreEqual(AttentionSeverity.High, items[0].Severity);
            StringAssert.Contains(items[0].Text, "is overdue across 2 invoices");
            StringAssert.Contains(items[0].Text, "9 days past due");
            Assert.AreEqual("~/Finance/Invoices.aspx", items[0].Href);
            Assert.AreEqual(AttentionSeverity.Medium, items[items.Count - 1].Severity, "high items are listed before medium ones");
        }

        [TestMethod]
        public void Stuck_parcels_name_the_stage_and_are_capped_at_three_stages()
        {
            var stuck = new List<StuckStage>
            {
                new StuckStage { Status = "Sorted", Count = 4, OldestDays = 6 },
                new StuckStage { Status = "Registered", Count = 2, OldestDays = 5 },
                new StuckStage { Status = "Assigned", Count = 1, OldestDays = 4 },
                new StuckStage { Status = "OutForDelivery", Count = 1, OldestDays = 4 }
            };
            List<AttentionItem> items = PerformanceInsights.Build(Healthy(), null, null, stuck, "day");

            Assert.AreEqual(3, items.Count);
            Assert.AreEqual("4 parcels at \"Sorted - awaiting a driver\" have not moved for more than 3 days (the oldest for 6 days).",
                items[0].Text);
            Assert.AreEqual("~/Admin/Shipments.aspx", items[0].Href);
        }

        [TestMethod]
        public void Failures_appearing_from_none_are_flagged_once_they_are_material()
        {
            List<AttentionItem> big = PerformanceInsights.Build(
                Snap(new[] { 50m }, new[] { 4m }, currentFailed: 4, previousFailed: 0), null, null, null, "day");
            Assert.AreEqual(1, big.Count);
            Assert.AreEqual("4 failed delivery attempts this period, against none in the previous period.", big[0].Text);

            List<AttentionItem> small = PerformanceInsights.Build(
                Snap(new[] { 50m }, new[] { 2m }, currentFailed: 2, previousFailed: 0), null, null, null, "day");
            Assert.AreEqual(0, small.Count, "a couple of failures with no history is below the alert threshold");
        }

        [TestMethod]
        public void Weak_buckets_are_listed_worst_first_and_truncated()
        {
            // Seven weak buckets with success 40..70% in shuffled order, plus one healthy bucket.
            var delivered = new[] { 6m, 4m, 7m, 5m, 6.5m, 4.5m, 5.5m, 30m };
            var failed = new[] { 4m, 6m, 3m, 5m, 3.5m, 5.5m, 4.5m, 0m };
            List<AttentionItem> items = PerformanceInsights.Build(Snap(delivered, failed), null, null, null, "week");

            Assert.AreEqual(6, items.Count, "five weakest plus one '...and N more' line");
            StringAssert.StartsWith(items[0].Text, "B2: delivery success 40.0%");
            StringAssert.StartsWith(items[1].Text, "B6: delivery success 45.0%");
            Assert.AreEqual("...and 2 weeks more below the success target (see the breakdown table).", items[5].Text);
        }

        [TestMethod]
        public void Drivers_are_flagged_only_with_enough_attempts()
        {
            var drivers = new List<DriverStanding>
            {
                new DriverStanding { Name = "Few", Delivered = 0, Failed = 2 },
                new DriverStanding { Name = "Weak", Delivered = 3, Failed = 2 },
                new DriverStanding { Name = "Fine", Delivered = 9, Failed = 1 }
            };
            List<AttentionItem> items = PerformanceInsights.Build(Healthy(), drivers, null, null, "day");

            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("Weak: 60.0% delivery success over 5 attempts (below 80%).", items[0].Text);
        }

        [TestMethod]
        public void Lost_and_damaged_parcels_link_to_their_report()
        {
            List<AttentionItem> items = PerformanceInsights.Build(
                Snap(new[] { 10m }, new[] { 0m }, lost: 1, damaged: 2), null, null, null, "day");
            Assert.AreEqual("1 parcel lost and 2 parcels damaged among this period's shipments.", items[0].Text);
            Assert.AreEqual("~/Reports/LostDamaged.aspx", items[0].Href);
        }

        [TestMethod]
        public void Long_empty_runs_collapse_but_short_ones_stay()
        {
            var rows = new List<string[]>
            {
                new[] { "Jan", "5" }, new[] { "Feb", "0" }, new[] { "Mar", "0" }, new[] { "Apr", "0" },
                new[] { "May", "2" }, new[] { "Jun", "0" }, new[] { "Jul", "0" }, new[] { "Aug", "1" }
            };
            var empty = new List<bool> { false, true, true, true, false, true, true, false };
            List<string[]> shown = PerformanceInsights.CollapseEmptyRuns(rows, empty, 3, "month");

            Assert.AreEqual(6, shown.Count);
            CollectionAssert.AreEqual(new[] { "Feb to Apr", "No activity (3 months)" }, shown[1]);
            Assert.AreEqual("Jun", shown[3][0], "a two-month gap is short enough to keep as rows");
        }

        [TestMethod]
        public void Plurals_read_naturally()
        {
            Assert.AreEqual("1 day", PerformanceInsights.Plural(1m, "day"));
            Assert.AreEqual("0 days", PerformanceInsights.Plural(0m, "day"));
            Assert.AreEqual("12 parcels", PerformanceInsights.Plural(12m, "parcel"));
        }
    }
}
