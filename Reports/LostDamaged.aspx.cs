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
    /// Lost &amp; Damaged Report - parcels booked in a period that were later flagged lost or
    /// damaged: how many, what share of all bookings that is, when they were booked, which
    /// destinations and drivers have an unusually HIGH RATE (not just the most parcels), and
    /// which lost parcels need a customer call and a claim now.
    ///
    /// Office staff and Admin only: it names drivers, which is not for drivers to see about
    /// colleagues. Exceptions are counted on the parcel's booking date, so the rate compares like
    /// with like. Because a loss is often reported days after booking, a recent period is shown
    /// without a favourable/unfavourable verdict until it has matured (ExceptionRules).
    /// </summary>
    public partial class LostDamaged : PeriodReportPage
    {
        protected override string PageUrl
        {
            get { return "~/Reports/LostDamaged.aspx"; }
        }

        protected override string DefaultPreset
        {
            get { return "90d"; }
        }

        protected override void Authorize()
        {
            RequireOperationsStaff();
        }

        private const string ExceptionsSql =
            "SELECT TOP (@n) s.TrackingNumber AS Waybill, s.ExceptionType, s.OriginCity + ' -> ' + s.DestCity AS Route, " +
            "       s.DeliveryStatus, ISNULL(u.FullName, 'Unassigned') AS Driver, s.ShipmentDate " +
            "FROM dbo.[Shipment] s LEFT JOIN dbo.[User] u ON u.UserID = s.DriverID " +
            "WHERE s.ExceptionType IN ('Lost', 'Damaged') AND s.ShipmentDate >= @from AND s.ShipmentDate < @to " +
            "ORDER BY s.ShipmentDate DESC";

        private const string CountsSql =
            "SELECT s.ExceptionType, COUNT(*) AS N FROM dbo.[Shipment] s " +
            "WHERE s.ExceptionType IN ('Lost', 'Damaged') AND s.ShipmentDate >= @from AND s.ShipmentDate < @to " +
            "GROUP BY s.ExceptionType";

        private const string ExceptionCase = "SUM(CASE WHEN s.ExceptionType IN ('Lost', 'Damaged') THEN 1 ELSE 0 END)";

        private const string ByDestinationSql =
            "SELECT ISNULL(NULLIF(LTRIM(RTRIM(s.DestCity)), ''), 'Unknown') AS Name, COUNT(*) AS Booked, " + ExceptionCase + " AS Ex " +
            "FROM dbo.[Shipment] s WHERE s.ShipmentDate >= @from AND s.ShipmentDate < @to " +
            "GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(s.DestCity)), ''), 'Unknown')";

        private const string ByDriverSql =
            "SELECT s.DriverID, u.FullName, COUNT(*) AS Booked, " + ExceptionCase + " AS Ex " +
            "FROM dbo.[Shipment] s JOIN dbo.[User] u ON u.UserID = s.DriverID " +
            "WHERE s.ShipmentDate >= @from AND s.ShipmentDate < @to GROUP BY s.DriverID, u.FullName";

        private static bool Is(DataRow r, string exception)
        {
            return string.Equals(Db.ToStr(r["ExceptionType"]), exception, StringComparison.OrdinalIgnoreCase);
        }

        private static void Counts(DateTime from, DateTime toExclusive, out decimal lost, out decimal damaged)
        {
            lost = 0m;
            damaged = 0m;
            foreach (DataRow r in Db.Query(CountsSql, "@from", from, "@to", toExclusive).Rows)
            {
                if (Is(r, "Lost")) lost = Db.ToInt(r["N"]);
                else damaged = Db.ToInt(r["N"]);
            }
        }

        private DataTable LoadExceptions(int top)
        {
            return Db.Query(ExceptionsSql, "@n", top, "@from", Period.From, "@to", Period.ToExclusive);
        }

        private List<ExceptionGroup> ByDestination()
        {
            var groups = new List<ExceptionGroup>();
            foreach (DataRow r in Db.Query(ByDestinationSql, "@from", Period.From, "@to", Period.ToExclusive).Rows)
            {
                groups.Add(new ExceptionGroup { Name = Db.ToStr(r["Name"]), Booked = Db.ToInt(r["Booked"]), Exceptions = Db.ToInt(r["Ex"]) });
            }
            return groups;
        }

        private List<ExceptionGroup> ByDriver()
        {
            DataTable dt = Db.Query(ByDriverSql, "@from", Period.From, "@to", Period.ToExclusive);
            var people = new List<KeyValuePair<int, string>>();
            foreach (DataRow r in dt.Rows) people.Add(new KeyValuePair<int, string>(Db.ToInt(r["DriverID"]), Db.ToStr(r["FullName"])));
            Dictionary<int, string> names = DisplayNames.Unique(people);
            var groups = new List<ExceptionGroup>();
            foreach (DataRow r in dt.Rows)
            {
                groups.Add(new ExceptionGroup { Name = names[Db.ToInt(r["DriverID"])], Booked = Db.ToInt(r["Booked"]), Exceptions = Db.ToInt(r["Ex"]) });
            }
            return groups;
        }

        private static string[] Cells(DataRow r, string dateFormat)
        {
            return new[]
            {
                Db.ToStr(r["Waybill"]), Db.ToStr(r["ExceptionType"]), Db.ToStr(r["Route"]),
                PerformanceInsights.StatusLabel(Db.ToStr(r["DeliveryStatus"])), Db.ToStr(r["Driver"]),
                Convert.ToDateTime(r["ShipmentDate"], CultureInfo.InvariantCulture).ToString(dateFormat, CultureInfo.InvariantCulture)
            };
        }

        private static readonly string[] Headers = { "Waybill", "Exception", "Route", "Current status", "Assigned driver", "Booked" };

        protected override DataTable ExportTable(out string fileName)
        {
            fileName = "LostDamaged_" + ReportQuery.IsoDate(Period.From) + "_to_" + ReportQuery.IsoDate(Period.To) + ".csv";
            var dt = new DataTable();
            foreach (string c in Headers) dt.Columns.Add(c, typeof(string));
            foreach (DataRow r in LoadExceptions(int.MaxValue).Rows) dt.Rows.Add(Cells(r, "yyyy-MM-dd"));
            return dt;
        }

        protected override string RenderBody()
        {
            AnalyticsScope scope = AnalyticsScope.Company();
            DateTime today = DateTime.Today;
            ReportPeriod previous = Period.Previous();
            decimal lost, damaged, lostBefore, damagedBefore;
            Counts(Period.From, Period.ToExclusive, out lost, out damaged);
            Counts(previous.From, previous.ToExclusive, out lostBefore, out damagedBefore);
            decimal booked = AnalyticsService.TotalsFor(scope, Period).Booked;
            decimal bookedBefore = AnalyticsService.TotalsFor(scope, previous).Booked;
            decimal? rate = Delta.Rate(lost + damaged, booked);
            decimal? rateBefore = Delta.Rate(lostBefore + damagedBefore, bookedBefore);
            bool mature = ExceptionRules.IsMature(Period.To, today);
            // A recent period is still collecting late reports, so it is not judged against a settled one.
            bool compare = bookedBefore > 0m && mature;

            rptHeader.Description = "Parcels booked in the chosen period that were later reported lost or damaged, how often that happens, " +
                                    "and which destinations and drivers have an unusually high rate.";
            AddPeriodFilters(compare);
            rptHeader.AddFilter("Counted by", "Booking date of the parcel");
            rptHeader.AddFilter("Scope", "Whole company");

            var html = new StringBuilder();
            if (!mature)
            {
                html.Append("<p class='lc-note'>Losses and damage are often reported days after booking. This period includes bookings from the last ")
                    .Append(ExceptionRules.MaturityDays).Append(" days, so its figures may still rise and are not compared with the previous period.</p>");
            }
            html.Append(ReportHtml.SectionTitle("report_problem", "Key figures"));
            html.Append("<div class='lc-kpis'>");
            html.Append(compare
                ? ReportHtml.Kpi("search_off", "Lost parcels", ReportHtml.Count(lost), lost, lostBefore, false)
                : ReportHtml.Stat("search_off", "Lost parcels", ReportHtml.Count(lost)));
            html.Append(compare
                ? ReportHtml.Kpi("broken_image", "Damaged parcels", ReportHtml.Count(damaged), damaged, damagedBefore, false)
                : ReportHtml.Stat("broken_image", "Damaged parcels", ReportHtml.Count(damaged)));
            string rateHint = "Lost or damaged, of " + PerformanceInsights.Plural(booked, "shipment") + " booked";
            html.Append(compare
                ? ReportHtml.KpiRate("percent", "Exception rate", rate, rateBefore, false, rateHint)
                : ReportHtml.Stat("percent", "Exception rate", Delta.FormatRate(rate), rateHint));
            html.Append(ReportHtml.Stat("inventory_2", "Shipments booked", ReportHtml.Count(booked), "The base for the exception rate"));
            html.Append("</div>");

            // ---- Charts
            string bucket = Period.Granularity == Granularity.Week ? "week" : Period.Granularity == Granularity.Month ? "month" : "day";
            html.Append(ReportHtml.SectionTitle("bar_chart", "Charts"));
            html.Append("<div class='lc-chart-grid'>");

            // The trend needs every exception in the period; there are few, so it uses the full list.
            DataTable all = LoadExceptions(int.MaxValue);
            string[] labels = Period.Labels();
            decimal[] lostSeries = Period.Rollup(Daily(all, "ShipmentDate", r => Is(r, "Lost")));
            decimal[] damagedSeries = Period.Rollup(Daily(all, "ShipmentDate", r => Is(r, "Damaged")));
            var trendRows = new List<string[]>();
            for (int i = 0; i < labels.Length; i++)
            {
                trendRows.Add(new[] { labels[i], ReportHtml.Count(lostSeries[i]), ReportHtml.Count(damagedSeries[i]) });
            }
            html.Append(ReportHtml.ChartCard("chExceptionTrend", "Lost and damaged parcels per " + bucket + " of booking",
                ReportHtml.Count(lost) + " lost and " + ReportHtml.Count(damaged) + " damaged among the parcels booked in the period.",
                "bar", "number",
                new
                {
                    labels = labels,
                    series = new object[]
                    {
                        new { label = "Lost", data = lostSeries, tone = "danger" },
                        new { label = "Damaged", data = damagedSeries, tone = "accent" }
                    }
                },
                new[] { Capital(bucket), "Lost", "Damaged" }, trendRows, lost + damaged == 0m, "lc-span-2"));

            var split = new Dictionary<string, decimal>();
            if (lost > 0m) split["Lost"] = lost;
            if (damaged > 0m) split["Damaged"] = damaged;
            html.Append(ReportHtml.CategoryDonut("chExceptionSplit", "Lost versus damaged",
                "How the " + PerformanceInsights.Plural(lost + damaged, "exception") + " in the period split.", split, "Exception"));

            List<ExceptionGroup> destinations = ByDestination();
            List<ExceptionGroup> drivers = ByDriver();
            html.Append(RateChart("chByDestination", "Exception rate by destination", "Destination", destinations));
            html.Append(RateChart("chByDriver", "Exception rate by assigned driver", "Assigned driver", drivers));
            html.Append("</div>");

            // ---- Needs attention
            html.Append(ReportHtml.SectionTitle("notification_important", "Needs attention"));
            html.Append(ReportHtml.Attention(Attention(all, rate, rateBefore, compare, destinations, drivers, today),
                "No parcels booked in this period were lost or damaged.", Href));

            // ---- Detail
            html.Append(ReportHtml.SectionTitle("table_rows", "Lost and damaged parcels (newest booking first)"));
            var detail = new List<string[]>();
            for (int i = 0; i < all.Rows.Count && i <= MaxDetailRows; i++) detail.Add(Cells(all.Rows[i], "dd MMM yyyy"));
            html.Append(DetailTable(Headers, detail, "Lost and damaged parcels", "No lost or damaged parcels among those booked in this period.",
                false, all.Rows.Count));
            return html.ToString();
        }

        /// <summary>The highest exception rates among groups with enough bookings to compare.</summary>
        private static string RateChart(string id, string title, string column, IList<ExceptionGroup> groups)
        {
            List<ExceptionGroup> ranked = ExceptionRules.Ranked(groups);
            if (ranked.Count > 8) ranked.RemoveRange(8, ranked.Count - 8);
            var labels = new List<string>();
            var rates = new List<decimal>();
            var rows = new List<string[]>();
            foreach (ExceptionGroup g in ranked)
            {
                labels.Add(g.Name);
                rates.Add(g.Rate.Value);
                rows.Add(new[] { g.Name, Delta.FormatRate(g.Rate), ReportHtml.Count(g.Exceptions), ReportHtml.Count(g.Booked) });
            }
            return ReportHtml.ChartCard(id, title,
                ranked.Count == 0
                    ? "No " + column.ToLowerInvariant() + " with at least " + ExceptionRules.MinBookings + " bookings had an exception."
                    : "Lost or damaged as a share of bookings, highest first, for those with at least " + ExceptionRules.MinBookings + " bookings.",
                "hbar", "percent",
                new { labels = labels, series = new object[] { new { label = "Exception rate", data = rates, tone = "danger" } } },
                new[] { column, "Exception rate", "Exceptions", "Booked" }, rows, ranked.Count == 0);
        }

        private List<AttentionItem> Attention(DataTable all, decimal? rate, decimal? rateBefore, bool compare,
            IList<ExceptionGroup> destinations, IList<ExceptionGroup> drivers, DateTime today)
        {
            var items = new List<AttentionItem>();
            bool admin = string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);

            // Lost parcels, longest-lost first: each one is a customer to call and a claim to open.
            var lostRows = new List<DataRow>();
            foreach (DataRow r in all.Rows) if (Is(r, "Lost")) lostRows.Add(r);
            lostRows.Reverse();
            for (int i = 0; i < lostRows.Count && i < 5; i++)
            {
                DataRow r = lostRows[i];
                int age = (today - Convert.ToDateTime(r["ShipmentDate"], CultureInfo.InvariantCulture).Date).Days;
                items.Add(new AttentionItem(AttentionSeverity.High,
                    "Parcel " + Db.ToStr(r["Waybill"]) + " (" + Db.ToStr(r["Route"]) + "), booked " + PerformanceInsights.Plural(age, "day") +
                    " ago, is flagged lost (recorded status: \"" + PerformanceInsights.StatusLabel(Db.ToStr(r["DeliveryStatus"])) +
                    "\") - contact the customer and open a claim.",
                    admin ? "~/Admin/Shipments.aspx" : null, admin ? "Open shipments" : null));
            }
            if (lostRows.Count > 5)
            {
                items.Add(new AttentionItem(AttentionSeverity.High,
                    "...and " + PerformanceInsights.Plural(lostRows.Count - 5, "more lost parcel") + " in the table below."));
            }

            if (compare && rate != null && rateBefore != null && rate.Value > rateBefore.Value)
            {
                items.Add(new AttentionItem(AttentionSeverity.Medium,
                    "The exception rate rose from " + Delta.FormatRate(rateBefore) + " to " + Delta.FormatRate(rate) + " against the previous period."));
            }
            foreach (ExceptionGroup g in ExceptionRules.Hotspots(destinations, rate))
            {
                items.Add(new AttentionItem(AttentionSeverity.Medium,
                    "Parcels to " + g.Name + " had a " + Delta.FormatRate(g.Rate) + " exception rate (" + ReportHtml.Count(g.Exceptions) + " of " +
                    ReportHtml.Count(g.Booked) + ") against " + Delta.FormatRate(rate) + " overall - check handling on that route."));
            }
            foreach (ExceptionGroup g in ExceptionRules.Hotspots(drivers, rate))
            {
                items.Add(new AttentionItem(AttentionSeverity.Medium,
                    "Parcels assigned to " + g.Name + " had a " + Delta.FormatRate(g.Rate) + " exception rate (" + ReportHtml.Count(g.Exceptions) +
                    " of " + ReportHtml.Count(g.Booked) + ") against " + Delta.FormatRate(rate) + " overall - worth a conversation."));
            }
            return items;
        }

        private static string Capital(string s)
        {
            return string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }
    }
}
