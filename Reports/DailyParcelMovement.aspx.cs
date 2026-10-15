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
    /// Parcel Movement Report - for a period: how many parcels were booked, sorted, dispatched
    /// and delivered, how many delivery attempts failed, how that compares with the previous
    /// period, how far this period's bookings have got (a pipeline schematic), where the work
    /// happened, which parcels are stuck, and every tracking event.
    ///
    /// Booked, delivered and failed attempts come from AnalyticsService, so they match every
    /// other report. Sorted and dispatched count distinct PARCELS from the tracking log (a parcel
    /// is assigned, in transit and out for delivery on its way, but is dispatched once).
    ///
    /// Admin and office staff see the whole company. A driver (Staff with a dbo.[Driver] row)
    /// sees only the events credited to them - the driver on the event, or the parcel's driver
    /// when the event names none - never a colleague's attempt, name or remarks.
    /// </summary>
    public partial class DailyParcelMovement : PeriodReportPage
    {
        /// <summary>0 for the company view, otherwise the driver whose movements are shown.</summary>
        private int driverScope;

        protected override string PageUrl
        {
            get { return "~/Reports/DailyParcelMovement.aspx"; }
        }

        protected override string DefaultPreset
        {
            get { return "7d"; }
        }

        protected override void Authorize()
        {
            RequireRole("Admin", "Staff");
            if (string.Equals(Role, "Staff", StringComparison.OrdinalIgnoreCase) && IsDriver(UserId)) driverScope = UserId;
        }

        private AnalyticsScope Scope
        {
            get { return driverScope > 0 ? AnalyticsScope.ForDriver(driverScope) : AnalyticsScope.Company(); }
        }

        // Every query below shares this filter. RECOMPILE: the company and driver cases need different plans.
        private const string EventFilter =
            " FROM dbo.[TrackingHistory] th JOIN dbo.[Shipment] s ON s.ShipmentID = th.ShipmentID " +
            " WHERE th.[Timestamp] >= @from AND th.[Timestamp] < @to " +
            "   AND (@drv = 0 OR ISNULL(th.DriverID, s.DriverID) = @drv) ";

        // Mirrors MovementStages.Of.
        private const string StageCase =
            "CASE WHEN th.Status = 'Registered' THEN 0 WHEN th.Status = 'Sorted' THEN 1 " +
            "     WHEN th.Status IN ('Assigned', 'InTransit', 'OutForDelivery') THEN 2 " +
            "     WHEN th.Status = 'Delivered' THEN 3 WHEN th.Status IN ('Failed', 'Requeued') THEN 4 ELSE -1 END";

        private const string StagesSql =
            "SELECT x.Stage, COUNT(DISTINCT x.ShipmentID) AS Parcels, COUNT(*) AS Events FROM (" +
            "  SELECT " + StageCase + " AS Stage, th.ShipmentID" + EventFilter +
            ") x GROUP BY x.Stage OPTION (RECOMPILE)";

        private const string ParcelsSql =
            "SELECT COUNT(DISTINCT th.ShipmentID)" + EventFilter + "OPTION (RECOMPILE)";

        private const string LocationsSql =
            "SELECT TOP (8) ISNULL(NULLIF(LTRIM(RTRIM(th.Location)), ''), 'Not recorded') AS L, COUNT(*) AS N" + EventFilter +
            "GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(th.Location)), ''), 'Not recorded') ORDER BY N DESC, L OPTION (RECOMPILE)";

        // Newest first; the screen asks for one row more than it shows, the CSV for all of them.
        private const string EventsSql =
            "SELECT TOP (@n) s.TrackingNumber AS Waybill, th.Status, th.Location, s.OriginCity + ' -> ' + s.DestCity AS Route, " +
            "       u.FullName AS Driver, th.[Timestamp] AS EventTime, th.Remarks " +
            " FROM dbo.[TrackingHistory] th JOIN dbo.[Shipment] s ON s.ShipmentID = th.ShipmentID " +
            " LEFT JOIN dbo.[User] u ON u.UserID = ISNULL(th.DriverID, s.DriverID) " +
            " WHERE th.[Timestamp] >= @from AND th.[Timestamp] < @to " +
            "   AND (@drv = 0 OR ISNULL(th.DriverID, s.DriverID) = @drv) " +
            "ORDER BY th.[Timestamp] DESC OPTION (RECOMPILE)";

        private DataTable LoadEvents(int top)
        {
            return Db.Query(EventsSql, "@n", top, "@from", Period.From, "@to", Period.ToExclusive, "@drv", driverScope);
        }

        /// <summary>Distinct parcels and events per movement stage.</summary>
        private void Stages(DateTime from, DateTime toExclusive, out int[] parcels, out int events)
        {
            parcels = new int[5];
            events = 0;
            foreach (DataRow r in Db.Query(StagesSql, "@from", from, "@to", toExclusive, "@drv", driverScope).Rows)
            {
                int stage = Db.ToInt(r["Stage"]);
                if (stage >= 0) parcels[stage] = Db.ToInt(r["Parcels"]);
                events += Db.ToInt(r["Events"]);
            }
        }

        private string[] Headers
        {
            // A driver's own report has no Driver column - every row is theirs.
            get
            {
                return driverScope > 0
                    ? new[] { "Waybill", "Event", "Time", "Location", "Route", "Remarks" }
                    : new[] { "Waybill", "Event", "Time", "Location", "Route", "Driver", "Remarks" };
            }
        }

        private string[] Cells(DataRow r, string timeFormat)
        {
            var cells = new List<string>
            {
                Db.ToStr(r["Waybill"]), MovementStages.EventLabel(Db.ToStr(r["Status"])),
                Convert.ToDateTime(r["EventTime"], CultureInfo.InvariantCulture).ToString(timeFormat, CultureInfo.InvariantCulture),
                Db.ToStr(r["Location"]), Db.ToStr(r["Route"])
            };
            if (driverScope == 0) cells.Add(Db.ToStr(r["Driver"]));
            cells.Add(Db.ToStr(r["Remarks"]));
            return cells.ToArray();
        }

        protected override DataTable ExportTable(out string fileName)
        {
            fileName = "ParcelMovement_" + ReportQuery.IsoDate(Period.From) + "_to_" + ReportQuery.IsoDate(Period.To) + ".csv";
            var dt = new DataTable();
            foreach (string c in Headers) dt.Columns.Add(c, typeof(string));
            foreach (DataRow r in LoadEvents(int.MaxValue).Rows) dt.Rows.Add(Cells(r, "yyyy-MM-dd HH:mm"));
            return dt;
        }

        protected override string RenderBody()
        {
            AnalyticsScope scope = Scope;
            ReportPeriod previous = Period.Previous();
            PerformanceSnapshot s = AnalyticsService.Build(scope, Period);
            int[] now, before;
            int events, eventsBefore;
            Stages(Period.From, Period.ToExclusive, out now, out events);
            Stages(previous.From, previous.ToExclusive, out before, out eventsBefore);
            int parcels = Db.ToInt(Db.Scalar(ParcelsSql, "@from", Period.From, "@to", Period.ToExclusive, "@drv", driverScope));
            int parcelsBefore = Db.ToInt(Db.Scalar(ParcelsSql, "@from", previous.From, "@to", previous.ToExclusive, "@drv", driverScope));
            bool compare = eventsBefore > 0 || s.Previous.Booked + s.Previous.Delivered + s.Previous.Failed > 0m;

            rptHeader.Description = driverScope > 0
                ? "Your parcel movements in the chosen period: what you dispatched and delivered, your failed attempts, and every event credited to you."
                : "Parcel movements in the chosen period: what was booked, sorted, dispatched and delivered, which delivery attempts failed, and how far this period's bookings have got.";
            AddPeriodFilters(compare);
            rptHeader.AddFilter("Scope", driverScope > 0 ? "Your parcels and attempts only" : "Whole company");

            var html = new StringBuilder();

            // ---- Key figures
            html.Append(ReportHtml.SectionTitle("insights", "Key figures"));
            html.Append("<div class='lc-kpis'>");
            html.Append(Figure("local_shipping", "Parcels moved", parcels, parcelsBefore, true, compare,
                ReportHtml.Count(events) + " tracking events"));
            html.Append(Figure("add_box", "Shipments booked", s.Current.Booked, s.Previous.Booked, true, compare, null));
            html.Append(Figure("call_split", "Parcels sorted", now[MovementStages.Sorted], before[MovementStages.Sorted], true, compare, null));
            html.Append(Figure("route", "Parcels dispatched", now[MovementStages.Dispatched], before[MovementStages.Dispatched], true, compare,
                "Assigned, in transit or out for delivery (each parcel once)"));
            html.Append(Figure("task_alt", "Deliveries", s.Current.Delivered, s.Previous.Delivered, true, compare, null));
            html.Append(Figure("cancel", "Failed delivery attempts", s.Current.Failed, s.Previous.Failed, false, compare, "Including re-queued attempts"));
            html.Append(compare
                ? ReportHtml.KpiRate("verified", "Delivery attempt success", s.Current.SuccessRate, s.Previous.SuccessRate, true,
                    "Target " + Target())
                : ReportHtml.Stat("verified", "Delivery attempt success", Delta.FormatRate(s.Current.SuccessRate), "Target " + Target()));
            html.Append("</div>");

            // ---- Charts
            string bucket = Period.Granularity == Granularity.Week ? "week" : Period.Granularity == Granularity.Month ? "month" : "day";
            html.Append(ReportHtml.SectionTitle("show_chart", "Trends and flow"));
            html.Append("<div class='lc-chart-grid'>");

            var trendRows = new List<string[]>();
            for (int i = 0; i < s.Labels.Length; i++)
            {
                trendRows.Add(new[] { s.Labels[i], ReportHtml.Count(s.Booked[i]), ReportHtml.Count(s.Delivered[i]), ReportHtml.Count(s.Failed[i]) });
            }
            html.Append(ReportHtml.ChartCard("chMovement", "Booked, delivered and failed attempts per " + bucket,
                ReportHtml.Count(s.Current.Booked) + " booked, " + ReportHtml.Count(s.Current.Delivered) + " delivered and " +
                ReportHtml.Count(s.Current.Failed) + " failed delivery attempts in the period.",
                "line", "number",
                new
                {
                    labels = s.Labels,
                    series = new object[]
                    {
                        new { label = "Booked", data = s.Booked, tone = "primary" },
                        new { label = "Delivered", data = s.Delivered, tone = "success" },
                        new { label = "Failed attempts", data = s.Failed, tone = "danger", dashed = true }
                    }
                },
                new[] { Capital(bucket), "Booked", "Delivered", "Failed attempts" }, trendRows,
                s.Current.Booked + s.Current.Delivered + s.Current.Failed == 0m, "lc-span-2"));

            html.Append("<div class='lc-span-2'>")
                .Append(ReportHtml.Pipeline(s.Pipeline, s.Cohort,
                    "How far the " + ReportHtml.Count(s.Cohort.Total) + " shipments booked in this period have got. " +
                    "Each stage counts parcels that reached it or went further."))
                .Append("</div>");

            var whereNow = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, int> kv in s.Cohort.ByStatus)
            {
                string label = string.Equals(kv.Key, "Delivered", StringComparison.OrdinalIgnoreCase) ? "Delivered"
                             : string.Equals(kv.Key, "Failed", StringComparison.OrdinalIgnoreCase) ? "Delivery failed"
                             : PerformanceInsights.StatusLabel(kv.Key);
                decimal n;
                whereNow.TryGetValue(label, out n);
                whereNow[label] = n + kv.Value;
            }
            html.Append(ReportHtml.CategoryDonut("chWhereNow", "Where this period's bookings are now",
                "The current status of the " + ReportHtml.Count(s.Cohort.Total) + " shipments booked in the period.", whereNow, "Status"));

            var locations = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in Db.Query(LocationsSql, "@from", Period.From, "@to", Period.ToExclusive, "@drv", driverScope).Rows)
            {
                locations[Db.ToStr(r["L"])] = Db.ToInt(r["N"]);
            }
            html.Append(ReportHtml.CategoryBars("chLocations", "Busiest locations",
                "Where the most tracking events were recorded.", locations, "Location", 8));
            html.Append("</div>");

            // ---- Needs attention
            html.Append(ReportHtml.SectionTitle("notification_important", "Needs attention"));
            html.Append(ReportHtml.Attention(Attention(s, compare),
                "Nothing needs attention: no parcels are stuck and delivery success is on target.", Href));

            // ---- Detail
            html.Append(ReportHtml.SectionTitle("table_rows", "All movements (newest first)"));
            var detail = new List<string[]>();
            foreach (DataRow r in LoadEvents(MaxDetailRows + 1).Rows) detail.Add(Cells(r, "dd MMM yyyy HH:mm"));
            html.Append(DetailTable(Headers, detail, "Parcel movements in the period", "No parcel movements were recorded in this period.",
                false, events));
            return html.ToString();
        }

        private List<AttentionItem> Attention(PerformanceSnapshot s, bool compare)
        {
            var items = new List<AttentionItem>();
            if (driverScope == 0)
            {
                // Point-in-time, whatever the period: these parcels are stuck right now.
                bool admin = string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);
                string asAt = DateTime.Now.ToString("dd MMM HH:mm", CultureInfo.InvariantCulture);
                foreach (StuckStage st in AnalyticsService.StuckParcels(AnalyticsScope.Company(), DateTime.Now, PerformanceInsights.StuckDays))
                {
                    items.Add(new AttentionItem(
                        st.OldestDays >= 2 * PerformanceInsights.StuckDays ? AttentionSeverity.High : AttentionSeverity.Medium,
                        "As at " + asAt + ", " + PerformanceInsights.Plural(st.Count, "parcel") + " " + (st.Count == 1 ? "has" : "have") +
                        " been at \"" + PerformanceInsights.StatusLabel(st.Status) + "\" for more than " + PerformanceInsights.StuckDays +
                        " days (oldest " + PerformanceInsights.Plural(st.OldestDays, "day") + ").",
                        admin ? "~/Admin/Shipments.aspx" : "~/Ops/Warehouse.aspx", admin ? "Open shipments" : "Open warehouse"));
                }
            }
            decimal attempts = s.Current.Delivered + s.Current.Failed;
            if (compare && s.Current.Failed >= PerformanceInsights.MinAttempts && s.Current.Failed > s.Previous.Failed)
            {
                items.Add(new AttentionItem(AttentionSeverity.Medium,
                    "Failed delivery attempts rose from " + ReportHtml.Count(s.Previous.Failed) + " to " + ReportHtml.Count(s.Current.Failed) +
                    " against the previous period."));
            }
            if (attempts >= PerformanceInsights.MinAttempts && s.Current.SuccessRate.Value < PerformanceInsights.SuccessTarget)
            {
                items.Add(new AttentionItem(AttentionSeverity.Medium,
                    "Delivery attempt success is " + Delta.FormatRate(s.Current.SuccessRate) + ", below the " + Target() + " target."));
            }
            return items;
        }

        private static string Figure(string icon, string label, decimal current, decimal previous, bool higherIsBetter, bool compare, string hint)
        {
            return compare
                ? ReportHtml.Kpi(icon, label, ReportHtml.Count(current), current, previous, higherIsBetter, hint)
                : ReportHtml.Stat(icon, label, ReportHtml.Count(current), hint);
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
