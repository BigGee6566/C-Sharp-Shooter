using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Tests.Unit
{
    /// <summary>The Comparison report's wording, highlights and two-period URL handling (pure).</summary>
    [TestClass]
    public class ComparisonTests
    {
        private static readonly DateTime Today = new DateTime(2026, 10, 15);

        private static ComparisonRow Row(List<ComparisonRow> rows, string metric)
        {
            foreach (ComparisonRow r in rows) if (r.Metric == metric) return r;
            Assert.Fail("missing row " + metric);
            return null;
        }

        private static List<ComparisonRow> SampleRows(int daysA = 30, int daysB = 30)
        {
            var a = new PeriodTotals { Booked = 100, Delivered = 90, Failed = 10, Invoiced = 4000, Collected = 3800, NewCustomers = 0, AvgDeliveryHours = 30 };
            var b = new PeriodTotals { Booked = 150, Delivered = 120, Failed = 30, Invoiced = 5000, Collected = 1000, NewCustomers = 5, AvgDeliveryHours = 30 };
            return ComparisonInsights.PeriodRows(a, b, daysA, daysB);
        }

        [TestMethod]
        public void Changes_are_measured_against_period_A_with_a_verdict_in_words()
        {
            List<ComparisonRow> rows = SampleRows();
            Assert.AreEqual("up 50.0% vs Period A (favourable)", Row(rows, "Shipments booked").Change);
            Assert.AreEqual("up 200.0% vs Period A (unfavourable)", Row(rows, "Failed delivery attempts").Change);
            Assert.AreEqual("down 10.0 percentage points vs Period A (unfavourable)", Row(rows, "Delivery attempt success").Change,
                "rates move in percentage points: 90% -> 80%");
            Assert.AreEqual("100 vs 150 attempts", Row(rows, "Delivery attempt success").Detail);
            Assert.AreEqual("no change vs Period A", Row(rows, "Average delivery time").Change);
        }

        [TestMethod]
        public void From_nothing_to_something_still_gets_a_verdict()
        {
            Assert.AreEqual("up from none in Period A (favourable)", Row(SampleRows(), "New customers").Change);
        }

        [TestMethod]
        public void Unequal_periods_judge_per_day_rows_not_totals()
        {
            List<ComparisonRow> rows = SampleRows(30, 15);
            Assert.AreEqual("up 50.0% vs Period A", Row(rows, "Shipments booked").Change, "a total over unequal lengths gets no verdict");
            Assert.IsNull(Row(rows, "Revenue invoiced").HigherIsBetter);
            Assert.AreEqual("up 200.0% vs Period A (favourable)", Row(rows, "Shipments booked per day").Change, "3.33 -> 10 per day");
            Assert.AreEqual("up 500.0% vs Period A (unfavourable)", Row(rows, "Failed delivery attempts per day").Change);
            Assert.AreEqual("down 10.0 percentage points vs Period A (unfavourable)", Row(rows, "Delivery attempt success").Change,
                "rates do not depend on length, so they keep their verdict");
        }

        [TestMethod]
        public void Equal_periods_show_no_redundant_per_day_rows_for_deliveries()
        {
            foreach (ComparisonRow r in SampleRows()) Assert.AreNotEqual("Failed delivery attempts per day", r.Metric);
        }

        [TestMethod]
        public void Per_day_rows_make_unequal_periods_comparable()
        {
            ComparisonRow perDay = Row(SampleRows(30, 15), "Shipments booked per day");
            Assert.AreEqual("3.33", perDay.Format(perDay.A), "100 over 30 days, shown to two decimals");
            Assert.AreEqual(10m, perDay.B, "150 over 15 days");
            Assert.AreEqual("per day", perDay.Unit);
        }

        [TestMethod]
        public void Per_day_change_uses_unrounded_values()
        {
            var a = new PeriodTotals { Booked = 14 };
            var b = new PeriodTotals { Booked = 16 };
            ComparisonRow perDay = Row(ComparisonInsights.PeriodRows(a, b, 1000, 1000), "Shipments booked per day");
            Assert.AreEqual("up 14.3% vs Period A (favourable)", perDay.Change, "0.014 vs 0.016 per day is +14.3%, not +100%");
        }

        [TestMethod]
        public void Rates_from_too_few_attempts_are_shown_but_not_judged()
        {
            var a = new PeriodTotals { Delivered = 1, Failed = 1 };
            var b = new PeriodTotals { Delivered = 40, Failed = 0 };
            ComparisonRow success = Row(ComparisonInsights.PeriodRows(a, b, 7, 7), "Delivery attempt success");
            Assert.AreEqual("up 50.0 percentage points vs Period A", success.Change);
            Assert.AreEqual("2 vs 40 attempts; too few to judge", success.Detail);
        }

        [TestMethod]
        public void Paid_so_far_carries_no_verdict()
        {
            List<ComparisonRow> rows = SampleRows();
            Assert.AreEqual("down 73.7% vs Period A", Row(rows, "Invoices paid so far").Change,
                "a younger period has simply had less time to be paid");
            Assert.IsNull(Row(rows, "Share of invoiced revenue paid so far").HigherIsBetter);
        }

        [TestMethod]
        public void Highlights_lead_with_a_success_move_then_the_biggest_judged_changes()
        {
            List<string> h = ComparisonInsights.PeriodHighlights(SampleRows(), true);
            Assert.AreEqual(3, h.Count);
            Assert.AreEqual("Delivery attempt success fell 10.0 percentage points (90.0% to 80.0%) - unfavourable.", h[0]);
            Assert.AreEqual("Failed delivery attempts rose 200.0% (10 to 30) - unfavourable.", h[1]);
            Assert.AreEqual("Shipments booked rose 50.0% (100 to 150) - favourable.", h[2]);
        }

        [TestMethod]
        public void Highlights_for_unequal_periods_use_per_day_rows_only()
        {
            List<string> h = ComparisonInsights.PeriodHighlights(SampleRows(30, 15), false);
            Assert.AreEqual(3, h.Count);
            StringAssert.StartsWith(h[1], "Failed delivery attempts per day rose 500.0%");
            StringAssert.StartsWith(h[2], "Shipments booked per day rose 200.0%");
        }

        [TestMethod]
        public void Highlights_skip_changes_from_tiny_bases()
        {
            var a = new PeriodTotals { Booked = 2, Delivered = 2, Invoiced = 300 };
            var b = new PeriodTotals { Booked = 8, Delivered = 8, Invoiced = 1200 };
            Assert.AreEqual(0, ComparisonInsights.PeriodHighlights(ComparisonInsights.PeriodRows(a, b, 7, 7), true).Count,
                "2 -> 8 shipments and R300 -> R1 200 are too small a base for a headline percentage");
        }

        [TestMethod]
        public void Elapsed_days_stop_at_today()
        {
            Assert.AreEqual(15, ComparisonInsights.ElapsedDays(new ReportPeriod(new DateTime(2026, 10, 1), new DateTime(2026, 10, 31), Granularity.Day), Today));
            Assert.AreEqual(31, ComparisonInsights.ElapsedDays(new ReportPeriod(new DateTime(2026, 8, 1), new DateTime(2026, 8, 31), Granularity.Day), Today));
            Assert.AreEqual(0, ComparisonInsights.ElapsedDays(new ReportPeriod(new DateTime(2026, 11, 1), new DateTime(2026, 11, 30), Granularity.Day), Today));
        }

        [TestMethod]
        public void Group_highlights_name_the_busiest_and_the_weakest_and_strongest()
        {
            var groups = new List<GroupFigures>
            {
                new GroupFigures { Name = "Metro-JHB", Booked = 60, Delivered = 50, Failed = 10, Invoiced = 9000m },
                new GroupFigures { Name = "Metro-CPT", Booked = 30, Delivered = 29, Failed = 1, Invoiced = 4000m },
                new GroupFigures { Name = "Regional", Booked = 10, Delivered = 1, Failed = 1, Invoiced = 900m }
            };
            List<string> h = ComparisonInsights.GroupHighlights(groups, "service area");
            Assert.AreEqual("Metro-JHB is the busiest service area: 60 shipments booked (60.0% of all).", h[0]);
            Assert.AreEqual("Lowest delivery success: Metro-JHB at 83.3% (60 attempts). Highest: Metro-CPT at 96.7%.", h[1],
                "Regional has too few attempts to be judged");
            Assert.AreEqual("Most revenue: Metro-JHB (R" + ((char)0xA0) + "9" + ((char)0xA0) + "000.00 invoiced).", h[2]);
        }

        [TestMethod]
        public void Group_highlights_with_one_qualifying_group_do_not_claim_level_success()
        {
            var groups = new List<GroupFigures>
            {
                new GroupFigures { Name = "Metro-JHB", Booked = 60, Delivered = 50, Failed = 10 },
                new GroupFigures { Name = "Regional", Booked = 2, Delivered = 1, Failed = 0 }
            };
            List<string> h = ComparisonInsights.GroupHighlights(groups, "service area");
            Assert.AreEqual("Only Metro-JHB had enough delivery attempts (at least 3) to judge success: 83.3%.", h[1]);
        }

        [TestMethod]
        public void Placeholder_groups_are_never_named_as_findings()
        {
            var groups = new List<GroupFigures>
            {
                new GroupFigures { Name = "Unassigned", Placeholder = true, Booked = 90, Delivered = 0, Failed = 9 },
                new GroupFigures { Name = "Thabo", Booked = 10, Delivered = 10, Failed = 0 }
            };
            List<string> h = ComparisonInsights.GroupHighlights(groups, "driver");
            StringAssert.StartsWith(h[0], "Thabo is the busiest driver");
            foreach (string s in h) Assert.IsFalse(s.Contains("Unassigned"), s);
        }

        [TestMethod]
        public void Previous_period_bookings_attach_by_key_and_stopped_groups_stay_visible()
        {
            var current = new List<GroupFigures>
            {
                new GroupFigures { Key = "1", Name = "Thabo", Booked = 30 },
                new GroupFigures { Key = "2", Name = "Lerato", Booked = 4 }
            };
            var previous = new List<GroupFigures>
            {
                new GroupFigures { Key = "1", Name = "Thabo", Booked = 20 },
                new GroupFigures { Key = "2", Name = "Lerato", Booked = 10 },
                new GroupFigures { Key = "3", Name = "Sipho", Booked = 8 }
            };
            ComparisonInsights.AttachPrevious(current, previous);
            Assert.AreEqual(3, current.Count, "Sipho stopped but is still listed");
            Assert.AreEqual(20, current[0].PreviousBooked);
            Assert.AreEqual(0, current[2].Booked);

            List<string> h = ComparisonInsights.GroupHighlights(current, "driver");
            CollectionAssert.Contains(h, "Fastest growth: Thabo, 20 to 30 shipments booked (up 50.0% vs previous period).");
            CollectionAssert.Contains(h, "Biggest drop: Sipho, 8 to 0 shipments booked (down 100.0% vs previous period).");
        }

        [TestMethod]
        public void Named_pairs_produce_the_expected_periods()
        {
            ReportPeriod a, b;
            ComparisonQuery.PairPeriods("30d", Today, out a, out b);
            Assert.AreEqual(new DateTime(2026, 9, 16), b.From);
            Assert.AreEqual(new DateTime(2026, 8, 17), a.From);
            Assert.AreEqual(30, a.Days);

            ComparisonQuery.PairPeriods("month", Today, out a, out b);
            Assert.AreEqual(new DateTime(2026, 9, 1), b.From);
            Assert.AreEqual(new DateTime(2026, 9, 30), b.To);
            Assert.AreEqual(new DateTime(2026, 8, 1), a.From);
            Assert.AreEqual(new DateTime(2026, 8, 31), a.To);

            ComparisonQuery.PairPeriods("ytd", new DateTime(2028, 2, 29), out a, out b);
            Assert.AreEqual(new DateTime(2027, 2, 28), a.To, "29 Feb maps to 28 Feb in a non-leap year");
        }

        [TestMethod]
        public void Custom_periods_round_trip_and_bad_dates_fall_back_with_a_message()
        {
            var q = new NameValueCollection { { "aFrom", "2026-01-01" }, { "aTo", "2026-01-31" }, { "bFrom", "2026-02-01" }, { "bTo", "2026-02-28" } };
            ReportPeriod a, b;
            string pair, problem;
            ComparisonQuery.Read(q, Today, out a, out b, out pair, out problem);
            Assert.IsNull(problem);
            Assert.AreEqual(ReportQuery.CustomPreset, pair);
            Assert.AreEqual(31, a.Days);
            Assert.AreEqual(28, b.Days);

            var badB = new NameValueCollection { { "aFrom", "2026-01-01" }, { "aTo", "2026-01-31" }, { "bFrom", "1999-12-01" }, { "bTo", "2026-02-01" } };
            ComparisonQuery.Read(badB, Today, out a, out b, out pair, out problem);
            StringAssert.StartsWith(problem, "Period B: ", "the message names the period that is wrong");

            var bad = new NameValueCollection { { "aFrom", "nope" } };
            ComparisonQuery.Read(bad, Today, out a, out b, out pair, out problem);
            StringAssert.Contains(problem, "all four dates");
            Assert.AreEqual("30d", pair);
        }

        [TestMethod]
        public void The_form_keeps_a_named_pair_until_a_date_is_edited()
        {
            ReportPeriod a, b;
            ComparisonQuery.PairPeriods("30d", Today, out a, out b);
            string shown = ComparisonQuery.Shown(a, b);

            Assert.AreEqual("?mode=periods&pair=30d",
                ComparisonQuery.FromForm("30d", "2026-08-17", "2026-09-15", "2026-09-16", "2026-10-15", shown));
            Assert.AreEqual("?mode=periods&aFrom=2026-08-01&aTo=2026-09-15&bFrom=2026-09-16&bTo=2026-10-15",
                ComparisonQuery.FromForm("30d", "2026-08-01", "2026-09-15", "2026-09-16", "2026-10-15", shown));
            Assert.AreEqual("?mode=periods&aFrom=%3cx%3e&aTo=2026-09-15&bFrom=2026-09-16&bTo=2026-10-15",
                ComparisonQuery.FromForm("custom", "<x>", "2026-09-15", "2026-09-16", "2026-10-15", shown), "typed text is encoded");
        }
    }
}
