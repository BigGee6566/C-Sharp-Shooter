using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Tests.Unit
{
    /// <summary>
    /// The shared reporting rules, tested without a database: period and bucket maths, the
    /// period-over-period wording, the delivery-pipeline funnel, and the two output encoders
    /// that keep chart data and the print footer from breaking out of their HTML context.
    /// </summary>
    [TestClass]
    public class ReportFoundationTests
    {
        // 2026-10-14 is a Wednesday; 2026-10-18 a Sunday; 2026-10-12 the Monday of that week.
        [DataTestMethod]
        [DataRow("2026-10-14", "2026-10-12")]
        [DataRow("2026-10-18", "2026-10-12")]   // Sunday belongs to the week that started Monday
        [DataRow("2026-10-12", "2026-10-12")]
        [DataRow("2027-01-01", "2026-12-28")]   // week crosses the year boundary
        public void Week_buckets_start_on_monday(string day, string expectedMonday)
        {
            Assert.AreEqual(DateTime.Parse(expectedMonday),
                ReportPeriod.BucketStart(DateTime.Parse(day), Granularity.Week));
        }

        [TestMethod]
        public void Month_bucket_is_the_first_of_the_month_and_time_is_ignored()
        {
            Assert.AreEqual(new DateTime(2026, 2, 1),
                ReportPeriod.BucketStart(new DateTime(2026, 2, 28, 23, 59, 0), Granularity.Month));
        }

        [TestMethod]
        public void Buckets_cover_the_range_including_empty_ones()
        {
            var daily = new ReportPeriod(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), Granularity.Day);
            Assert.AreEqual(31, daily.Buckets().Count);

            var monthly = new ReportPeriod(new DateTime(2025, 11, 15), new DateTime(2026, 2, 3), Granularity.Month);
            CollectionAssert.AreEqual(
                new[] { new DateTime(2025, 11, 1), new DateTime(2025, 12, 1), new DateTime(2026, 1, 1), new DateTime(2026, 2, 1) },
                new List<DateTime>(monthly.Buckets()));
        }

        [TestMethod]
        public void Range_is_inclusive_and_a_reversed_range_is_corrected()
        {
            var p = new ReportPeriod(new DateTime(2026, 3, 31), new DateTime(2026, 3, 1), Granularity.Day);
            Assert.AreEqual(new DateTime(2026, 3, 1), p.From);
            Assert.AreEqual(new DateTime(2026, 3, 31), p.To);
            Assert.AreEqual(31, p.Days);
            Assert.AreEqual(new DateTime(2026, 4, 1), p.ToExclusive, "SQL upper bound is midnight after the inclusive end");
        }

        [TestMethod]
        public void Previous_period_is_the_same_length_immediately_before()
        {
            var p = new ReportPeriod(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31), Granularity.Day);
            ReportPeriod prev = p.Previous();
            Assert.AreEqual(new DateTime(2026, 1, 29), prev.From);
            Assert.AreEqual(new DateTime(2026, 2, 28), prev.To);
            Assert.AreEqual(p.Days, prev.Days);
        }

        [TestMethod]
        public void Rollup_sums_days_into_buckets_and_ignores_days_outside_the_range()
        {
            var week = new ReportPeriod(new DateTime(2026, 10, 12), new DateTime(2026, 10, 25), Granularity.Week);
            var daily = new Dictionary<DateTime, decimal>
            {
                { new DateTime(2026, 10, 11), 99m },   // the day before - must not count
                { new DateTime(2026, 10, 12), 2m },
                { new DateTime(2026, 10, 18), 3m },    // Sunday, first week
                { new DateTime(2026, 10, 19), 4m },    // Monday, second week
                { new DateTime(2026, 10, 26), 99m }    // the day after - must not count
            };
            CollectionAssert.AreEqual(new[] { 5m, 4m }, week.Rollup(daily));
            Assert.AreEqual(9m, week.Total(daily));
        }

        [TestMethod]
        public void Validate_caps_daily_ranges_and_accepts_sane_ones()
        {
            Assert.IsNull(ReportPeriod.Validate(new DateTime(2026, 1, 1), new DateTime(2026, 3, 31), Granularity.Day));
            Assert.IsNotNull(ReportPeriod.Validate(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), Granularity.Day));
            Assert.IsNull(ReportPeriod.Validate(new DateTime(2025, 1, 1), new DateTime(2026, 12, 31), Granularity.Month));
            Assert.IsNotNull(ReportPeriod.Validate(new DateTime(1999, 1, 1), new DateTime(1999, 1, 2), Granularity.Day));
        }

        [TestMethod]
        public void Presets_produce_the_expected_ranges()
        {
            var today = new DateTime(2026, 10, 15);
            ReportPeriod last7 = ReportPeriod.Preset("7d", today);
            Assert.AreEqual(new DateTime(2026, 10, 9), last7.From);
            Assert.AreEqual(today, last7.To);

            ReportPeriod lastMonth = ReportPeriod.Preset("lastmonth", today);
            Assert.AreEqual(new DateTime(2026, 9, 1), lastMonth.From);
            Assert.AreEqual(new DateTime(2026, 9, 30), lastMonth.To);

            ReportPeriod twelve = ReportPeriod.Preset("12m", today);
            Assert.AreEqual(new DateTime(2025, 11, 1), twelve.From);
            Assert.AreEqual(Granularity.Month, twelve.Granularity);
            Assert.AreEqual(12, twelve.Buckets().Count);
        }

        [TestMethod]
        public void Delta_wording_handles_zero_previous_and_no_change()
        {
            Assert.AreEqual("up 25.0% vs previous period", Delta.Describe(125m, 100m));
            Assert.AreEqual("down 50.0% vs previous period", Delta.Describe(50m, 100m));
            Assert.AreEqual("new this period (none previously)", Delta.Describe(7m, 0m));
            Assert.AreEqual("no change vs previous period", Delta.Describe(0m, 0m));
            Assert.IsNull(Delta.PercentChange(5m, 0m));
        }

        [TestMethod]
        public void Rate_is_na_when_the_denominator_is_zero()
        {
            Assert.AreEqual("n/a", Delta.FormatRate(Delta.Rate(0m, 0m)));
            Assert.AreEqual("50.0%", Delta.FormatRate(Delta.Rate(1m, 2m)));
            Assert.AreEqual("70.6%", Delta.FormatRate(Delta.Rate(300m, 425m)));
        }

        [TestMethod]
        public void Rounding_is_half_up_so_tables_match_chart_tooltips()
        {
            Assert.AreEqual("6.3%", Delta.FormatRate(Delta.Rate(1m, 16m)), "6.25 rounds up, not to even");
            Assert.AreEqual("up 6.3% vs previous period", Delta.Describe(17m, 16m));
        }

        [TestMethod]
        public void A_change_that_rounds_to_zero_is_described_as_about_the_same()
        {
            Assert.AreEqual("about the same as previous period (under 0.1% change)", Delta.Describe(100001m, 100000m));
        }

        [TestMethod]
        public void Chart_json_cannot_break_out_of_its_script_element()
        {
            string json = ChartJson.Serialize(new { name = "</script><script>alert(1)</script> & co\u2028" });
            Assert.IsFalse(json.Contains("<"), "no raw < may survive");
            Assert.IsFalse(json.Contains(">"), "no raw > may survive");
            Assert.IsFalse(json.Contains("&"), "no raw & may survive");
            Assert.IsFalse(json.Contains("\u2028"), "a raw line separator would end a JS string literal");
            StringAssert.Contains(json, "\\u003c/script\\u003e");

            string block = ChartJson.ScriptBlock("x\"y", new { a = 1 });
            StringAssert.StartsWith(block, "<script type=\"application/json\" id=\"x&quot;y\">");
        }

        [TestMethod]
        public void Css_string_encoding_neutralises_quotes_backslashes_and_tags()
        {
            string enc = UiHelper.CssStringEnc("O\"Brien\\ </style><b>\nx");
            Assert.IsFalse(enc.Contains("\""), "a raw quote would end the CSS string");
            Assert.IsFalse(enc.Contains("<"), "a raw < could start </style>");
            Assert.IsFalse(enc.Contains("\n"));
            StringAssert.StartsWith(enc, "O\\000022 Brien\\00005C ");
            Assert.AreEqual("Generated by Ann (Administrator) on 2026/10/15 10:00",
                UiHelper.CssStringEnc("Generated by Ann (Administrator) on 2026/10/15 10:00"));
        }

        [TestMethod]
        public void Css_string_encoding_escapes_an_emoji_as_one_code_point()
        {
            string name = "a" + char.ConvertFromUtf32(0x1F600) + "b";
            Assert.AreEqual("a\\01F600 b", UiHelper.CssStringEnc(name), "not as two surrogate halves");
        }

        [TestMethod]
        public void Company_only_figures_refuse_a_customer_or_driver_scope()
        {
            // These throw before any SQL runs, so no database is needed.
            Assert.ThrowsException<UnauthorizedAccessException>(() =>
                AnalyticsService.DriverStandings(AnalyticsScope.ForCustomer(5), DateTime.Today, DateTime.Today.AddDays(1)));
            Assert.ThrowsException<UnauthorizedAccessException>(() =>
                AnalyticsService.Counts(AnalyticsScope.ForDriver(7)));
            Assert.ThrowsException<ArgumentNullException>(() => AnalyticsService.Counts(null));
        }

        [TestMethod]
        public void Scopes_reject_a_missing_identity()
        {
            Assert.ThrowsException<ArgumentException>(() => AnalyticsScope.ForCustomer(0));
            Assert.ThrowsException<ArgumentException>(() => AnalyticsScope.ForDriver(-1));
            Assert.AreEqual(0, (int)ScopeKind.None, "the default enum value must grant nothing");
        }

        [TestMethod]
        public void The_service_refuses_an_out_of_range_period_before_querying()
        {
            var tooLong = new ReportPeriod(new DateTime(2020, 1, 1), new DateTime(2025, 1, 1), Granularity.Day);
            Assert.ThrowsException<ArgumentException>(() => AnalyticsService.Build(AnalyticsScope.Company(), tooLong));
            var ancient = new ReportPeriod(new DateTime(1900, 1, 1), new DateTime(1900, 1, 2), Granularity.Day);
            Assert.ThrowsException<ArgumentException>(() => AnalyticsService.TotalsFor(AnalyticsScope.Company(), ancient));
        }

        [DataTestMethod]
        [DataRow("Registered", 0)]
        [DataRow("Sorted", 1)]
        [DataRow("Assigned", 2)]
        [DataRow("InTransit", 2)]
        [DataRow("OutForDelivery", 3)]
        [DataRow("Failed", 3)]
        [DataRow("Delivered", 4)]
        [DataRow("SomethingNew", 0)]
        [DataRow(null, 0)]
        public void Stage_rank_follows_the_delivery_lifecycle(string status, int expected)
        {
            Assert.AreEqual(expected, AnalyticsService.StageRank(status));
        }

        [TestMethod]
        public void Pipeline_only_narrows_and_flags_the_biggest_drop()
        {
            var byStatus = new Dictionary<string, int>
            {
                { "Registered", 10 }, { "Sorted", 5 }, { "Assigned", 5 },
                { "OutForDelivery", 5 }, { "Failed", 5 }, { "Delivered", 70 }
            };
            IList<PipelineStage> stages = AnalyticsService.BuildPipeline(byStatus);

            CollectionAssert.AreEqual(new[] { 100, 90, 85, 80, 70 },
                new[] { stages[0].Value, stages[1].Value, stages[2].Value, stages[3].Value, stages[4].Value });
            Assert.AreEqual(90.0m, stages[1].ConversionFromPrevious);
            Assert.IsNull(stages[0].ConversionFromPrevious);
            Assert.IsTrue(stages[1].IsBiggestDrop, "Booked -> Sorted loses 10, the most of any step");
            Assert.AreEqual(1, CountFlags(stages));
        }

        [TestMethod]
        public void Empty_pipeline_has_no_drop_and_no_rates()
        {
            IList<PipelineStage> stages = AnalyticsService.BuildPipeline(new Dictionary<string, int>());
            Assert.AreEqual(5, stages.Count);
            Assert.AreEqual(0, CountFlags(stages));
            Assert.IsNull(stages[1].ConversionFromPrevious);
            StringAssert.Contains(ReportHtml.Pipeline(stages, new CohortSummary(), "x"), "No parcels at the previous stage");
        }

        [TestMethod]
        public void Pipeline_html_encodes_its_caption_and_uses_the_cohort_failed_count()
        {
            var cohort = new CohortSummary { Failed = 3 };
            string html = ReportHtml.Pipeline(AnalyticsService.BuildPipeline(null), cohort, "<img src=x onerror=alert(1)>");
            Assert.IsFalse(html.Contains("<img"), "the caption must be encoded");
            StringAssert.Contains(html, "<strong>3</strong>");
        }

        [TestMethod]
        public void Kpi_states_the_verdict_in_words()
        {
            StringAssert.Contains(ReportHtml.Kpi("x", "Failed", "9", 9m, 5m, false), "(unfavourable)",
                "more failures is bad news - that must not be conveyed by red alone");
            StringAssert.Contains(ReportHtml.Kpi("x", "Revenue", "R 9", 9m, 5m, true), "(favourable)");
        }

        [TestMethod]
        public void Chart_card_fallback_table_starts_open_and_names_its_chart()
        {
            string html = ReportHtml.ChartCard("c1", "Deliveries", "summary", "bar", "number",
                new { labels = new[] { "a" } }, new[] { "Col" }, new List<string[]> { new[] { "1" } }, false);
            StringAssert.Contains(html, "<details class='lc-chart-table' open>");
            StringAssert.Contains(html, "for Deliveries");
            StringAssert.Contains(html, "aria-describedby='c1-summary'");
        }

        [TestMethod]
        public void Kpi_with_no_previous_base_is_neutral_not_judged()
        {
            string html = ReportHtml.Kpi("x", "Revenue", "R 9", 9m, 0m, true);
            StringAssert.Contains(html, "new this period (none previously)");
            Assert.IsFalse(html.Contains("favourable"), "there is nothing to compare against, so no verdict");
            StringAssert.Contains(html, "lc-tone-flat");
        }

        [TestMethod]
        public void Table_can_render_its_last_row_as_a_total_footer()
        {
            var rows = new List<string[]> { new[] { "Jan", "1" }, new[] { "Total", "1" } };
            string html = ReportHtml.Table(new[] { "Month", "Booked" }, rows, "Breakdown", true);
            StringAssert.Contains(html, "<tfoot><tr class='lc-total'><th scope='row'>Total</th><td>1</td></tr></tfoot>");
            Assert.AreEqual(1, CountOccurrences(html, "<tbody><tr>"), "only the data row stays in the body");

            string plain = ReportHtml.Table(new[] { "Month", "Booked" }, rows);
            Assert.IsFalse(plain.Contains("<tfoot>"), "the default keeps every row in the body");
        }

        [TestMethod]
        public void Report_numbers_use_one_fixed_format_on_every_machine()
        {
            string nbsp = ((char)0xA0).ToString();
            Assert.AreEqual("1" + nbsp + "234" + nbsp + "567", ReportHtml.Count(1234567m));
            Assert.AreEqual("R" + nbsp + "58" + nbsp + "924.50", ReportHtml.Money(58924.5m),
                "'.' decimal point whatever the Windows locale, and no line break inside an amount");
        }

        [TestMethod]
        public void Durations_switch_from_hours_to_days_after_two_days()
        {
            Assert.AreEqual("2.1 days", ReportHtml.Duration(50m), "50 hours is over two days");
            Assert.AreEqual("36.0 hours", ReportHtml.Duration(36m));
            Assert.AreEqual("n/a", ReportHtml.Duration(null));
        }

        [TestMethod]
        public void A_change_that_rounds_to_zero_gets_no_verdict()
        {
            string html = ReportHtml.Kpi("x", "Booked", "100 001", 100001m, 100000m, true);
            StringAssert.Contains(html, "about the same as previous period (under 0.1% change)");
            Assert.IsFalse(html.Contains("favourable"));
        }

        [TestMethod]
        public void Rate_kpis_without_both_values_or_with_equal_values_are_neutral()
        {
            Assert.IsFalse(ReportHtml.KpiRate("x", "S", 90m, null, true).Contains("favourable"));
            Assert.IsFalse(ReportHtml.KpiRate("x", "S", null, 90m, true).Contains("favourable"));
            StringAssert.Contains(ReportHtml.KpiRate("x", "S", 90m, 90m, true), "no change vs previous period");
            StringAssert.Contains(ReportHtml.KpiRate("x", "Failure rate", 5m, 3m, false), "(unfavourable)");
        }

        [TestMethod]
        public void Table_rows_carry_a_row_header_and_encode_every_cell()
        {
            var rows = new List<string[]> { new[] { "<b>Jan</b>", "<i>1</i>" } };
            string html = ReportHtml.Table(new[] { "Month", "Booked" }, rows, "T", false, true);
            StringAssert.Contains(html, "<tr><th scope='row'>&lt;b&gt;Jan&lt;/b&gt;</th><td>&lt;i&gt;1&lt;/i&gt;</td></tr>");
            StringAssert.Contains(html, "class='report-table lc-table-num'");
        }

        [TestMethod]
        public void A_table_whose_only_row_is_the_total_has_an_empty_body()
        {
            string html = ReportHtml.Table(new[] { "A" }, new List<string[]> { new[] { "Total" } }, "T", true);
            StringAssert.Contains(html, "<tbody></tbody><tfoot><tr class='lc-total'><th scope='row'>Total</th></tr></tfoot>");
        }

        private static int CountOccurrences(string text, string token)
        {
            int n = 0, at = 0;
            while ((at = text.IndexOf(token, at, StringComparison.Ordinal)) >= 0) { n++; at += token.Length; }
            return n;
        }

        private static int CountFlags(IList<PipelineStage> stages)
        {
            int n = 0;
            foreach (PipelineStage s in stages) if (s.IsBiggestDrop) n++;
            return n;
        }
    }
}
