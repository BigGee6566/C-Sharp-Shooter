using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Controls;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Reports
{
    /// <summary>
    /// Business Performance Report - the management overview. For any day, week or month range
    /// it answers: is the business healthy (key figures vs the previous period), what changed
    /// (trend charts), where parcels get stuck (outcomes, pipeline, destinations, drivers), and
    /// what needs action now (an ordered, linked "Needs attention" list).
    ///
    /// Admin-only, because it shows revenue. Every figure comes from AnalyticsService in the
    /// company scope, so it agrees with every other report and dashboard by construction; the
    /// attention rules live in PerformanceInsights where they are unit-tested. The period lives
    /// in the URL (ReportQuery): Apply and Export both redirect to a GET built from the form.
    /// </summary>
    public partial class Performance : SecurePage
    {
        protected ReportHeader rptHeader;
        protected ReportFooter rptFooter;
        protected DropDownList ddlPreset, ddlGroup;
        protected TextBox txtFrom, txtTo;
        protected Button btnApply, btnExport;
        protected HiddenField hfShownFrom, hfShownTo;
        protected Panel pnlError, pnlFilters;
        protected Literal litError, litBody;

        private const string PageUrl = "~/Reports/Performance.aspx";

        private ReportPeriod period;
        private string presetKey;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Admin");

            string problem;
            period = ReportQuery.Read(Request.QueryString, DateTime.Today, "30d", out presetKey, out problem);

            if (IsPostBack) return;

            if (problem == null && string.Equals(Request.QueryString["export"], "csv", StringComparison.OrdinalIgnoreCase))
            {
                ExportCsv();
                return;
            }

            ReportQuery.BindPresets(ddlPreset);
            ReportQuery.BindGroups(ddlGroup);
            ddlPreset.SelectedValue = presetKey;
            ddlGroup.SelectedValue = ReportQuery.GroupSelection(Request.QueryString);
            txtFrom.Text = ReportQuery.IsoDate(period.From);
            txtTo.Text = ReportQuery.IsoDate(period.To);

            if (problem != null)
            {
                ShowError(problem, presetKey);
            }

            // What the boxes showed, so Apply can tell whether the user edited the dates.
            hfShownFrom.Value = txtFrom.Text;
            hfShownTo.Value = txtTo.Text;

            ChartAssets.Register(this);
            Render();
        }

        protected void btnApply_Click(object sender, EventArgs e)
        {
            Response.Redirect(ResolveUrl(PageUrl) + FormQuery());
        }

        /// <summary>Exports exactly what the form says - the same path as Apply - as a GET.</summary>
        protected void btnExport_Click(object sender, EventArgs e)
        {
            Response.Redirect(ResolveUrl(PageUrl) + FormQuery() + "&export=csv");
        }

        private string FormQuery()
        {
            return ReportQuery.FromForm(ddlPreset.SelectedValue, txtFrom.Text, txtTo.Text, ddlGroup.SelectedValue,
                hfShownFrom.Value, hfShownTo.Value);
        }

        private void ExportCsv()
        {
            PerformanceSnapshot s = AnalyticsService.Build(AnalyticsScope.Company(), period);
            string name = "BusinessPerformance_" + ReportQuery.IsoDate(period.From) + "_to_" + ReportQuery.IsoDate(period.To) + ".csv";
            ReportExport.ToCsv(Response, BreakdownCsv(s), name);
        }

        private void ShowError(string problem, string fallbackPreset)
        {
            pnlError.Visible = true;
            litError.Text = HttpUtility.HtmlEncode(problem + " Showing " +
                ReportQuery.PresetLabel(fallbackPreset).ToLowerInvariant() + " instead.");

            // When the dates were the problem, keep what the user typed and mark the fields.
            string rawFrom = Request.QueryString["from"], rawTo = Request.QueryString["to"];
            if (!string.IsNullOrEmpty(rawFrom) || !string.IsNullOrEmpty(rawTo))
            {
                txtFrom.Text = rawFrom ?? string.Empty;
                txtTo.Text = rawTo ?? string.Empty;
                foreach (TextBox box in new[] { txtFrom, txtTo })
                {
                    box.Attributes["aria-invalid"] = "true";
                    box.Attributes["aria-describedby"] = pnlError.ClientID;
                }
            }
        }

        private void Render()
        {
            AnalyticsScope scope = AnalyticsScope.Company();
            DateTime now = DateTime.Now;
            PerformanceSnapshot s = AnalyticsService.Build(scope, period);
            CompanyCounts counts = AnalyticsService.Counts(scope);
            IList<DriverStanding> drivers = AnalyticsService.DriverStandings(scope, period.From, period.ToExclusive);
            Receivables receivables = AnalyticsService.ReceivablesFor(scope, period.From, period.ToExclusive, now);
            IList<StuckStage> stuck = AnalyticsService.StuckParcels(scope, now, PerformanceInsights.StuckDays);
            IList<NamedValue> topCustomers = AnalyticsService.TopCustomers(scope, period.From, period.ToExclusive, 6);

            bool compare = HasActivity(s.Previous);

            rptHeader.AddFilter("Period", DescribeRange(period));
            rptHeader.AddFilter("Grouped by", ReportQuery.GroupLabel(period.Granularity));
            rptHeader.AddFilter("Compared with", DescribeRange(s.PreviousPeriod) +
                (compare ? string.Empty : " - no activity, so comparisons are hidden"));
            rptHeader.AddFilter("Scope", "Whole company - all customers, drivers and routes");

            var sb = new StringBuilder();
            sb.Append(KeyFigures(s, counts, receivables, compare, now));
            sb.Append(Trends(s, topCustomers, compare));
            sb.Append(Outcomes(s, drivers));
            sb.Append(ReportHtml.SectionTitle("table_chart", "Period-by-period breakdown"));
            sb.Append("<div class='lc-chart-card'>").Append(BreakdownHtml(s)).Append("</div>");
            sb.Append(ReportHtml.SectionTitle("flag", "Needs attention"));
            sb.Append(ReportHtml.Attention(
                PerformanceInsights.Build(s, drivers, receivables, stuck, BucketWord(false)),
                "No exceptions found: nothing is overdue, no parcels are stuck, none were lost or damaged, and delivery success met the " +
                PerformanceInsights.SuccessTarget.ToString("0", CultureInfo.InvariantCulture) + "% target.",
                ResolveUrl));
            litBody.Text = sb.ToString();
        }

        // ------------------------------------------------------------------ sections

        private string KeyFigures(PerformanceSnapshot s, CompanyCounts counts, Receivables r, bool compare, DateTime now)
        {
            PeriodTotals c = s.Current, p = s.Previous;
            decimal attempts = c.Delivered + c.Failed;
            string target = PerformanceInsights.SuccessTarget.ToString("0", CultureInfo.InvariantCulture) + "%";
            string targetState = c.SuccessRate == null ? "nothing to measure yet"
                               : c.SuccessRate.Value >= PerformanceInsights.SuccessTarget ? "met" : "below target";
            string successHint = ReportHtml.Count(c.Delivered) + " of " + ReportHtml.Count(attempts) +
                                 " attempts succeeded. Target " + target + ": " + targetState + ".";

            var sb = new StringBuilder();
            sb.Append(ReportHtml.SectionTitle("insights", "Key figures"));
            if (!compare)
            {
                sb.Append("<p class='lc-note'>The previous period (").Append(HttpUtility.HtmlEncode(DescribeRange(s.PreviousPeriod)))
                  .Append(") had no activity, so the figures below are shown without a comparison.</p>");
            }

            sb.Append("<h3 class='lc-subtitle'>Deliveries</h3><div class='lc-kpis'>")
              .Append(Figure(compare, "inventory_2", "Shipments booked", ReportHtml.Count(c.Booked), c.Booked, p.Booked, true))
              .Append(Figure(compare, "task_alt", "Successful deliveries", ReportHtml.Count(c.Delivered), c.Delivered, p.Delivered, true))
              .Append(Figure(compare, "report", "Failed delivery attempts", ReportHtml.Count(c.Failed), c.Failed, p.Failed, false));
            sb.Append(compare
                ? ReportHtml.KpiRate("verified", "Delivery attempt success", c.SuccessRate, p.SuccessRate, true, successHint)
                : ReportHtml.Stat("verified", "Delivery attempt success", Delta.FormatRate(c.SuccessRate), successHint));
            sb.Append(compare && c.AvgDeliveryHours != null && p.AvgDeliveryHours != null
                ? ReportHtml.Kpi("schedule", "Average delivery time", ReportHtml.Duration(c.AvgDeliveryHours),
                    c.AvgDeliveryHours.Value, p.AvgDeliveryHours.Value, false, "From booking to delivery")
                : ReportHtml.Stat("schedule", "Average delivery time", ReportHtml.Duration(c.AvgDeliveryHours), "From booking to delivery"));
            sb.Append("</div>");

            decimal? paidRate = c.CollectionRate;
            sb.Append("<h3 class='lc-subtitle'>Money and customers</h3><div class='lc-kpis'>")
              .Append(Figure(compare, "receipt_long", "Revenue invoiced", ReportHtml.Money(c.Invoiced), c.Invoiced, p.Invoiced, true));
            sb.Append(compare && c.RevenuePerShipment != null && p.RevenuePerShipment != null
                ? ReportHtml.Kpi("sell", "Revenue per shipment", ReportHtml.Money(c.RevenuePerShipment.Value),
                    c.RevenuePerShipment.Value, p.RevenuePerShipment.Value, true)
                : ReportHtml.Stat("sell", "Revenue per shipment",
                    c.RevenuePerShipment == null ? "n/a" : ReportHtml.Money(c.RevenuePerShipment.Value)));
            // Shown without a verdict: a recent period has simply had less time to be paid.
            sb.Append(ReportHtml.Stat("payments", "Invoices paid so far", ReportHtml.Money(c.Collected),
                Delta.FormatRate(paidRate) + " of " + ReportHtml.Money(c.Invoiced) + " invoiced. Overdue: " + ReportHtml.Money(r.Overdue) +
                "; not yet due: " + ReportHtml.Money(r.NotYetDue) + "."));
            sb.Append(Figure(compare, "person_add", "New customers", ReportHtml.Count(c.NewCustomers), c.NewCustomers, p.NewCustomers, true))
              .Append("</div>");

            CohortSummary cohort = s.Cohort;
            sb.Append("<h3 class='lc-subtitle'>Operations</h3><div class='lc-kpis'>")
              .Append(ReportHtml.Stat("hourglass_top", "Still in progress", ReportHtml.Count(cohort.InProgress),
                    Delta.FormatRate(Delta.Rate(cohort.InProgress, cohort.Total)) + " of this period's " + ReportHtml.Count(cohort.Total) + " shipments"))
              .Append(ReportHtml.Stat("inventory", "Lost or damaged", ReportHtml.Count(cohort.Lost + cohort.Damaged),
                    ReportHtml.Count(cohort.Lost) + " lost, " + ReportHtml.Count(cohort.Damaged) + " damaged among this period's shipments"))
              .Append(ReportHtml.Stat("local_shipping", "Active drivers",
                    ReportHtml.Count(c.ActiveDrivers) + " of " + ReportHtml.Count(counts.Drivers),
                    "Drivers with a delivery or attempt in the period"))
              .Append(ReportHtml.Stat("group", "Registered users", ReportHtml.Count(counts.Users),
                    ReportHtml.Count(counts.Customers) + " customers and " + ReportHtml.Count(counts.Staff) + " staff, as at " +
                    now.ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture)))
              .Append("</div>");
            return sb.ToString();
        }

        private static string Figure(bool compare, string icon, string label, string value, decimal current, decimal previous, bool higherIsBetter)
        {
            return compare
                ? ReportHtml.Kpi(icon, label, value, current, previous, higherIsBetter)
                : ReportHtml.Stat(icon, label, value);
        }

        private string Trends(PerformanceSnapshot s, IList<NamedValue> topCustomers, bool compare)
        {
            PeriodTotals c = s.Current, p = s.Previous;
            string partial = period.PartialNote();
            // The previous-period overlay lines up exactly only day-for-day; weekly and monthly
            // windows can hold a different number of buckets, so there the total is quoted instead.
            bool overlay = compare && period.Granularity == Granularity.Day;

            var bookedSeries = new List<object> { new { label = "This period", data = s.Booked, tone = "primary" } };
            if (overlay) bookedSeries.Add(new { label = "Previous period", data = s.PrevBooked, tone = "neutral", dashed = true });

            string bookedSummary = ReportHtml.Count(c.Booked) + " shipments booked" +
                (compare ? ", " + Delta.Describe(c.Booked, p.Booked) : string.Empty) + "." +
                (overlay ? " Dashed line: the previous period, day by day." : compare ? " Previous period total: " + ReportHtml.Count(p.Booked) + "." : string.Empty) +
                (partial == null ? string.Empty : " " + partial);

            var sb = new StringBuilder();
            sb.Append(ReportHtml.SectionTitle("show_chart", "Trends"));
            sb.Append("<div class='lc-chart-grid'>");

            sb.Append(ReportHtml.ChartCard("chBooked", "Shipments booked per " + BucketWord(false), bookedSummary,
                "line", "number",
                new { labels = s.Labels, series = bookedSeries },
                overlay ? new[] { BucketWord(true), "This period", "Previous period" } : new[] { BucketWord(true), "Booked" },
                overlay ? Rows(s.Labels, s.Booked, s.PrevBooked, false) : Rows(s.Labels, s.Booked),
                AllZero(s.Booked), "lc-span-2"));

            sb.Append(ReportHtml.ChartCard("chOutcomes", "Successful deliveries vs failed attempts",
                ReportHtml.Count(c.Delivered) + " successful deliveries and " + ReportHtml.Count(c.Failed) +
                " failed attempts; attempt success " + Delta.FormatRate(c.SuccessRate) + "." +
                (partial == null ? string.Empty : " " + partial),
                "line", "number",
                new
                {
                    labels = s.Labels,
                    series = new object[]
                    {
                        new { label = "Successful deliveries", data = s.Delivered, tone = "success" },
                        new { label = "Failed attempts", data = s.Failed, tone = "danger" }
                    }
                },
                new[] { BucketWord(true), "Successful", "Failed attempts" },
                Rows(s.Labels, s.Delivered, s.Failed, false), AllZero(s.Delivered) && AllZero(s.Failed)));

            sb.Append(ReportHtml.ChartCard("chRevenue", "Revenue: invoiced vs paid so far",
                ReportHtml.Money(c.Invoiced) + " invoiced, of which " + ReportHtml.Money(c.Collected) + " has been paid (" +
                Delta.FormatRate(c.CollectionRate) + ").",
                "bar", "currency",
                new
                {
                    labels = s.Labels,
                    series = new object[]
                    {
                        new { label = "Invoiced", data = s.Invoiced, tone = "primary" },
                        new { label = "Paid so far", data = s.Collected, tone = "success" }
                    }
                },
                new[] { BucketWord(true), "Invoiced", "Paid so far" },
                Rows(s.Labels, s.Invoiced, s.Collected, true), AllZero(s.Invoiced)));

            sb.Append(ReportHtml.ChartCard("chCustomers", "New customer accounts",
                ReportHtml.Count(c.NewCustomers) + " new customers" +
                (compare ? ", " + Delta.Describe(c.NewCustomers, p.NewCustomers) : string.Empty) + ".",
                "bar", "number",
                new { labels = s.Labels, series = new object[] { new { label = "New customers", data = s.NewCustomers, tone = "info" } } },
                new[] { BucketWord(true), "New customers" },
                Rows(s.Labels, s.NewCustomers), AllZero(s.NewCustomers)));

            var customerNames = new List<string>();
            var customerValues = new List<decimal>();
            foreach (NamedValue nv in topCustomers)
            {
                customerNames.Add(nv.Name);
                customerValues.Add(nv.Value);
            }
            string customerSummary = customerNames.Count == 0
                ? "No invoices were raised in this period."
                : "The " + customerNames.Count + " customers billed most. The largest, " + customerNames[0] + ", accounts for " +
                  Delta.FormatRate(Delta.Rate(customerValues[0], c.Invoiced)) + " of revenue invoiced.";
            sb.Append(ReportHtml.ChartCard("chTopCustomers", "Top customers by revenue", customerSummary,
                "hbar", "currency",
                new { labels = customerNames, series = new object[] { new { label = "Invoiced", data = customerValues, tone = "primary" } } },
                new[] { "Customer", "Invoiced" }, MoneyRows(customerNames, customerValues), customerNames.Count == 0, "lc-span-2"));

            sb.Append("</div>");
            return sb.ToString();
        }

        private string Outcomes(PerformanceSnapshot s, IList<DriverStanding> drivers)
        {
            CohortSummary cohort = s.Cohort;
            var sb = new StringBuilder();
            sb.Append(ReportHtml.SectionTitle("account_tree", "Outcomes and bottlenecks"));
            sb.Append("<div class='lc-chart-grid'>");

            string[] outcomeLabels = { "Delivered", "Still in progress", "Failed (parcels)" };
            decimal[] outcomeValues = { cohort.Delivered, cohort.InProgress, cohort.Failed };
            sb.Append(ReportHtml.ChartCard("chCohort", "What happened to this period's shipments",
                "Of " + ReportHtml.Count(cohort.Total) + " shipments booked: " + ReportHtml.Count(cohort.Delivered) + " delivered, " +
                ReportHtml.Count(cohort.InProgress) + " still in progress, " + ReportHtml.Count(cohort.Failed) +
                " ended as failed. (Failed delivery attempts above count every try; this counts parcels.)",
                "donut", "number",
                new { labels = outcomeLabels, values = outcomeValues, tones = new[] { "success", "info", "danger" } },
                new[] { "Outcome", "Shipments", "Share" },
                ShareRows(outcomeLabels, outcomeValues), cohort.Total == 0));

            var destLabels = new List<string>();
            var destValues = new List<decimal>();
            foreach (NamedValue nv in s.TopDestinations)
            {
                destLabels.Add(nv.Name);
                destValues.Add(nv.Value);
            }
            sb.Append(ReportHtml.ChartCard("chDest", "Top destinations",
                destLabels.Count == 0
                    ? "No shipments were booked in this period."
                    : "Where this period's shipments are going. Busiest: " + destLabels[0] + " (" + ReportHtml.Count(destValues[0]) + ").",
                "hbar", "number",
                new { labels = destLabels, series = new object[] { new { label = "Shipments", data = destValues, tone = "primary" } } },
                new[] { "Destination", "Shipments" }, CountRows(destLabels, destValues), destLabels.Count == 0));

            sb.Append("<div class='lc-span-2'>")
              .Append(ReportHtml.Pipeline(s.Pipeline, cohort,
                    "How far the " + ReportHtml.Count(cohort.Total) + " shipments booked in this period have got. " +
                    "Each stage counts parcels that reached it or went further."))
              .Append("</div>");

            // Busiest by attempts (not deliveries), so a driver with many failures is never cut off.
            var active = new List<DriverStanding>();
            foreach (DriverStanding d in drivers) if (d.Delivered + d.Failed > 0) active.Add(d);
            active.Sort((a, b) =>
            {
                int byAttempts = (b.Delivered + b.Failed).CompareTo(a.Delivered + a.Failed);
                return byAttempts != 0 ? byAttempts : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
            if (active.Count > 8) active = active.GetRange(0, 8);

            var driverNames = new List<string>();
            var driverDelivered = new List<decimal>();
            var driverFailed = new List<decimal>();
            var driverRows = new List<string[]>();
            foreach (DriverStanding d in active)
            {
                driverNames.Add(d.Name);
                driverDelivered.Add(d.Delivered);
                driverFailed.Add(d.Failed);
                driverRows.Add(new[]
                {
                    d.Name, ReportHtml.Count(d.Delivered), ReportHtml.Count(d.Failed), Delta.FormatRate(d.SuccessRate),
                    d.AverageRating == null ? "-" : d.AverageRating.Value.ToString("0.0", CultureInfo.InvariantCulture) + " / 5"
                });
            }
            sb.Append(ReportHtml.ChartCard("chDrivers", "Deliveries by driver",
                active.Count == 0
                    ? "No driver completed or attempted a delivery in this period."
                    : "Successful deliveries and failed attempts for the " + active.Count + " drivers with the most attempts.",
                "hbar", "number",
                new
                {
                    labels = driverNames,
                    series = new object[]
                    {
                        new { label = "Successful", data = driverDelivered, tone = "success" },
                        new { label = "Failed attempts", data = driverFailed, tone = "danger" }
                    }
                },
                new[] { "Driver", "Successful", "Failed attempts", "Success rate", "Average rating" },
                driverRows, active.Count == 0, "lc-span-2"));

            sb.Append("</div>");
            return sb.ToString();
        }

        // ------------------------------------------------------------------ breakdown

        /// <summary>The on-screen/printed breakdown: runs of 3+ empty buckets collapse to one line.</summary>
        private string BreakdownHtml(PerformanceSnapshot s)
        {
            var rows = new List<string[]>();
            var empty = new List<bool>();
            for (int i = 0; i < s.Labels.Length; i++)
            {
                rows.Add(new[]
                {
                    s.Labels[i],
                    ReportHtml.Count(s.Booked[i]), ReportHtml.Count(s.Delivered[i]), ReportHtml.Count(s.Failed[i]),
                    Delta.FormatRate(Delta.Rate(s.Delivered[i], s.Delivered[i] + s.Failed[i])),
                    ReportHtml.Money(s.Invoiced[i]), ReportHtml.Money(s.Collected[i]), ReportHtml.Count(s.NewCustomers[i])
                });
                empty.Add(s.Booked[i] == 0m && s.Delivered[i] == 0m && s.Failed[i] == 0m &&
                          s.Invoiced[i] == 0m && s.Collected[i] == 0m && s.NewCustomers[i] == 0m);
            }

            List<string[]> shown = PerformanceInsights.CollapseEmptyRuns(rows, empty, 3, BucketWord(false));
            PeriodTotals c = s.Current;
            shown.Add(new[]
            {
                "Total", ReportHtml.Count(c.Booked), ReportHtml.Count(c.Delivered), ReportHtml.Count(c.Failed),
                Delta.FormatRate(c.SuccessRate), ReportHtml.Money(c.Invoiced), ReportHtml.Money(c.Collected), ReportHtml.Count(c.NewCustomers)
            });

            return ReportHtml.Table(
                new[] { BucketWord(true), "Booked", "Delivered", "Failed attempts", "Success rate", "Invoiced", "Paid so far", "New customers" },
                shown, "Period-by-period breakdown", true, true);
        }

        /// <summary>
        /// The CSV export keeps every bucket and writes plain invariant numbers (no grouping,
        /// "." decimal) so any spreadsheet in any locale reads them as numbers.
        /// </summary>
        private DataTable BreakdownCsv(PerformanceSnapshot s)
        {
            var dt = new DataTable();
            dt.Columns.Add(BucketWord(true), typeof(string));
            dt.Columns.Add("Booked", typeof(string));
            dt.Columns.Add("Delivered", typeof(string));
            dt.Columns.Add("Failed attempts", typeof(string));
            dt.Columns.Add("Success rate", typeof(string));
            dt.Columns.Add("Invoiced (R)", typeof(string));
            dt.Columns.Add("Paid so far (R)", typeof(string));
            dt.Columns.Add("New customers", typeof(string));

            for (int i = 0; i < s.Labels.Length; i++)
            {
                dt.Rows.Add(s.Labels[i], N(s.Booked[i]), N(s.Delivered[i]), N(s.Failed[i]),
                    Delta.FormatRate(Delta.Rate(s.Delivered[i], s.Delivered[i] + s.Failed[i])),
                    M(s.Invoiced[i]), M(s.Collected[i]), N(s.NewCustomers[i]));
            }
            PeriodTotals c = s.Current;
            dt.Rows.Add("Total", N(c.Booked), N(c.Delivered), N(c.Failed), Delta.FormatRate(c.SuccessRate),
                M(c.Invoiced), M(c.Collected), N(c.NewCustomers));
            return dt;
        }

        private static string N(decimal v)
        {
            return v.ToString("0", CultureInfo.InvariantCulture);
        }

        private static string M(decimal v)
        {
            return v.ToString("0.00", CultureInfo.InvariantCulture);
        }

        // ------------------------------------------------------------------ helpers

        private static bool HasActivity(PeriodTotals t)
        {
            return t != null && (t.Booked > 0 || t.Delivered > 0 || t.Failed > 0 || t.Invoiced > 0 || t.NewCustomers > 0);
        }

        private string BucketWord(bool capital)
        {
            string w = period.Granularity == Granularity.Week ? "week" : period.Granularity == Granularity.Month ? "month" : "day";
            return capital ? char.ToUpperInvariant(w[0]) + w.Substring(1) : w;
        }

        private static string DescribeRange(ReportPeriod p)
        {
            return p.From.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) + " to " +
                   p.To.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) +
                   " (" + p.Days + (p.Days == 1 ? " day)" : " days)");
        }

        private static bool AllZero(IList<decimal> values)
        {
            foreach (decimal v in values) if (v != 0m) return false;
            return true;
        }

        private static List<string[]> Rows(string[] labels, decimal[] a, decimal[] b, bool money)
        {
            var rows = new List<string[]>();
            for (int i = 0; i < labels.Length; i++)
            {
                rows.Add(new[]
                {
                    labels[i],
                    money ? ReportHtml.Money(a[i]) : ReportHtml.Count(a[i]),
                    money ? ReportHtml.Money(b[i]) : ReportHtml.Count(b[i])
                });
            }
            return rows;
        }

        private static List<string[]> Rows(string[] labels, decimal[] a)
        {
            var rows = new List<string[]>();
            for (int i = 0; i < labels.Length; i++) rows.Add(new[] { labels[i], ReportHtml.Count(a[i]) });
            return rows;
        }

        private static List<string[]> CountRows(IList<string> labels, IList<decimal> values)
        {
            var rows = new List<string[]>();
            for (int i = 0; i < labels.Count; i++) rows.Add(new[] { labels[i], ReportHtml.Count(values[i]) });
            return rows;
        }

        private static List<string[]> MoneyRows(IList<string> labels, IList<decimal> values)
        {
            var rows = new List<string[]>();
            for (int i = 0; i < labels.Count; i++) rows.Add(new[] { labels[i], ReportHtml.Money(values[i]) });
            return rows;
        }

        private static List<string[]> ShareRows(string[] labels, decimal[] values)
        {
            decimal total = 0m;
            foreach (decimal v in values) total += v;
            var rows = new List<string[]>();
            for (int i = 0; i < labels.Length; i++)
            {
                rows.Add(new[] { labels[i], ReportHtml.Count(values[i]), Delta.FormatRate(Delta.Rate(values[i], total)) });
            }
            return rows;
        }
    }
}
