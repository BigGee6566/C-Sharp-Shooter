using System;
using System.Collections.Specialized;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Tests.Unit
{
    /// <summary>
    /// The shared period picker turns untrusted query-string text into a validated period.
    /// It must never throw on garbage, must fall back with an explanation, and must
    /// round-trip so a bookmarked report reopens exactly as it was.
    /// </summary>
    [TestClass]
    public class ReportQueryTests
    {
        private static readonly DateTime Today = new DateTime(2026, 10, 15);

        private static NameValueCollection Q(params string[] pairs)
        {
            var q = new NameValueCollection();
            for (int i = 0; i + 1 < pairs.Length; i += 2) q[pairs[i]] = pairs[i + 1];
            return q;
        }

        [TestMethod]
        public void No_query_uses_the_default_preset()
        {
            string preset, problem;
            ReportPeriod p = ReportQuery.Read(Q(), Today, "30d", out preset, out problem);
            Assert.AreEqual("30d", preset);
            Assert.IsNull(problem);
            Assert.AreEqual(new DateTime(2026, 9, 16), p.From);
            Assert.AreEqual(Today, p.To);
        }

        [TestMethod]
        public void Explicit_dates_win_and_pick_a_sensible_bucket_size()
        {
            string preset, problem;
            ReportPeriod p = ReportQuery.Read(Q("from", "2026-01-01", "to", "2026-06-30"), Today, "30d", out preset, out problem);
            Assert.AreEqual(ReportQuery.CustomPreset, preset);
            Assert.IsNull(problem);
            Assert.AreEqual(Granularity.Week, p.Granularity, "181 days with no group chosen -> weekly");
        }

        [TestMethod]
        public void Garbage_dates_fall_back_with_a_message_instead_of_throwing()
        {
            string preset, problem;
            ReportPeriod p = ReportQuery.Read(Q("from", "<script>", "to", "2026-13-45"), Today, "30d", out preset, out problem);
            Assert.AreEqual("30d", preset);
            Assert.IsNotNull(problem);
            Assert.AreEqual(Today, p.To);
        }

        [TestMethod]
        public void An_out_of_range_custom_period_is_rejected_with_its_reason()
        {
            string preset, problem;
            ReportQuery.Read(Q("from", "2020-01-01", "to", "2026-01-01", "group", "day"), Today, "7d", out preset, out problem);
            StringAssert.Contains(problem, "Daily view covers at most");
            Assert.AreEqual("7d", preset);
        }

        [TestMethod]
        public void A_preset_can_be_regrouped_but_not_into_an_invalid_size()
        {
            string preset, problem;
            ReportPeriod monthly = ReportQuery.Read(Q("preset", "90d", "group", "month"), Today, "30d", out preset, out problem);
            Assert.AreEqual(Granularity.Month, monthly.Granularity);

            ReportPeriod yearDaily = ReportQuery.Read(Q("preset", "12m", "group", "day"), Today, "30d", out preset, out problem);
            Assert.AreEqual(Granularity.Month, yearDaily.Granularity, "a year by day exceeds the cap, so the preset's own size stays");
        }

        [TestMethod]
        public void Unknown_preset_and_group_values_are_ignored()
        {
            string preset, problem;
            ReportPeriod p = ReportQuery.Read(Q("preset", "'; DROP TABLE x;--", "group", "fortnight"), Today, "30d", out preset, out problem);
            Assert.AreEqual("30d", preset);
            Assert.AreEqual(Granularity.Day, p.Granularity);
        }

        [TestMethod]
        public void ToQuery_round_trips_through_Read()
        {
            var original = new ReportPeriod(new DateTime(2026, 3, 2), new DateTime(2026, 5, 31), Granularity.Week);
            string qs = ReportQuery.ToQuery(original, ReportQuery.CustomPreset);
            Assert.AreEqual("?from=2026-03-02&to=2026-05-31&group=week", qs);

            var parsed = System.Web.HttpUtility.ParseQueryString(qs.Substring(1));
            string preset, problem;
            ReportPeriod back = ReportQuery.Read(parsed, Today, "30d", out preset, out problem);
            Assert.AreEqual(original.From, back.From);
            Assert.AreEqual(original.To, back.To);
            Assert.AreEqual(original.Granularity, back.Granularity);

            Assert.AreEqual("?preset=12m&group=month", ReportQuery.ToQuery(ReportPeriod.Preset("12m", Today), "12m"));
        }

        [TestMethod]
        public void Rates_are_compared_in_percentage_points()
        {
            Assert.AreEqual("down 4.0 points vs previous period", Delta.DescribePoints(88m, 92m));
            Assert.AreEqual("up 2.5 points vs previous period", Delta.DescribePoints(52.5m, 50m));
            Assert.AreEqual("no previous period to compare", Delta.DescribePoints(90m, null));
            Assert.AreEqual("nothing to measure this period", Delta.DescribePoints(null, 90m));
            Assert.AreEqual("no change vs previous period", Delta.DescribePoints(90m, 90.04m));
        }

        [TestMethod]
        public void Editing_the_dates_makes_a_custom_range_even_without_javascript()
        {
            // Quick range still says "30d", but the user changed the From box.
            string qs = ReportQuery.FromForm("30d", "2026-08-01", "2026-08-31", "auto", "2026-09-16", "2026-10-15");
            Assert.AreEqual("?from=2026-08-01&to=2026-08-31", qs);

            string untouched = ReportQuery.FromForm("30d", "2026-09-16", "2026-10-15", "week", "2026-09-16", "2026-10-15");
            Assert.AreEqual("?preset=30d&group=week", untouched, "unchanged dates keep the preset; explicit group kept");
        }

        [TestMethod]
        public void Unreadable_form_dates_are_passed_on_encoded_for_the_next_page_to_explain()
        {
            string qs = ReportQuery.FromForm("custom", "<b>x</b>", "2026-02-30", "auto", "", "");
            Assert.AreEqual("?from=%3cb%3ex%3c%2fb%3e&to=2026-02-30", qs);

            string preset, problem;
            ReportQuery.Read(System.Web.HttpUtility.ParseQueryString(qs.Substring(1)), Today, "30d", out preset, out problem);
            StringAssert.Contains(problem, "real calendar dates");
        }

        [TestMethod]
        public void Automatic_grouping_follows_the_range_and_shows_as_automatic()
        {
            Assert.AreEqual(ReportQuery.AutoGroup, ReportQuery.GroupSelection(Q("preset", "12m")));
            Assert.AreEqual("week", ReportQuery.GroupSelection(Q("group", "WEEK")), "group matching is case-insensitive");

            string preset, problem;
            ReportPeriod p = ReportQuery.Read(Q("preset", "30d", "group", "auto"), Today, "7d", out preset, out problem);
            Assert.AreEqual(Granularity.Day, p.Granularity);
            Assert.IsNull(problem);
        }

        [TestMethod]
        public void An_impossible_regroup_is_explained_not_silently_ignored()
        {
            string preset, problem;
            ReportPeriod p = ReportQuery.Read(Q("preset", "12m", "group", "day"), Today, "30d", out preset, out problem);
            Assert.AreEqual(Granularity.Month, p.Granularity);
            Assert.AreEqual("Grouping by day is not available for last 12 months, so it is shown by month.", problem);
        }

        [TestMethod]
        public void Custom_with_no_dates_and_half_a_range_are_explained()
        {
            string preset, problem;
            ReportQuery.Read(Q("preset", "custom"), Today, "30d", out preset, out problem);
            Assert.AreEqual("Choose both a From and a To date for a custom range.", problem);

            ReportQuery.Read(Q("from", "2026-01-01"), Today, "30d", out preset, out problem);
            Assert.IsNotNull(problem, "a From date with no To date cannot make a period");
            Assert.AreEqual("30d", preset);
        }

        [TestMethod]
        public void A_null_query_is_just_the_default()
        {
            string preset, problem;
            ReportPeriod p = ReportQuery.Read(null, Today, "7d", out preset, out problem);
            Assert.AreEqual("7d", preset);
            Assert.IsNull(problem);
            Assert.AreEqual(7, p.Days);
        }

        [TestMethod]
        public void Range_caps_hold_at_their_exact_boundaries()
        {
            var start = new DateTime(2026, 1, 1);
            Assert.IsNull(ReportPeriod.Validate(start, start.AddDays(92), Granularity.Day, Today), "93 days by day is allowed");
            Assert.IsNotNull(ReportPeriod.Validate(start, start.AddDays(93), Granularity.Day, Today), "94 days by day is not");
            Assert.IsNull(ReportPeriod.Validate(new DateTime(2024, 1, 1), new DateTime(2024, 1, 1).AddDays(741), Granularity.Week, Today));
            Assert.IsNotNull(ReportPeriod.Validate(new DateTime(2024, 1, 1), new DateTime(2024, 1, 1).AddDays(742), Granularity.Week, Today));
        }

        [TestMethod]
        public void A_period_ending_more_than_a_year_ahead_is_refused()
        {
            Assert.IsNull(ReportPeriod.Validate(Today, Today.AddYears(1), Granularity.Month, Today));
            Assert.IsNotNull(ReportPeriod.Validate(Today, Today.AddYears(1).AddDays(1), Granularity.Month, Today));
        }

        [TestMethod]
        public void A_falling_success_rate_is_called_unfavourable()
        {
            string html = ReportHtml.KpiRate("x", "Delivery success", 88m, 92m, true);
            StringAssert.Contains(html, "88.0%");
            StringAssert.Contains(html, "down 4.0 points vs previous period (unfavourable)");
        }
    }
}
