using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Tests.Unit
{
    /// <summary>The decision rules behind the Parcel Movement, Driver, Revenue and Lost/Damaged reports (pure).</summary>
    [TestClass]
    public class ReportRulesTests
    {
        private static readonly DateTime Today = new DateTime(2026, 10, 15);

        [TestMethod]
        public void Movement_stages_count_failed_and_requeued_as_attempts_and_dispatch_once()
        {
            Assert.AreEqual(MovementStages.FailedAttempt, MovementStages.Of("Failed"));
            Assert.AreEqual(MovementStages.FailedAttempt, MovementStages.Of("requeued"), "case does not matter");
            Assert.AreEqual(MovementStages.Dispatched, MovementStages.Of("Assigned"));
            Assert.AreEqual(MovementStages.Dispatched, MovementStages.Of("InTransit"));
            Assert.AreEqual(MovementStages.Dispatched, MovementStages.Of("OutForDelivery"));
            Assert.AreEqual(-1, MovementStages.Of("Cancelled"));
            Assert.AreEqual("Attempt failed - re-queued", MovementStages.EventLabel("Requeued"));
            Assert.AreEqual("Unknown", MovementStages.EventLabel(null));
        }

        [TestMethod]
        public void Drivers_are_judged_only_with_enough_attempts_and_against_the_thresholds()
        {
            Assert.AreEqual(DriverRules.NoAttempts, DriverRules.Classify(0, 0));
            Assert.AreEqual(DriverRules.TooFew, DriverRules.Classify(1, 1), "2 attempts is below the minimum of 3");
            Assert.AreEqual(DriverRules.BelowAlert, DriverRules.Classify(3, 1), "75%");
            Assert.AreEqual(DriverRules.BelowTarget, DriverRules.Classify(4, 1), "80% is at the alert level, under the 90% target");
            Assert.AreEqual(DriverRules.OnTarget, DriverRules.Classify(9, 1), "90% meets the target");
            Assert.IsTrue(DriverRules.Rank(DriverRules.BelowAlert) < DriverRules.Rank(DriverRules.OnTarget));
            Assert.IsTrue(DriverRules.Rank(DriverRules.OnTarget) < DriverRules.Rank(DriverRules.NoAttempts));
        }

        [TestMethod]
        public void Driver_change_is_in_points_and_needs_both_periods_judged()
        {
            Assert.AreEqual("down 20.0 points", DriverRules.ChangeInPoints(8, 2, 9, 0));
            Assert.AreEqual("no change", DriverRules.ChangeInPoints(9, 1, 18, 2));
            Assert.AreEqual("n/a", DriverRules.ChangeInPoints(9, 1, 1, 0), "the previous period had too few attempts");
        }

        [TestMethod]
        public void A_rating_alert_needs_enough_ratings()
        {
            Assert.IsFalse(DriverRules.RatingNeedsAttention(1m, 1), "one bad review is not a pattern");
            Assert.IsTrue(DriverRules.RatingNeedsAttention(2.5m, DriverRules.MinRatings));
            Assert.IsFalse(DriverRules.RatingNeedsAttention(3m, 20), "3.0 is not below the alert");
            Assert.IsFalse(DriverRules.RatingNeedsAttention(null, 20));
        }

        [TestMethod]
        public void Collection_is_only_judged_once_every_invoice_is_due()
        {
            Assert.IsFalse(RevenueRules.AllInvoicesDue(new DateTime(2026, 10, 10), Today), "invoices from 10 Oct are due 17 Oct");
            Assert.IsFalse(RevenueRules.AllInvoicesDue(new DateTime(2026, 10, 8), Today), "due today, not yet overdue");
            Assert.IsTrue(RevenueRules.AllInvoicesDue(new DateTime(2026, 9, 30), Today));
        }

        [TestMethod]
        public void A_recent_period_is_not_mature_for_exception_comparison()
        {
            Assert.IsFalse(ExceptionRules.IsMature(new DateTime(2026, 10, 10), Today));
            Assert.IsTrue(ExceptionRules.IsMature(new DateTime(2026, 10, 1), Today), "exactly 14 days old");
        }

        [TestMethod]
        public void Exception_groups_are_ranked_by_rate_and_need_enough_bookings()
        {
            var groups = new List<ExceptionGroup>
            {
                new ExceptionGroup { Name = "Busy", Booked = 200, Exceptions = 6 },     // 3%
                new ExceptionGroup { Name = "Small", Booked = 4, Exceptions = 2 },      // too few bookings
                new ExceptionGroup { Name = "Risky", Booked = 20, Exceptions = 4 },     // 20%
                new ExceptionGroup { Name = "Clean", Booked = 50, Exceptions = 0 }
            };
            List<ExceptionGroup> ranked = ExceptionRules.Ranked(groups);
            Assert.AreEqual(2, ranked.Count);
            Assert.AreEqual("Risky", ranked[0].Name, "the highest rate, not the most exceptions, comes first");

            List<ExceptionGroup> hot = ExceptionRules.Hotspots(groups, 4.5m);
            Assert.AreEqual(1, hot.Count, "only Risky is at least double the overall rate");
            Assert.AreEqual(0, ExceptionRules.Hotspots(groups, null).Count);
        }

        [TestMethod]
        public void Shared_names_are_told_apart_by_id()
        {
            Dictionary<int, string> names = DisplayNames.Unique(new[]
            {
                new KeyValuePair<int, string>(7, "Sam Dube"),
                new KeyValuePair<int, string>(9, "Sam Dube"),
                new KeyValuePair<int, string>(4, "Lerato Modise")
            });
            Assert.AreEqual("Sam Dube (#7)", names[7]);
            Assert.AreEqual("Sam Dube (#9)", names[9]);
            Assert.AreEqual("Lerato Modise", names[4]);
        }

        [TestMethod]
        public void Detail_tables_cap_rows_and_say_how_many_were_left_out()
        {
            var rows = new List<string[]>();
            for (int i = 0; i < 4; i++) rows.Add(new[] { "W" + i, "x" });
            string html = ReportHtml.DetailTable(new[] { "Waybill", "Value" }, rows, "Test", "Nothing", false, 3, 10);
            StringAssert.Contains(html, "Showing the first 3 of 10 rows");
            Assert.IsFalse(html.Contains("W3"), "the fourth row is cut");

            string all = ReportHtml.DetailTable(new[] { "Waybill", "Value" }, rows, "Test", "Nothing", false, 10);
            Assert.IsFalse(all.Contains("Showing the first"));
            StringAssert.Contains(ReportHtml.DetailTable(new[] { "A" }, new List<string[]>(), "Test", "Nothing <here>", false, 10),
                "Nothing &lt;here&gt;", "the empty text is encoded");
        }

        [TestMethod]
        public void A_failing_dashboard_panel_shows_a_notice_instead_of_breaking_the_page()
        {
            string html = RoleDashboards.Render(() => { throw new InvalidOperationException("database down"); }, null);
            StringAssert.Contains(html, "unavailable right now");
            Assert.IsFalse(html.Contains("database down"), "no internals reach the page");
        }

        [TestMethod]
        public void Company_dashboard_panels_are_cached_briefly()
        {
            string key = RoleDashboards.CacheKey("test-" + Guid.NewGuid().ToString("N"), Today, null);
            Assert.AreEqual("first", RoleDashboards.Render(() => "first", key));
            Assert.AreEqual("first", RoleDashboards.Render(() => "second", key), "served from the cache");
            Assert.AreEqual("second", RoleDashboards.Render(() => "second", null), "per-user panels are never cached");
        }
    }
}
