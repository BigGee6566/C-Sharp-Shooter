using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Reports
{
    /// <summary>
    /// Driver Performance Report - for a period: how much each driver delivered, how many of
    /// their delivery attempts failed, their success rate against the target and against the
    /// previous period, their customer rating and what they are carrying right now, with the
    /// drivers who need a conversation listed first.
    ///
    /// Office staff and Admin only (cross-driver data is not for drivers to see about
    /// colleagues). No revenue is shown. Attempts are credited to the driver who made them,
    /// with the same definitions as every other report (AnalyticsService); how a driver is
    /// judged lives in DriverRules (unit-tested).
    /// </summary>
    public partial class DriverPerformance : PeriodReportPage
    {
        protected override string PageUrl
        {
            get { return "~/Reports/DriverPerformance.aspx"; }
        }

        protected override void Authorize()
        {
            RequireOperationsStaff();
        }

        private const string OnTheRoadSql =
            "SELECT s.DriverID, COUNT(*) AS N FROM dbo.[Shipment] s " +
            "WHERE s.DriverID IS NOT NULL AND s.DeliveryStatus IN ('Assigned', 'InTransit', 'OutForDelivery') " +
            "GROUP BY s.DriverID";

        private const string RatingSql =
            "SELECT AVG(CAST(r.RatingScore AS DECIMAL(5,2))) AS A, COUNT(*) AS N FROM dbo.[Rating] r " +
            "WHERE r.CreatedDate >= @f AND r.CreatedDate < @t";

        private sealed class Row
        {
            public DriverStanding Now;
            public DriverStanding Before;
            public int OnTheRoad;
            /// <summary>The driver's name, with their ID added when two drivers share it.</summary>
            public string Name;

            public int Attempts
            {
                get { return Now.Delivered + Now.Failed; }
            }

            public bool Judged
            {
                get { return Attempts >= PerformanceInsights.MinAttempts; }
            }

            public string Standing
            {
                get { return DriverRules.Classify(Now.Delivered, Now.Failed); }
            }

            public string ChangeVsBefore
            {
                get { return Before == null ? "n/a" : DriverRules.ChangeInPoints(Now.Delivered, Now.Failed, Before.Delivered, Before.Failed); }
            }

            public string Rating
            {
                get
                {
                    return Now.AverageRating == null ? "n/a"
                        : Now.AverageRating.Value.ToString("0.0", ReportHtml.Numbers) + " (" + PerformanceInsights.Plural(Now.RatingCount, "rating") + ")";
                }
            }
        }

        /// <summary>Every registered driver: worst standing first, then lowest success, then name.</summary>
        private List<Row> Rows()
        {
            AnalyticsScope scope = AnalyticsScope.Company();
            ReportPeriod previous = Period.Previous();
            var before = new Dictionary<int, DriverStanding>();
            foreach (DriverStanding d in AnalyticsService.DriverStandings(scope, previous.From, previous.ToExclusive)) before[d.DriverId] = d;
            var road = new Dictionary<int, int>();
            foreach (DataRow r in Db.Query(OnTheRoadSql).Rows) road[Db.ToInt(r["DriverID"])] = Db.ToInt(r["N"]);

            var rows = new List<Row>();
            var people = new List<KeyValuePair<int, string>>();
            foreach (DriverStanding d in AnalyticsService.DriverStandings(scope, Period.From, Period.ToExclusive))
            {
                DriverStanding b;
                before.TryGetValue(d.DriverId, out b);
                int n;
                road.TryGetValue(d.DriverId, out n);
                rows.Add(new Row { Now = d, Before = b, OnTheRoad = n });
                people.Add(new KeyValuePair<int, string>(d.DriverId, d.Name));
            }
            Dictionary<int, string> names = DisplayNames.Unique(people);
            foreach (Row r in rows) r.Name = names[r.Now.DriverId];

            rows.Sort((a, b) =>
            {
                int byStanding = DriverRules.Rank(a.Standing).CompareTo(DriverRules.Rank(b.Standing));
                if (byStanding != 0) return byStanding;
                decimal ra = a.Now.SuccessRate ?? 101m, rb = b.Now.SuccessRate ?? 101m;
                int byRate = ra.CompareTo(rb);
                return byRate != 0 ? byRate : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
            return rows;
        }

        protected override DataTable ExportTable(out string fileName)
        {
            fileName = "DriverPerformance_" + ReportQuery.IsoDate(Period.From) + "_to_" + ReportQuery.IsoDate(Period.To) + ".csv";
            var dt = new DataTable();
            foreach (string c in new[] { "Driver", "Delivered (count)", "Failed attempts (count)", "Attempt success (percent)",
                                         "Change vs previous period", "On the road now (count)", "Average rating (1-5)",
                                         "Ratings (count)", "Standing" })
            {
                dt.Columns.Add(c, typeof(string));
            }
            foreach (Row r in Rows())
            {
                dt.Rows.Add(r.Name, r.Now.Delivered.ToString(CultureInfo.InvariantCulture), r.Now.Failed.ToString(CultureInfo.InvariantCulture),
                    r.Now.SuccessRate == null ? string.Empty : r.Now.SuccessRate.Value.ToString("0.0", CultureInfo.InvariantCulture),
                    r.ChangeVsBefore, r.OnTheRoad.ToString(CultureInfo.InvariantCulture),
                    r.Now.AverageRating == null ? string.Empty : r.Now.AverageRating.Value.ToString("0.0", CultureInfo.InvariantCulture),
                    r.Now.RatingCount.ToString(CultureInfo.InvariantCulture), r.Standing);
            }
            return dt;
        }

        protected override string RenderBody()
        {
            AnalyticsScope scope = AnalyticsScope.Company();
            PerformanceSnapshot s = AnalyticsService.Build(scope, Period);
            List<Row> rows = Rows();
            DataTable rating = Db.Query(RatingSql, "@f", Period.From, "@t", Period.ToExclusive);
            decimal? avgRating = rating.Rows.Count == 0 || rating.Rows[0]["A"] == DBNull.Value
                ? (decimal?)null : Math.Round(Db.ToDecimal(rating.Rows[0]["A"]), 1, MidpointRounding.AwayFromZero);
            int ratings = rating.Rows.Count == 0 ? 0 : Db.ToInt(rating.Rows[0]["N"]);

            bool compare = s.Previous.Delivered + s.Previous.Failed > 0m;
            int busy = 0, belowAlert = 0, onTheRoad = 0;
            foreach (Row r in rows)
            {
                if (r.Attempts > 0) busy++;
                if (r.Standing == DriverRules.BelowAlert) belowAlert++;
                onTheRoad += r.OnTheRoad;
            }

            rptHeader.Description = "How each driver performed in the chosen period: deliveries, failed attempts, success against the " +
                                    Target() + " target, customer ratings and what they are carrying now.";
            AddPeriodFilters(compare);
            rptHeader.AddFilter("Scope", "All registered drivers");

            var html = new StringBuilder();
            html.Append(ReportHtml.SectionTitle("insights", "Key figures"));
            html.Append("<div class='lc-kpis'>");
            html.Append(ReportHtml.Stat("badge", "Drivers", ReportHtml.Count(rows.Count),
                PerformanceInsights.Plural(busy, "driver") + " made delivery attempts in the period"));
            html.Append(compare
                ? ReportHtml.Kpi("task_alt", "Deliveries", ReportHtml.Count(s.Current.Delivered), s.Current.Delivered, s.Previous.Delivered, true)
                : ReportHtml.Stat("task_alt", "Deliveries", ReportHtml.Count(s.Current.Delivered)));
            html.Append(compare
                ? ReportHtml.Kpi("cancel", "Failed delivery attempts", ReportHtml.Count(s.Current.Failed), s.Current.Failed, s.Previous.Failed, false,
                    "Including re-queued attempts")
                : ReportHtml.Stat("cancel", "Failed delivery attempts", ReportHtml.Count(s.Current.Failed), "Including re-queued attempts"));
            html.Append(compare
                ? ReportHtml.KpiRate("verified", "Team attempt success", s.Current.SuccessRate, s.Previous.SuccessRate, true, "Target " + Target())
                : ReportHtml.Stat("verified", "Team attempt success", Delta.FormatRate(s.Current.SuccessRate), "Target " + Target()));
            html.Append(ReportHtml.Stat("warning", "Drivers below alert level", ReportHtml.Count(belowAlert),
                "Success under " + PerformanceInsights.DriverAlert.ToString("0", CultureInfo.InvariantCulture) + "% with at least " +
                PerformanceInsights.MinAttempts + " attempts"));
            html.Append(ReportHtml.Stat("star", "Average customer rating",
                avgRating == null ? "n/a" : avgRating.Value.ToString("0.0", ReportHtml.Numbers) + " / 5",
                PerformanceInsights.Plural(ratings, "rating") + " in the period"));
            html.Append(ReportHtml.Stat("local_shipping", "Parcels on the road now", ReportHtml.Count(onTheRoad),
                "Assigned, in transit or out for delivery"));
            html.Append("</div>");

            // ---- Charts
            string bucket = Period.Granularity == Granularity.Week ? "week" : Period.Granularity == Granularity.Month ? "month" : "day";
            html.Append(ReportHtml.SectionTitle("bar_chart", "Charts"));
            html.Append("<div class='lc-chart-grid'>");

            var trendRows = new List<string[]>();
            for (int i = 0; i < s.Labels.Length; i++)
            {
                trendRows.Add(new[] { s.Labels[i], ReportHtml.Count(s.Delivered[i]), ReportHtml.Count(s.Failed[i]) });
            }
            html.Append(ReportHtml.ChartCard("chTeamTrend", "Team deliveries and failed attempts per " + bucket,
                ReportHtml.Count(s.Current.Delivered) + " deliveries and " + ReportHtml.Count(s.Current.Failed) + " failed attempts in the period.",
                "line", "number",
                new
                {
                    labels = s.Labels,
                    series = new object[]
                    {
                        new { label = "Delivered", data = s.Delivered, tone = "success" },
                        new { label = "Failed attempts", data = s.Failed, tone = "danger", dashed = true }
                    }
                },
                new[] { Capital(bucket), "Delivered", "Failed attempts" }, trendRows,
                s.Current.Delivered + s.Current.Failed == 0m, "lc-span-2"));

            var active = new List<Row>();
            foreach (Row r in rows) if (r.Attempts > 0) active.Add(r);
            active.Sort((a, b) => b.Attempts != a.Attempts ? b.Attempts.CompareTo(a.Attempts) : string.CompareOrdinal(a.Name, b.Name));
            if (active.Count > 12) active.RemoveRange(12, active.Count - 12);
            var names = new List<string>();
            var delivered = new List<decimal>();
            var failed = new List<decimal>();
            var volumeRows = new List<string[]>();
            foreach (Row r in active)
            {
                names.Add(r.Name);
                delivered.Add(r.Now.Delivered);
                failed.Add(r.Now.Failed);
                volumeRows.Add(new[] { r.Name, ReportHtml.Count(r.Now.Delivered), ReportHtml.Count(r.Now.Failed) });
            }
            html.Append(ReportHtml.ChartCard("chDriverVolumes", "Deliveries and failed attempts by driver",
                active.Count == 0 ? "No delivery attempts in this period." : "The " + active.Count + " busiest drivers by delivery attempts.",
                "hbar", "number",
                new
                {
                    labels = names,
                    series = new object[]
                    {
                        new { label = "Delivered", data = delivered, tone = "success" },
                        new { label = "Failed attempts", data = failed, tone = "danger" }
                    }
                },
                new[] { "Driver", "Delivered", "Failed attempts" }, volumeRows, active.Count == 0,
                active.Count > 6 ? "lc-tall" : null));

            var rateNames = new List<string>();
            var rates = new List<decimal>();
            var rateRows = new List<string[]>();
            foreach (Row r in rows)
            {
                // rows are already weakest first
                if (!r.Judged) continue;
                rateNames.Add(r.Name);
                rates.Add(r.Now.SuccessRate.Value);
                rateRows.Add(new[] { r.Name, Delta.FormatRate(r.Now.SuccessRate), ReportHtml.Count(r.Attempts), r.Standing });
            }
            html.Append(ReportHtml.ChartCard("chDriverSuccess", "Delivery attempt success by driver",
                rateNames.Count == 0
                    ? "Not enough delivery attempts in this period to compare drivers."
                    : "Weakest first, for drivers with at least " + PerformanceInsights.MinAttempts + " attempts. Target " + Target() +
                      "; below " + PerformanceInsights.DriverAlert.ToString("0", CultureInfo.InvariantCulture) + "% needs attention.",
                "hbar", "percent",
                new { labels = rateNames, series = new object[] { new { label = "Attempt success", data = rates, tone = "primary" } } },
                new[] { "Driver", "Attempt success", "Attempts", "Standing" }, rateRows, rateNames.Count == 0,
                rateNames.Count > 6 ? "lc-tall" : null));

            var share = new Dictionary<string, decimal>();
            decimal byDrivers = 0m;
            foreach (Row r in rows)
            {
                if (r.Now.Delivered == 0) continue;
                share[r.Name] = r.Now.Delivered;
                byDrivers += r.Now.Delivered;
            }
            // Deliveries whose driver is no longer registered, so the slices add up to the KPI.
            if (s.Current.Delivered > byDrivers) share["No registered driver"] = s.Current.Delivered - byDrivers;
            html.Append(ReportHtml.CategoryDonut("chDeliveryShare", "Share of deliveries",
                "Who made the " + ReportHtml.Count(s.Current.Delivered) + " deliveries in the period.", share, "Driver"));
            html.Append("</div>");

            // ---- Needs attention
            html.Append(ReportHtml.SectionTitle("notification_important", "Needs attention"));
            html.Append(ReportHtml.Attention(Attention(rows), "Every driver with enough attempts is on target.", Href));

            // ---- Detail
            html.Append(ReportHtml.SectionTitle("table_rows", "All drivers (weakest first)"));
            var detail = new List<string[]>();
            foreach (Row r in rows)
            {
                detail.Add(new[]
                {
                    r.Name, ReportHtml.Count(r.Now.Delivered), ReportHtml.Count(r.Now.Failed),
                    !r.Judged && r.Attempts > 0 ? "n/a (few attempts)" : Delta.FormatRate(r.Now.SuccessRate),
                    r.ChangeVsBefore, ReportHtml.Count(r.OnTheRoad), r.Rating, r.Standing
                });
            }
            html.Append(DetailTable(new[] { "Driver", "Delivered", "Failed attempts", "Attempt success", "Change vs previous period",
                                            "On the road now", "Average rating", "Standing" },
                detail, "Driver figures for the period, weakest first", "There are no registered drivers.", true));
            return html.ToString();
        }

        /// <summary>Below alert first, then below target and low ratings, then idle drivers - rows are already weakest first.</summary>
        private static List<AttentionItem> Attention(IList<Row> rows)
        {
            var high = new List<AttentionItem>();
            var medium = new List<AttentionItem>();
            foreach (Row r in rows)
            {
                if (r.Standing == DriverRules.BelowAlert)
                {
                    high.Add(new AttentionItem(AttentionSeverity.High,
                        r.Name + " delivered " + Delta.FormatRate(r.Now.SuccessRate) + " of " + PerformanceInsights.Plural(r.Attempts, "attempt") +
                        " - below the " + PerformanceInsights.DriverAlert.ToString("0", CultureInfo.InvariantCulture) + "% alert level."));
                }
                else if (r.Standing == DriverRules.BelowTarget)
                {
                    medium.Add(new AttentionItem(AttentionSeverity.Medium,
                        r.Name + " is at " + Delta.FormatRate(r.Now.SuccessRate) + " attempt success, below the " + Target() + " target."));
                }
                if (DriverRules.RatingNeedsAttention(r.Now.AverageRating, r.Now.RatingCount))
                {
                    medium.Add(new AttentionItem(AttentionSeverity.Medium,
                        r.Name + "'s average customer rating is " + r.Now.AverageRating.Value.ToString("0.0", ReportHtml.Numbers) +
                        " out of 5 across " + PerformanceInsights.Plural(r.Now.RatingCount, "rating") + "."));
                }
            }
            var items = new List<AttentionItem>(high);
            items.AddRange(medium);

            var idle = new List<string>();
            foreach (Row r in rows) if (r.Attempts == 0 && r.OnTheRoad == 0) idle.Add(r.Name);
            if (idle.Count > 0 && idle.Count < rows.Count)
            {
                items.Add(new AttentionItem(AttentionSeverity.Info,
                    PerformanceInsights.Plural(idle.Count, "driver") + " made no delivery attempts and carry nothing now: " +
                    string.Join(", ", idle.GetRange(0, Math.Min(5, idle.Count))) +
                    (idle.Count > 5 ? " and " + (idle.Count - 5) + " more" : string.Empty) + "."));
            }
            return items;
        }

        private static string Target()
        {
            return PerformanceInsights.SuccessTarget.ToString("0", CultureInfo.InvariantCulture) + "%";
        }

        private static string Capital(string s)
        {
            return string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }
    }
}
