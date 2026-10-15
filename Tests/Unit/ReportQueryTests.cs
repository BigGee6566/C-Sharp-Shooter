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
        public void A_falling_success_rate_is_called_unfavourable()
        {
            string html = ReportHtml.KpiRate("x", "Delivery success", 88m, 92m, true);
            StringAssert.Contains(html, "88.0%");
            StringAssert.Contains(html, "down 4.0 points vs previous period (unfavourable)");
        }
    }
}
