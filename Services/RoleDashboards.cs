using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.Caching;
using ForteLogisticsApp.Data;

namespace ForteLogisticsApp.Services
{
    /// <summary>
    /// The "at a glance" panel on each role's dashboard: the handful of figures that role decides
    /// on, against the previous period of the same length, what needs attention (right after the
    /// figures), then charts. Every figure comes from AnalyticsService in the viewer's own scope,
    /// so a dashboard always agrees with the reports, a customer only ever sees their own
    /// shipments and money, and a driver only their own deliveries (and never revenue).
    ///
    /// Pages call <see cref="Render"/>, which never throws: a failing query shows a short notice
    /// instead of taking the page (and any action the user just completed) down with it. The
    /// company-wide panels are cached for a minute, so busy dashboards do not re-run the same
    /// aggregates on every refresh.
    /// </summary>
    public static class RoleDashboards
    {
        private const int MaxAttention = 5;
        private const int CacheSeconds = 60;

        /// <summary>Overdue and owed money is counted over all time: old debt is still debt.</summary>
        private static readonly DateTime AllTimeFrom = new DateTime(2000, 1, 1);

        /// <summary>
        /// Builds a panel safely. <paramref name="cacheKey"/> (company-wide panels only) caches the
        /// HTML briefly; per-user panels pass null.
        /// </summary>
        public static string Render(Func<string> build, string cacheKey)
        {
            try
            {
                if (cacheKey == null) return build();
                var cached = HttpRuntime.Cache[cacheKey] as string;
                if (cached != null) return cached;
                string html = build();
                HttpRuntime.Cache.Insert(cacheKey, html, null, DateTime.UtcNow.AddSeconds(CacheSeconds), Cache.NoSlidingExpiration);
                return html;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dashboard summary failed: " + ex);
                return "<p class='lc-note' role='status'>The summary figures are unavailable right now. " +
                       "Everything else on this page works; try refreshing in a minute.</p>";
            }
        }

        public static string CacheKey(string panel, DateTime now, string variant)
        {
            return "lc-dashboard:" + panel + ":" + (variant ?? string.Empty) + ":" + now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        }

        // ------------------------------------------------------------------ Admin

        public static string Admin(DateTime now, Func<string, string> resolve)
        {
            AnalyticsScope scope = AnalyticsScope.Company();
            ReportPeriod period = ReportPeriod.Preset("30d", now.Date);
            PerformanceSnapshot s = AnalyticsService.Build(scope, period);
            CompanyCounts counts = AnalyticsService.Counts(scope);
            Receivables owed = AllTimeReceivables(scope, now);
            IList<DriverStanding> drivers = AnalyticsService.DriverStandings(scope, period.From, period.ToExclusive);
            IList<StuckStage> stuck = AnalyticsService.StuckParcels(scope, now, PerformanceInsights.StuckDays);
            bool compare = HasActivity(s.Previous, true);

            var html = new StringBuilder();
            html.Append(Heading("insights", "Last 30 days", period, compare));
            html.Append("<div class='lc-kpis'>");
            html.Append(Figure("add_box", "Shipments booked", s.Current.Booked, s.Previous.Booked, true, compare, false));
            html.Append(Figure("task_alt", "Deliveries", s.Current.Delivered, s.Previous.Delivered, true, compare, false));
            html.Append(Success(s, compare));
            html.Append(Figure("receipt_long", "Revenue invoiced", s.Current.Invoiced, s.Previous.Invoiced, true, compare, true));
            html.Append("</div>");

            int officeStaff = Math.Max(0, counts.Staff - counts.StaffDrivers);
            html.Append(ReportHtml.SectionTitle("schedule", "Right now"));
            html.Append("<div class='lc-kpis'>");
            html.Append(ReportHtml.Stat("group", "Users", ReportHtml.Count(counts.Users),
                ReportHtml.Count(counts.Customers) + " customers, " + ReportHtml.Count(counts.StaffDrivers) + " drivers, " +
                ReportHtml.Count(officeStaff) + " office staff, " + ReportHtml.Count(counts.Admins) + " admin"));
            html.Append(OverdueStat(owed));
            html.Append(ReportHtml.Stat("local_shipping", "Drivers on the road", ReportHtml.Count(counts.DriversOnRoute),
                ReportHtml.Count(counts.DriversAvailable) + " available of " + ReportHtml.Count(counts.Drivers)));
            html.Append("</div>");

            // Period findings from the shared rules; money is judged on ALL overdue debt instead.
            var items = new List<AttentionItem>();
            AddOverdue(items, owed, "~/Finance/Invoices.aspx", "Open billing");
            items.AddRange(PerformanceInsights.Build(s, drivers, null, stuck, "day"));
            html.Append(Attention(items, "Nothing needs attention right now.", resolve));

            html.Append("<div class='lc-chart-grid'>");
            html.Append(Trend("dbAdminTrend", s, "lc-span-2"));
            var userMix = new Dictionary<string, decimal>
            {
                { "Customers", counts.Customers }, { "Drivers", counts.StaffDrivers }, { "Office staff", officeStaff }, { "Administrators", counts.Admins }
            };
            html.Append(ReportHtml.CategoryDonut("dbUsers", "Who uses the system",
                ReportHtml.Count(counts.Users) + " user accounts, by type.", userMix, "Type of user"));
            html.Append(Money("dbAdminMoney", s));
            html.Append("<div class='lc-span-2'>").Append(Pipeline(s)).Append("</div>");
            html.Append("</div>");

            html.Append(Links(resolve, "Admin reports", new[]
            {
                new[] { "~/Reports/Performance.aspx", "monitoring", "Business Performance report" },
                new[] { "~/Reports/Comparison.aspx", "compare_arrows", "Compare periods or groups" },
                new[] { "~/Reports/MonthlyRevenue.aspx", "trending_up", "Revenue report" }
            }));
            return html.ToString();
        }

        // ------------------------------------------------------------------ Operations (no revenue)

        public static string Operations(DateTime now, Func<string, string> resolve, bool isAdmin)
        {
            AnalyticsScope scope = AnalyticsScope.Company();
            ReportPeriod week = ReportPeriod.Preset("7d", now.Date);
            // Daily buckets: the last is today, the one before it yesterday - no extra queries.
            PerformanceSnapshot s = AnalyticsService.Build(scope, week);
            int last = s.Labels.Length - 1;
            IList<StuckStage> stuck = AnalyticsService.StuckParcels(scope, now, PerformanceInsights.StuckDays);
            CompanyCounts counts = AnalyticsService.Counts(scope);
            bool compare = HasActivity(s.Previous, false);

            var html = new StringBuilder();
            html.Append(ReportHtml.SectionTitle("today", "Today so far"));
            html.Append("<div class='lc-kpis'>");
            // Today is still running, so yesterday is context, not a verdict.
            html.Append(ReportHtml.Stat("add_box", "Booked today", ReportHtml.Count(s.Booked[last]),
                "Yesterday (full day): " + ReportHtml.Count(last > 0 ? s.Booked[last - 1] : 0m)));
            html.Append(ReportHtml.Stat("task_alt", "Delivered today", ReportHtml.Count(s.Delivered[last]),
                "Yesterday (full day): " + ReportHtml.Count(last > 0 ? s.Delivered[last - 1] : 0m)));
            html.Append(ReportHtml.Stat("cancel", "Failed attempts today", ReportHtml.Count(s.Failed[last]),
                "Yesterday (full day): " + ReportHtml.Count(last > 0 ? s.Failed[last - 1] : 0m)));
            html.Append(ReportHtml.Stat("local_shipping", "Drivers available", ReportHtml.Count(counts.DriversAvailable),
                ReportHtml.Count(counts.DriversOnRoute) + " on the road"));
            html.Append("</div>");

            var items = new List<AttentionItem>();
            foreach (StuckStage st in stuck)
            {
                // Send people to the page that can move the parcel on.
                bool beforeDispatch = MovementStages.Of(st.Status) <= MovementStages.Sorted;
                string href = isAdmin ? "~/Admin/Shipments.aspx" : beforeDispatch ? "~/Ops/Warehouse.aspx" : "~/Ops/RoutePlanning.aspx";
                string link = isAdmin ? "Review shipments" : beforeDispatch ? "Open warehouse" : "Open route planning";
                items.Add(new AttentionItem(st.OldestDays >= 2 * PerformanceInsights.StuckDays ? AttentionSeverity.High : AttentionSeverity.Medium,
                    PerformanceInsights.Plural(st.Count, "parcel") + " at \"" + PerformanceInsights.StatusLabel(st.Status) +
                    "\" have not moved for more than " + PerformanceInsights.StuckDays + " days (the oldest for " +
                    PerformanceInsights.Plural(st.OldestDays, "day") + ").", href, link));
            }
            AddSuccessWarning(items, s.Current, "this week");
            html.Append(Attention(items, "Nothing needs attention: no parcels are stuck and deliveries are on target.", resolve));

            html.Append(Heading("date_range", "Last 7 days", week, compare));
            html.Append("<div class='lc-kpis'>");
            html.Append(Figure("add_box", "Shipments booked", s.Current.Booked, s.Previous.Booked, true, compare, false));
            html.Append(Figure("task_alt", "Deliveries", s.Current.Delivered, s.Previous.Delivered, true, compare, false));
            html.Append(Success(s, compare));
            html.Append(ReportHtml.Stat("schedule", "Average delivery time", ReportHtml.Duration(s.Current.AvgDeliveryHours),
                "From booking to delivery"));
            html.Append("</div>");

            html.Append("<div class='lc-chart-grid'>");
            html.Append(Trend("dbOpsTrend", s, "lc-span-2"));
            html.Append(ReportHtml.CategoryDonut("dbOpsInProgress", "Parcels in progress, by stage",
                "Every parcel not yet delivered or failed, by where it is now.", InProgress(), "Stage"));
            var destinations = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (NamedValue d in s.TopDestinations) destinations[d.Name] = d.Value;
            html.Append(ReportHtml.CategoryBars("dbOpsDestinations", "Busiest destinations",
                "Where this week's bookings are going.", destinations, "Destination", 6));
            html.Append("<div class='lc-span-2'>").Append(Pipeline(s)).Append("</div>");
            html.Append("</div>");

            html.Append(Links(resolve, "Operations reports", new[]
            {
                new[] { "~/Reports/DailyParcelMovement.aspx", "analytics", "Parcel Movement report" },
                new[] { "~/Reports/DriverPerformance.aspx", "speed", "Driver Performance report" },
                new[] { "~/Reports/LostDamaged.aspx", "report_problem", "Lost and Damaged report" }
            }));
            return html.ToString();
        }

        private const string InProgressSql =
            "SELECT s.DeliveryStatus, COUNT(*) AS N FROM dbo.[Shipment] s " +
            "WHERE s.DeliveryStatus NOT IN ('Delivered', 'Failed') GROUP BY s.DeliveryStatus";

        private static Dictionary<string, decimal> InProgress()
        {
            var stages = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in Db.Query(InProgressSql).Rows)
            {
                string label = PerformanceInsights.StatusLabel(Db.ToStr(r["DeliveryStatus"]));
                decimal n;
                stages.TryGetValue(label, out n);
                stages[label] = n + Db.ToInt(r["N"]);
            }
            return stages;
        }

        // ------------------------------------------------------------------ Finance

        private const string UninvoicedSql =
            "SELECT COUNT(*) FROM dbo.[Shipment] s WHERE s.DeliveryStatus = 'Delivered' " +
            "AND NOT EXISTS (SELECT 1 FROM dbo.[Payment] p WHERE p.ShipmentID = s.ShipmentID AND p.PaymentStatus <> 'Cancelled')";

        public static string Finance(DateTime now, Func<string, string> resolve)
        {
            AnalyticsScope scope = AnalyticsScope.Company();
            ReportPeriod month = ReportPeriod.Preset("30d", now.Date);
            ReportPeriod year = ReportPeriod.Preset("12m", now.Date);
            PeriodTotals current = AnalyticsService.TotalsFor(scope, month);
            PeriodTotals before = AnalyticsService.TotalsFor(scope, month.Previous());
            PerformanceSnapshot y = AnalyticsService.Build(scope, year);
            Receivables owed = AllTimeReceivables(scope, now);
            IList<NamedValue> customers = AnalyticsService.TopCustomers(scope, month.From, month.ToExclusive, 6);
            int uninvoiced = Db.ToInt(Db.Scalar(UninvoicedSql));
            bool compare = before.Invoiced > 0m;

            var html = new StringBuilder();
            html.Append(Heading("payments", "Last 30 days", month, compare));
            html.Append("<div class='lc-kpis'>");
            html.Append(Figure("receipt_long", "Revenue invoiced", current.Invoiced, before.Invoiced, true, compare, true));
            html.Append(ReportHtml.Stat("account_balance_wallet", "Paid so far", ReportHtml.Money(current.Collected),
                Delta.FormatRate(current.CollectionRate) + " of this period's invoices"));
            html.Append("</div>");

            html.Append(ReportHtml.SectionTitle("account_balance", "Owed to us now (all time)"));
            html.Append("<div class='lc-kpis'>");
            html.Append(ReportHtml.Stat("hourglass_top", "Still owed", ReportHtml.Money(owed.Outstanding),
                ReportHtml.Money(owed.NotYetDue) + " not yet due"));
            html.Append(OverdueStat(owed));
            html.Append(ReportHtml.Stat("receipt", "Deliveries not invoiced", ReportHtml.Count(uninvoiced), "Delivered with no live invoice"));
            html.Append("</div>");

            var items = new List<AttentionItem>();
            AddOverdue(items, owed, "~/Finance/Invoices.aspx", "Open invoices");
            if (uninvoiced > 0)
            {
                items.Add(new AttentionItem(AttentionSeverity.Medium,
                    PerformanceInsights.Plural(uninvoiced, "delivered shipment") + " " + (uninvoiced == 1 ? "has" : "have") + " no invoice yet.",
                    "~/Finance/Invoices.aspx", "Raise invoices"));
            }
            html.Append(Attention(items, "Nothing needs attention: nothing is overdue and every delivery is invoiced.", resolve));

            html.Append("<div class='lc-chart-grid'>");
            var rows = new List<string[]>();
            for (int i = 0; i < y.Labels.Length; i++) rows.Add(new[] { y.Labels[i], ReportHtml.Money(y.Invoiced[i]), ReportHtml.Money(y.Collected[i]) });
            html.Append(ReportHtml.ChartCard("dbFinanceYear", "Revenue invoiced and paid, last 12 months",
                ReportHtml.Money(y.Current.Invoiced) + " invoiced over the 12 months, " + ReportHtml.Money(y.Current.Collected) +
                " of it paid so far. The latest month is still in progress.",
                "line", "currency",
                new
                {
                    labels = y.Labels,
                    series = new object[]
                    {
                        new { label = "Invoiced", data = y.Invoiced, tone = "primary" },
                        new { label = "Paid so far", data = y.Collected, tone = "success", dashed = true }
                    }
                },
                new[] { "Month", "Invoiced", "Paid so far" }, rows, y.Current.Invoiced == 0m, "lc-span-2"));

            var position = new Dictionary<string, decimal> { { "Not yet due", owed.NotYetDue }, { "Overdue", owed.Overdue } };
            html.Append(ReportHtml.CategoryDonut("dbFinancePosition", "What is still owed",
                ReportHtml.Money(owed.Outstanding) + " unpaid, split by whether it is past its due date.", position, "Position", "currency"));
            var top = new Dictionary<string, decimal>();
            foreach (NamedValue c in customers)
            {
                decimal v;
                top.TryGetValue(c.Name, out v);
                top[c.Name] = v + c.Value;
            }
            html.Append(ReportHtml.CategoryBars("dbFinanceCustomers", "Top customers, last 30 days",
                "Invoiced in the last 30 days.", top, "Customer", 6, "currency"));
            html.Append("</div>");

            html.Append(Links(resolve, "Finance reports", new[]
            {
                new[] { "~/Reports/MonthlyRevenue.aspx", "trending_up", "Revenue report" },
                new[] { "~/Reports/Comparison.aspx?mode=groups&by=client", "compare_arrows", "Revenue by customer type" }
            }));
            return html.ToString();
        }

        // ------------------------------------------------------------------ Customer (own data only)

        public static string Customer(int customerUserId, DateTime now, Func<string, string> resolve)
        {
            AnalyticsScope scope = AnalyticsScope.ForCustomer(customerUserId);
            ReportPeriod year = ReportPeriod.Preset("12m", now.Date);
            PerformanceSnapshot s = AnalyticsService.Build(scope, year);
            Receivables owed = AllTimeReceivables(scope, now);
            IList<StuckStage> stuck = AnalyticsService.StuckParcels(scope, now, PerformanceInsights.StuckDays);

            var html = new StringBuilder();
            html.Append(Heading("insights", "Your shipping: last 12 months", year, false));

            // Anything to act on comes first. Overdue counts invoices billed to you personally.
            var items = new List<AttentionItem>();
            if (owed.OverdueInvoices > 0)
            {
                items.Add(new AttentionItem(AttentionSeverity.High,
                    ReportHtml.Money(owed.Overdue) + " is overdue across " + PerformanceInsights.Plural(owed.OverdueInvoices, "invoice") +
                    " billed to you.", "~/Customer/Invoices.aspx", "Pay now"));
            }
            foreach (StuckStage st in stuck)
            {
                items.Add(new AttentionItem(AttentionSeverity.Info,
                    PerformanceInsights.Plural(st.Count, "parcel") + " of yours " + (st.Count == 1 ? "has" : "have") + " been at \"" +
                    PerformanceInsights.StatusLabel(st.Status) + "\" for more than " + PerformanceInsights.StuckDays +
                    " days. We are on it; you can follow it on the tracking page.", "~/Customer/Track.aspx", "Track"));
            }
            if (items.Count > 0) html.Append(Attention(items, string.Empty, resolve));

            html.Append("<div class='lc-kpis'>");
            html.Append(ReportHtml.Stat("inventory_2", "Shipments booked (12 months)", ReportHtml.Count(s.Current.Booked)));
            html.Append(ReportHtml.Stat("task_alt", "Delivered (12 months)", ReportHtml.Count(s.Current.Delivered)));
            html.Append(ReportHtml.Stat("schedule", "Average delivery time", ReportHtml.Duration(s.Current.AvgDeliveryHours),
                "From booking to your recipient"));
            html.Append(ReportHtml.Stat("receipt_long", "Spent on shipping (12 months)", ReportHtml.Money(s.Current.Invoiced),
                "Invoiced to you, cancelled invoices excluded"));
            html.Append("</div>");

            html.Append("<div class='lc-chart-grid'>");
            var rows = new List<string[]>();
            for (int i = 0; i < s.Labels.Length; i++) rows.Add(new[] { s.Labels[i], ReportHtml.Count(s.Booked[i]), ReportHtml.Count(s.Delivered[i]) });
            html.Append(ReportHtml.ChartCard("dbCustShipments", "Your shipments per month",
                ReportHtml.Count(s.Current.Booked) + " booked and " + ReportHtml.Count(s.Current.Delivered) +
                " delivered in the last 12 months. The latest month is still in progress.",
                "bar", "number",
                new
                {
                    labels = s.Labels,
                    series = new object[]
                    {
                        new { label = "Booked", data = s.Booked, tone = "primary" },
                        new { label = "Delivered", data = s.Delivered, tone = "success" }
                    }
                },
                new[] { "Month", "Booked", "Delivered" }, rows, s.Current.Booked + s.Current.Delivered == 0m, "lc-span-2"));

            var status = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, int> kv in s.Cohort.ByStatus)
            {
                string label = string.Equals(kv.Key, "Delivered", StringComparison.OrdinalIgnoreCase) ? "Delivered"
                             : string.Equals(kv.Key, "Failed", StringComparison.OrdinalIgnoreCase) ? "Delivery failed"
                             : PerformanceInsights.StatusLabel(kv.Key);
                decimal n;
                status.TryGetValue(label, out n);
                status[label] = n + kv.Value;
            }
            html.Append(ReportHtml.CategoryDonut("dbCustStatus", "Where your parcels are",
                "The current status of the shipments you booked in the last 12 months.", status, "Status"));

            var spendRows = new List<string[]>();
            for (int i = 0; i < s.Labels.Length; i++) spendRows.Add(new[] { s.Labels[i], ReportHtml.Money(s.Invoiced[i]) });
            html.Append(ReportHtml.ChartCard("dbCustSpend", "Your spend per month",
                ReportHtml.Money(s.Current.Invoiced) + " invoiced to you in the last 12 months. The latest month is still in progress.",
                "line", "currency",
                new { labels = s.Labels, series = new object[] { new { label = "Invoiced", data = s.Invoiced, tone = "primary" } } },
                new[] { "Month", "Invoiced" }, spendRows, s.Current.Invoiced == 0m));
            html.Append("</div>");
            return html.ToString();
        }

        // ------------------------------------------------------------------ Driver (own data, no revenue)

        private const string DriverRatingSql =
            "SELECT AVG(CAST(r.RatingScore AS DECIMAL(5,2))) AS A, COUNT(*) AS N FROM dbo.[Rating] r " +
            "WHERE r.DriverID = @d AND r.CreatedDate >= @f AND r.CreatedDate < @t";

        public static string Driver(int driverUserId, DateTime now, Func<string, string> resolve)
        {
            AnalyticsScope scope = AnalyticsScope.ForDriver(driverUserId);
            ReportPeriod period = ReportPeriod.Preset("30d", now.Date);
            PerformanceSnapshot s = AnalyticsService.Build(scope, period);
            IList<StuckStage> stuck = AnalyticsService.StuckParcels(scope, now, PerformanceInsights.StuckDays);
            DataTable rating = Db.Query(DriverRatingSql, "@d", driverUserId, "@f", period.From, "@t", period.ToExclusive);
            decimal? avg = rating.Rows.Count == 0 || rating.Rows[0]["A"] == DBNull.Value
                ? (decimal?)null : Math.Round(Db.ToDecimal(rating.Rows[0]["A"]), 1, MidpointRounding.AwayFromZero);
            int ratings = rating.Rows.Count == 0 ? 0 : Db.ToInt(rating.Rows[0]["N"]);
            bool compare = s.Previous.Delivered + s.Previous.Failed > 0m;

            var html = new StringBuilder();
            html.Append(Heading("speed", "Your performance: last 30 days", period, compare));
            html.Append("<div class='lc-kpis'>");
            html.Append(Figure("task_alt", "Deliveries (30 days)", s.Current.Delivered, s.Previous.Delivered, true, compare, false));
            html.Append(Figure("cancel", "Failed attempts (30 days)", s.Current.Failed, s.Previous.Failed, false, compare, false));
            html.Append(Success(s, compare));
            html.Append(ReportHtml.Stat("star", "Customer rating",
                avg == null ? "n/a" : avg.Value.ToString("0.0", ReportHtml.Numbers) + " / 5", PerformanceInsights.Plural(ratings, "rating")));
            html.Append("</div>");

            var items = new List<AttentionItem>();
            AddSuccessWarning(items, s.Current, "over the last 30 days");
            foreach (StuckStage st in stuck)
            {
                items.Add(new AttentionItem(AttentionSeverity.Medium,
                    PerformanceInsights.Plural(st.Count, "parcel") + " on your manifest " + (st.Count == 1 ? "has" : "have") + " been at \"" +
                    PerformanceInsights.StatusLabel(st.Status) + "\" for more than " + PerformanceInsights.StuckDays +
                    " days - collect or deliver them, or tell the office.", "~/Driver/DeliveryConfirmation.aspx", "Confirm a delivery"));
            }
            html.Append(Attention(items, "You are on target and nothing on your manifest is stuck.", resolve));

            html.Append("<div class='lc-chart-grid'>");
            var rows = new List<string[]>();
            for (int i = 0; i < s.Labels.Length; i++) rows.Add(new[] { s.Labels[i], ReportHtml.Count(s.Delivered[i]), ReportHtml.Count(s.Failed[i]) });
            html.Append(ReportHtml.ChartCard("dbDriverTrend", "Your deliveries and failed attempts per day",
                ReportHtml.Count(s.Current.Delivered) + " deliveries and " + ReportHtml.Count(s.Current.Failed) + " failed attempts in the last 30 days.",
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
                new[] { "Day", "Delivered", "Failed attempts" }, rows, s.Current.Delivered + s.Current.Failed == 0m, "lc-span-2"));
            html.Append("</div>");
            html.Append(Links(resolve, "Your reports", new[] { new[] { "~/Reports/DailyParcelMovement.aspx", "analytics", "My parcel movement report" } }));
            return html.ToString();
        }

        // ------------------------------------------------------------------ shared pieces

        private static Receivables AllTimeReceivables(AnalyticsScope scope, DateTime now)
        {
            // A far but SQL-safe upper bound, so invoices dated ahead are still counted.
            return AnalyticsService.ReceivablesFor(scope, AllTimeFrom, now.Date.AddYears(5), now);
        }

        private static string OverdueStat(Receivables owed)
        {
            return ReportHtml.Stat("assignment_late", "Overdue (all time)", ReportHtml.Money(owed.Overdue),
                owed.OverdueInvoices == 0 ? "Nothing overdue"
                    : PerformanceInsights.Plural(owed.OverdueInvoices, "invoice") + ", oldest " + PerformanceInsights.Plural(owed.OldestOverdueDays, "day") + " late");
        }

        private static void AddOverdue(List<AttentionItem> items, Receivables owed, string href, string link)
        {
            if (owed.OverdueInvoices == 0) return;
            items.Add(new AttentionItem(AttentionSeverity.High,
                ReportHtml.Money(owed.Overdue) + " is overdue across " + PerformanceInsights.Plural(owed.OverdueInvoices, "invoice") +
                "; the oldest is " + PerformanceInsights.Plural(owed.OldestOverdueDays, "day") + " late.", href, link));
        }

        /// <summary>Whether the previous period had anything to compare with; revenue only counts where the panel shows it.</summary>
        private static bool HasActivity(PeriodTotals t, bool includeMoney)
        {
            return t.Booked + t.Delivered + t.Failed + (includeMoney ? t.Invoiced : 0m) > 0m;
        }

        private static string Heading(string icon, string title, ReportPeriod period, bool compared)
        {
            string range = period.From.ToString("dd MMM", CultureInfo.InvariantCulture) + " to " +
                           period.To.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
            return ReportHtml.SectionTitle(icon, title) +
                   "<p class='lc-subtitle'>" + HttpUtility.HtmlEncode(range +
                   (compared ? ", compared with the " + period.Days + " days before" : string.Empty)) + "</p>";
        }

        private static string Attention(List<AttentionItem> items, string allClear, Func<string, string> resolve)
        {
            // High first; within a level, the order the rules produced.
            var ordered = new List<AttentionItem>();
            foreach (AttentionSeverity level in new[] { AttentionSeverity.High, AttentionSeverity.Medium, AttentionSeverity.Info })
            {
                foreach (AttentionItem i in items) if (i.Severity == level && ordered.Count < MaxAttention) ordered.Add(i);
            }
            return ReportHtml.SectionTitle("notification_important", "Needs attention") + ReportHtml.Attention(ordered, allClear, resolve);
        }

        private static string Figure(string icon, string label, decimal current, decimal previous, bool higherIsBetter, bool compare, bool money)
        {
            string value = money ? ReportHtml.Money(current) : ReportHtml.Count(current);
            return compare
                ? ReportHtml.Kpi(icon, label, value, current, previous, higherIsBetter)
                : ReportHtml.Stat(icon, label, value);
        }

        private static string Success(PerformanceSnapshot s, bool compare)
        {
            string target = "Target " + PerformanceInsights.SuccessTarget.ToString("0", CultureInfo.InvariantCulture) + "%";
            return compare
                ? ReportHtml.KpiRate("verified", "Delivery attempt success", s.Current.SuccessRate, s.Previous.SuccessRate, true, target)
                : ReportHtml.Stat("verified", "Delivery attempt success", Delta.FormatRate(s.Current.SuccessRate), target);
        }

        private static void AddSuccessWarning(List<AttentionItem> items, PeriodTotals t, string when)
        {
            if (t.Delivered + t.Failed >= PerformanceInsights.MinAttempts && t.SuccessRate.Value < PerformanceInsights.SuccessTarget)
            {
                items.Add(new AttentionItem(AttentionSeverity.Medium,
                    "Delivery attempt success " + when + " is " + Delta.FormatRate(t.SuccessRate) + ", below the " +
                    PerformanceInsights.SuccessTarget.ToString("0", CultureInfo.InvariantCulture) + "% target."));
            }
        }

        private static string Trend(string id, PerformanceSnapshot s, string extraClass)
        {
            var rows = new List<string[]>();
            for (int i = 0; i < s.Labels.Length; i++)
            {
                rows.Add(new[] { s.Labels[i], ReportHtml.Count(s.Booked[i]), ReportHtml.Count(s.Delivered[i]), ReportHtml.Count(s.Failed[i]) });
            }
            return ReportHtml.ChartCard(id, "Booked, delivered and failed attempts per day",
                ReportHtml.Count(s.Current.Booked) + " booked, " + ReportHtml.Count(s.Current.Delivered) + " delivered and " +
                ReportHtml.Count(s.Current.Failed) + " failed delivery attempts. Today is still in progress.",
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
                new[] { "Day", "Booked", "Delivered", "Failed attempts" }, rows,
                s.Current.Booked + s.Current.Delivered + s.Current.Failed == 0m, extraClass);
        }

        private static string Money(string id, PerformanceSnapshot s)
        {
            var rows = new List<string[]>
            {
                new[] { "Invoiced", ReportHtml.Money(s.Current.Invoiced), ReportHtml.Money(s.Previous.Invoiced) },
                new[] { "Paid so far", ReportHtml.Money(s.Current.Collected), ReportHtml.Money(s.Previous.Collected) }
            };
            return ReportHtml.ChartCard(id, "Revenue: this period and the one before",
                "Money invoiced and how much of it has been paid so far, for the last 30 days and the 30 before. " +
                "Newer invoices have had less time to be paid.",
                "bar", "currency",
                new
                {
                    labels = new[] { "Invoiced", "Paid so far" },
                    series = new object[]
                    {
                        new { label = "Previous 30 days", data = new[] { s.Previous.Invoiced, s.Previous.Collected }, tone = "neutral", dashed = true },
                        new { label = "Last 30 days", data = new[] { s.Current.Invoiced, s.Current.Collected }, tone = "success" }
                    }
                },
                new[] { "Measure", "Last 30 days", "Previous 30 days" }, rows,
                s.Current.Invoiced + s.Previous.Invoiced == 0m);
        }

        private static string Pipeline(PerformanceSnapshot s)
        {
            return ReportHtml.Pipeline(s.Pipeline, s.Cohort,
                "How far the " + ReportHtml.Count(s.Cohort.Total) + " shipments booked in this period have got. " +
                "Each stage counts parcels that reached it or went further; the newest bookings are still on their way, " +
                "so later stages fill up over time.");
        }

        /// <summary>Links to the full reports behind a dashboard.</summary>
        private static string Links(Func<string, string> resolve, string label, string[][] links)
        {
            var sb = new StringBuilder("<nav class='lc-dash-links' aria-label='" + HttpUtility.HtmlAttributeEncode(label) + "'>");
            foreach (string[] l in links)
            {
                sb.Append("<a class='fcl-btn fcl-btn-dark' href='").Append(HttpUtility.HtmlAttributeEncode(resolve(l[0]))).Append("'>")
                  .Append("<span class='material-symbols-outlined' aria-hidden='true'>").Append(HttpUtility.HtmlEncode(l[1])).Append("</span>")
                  .Append(HttpUtility.HtmlEncode(l[2])).Append("</a>");
            }
            return sb.Append("</nav>").ToString();
        }
    }
}
