using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Controls;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Reports
{
    /// <summary>
    /// Business Performance Report - the management overview. It answers, for any day, week
    /// or month range: is the business healthy (key figures vs the previous period), what
    /// changed (trend charts), where parcels get stuck (outcome donut, pipeline schematic,
    /// destinations, drivers), and what needs action (the "Needs attention" list).
    ///
    /// Admin-only, because it shows revenue. Every figure comes from AnalyticsService in the
    /// company scope, so it agrees with every other report and dashboard by construction.
    /// The period lives in the URL (see ReportQuery); Apply redirects to a GET.
    /// </summary>
    public partial class Performance : SecurePage
    {
        protected ReportHeader rptHeader;
        protected ReportFooter rptFooter;
        protected DropDownList ddlPreset, ddlGroup;
        protected TextBox txtFrom, txtTo;
        protected Button btnApply, btnExport;
        protected Label lblError;
        protected Literal litBody;

        /// <summary>Delivery-success level below which a bucket is flagged for attention.</summary>
        private const decimal SuccessTarget = 90m;

        /// <summary>Driver success level below which a driver is flagged (with at least MinAttempts).</summary>
        private const decimal DriverAlert = 80m;
        private const int MinAttempts = 3;

        private ReportPeriod period;
        private string presetKey;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Admin");

            string problem;
            period = ReportQuery.Read(Request.QueryString, DateTime.Today, "30d", out presetKey, out problem);

            if (!IsPostBack)
            {
                ReportQuery.BindPresets(ddlPreset);
                ReportQuery.BindGroups(ddlGroup);
                ddlPreset.SelectedValue = presetKey;
                ddlGroup.SelectedValue = ReportQuery.GroupKey(period.Granularity);
                txtFrom.Text = ReportQuery.IsoDate(period.From);
                txtTo.Text = ReportQuery.IsoDate(period.To);

                if (problem != null)
                {
                    ShowError(problem + " Showing " + ReportQuery.PresetLabel(presetKey).ToLowerInvariant() + " instead.");
                }

                ChartAssets.Register(this);
                Render();
            }
        }

        protected void btnApply_Click(object sender, EventArgs e)
        {
            string preset = ddlPreset.SelectedValue;
            string group = ddlGroup.SelectedValue;

            if (ReportQuery.IsKnownPreset(preset))
            {
                Response.Redirect(Request.Path + "?preset=" + HttpUtility.UrlEncode(preset) +
                                  "&group=" + HttpUtility.UrlEncode(group));
                return;
            }

            // Custom dates are passed through as typed; ReportQuery.Read validates them on the
            // GET and explains any problem, so there is one validation path.
            Response.Redirect(Request.Path + "?from=" + HttpUtility.UrlEncode(txtFrom.Text.Trim()) +
                              "&to=" + HttpUtility.UrlEncode(txtTo.Text.Trim()) +
                              "&group=" + HttpUtility.UrlEncode(group));
        }

        protected void btnExport_Click(object sender, EventArgs e)
        {
            PerformanceSnapshot s = AnalyticsService.Build(AnalyticsScope.Company(), period);
            string name = "BusinessPerformance_" + ReportQuery.IsoDate(period.From) + "_to_" + ReportQuery.IsoDate(period.To) + ".csv";
            ReportExport.ToCsv(Response, Breakdown(s), name);
        }

        private void ShowError(string message)
        {
            lblError.CssClass = UiHelper.AlertClass("warning") + " lc-filter-error";
            lblError.Text = HttpUtility.HtmlEncode(message);
        }

        private void Render()
        {
            AnalyticsScope scope = AnalyticsScope.Company();
            PerformanceSnapshot s = AnalyticsService.Build(scope, period);
            CompanyCounts counts = AnalyticsService.Counts(scope);
            IList<DriverStanding> drivers = AnalyticsService.DriverStandings(scope, period.From, period.ToExclusive);

            rptHeader.AddFilter("Period", DescribeRange(period));
            rptHeader.AddFilter("Grouped by", ReportQuery.GroupLabel(period.Granularity));
            rptHeader.AddFilter("Compared with", DescribeRange(s.PreviousPeriod));
            rptHeader.AddFilter("Scope", "Whole company - all customers, drivers and routes");

            var sb = new StringBuilder();
            sb.Append(KeyFigures(s, counts));
            sb.Append(Trends(s));
            sb.Append(Outcomes(s, drivers));
            sb.Append(ReportHtml.SectionTitle("table_chart", "Period-by-period breakdown"));
            sb.Append("<div class='lc-chart-card'>").Append(BreakdownHtml(s)).Append("</div>");
            sb.Append(ReportHtml.SectionTitle("flag", "Needs attention"));
            sb.Append(ReportHtml.Exceptions(Attention(s, drivers),
                "Nothing needs attention for this period: every " + BucketWord(false) +
                " met the " + SuccessTarget.ToString("0") + "% delivery-success target, all invoices are paid and no parcels were flagged lost or damaged."));
            litBody.Text = sb.ToString();
        }

        // ------------------------------------------------------------------ sections

        private string KeyFigures(PerformanceSnapshot s, CompanyCounts counts)
        {
            PeriodTotals c = s.Current, p = s.Previous;
            var sb = new StringBuilder();
            sb.Append(ReportHtml.SectionTitle("insights", "Key figures"));
            sb.Append("<div class='lc-kpis'>")
              .Append(ReportHtml.Kpi("inventory_2", "Shipments booked", ReportHtml.Count(c.Booked), c.Booked, p.Booked, true))
              .Append(ReportHtml.Kpi("task_alt", "Successful deliveries", ReportHtml.Count(c.Delivered), c.Delivered, p.Delivered, true))
              .Append(ReportHtml.Kpi("report", "Failed delivery attempts", ReportHtml.Count(c.Failed), c.Failed, p.Failed, false))
              .Append(ReportHtml.KpiRate("verified", "Delivery success rate", c.SuccessRate, p.SuccessRate, true,
                    "Target " + SuccessTarget.ToString("0") + "%"))
              .Append(ReportHtml.Kpi("receipt_long", "Revenue invoiced", ReportHtml.Money(c.Invoiced), c.Invoiced, p.Invoiced, true))
              .Append(ReportHtml.Kpi("payments", "Revenue collected", ReportHtml.Money(c.Collected), c.Collected, p.Collected, true,
                    Delta.FormatRate(c.CollectionRate) + " of this period's invoices paid"))
              .Append(ReportHtml.Kpi("person_add", "New customers", ReportHtml.Count(c.NewCustomers), c.NewCustomers, p.NewCustomers, true))
              .Append(ReportHtml.Kpi("local_shipping", "Active drivers", ReportHtml.Count(c.ActiveDrivers), c.ActiveDrivers, p.ActiveDrivers, true,
                    "Drivers with a delivery or attempt"))
              .Append("</div>");

            sb.Append("<div class='lc-kpis' style='margin-top:14px'>")
              .Append(ReportHtml.Stat("group", "Registered users", ReportHtml.Count(counts.Users),
                    ReportHtml.Count(counts.Customers) + " customers, " + ReportHtml.Count(counts.Staff) + " staff"))
              .Append(ReportHtml.Stat("badge", "Drivers", ReportHtml.Count(counts.Drivers),
                    ReportHtml.Count(counts.DriversAvailable) + " available, " + ReportHtml.Count(counts.DriversOnRoute) + " on route right now"))
              .Append(ReportHtml.Stat("hourglass_top", "Still in progress", ReportHtml.Count(s.Cohort.InProgress),
                    "Of the shipments booked in this period"))
              .Append("</div>");
            return sb.ToString();
        }

        private string Trends(PerformanceSnapshot s)
        {
            PeriodTotals c = s.Current, p = s.Previous;
            var sb = new StringBuilder();
            sb.Append(ReportHtml.SectionTitle("show_chart", "Trends"));
            sb.Append("<div class='lc-chart-grid'>");

            sb.Append(ReportHtml.ChartCard("chBooked", "Shipments booked per " + BucketWord(false),
                ReportHtml.Count(c.Booked) + " shipments booked, " + Delta.Describe(c.Booked, p.Booked) +
                ". Dashed line: the previous period, bucket by bucket.",
                "line", "number",
                new
                {
                    labels = s.Labels,
                    series = new object[]
                    {
                        new { label = "This period", data = s.Booked, tone = "primary" },
                        new { label = "Previous period", data = s.PrevBooked, tone = "neutral", dashed = true }
                    }
                },
                new[] { BucketWord(true), "This period", "Previous period" },
                Rows(s.Labels, s.Booked, s.PrevBooked, false), AllZero(s.Booked) && AllZero(s.PrevBooked), "lc-span-2"));

            sb.Append(ReportHtml.ChartCard("chOutcomes", "Successful deliveries vs failed attempts",
                ReportHtml.Count(c.Delivered) + " successful deliveries and " + ReportHtml.Count(c.Failed) +
                " failed attempts; success rate " + Delta.FormatRate(c.SuccessRate) + ".",
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

            sb.Append(ReportHtml.ChartCard("chRevenue", "Revenue: invoiced vs collected",
                ReportHtml.Money(c.Invoiced) + " invoiced and " + ReportHtml.Money(c.Collected) + " collected so far (" +
                Delta.FormatRate(c.CollectionRate) + ").",
                "bar", "currency",
                new
                {
                    labels = s.Labels,
                    series = new object[]
                    {
                        new { label = "Invoiced", data = s.Invoiced, tone = "primary" },
                        new { label = "Collected", data = s.Collected, tone = "success" }
                    }
                },
                new[] { BucketWord(true), "Invoiced", "Collected" },
                Rows(s.Labels, s.Invoiced, s.Collected, true), AllZero(s.Invoiced)));

            sb.Append(ReportHtml.ChartCard("chCustomers", "New customer accounts",
                ReportHtml.Count(c.NewCustomers) + " new customers, " + Delta.Describe(c.NewCustomers, p.NewCustomers) + ".",
                "bar", "number",
                new
                {
                    labels = s.Labels,
                    series = new object[] { new { label = "New customers", data = s.NewCustomers, tone = "info" } }
                },
                new[] { BucketWord(true), "New customers" },
                Rows(s.Labels, s.NewCustomers), AllZero(s.NewCustomers), "lc-span-2"));

            sb.Append("</div>");
            return sb.ToString();
        }

        private string Outcomes(PerformanceSnapshot s, IList<DriverStanding> drivers)
        {
            CohortSummary cohort = s.Cohort;
            var sb = new StringBuilder();
            sb.Append(ReportHtml.SectionTitle("account_tree", "Outcomes and bottlenecks"));
            sb.Append("<div class='lc-chart-grid'>");

            string[] outcomeLabels = { "Delivered", "In progress", "Failed" };
            decimal[] outcomeValues = { cohort.Delivered, cohort.InProgress, cohort.Failed };
            sb.Append(ReportHtml.ChartCard("chCohort", "What happened to this period's shipments",
                "Of " + ReportHtml.Count(cohort.Total) + " shipments booked: " + ReportHtml.Count(cohort.Delivered) + " delivered, " +
                ReportHtml.Count(cohort.InProgress) + " still in progress, " + ReportHtml.Count(cohort.Failed) + " failed.",
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
                new[] { "Destination", "Shipments" }, PairRows(destLabels, destValues), destLabels.Count == 0));

            sb.Append("<div class='lc-span-2'>")
              .Append(ReportHtml.Pipeline(s.Pipeline, cohort,
                    "How far the " + ReportHtml.Count(cohort.Total) + " shipments booked in this period have got. " +
                    "Each stage counts parcels that reached it or went further."))
              .Append("</div>");

            var active = new List<DriverStanding>();
            foreach (DriverStanding d in drivers) if (d.Delivered + d.Failed > 0) active.Add(d);
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
                    d.AverageRating == null ? "-" : d.AverageRating.Value.ToString("0.0") + " / 5"
                });
            }
            sb.Append(ReportHtml.ChartCard("chDrivers", "Deliveries by driver",
                active.Count == 0
                    ? "No driver completed or attempted a delivery in this period."
                    : "Successful deliveries and failed attempts for the " + active.Count + " busiest drivers.",
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

        private List<string> Attention(PerformanceSnapshot s, IList<DriverStanding> drivers)
        {
            var items = new List<string>();
            PeriodTotals c = s.Current, p = s.Previous;

            int flagged = 0;
            for (int i = 0; i < s.Labels.Length; i++)
            {
                decimal resolved = s.Delivered[i] + s.Failed[i];
                decimal? rate = Delta.Rate(s.Delivered[i], resolved);
                if (resolved < MinAttempts || rate == null || rate.Value >= SuccessTarget) continue;
                flagged++;
                if (flagged <= 6)
                {
                    items.Add(s.Labels[i] + ": delivery success " + Delta.FormatRate(rate) + " (" +
                              ReportHtml.Count(s.Delivered[i]) + " of " + ReportHtml.Count(resolved) +
                              " attempts) - below the " + SuccessTarget.ToString("0") + "% target.");
                }
            }
            if (flagged > 6) items.Add("... and " + (flagged - 6) + " more " + BucketWord(false) + "s below the success target.");

            if (c.Failed > p.Failed && p.Failed > 0)
            {
                items.Add("Failed delivery attempts rose from " + ReportHtml.Count(p.Failed) + " to " + ReportHtml.Count(c.Failed) +
                          " (" + Delta.Describe(c.Failed, p.Failed) + ").");
            }

            decimal unpaid = c.Invoiced - c.Collected;
            if (unpaid > 0)
            {
                items.Add(ReportHtml.Money(unpaid) + " of this period's invoices is still unpaid (" +
                          Delta.FormatRate(c.CollectionRate) + " collected so far).");
            }

            if (s.Cohort.Lost > 0 || s.Cohort.Damaged > 0)
            {
                items.Add(ReportHtml.Count(s.Cohort.Lost) + " parcel(s) flagged lost and " + ReportHtml.Count(s.Cohort.Damaged) +
                          " flagged damaged among this period's shipments - see the Lost & Damaged report.");
            }

            foreach (DriverStanding d in drivers)
            {
                int attempts = d.Delivered + d.Failed;
                if (attempts >= MinAttempts && d.SuccessRate != null && d.SuccessRate.Value < DriverAlert)
                {
                    items.Add(d.Name + ": " + Delta.FormatRate(d.SuccessRate) + " delivery success over " + attempts +
                              " attempts (below " + DriverAlert.ToString("0") + "%).");
                }
            }

            foreach (PipelineStage stage in s.Pipeline)
            {
                if (!stage.IsBiggestDrop) continue;
                int index = s.Pipeline.IndexOf(stage);
                int lost = s.Pipeline[index - 1].Value - stage.Value;
                items.Add("Biggest pipeline drop-off: " + ReportHtml.Count(lost) + " of this period's parcels have not reached \"" +
                          stage.Label + "\" yet.");
            }
            return items;
        }

        // ------------------------------------------------------------------ breakdown

        /// <summary>The per-bucket table - shown on the page, printed, and exported as CSV.</summary>
        private DataTable Breakdown(PerformanceSnapshot s)
        {
            var dt = new DataTable();
            dt.Columns.Add(BucketWord(true), typeof(string));
            dt.Columns.Add("Booked", typeof(decimal));
            dt.Columns.Add("Delivered", typeof(decimal));
            dt.Columns.Add("Failed attempts", typeof(decimal));
            dt.Columns.Add("Success rate", typeof(string));
            dt.Columns.Add("Invoiced (R)", typeof(decimal));
            dt.Columns.Add("Collected (R)", typeof(decimal));
            dt.Columns.Add("New customers", typeof(decimal));

            for (int i = 0; i < s.Labels.Length; i++)
            {
                dt.Rows.Add(s.Labels[i], s.Booked[i], s.Delivered[i], s.Failed[i],
                    Delta.FormatRate(Delta.Rate(s.Delivered[i], s.Delivered[i] + s.Failed[i])),
                    s.Invoiced[i], s.Collected[i], s.NewCustomers[i]);
            }
            PeriodTotals c = s.Current;
            dt.Rows.Add("Total", c.Booked, c.Delivered, c.Failed, Delta.FormatRate(c.SuccessRate),
                c.Invoiced, c.Collected, c.NewCustomers);
            return dt;
        }

        private string BreakdownHtml(PerformanceSnapshot s)
        {
            DataTable dt = Breakdown(s);
            var headers = new string[dt.Columns.Count];
            for (int i = 0; i < headers.Length; i++) headers[i] = dt.Columns[i].ColumnName;

            var rows = new List<string[]>();
            foreach (DataRow r in dt.Rows)
            {
                rows.Add(new[]
                {
                    Convert.ToString(r[0]),
                    ReportHtml.Count((decimal)r[1]), ReportHtml.Count((decimal)r[2]), ReportHtml.Count((decimal)r[3]),
                    Convert.ToString(r[4]),
                    ReportHtml.Money((decimal)r[5]), ReportHtml.Money((decimal)r[6]), ReportHtml.Count((decimal)r[7])
                });
            }
            return ReportHtml.Table(headers, rows, "Period-by-period breakdown");
        }

        // ------------------------------------------------------------------ helpers

        private string BucketWord(bool capital)
        {
            string w = period.Granularity == Granularity.Week ? "week" : period.Granularity == Granularity.Month ? "month" : "day";
            return capital ? char.ToUpperInvariant(w[0]) + w.Substring(1) : w;
        }

        private static string DescribeRange(ReportPeriod p)
        {
            return p.From.ToString("dd MMM yyyy") + " to " + p.To.ToString("dd MMM yyyy") +
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

        private static List<string[]> PairRows(IList<string> labels, IList<decimal> values)
        {
            var rows = new List<string[]>();
            for (int i = 0; i < labels.Count; i++) rows.Add(new[] { labels[i], ReportHtml.Count(values[i]) });
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
