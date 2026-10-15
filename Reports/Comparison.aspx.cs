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
    /// Comparison Report - "what changed, and where?". Two modes, chosen by tab:
    ///   Period vs period - any two date ranges side by side (Period A is the baseline): every
    ///     key figure with its change in words, per-day rates when the lengths differ, aligned
    ///     trend lines, volume and money bars, and the biggest movements.
    ///   Compare groups - one period split by service area, customer type or driver: share,
    ///     volumes, delivery success and the change against the previous period per group,
    ///     with plain-language highlights.
    /// Admin-only (it shows revenue). All figures come from AnalyticsService in the company
    /// scope; the comparison wording and URL handling live in Services/Comparison.cs (tested).
    /// </summary>
    public partial class Comparison : SecurePage
    {
        protected ReportHeader rptHeader;
        protected ReportFooter rptFooter;
        protected Panel pnlError, pnlPeriods, pnlGroups;
        protected Literal litError, litTabs, litBody;
        protected DropDownList ddlPair, ddlGroupBy, ddlPreset;
        protected TextBox txtAFrom, txtATo, txtBFrom, txtBTo, txtFrom, txtTo;
        protected Button btnApplyPeriods, btnExportPeriods, btnApplyGroups, btnExportGroups;
        protected HiddenField hfShownPeriods, hfShownFrom, hfShownTo;

        private const string PageUrl = "~/Reports/Comparison.aspx";

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Admin");
            if (IsPostBack) return;

            bool groups = string.Equals(Request.QueryString["mode"], "groups", StringComparison.OrdinalIgnoreCase);
            pnlPeriods.Visible = !groups;
            pnlGroups.Visible = groups;
            litTabs.Text = Tabs(groups);
            Title = groups ? "Comparison Report - Compare groups" : "Comparison Report - Period vs period";

            if (groups) LoadGroups();
            else LoadPeriods();
        }

        // ------------------------------------------------------------------ actions

        protected void btnApplyPeriods_Click(object sender, EventArgs e)
        {
            Response.Redirect(ResolveUrl(PageUrl) + PeriodsQuery());
        }

        protected void btnExportPeriods_Click(object sender, EventArgs e)
        {
            Response.Redirect(ResolveUrl(PageUrl) + PeriodsQuery() + "&export=csv");
        }

        protected void btnApplyGroups_Click(object sender, EventArgs e)
        {
            Response.Redirect(ResolveUrl(PageUrl) + GroupsQuery());
        }

        protected void btnExportGroups_Click(object sender, EventArgs e)
        {
            Response.Redirect(ResolveUrl(PageUrl) + GroupsQuery() + "&export=csv");
        }

        private string PeriodsQuery()
        {
            return ComparisonQuery.FromForm(ddlPair.SelectedValue, txtAFrom.Text, txtATo.Text, txtBFrom.Text, txtBTo.Text,
                hfShownPeriods.Value);
        }

        private string GroupsQuery()
        {
            return ReportQuery.FromForm(ddlPreset.SelectedValue, txtFrom.Text, txtTo.Text, ReportQuery.AutoGroup,
                       hfShownFrom.Value, hfShownTo.Value) +
                   "&mode=groups&by=" + HttpUtility.UrlEncode(DimensionKey(ParseDimension(ddlGroupBy.SelectedValue)));
        }

        private bool WantsExport()
        {
            return string.Equals(Request.QueryString["export"], "csv", StringComparison.OrdinalIgnoreCase);
        }

        private string Tabs(bool groups)
        {
            string periodsUrl = ResolveUrl(PageUrl) + "?mode=periods";
            string groupsUrl = ResolveUrl(PageUrl) + "?mode=groups";
            return Tab(periodsUrl, "date_range", "Period vs period", !groups) + Tab(groupsUrl, "category", "Compare groups", groups);
        }

        private static string Tab(string url, string icon, string text, bool current)
        {
            // The current tab also carries a tick, so it does not rely on its fill colour alone.
            return "<a class='lc-tab" + (current ? " lc-tab-current" : string.Empty) + "' href='" + HttpUtility.HtmlAttributeEncode(url) + "'" +
                   (current ? " aria-current='page'" : string.Empty) + "><span class='material-symbols-outlined' aria-hidden='true'>" +
                   (current ? "check" : icon) + "</span>" + HttpUtility.HtmlEncode(text) + "</a>";
        }

        private void ShowError(string message)
        {
            pnlError.Visible = true;
            litError.Text = HttpUtility.HtmlEncode(message);
        }

        /// <summary>Puts back what the user typed and marks the boxes, so a mistake can be corrected rather than retyped.</summary>
        private void MarkInvalid(TextBox box, string raw, bool invalid)
        {
            box.Text = raw ?? string.Empty;
            if (!invalid) return;
            box.Attributes["aria-invalid"] = "true";
            box.Attributes["aria-describedby"] = pnlError.ClientID;
        }

        // ------------------------------------------------------------------ period vs period

        private void LoadPeriods()
        {
            DateTime today = DateTime.Today;
            ReportPeriod a, b;
            string pairKey, problem;
            ComparisonQuery.Read(Request.QueryString, today, out a, out b, out pairKey, out problem);

            // Bucket both periods the same way so their trend lines can be laid over each other.
            Granularity g = ReportQuery.ParseGroup(null, Math.Max(a.Days, b.Days));
            a = new ReportPeriod(a.From, a.To, g);
            b = new ReportPeriod(b.From, b.To, g);

            // Per-day figures divide by the days that have actually started.
            int daysA = ComparisonInsights.ElapsedDays(a, today);
            int daysB = ComparisonInsights.ElapsedDays(b, today);
            bool sameLength = daysA == daysB;

            AnalyticsScope scope = AnalyticsScope.Company();
            PerformanceSnapshot sa = AnalyticsService.Build(scope, a);
            PerformanceSnapshot sb = AnalyticsService.Build(scope, b);
            List<ComparisonRow> rows = ComparisonInsights.PeriodRows(sa.Current, sb.Current, daysA, daysB);
            string headA = "Period A (" + ShortRange(a) + ")";
            string headB = "Period B (" + ShortRange(b) + ")";

            if (problem == null && WantsExport())
            {
                ReportExport.ToCsv(Response, PeriodsCsv(rows, a, b),
                    "Comparison_" + ReportQuery.IsoDate(a.From) + "_vs_" + ReportQuery.IsoDate(b.From) + ".csv");
                return;
            }

            foreach (string[] p in ComparisonQuery.PairOptions) ddlPair.Items.Add(new ListItem(p[1], p[0]));
            ddlPair.SelectedValue = pairKey;
            txtAFrom.Text = ReportQuery.IsoDate(a.From);
            txtATo.Text = ReportQuery.IsoDate(a.To);
            txtBFrom.Text = ReportQuery.IsoDate(b.From);
            txtBTo.Text = ReportQuery.IsoDate(b.To);
            hfShownPeriods.Value = ComparisonQuery.Shown(a, b);
            if (problem != null) ShowPeriodsError(problem);

            rptHeader.Description = "Two periods side by side. Period A is the baseline; every change is Period B measured against it.";
            rptHeader.AddFilter("Period A (baseline)", Range(a));
            rptHeader.AddFilter("Period B", Range(b));
            rptHeader.AddFilter("Scope", "Whole company");

            ChartAssets.Register(this);
            var html = new StringBuilder();

            if (!sameLength)
            {
                html.Append("<p class='lc-note'>Period A covers ").Append(Days(daysA)).Append(" and Period B ").Append(Days(daysB))
                    .Append(", so the totals are shown without a favourable or unfavourable verdict. Judge the per-day rows, rates and averages instead.</p>");
            }
            if (daysA < a.Days || daysB < b.Days)
            {
                html.Append("<p class='lc-note'>Days after today are not counted in the per-day figures.</p>");
            }
            if (a.From <= b.To && b.From <= a.To)
            {
                html.Append("<p class='lc-note'>The two periods overlap, so some activity is counted in both.</p>");
            }
            else if (a.From > b.To)
            {
                html.Append("<p class='lc-note'>Period A (the baseline) is later than Period B, so \"up\" means Period B is higher than the later period.</p>");
            }

            html.Append(ReportHtml.SectionTitle("trending_up", "Biggest movements"));
            html.Append(Highlights(ComparisonInsights.PeriodHighlights(rows, sameLength),
                "Nothing moved enough to highlight. Changes from very small numbers are left out; see the table below."));

            html.Append(ReportHtml.SectionTitle("compare_arrows", "Side by side"));
            var tableRows = new List<string[]>();
            foreach (ComparisonRow r in rows)
            {
                string measure = r.Detail == null ? r.Metric : r.Metric + " (" + r.Detail + ")";
                tableRows.Add(new[] { measure, r.Format(r.A), r.Format(r.B), r.Change });
            }
            html.Append("<div class='lc-chart-card lc-wrap-last'>")
                .Append(ReportHtml.Table(new[] { "Measure", headA, headB, "Change (B vs A)" }, tableRows,
                    headA + " against " + headB, false, true))
                .Append("</div>");

            string word = BucketWord(g);
            string alignment = " The lines are matched by position (" + word + " 1 of each period, then " + word +
                               " 2, ...), not by calendar date." +
                               (g == Granularity.Day ? string.Empty : " The first and last " + word + "s may be partial.");

            html.Append(ReportHtml.SectionTitle("show_chart", "Charts"));
            html.Append("<div class='lc-chart-grid'>");
            html.Append(OverlayChart("chBookedAB", "Shipments booked: Period B vs Period A", sa.Booked, sb.Booked, "number", g,
                ReportHtml.Count(sb.Current.Booked) + " booked in Period B against " + ReportHtml.Count(sa.Current.Booked) +
                " in Period A." + alignment));
            html.Append(OverlayChart("chRevenueAB", "Revenue invoiced: Period B vs Period A", sa.Invoiced, sb.Invoiced, "currency", g,
                ReportHtml.Money(sb.Current.Invoiced) + " invoiced in Period B against " + ReportHtml.Money(sa.Current.Invoiced) +
                " in Period A." + alignment));

            string[] volumeLabels = { "Booked", "Delivered", "Failed attempts" };
            html.Append(ReportHtml.ChartCard("chVolumesAB", "Volumes", "Shipments booked, delivered and failed attempts in each period.",
                "bar", "number",
                new
                {
                    labels = volumeLabels,
                    series = new object[]
                    {
                        new { label = "Period A", data = new[] { sa.Current.Booked, sa.Current.Delivered, sa.Current.Failed }, tone = "neutral", dashed = true },
                        new { label = "Period B", data = new[] { sb.Current.Booked, sb.Current.Delivered, sb.Current.Failed }, tone = "primary" }
                    }
                },
                new[] { "Measure", headA, headB },
                new List<string[]>
                {
                    new[] { "Booked", ReportHtml.Count(sa.Current.Booked), ReportHtml.Count(sb.Current.Booked) },
                    new[] { "Delivered", ReportHtml.Count(sa.Current.Delivered), ReportHtml.Count(sb.Current.Delivered) },
                    new[] { "Failed attempts", ReportHtml.Count(sa.Current.Failed), ReportHtml.Count(sb.Current.Failed) }
                },
                sa.Current.Booked + sb.Current.Booked + sa.Current.Delivered + sb.Current.Delivered == 0m));

            string[] moneyLabels = { "Invoiced", "Paid so far" };
            html.Append(ReportHtml.ChartCard("chMoneyAB", "Money",
                "Revenue invoiced in each period, and how much of it has been paid as at " +
                today.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) +
                ". The newer period's invoices have had less time to be paid, so compare the paid bars with care.",
                "bar", "currency",
                new
                {
                    labels = moneyLabels,
                    series = new object[]
                    {
                        new { label = "Period A", data = new[] { sa.Current.Invoiced, sa.Current.Collected }, tone = "neutral", dashed = true },
                        new { label = "Period B", data = new[] { sb.Current.Invoiced, sb.Current.Collected }, tone = "success" }
                    }
                },
                new[] { "Measure", headA, headB },
                new List<string[]>
                {
                    new[] { "Invoiced", ReportHtml.Money(sa.Current.Invoiced), ReportHtml.Money(sb.Current.Invoiced) },
                    new[] { "Paid so far", ReportHtml.Money(sa.Current.Collected), ReportHtml.Money(sb.Current.Collected) }
                },
                sa.Current.Invoiced + sb.Current.Invoiced == 0m));
            html.Append("</div>");

            litBody.Text = html.ToString();
        }

        private void ShowPeriodsError(string problem)
        {
            ShowError(problem + " Showing the last 30 days against the 30 before instead.");

            string[] keys = { "aFrom", "aTo", "bFrom", "bTo" };
            TextBox[] boxes = { txtAFrom, txtATo, txtBFrom, txtBTo };
            bool typed = false;
            foreach (string k in keys) if (!string.IsNullOrEmpty(Request.QueryString[k])) typed = true;
            if (!typed) return;

            bool aBad = problem.StartsWith("Period A", StringComparison.Ordinal);
            bool bBad = problem.StartsWith("Period B", StringComparison.Ordinal);
            for (int i = 0; i < keys.Length; i++)
            {
                string raw = Request.QueryString[keys[i]];
                DateTime d;
                bool invalid = !ReportQuery.TryDate(raw, out d) || (i < 2 ? aBad : bBad);
                MarkInvalid(boxes[i], raw, invalid);
            }
        }

        /// <summary>A line chart laying Period B over Period A bucket by bucket ("Day 1", "Day 2", ...).</summary>
        private static string OverlayChart(string id, string title, decimal[] a, decimal[] b, string format, Granularity g, string summary)
        {
            string bucket = Capital(BucketWord(g));
            int n = Math.Max(a.Length, b.Length);
            var labels = new string[n];
            var seriesA = new object[n];
            var seriesB = new object[n];
            var rows = new List<string[]>();
            bool any = false;
            for (int i = 0; i < n; i++)
            {
                labels[i] = bucket + " " + (i + 1).ToString(CultureInfo.InvariantCulture);
                seriesA[i] = i < a.Length ? (object)a[i] : null;
                seriesB[i] = i < b.Length ? (object)b[i] : null;
                if ((i < a.Length && a[i] != 0m) || (i < b.Length && b[i] != 0m)) any = true;
                rows.Add(new[]
                {
                    labels[i],
                    i < a.Length ? Value(a[i], format) : "-",
                    i < b.Length ? Value(b[i], format) : "-"
                });
            }

            return ReportHtml.ChartCard(id, title, summary, "line", format,
                new
                {
                    labels = labels,
                    series = new object[]
                    {
                        new { label = "Period B", data = seriesB, tone = "primary" },
                        new { label = "Period A (baseline)", data = seriesA, tone = "neutral", dashed = true }
                    }
                },
                new[] { bucket, "Period A", "Period B" }, rows, !any);
        }

        private static string Value(decimal v, string format)
        {
            return format == "currency" ? ReportHtml.Money(v) : ReportHtml.Count(v);
        }

        private static DataTable PeriodsCsv(IList<ComparisonRow> rows, ReportPeriod a, ReportPeriod b)
        {
            var dt = new DataTable();
            dt.Columns.Add("Measure", typeof(string));
            dt.Columns.Add("Unit", typeof(string));
            dt.Columns.Add("Period A " + ReportQuery.IsoDate(a.From) + " to " + ReportQuery.IsoDate(a.To), typeof(string));
            dt.Columns.Add("Period B " + ReportQuery.IsoDate(b.From) + " to " + ReportQuery.IsoDate(b.To), typeof(string));
            dt.Columns.Add("Change (B vs A)", typeof(string));
            dt.Columns.Add("Sample", typeof(string));
            foreach (ComparisonRow r in rows)
            {
                dt.Rows.Add(r.Metric, r.Unit, Plain(r.A, r.Kind), Plain(r.B, r.Kind), r.Change, r.Detail ?? string.Empty);
            }
            return dt;
        }

        private static string Plain(decimal? v, MetricKind kind)
        {
            if (v == null) return string.Empty;
            return kind == MetricKind.Count
                ? v.Value.ToString("0.##", CultureInfo.InvariantCulture)
                : v.Value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        // ------------------------------------------------------------------ compare groups

        private void LoadGroups()
        {
            string presetKey, problem;
            ReportPeriod period = ReportQuery.Read(Request.QueryString, DateTime.Today, "30d", out presetKey, out problem);
            ReportPeriod previous = period.Previous();
            CompareDimension by = ParseDimension(Request.QueryString["by"]);
            AnalyticsScope scope = AnalyticsScope.Company();
            var groups = new List<GroupFigures>(AnalyticsService.BreakdownBy(scope, by, period.From, period.ToExclusive));
            ComparisonInsights.AttachPrevious(groups, AnalyticsService.BreakdownBy(scope, by, previous.From, previous.ToExclusive));

            if (problem == null && WantsExport())
            {
                ReportExport.ToCsv(Response, GroupsCsv(groups, by, period, previous),
                    "Comparison_by_" + DimensionKey(by) + "_" + ReportQuery.IsoDate(period.From) + ".csv");
                return;
            }

            ddlGroupBy.Items.Add(new ListItem("Service area", DimensionKey(CompareDimension.ServiceArea)));
            ddlGroupBy.Items.Add(new ListItem("Customer type", DimensionKey(CompareDimension.ClientType)));
            ddlGroupBy.Items.Add(new ListItem("Driver", DimensionKey(CompareDimension.Driver)));
            ddlGroupBy.SelectedValue = DimensionKey(by);
            ReportQuery.BindPresets(ddlPreset);
            ddlPreset.SelectedValue = presetKey;
            txtFrom.Text = ReportQuery.IsoDate(period.From);
            txtTo.Text = ReportQuery.IsoDate(period.To);
            hfShownFrom.Value = txtFrom.Text;
            hfShownTo.Value = txtTo.Text;
            if (problem != null) ShowGroupsError(problem, presetKey);

            string word = DimensionWord(by);
            rptHeader.Description = "One period split by " + word + ": how much each " + word +
                                    " handles, how well it delivers, and how its bookings changed against the previous period.";
            rptHeader.AddFilter("Period", Range(period));
            rptHeader.AddFilter("Compared with", Range(previous));
            rptHeader.AddFilter("Compared by", Capital(word));
            rptHeader.AddFilter("Scope", "Whole company");

            ChartAssets.Register(this);
            var html = new StringBuilder();
            if (by == CompareDimension.Driver)
            {
                html.Append("<p class='lc-note'>Booked shipments count against the driver they are currently assigned to; ")
                    .Append("failed attempts count against the driver who made them.</p>");
            }
            html.Append(ReportHtml.SectionTitle("lightbulb", "Highlights"));
            html.Append(Highlights(ComparisonInsights.GroupHighlights(groups, word), "There was no activity in this period."));

            int totalBooked = 0;
            foreach (GroupFigures g in groups) totalBooked += g.Booked;

            var top = new List<GroupFigures>();
            foreach (GroupFigures g in groups) if (g.Booked + g.Delivered + g.Failed > 0 && top.Count < 10) top.Add(g);

            html.Append(ReportHtml.SectionTitle("bar_chart", "Charts"));
            html.Append("<div class='lc-chart-grid'>");

            var names = new List<string>();
            var booked = new List<decimal>();
            var delivered = new List<decimal>();
            var volumeRows = new List<string[]>();
            foreach (GroupFigures g in top)
            {
                names.Add(g.Name);
                booked.Add(g.Booked);
                delivered.Add(g.Delivered);
                volumeRows.Add(new[] { g.Name, ReportHtml.Count(g.Booked), ReportHtml.Count(g.Delivered) });
            }
            html.Append(ReportHtml.ChartCard("chGroupVolumes", "Shipments booked and delivered by " + word,
                top.Count == 0 ? "No activity in this period." : "The " + top.Count + " busiest " + word + "s.",
                "hbar", "number",
                new
                {
                    labels = names,
                    series = new object[]
                    {
                        new { label = "Booked", data = booked, tone = "primary" },
                        new { label = "Delivered", data = delivered, tone = "success" }
                    }
                },
                new[] { Capital(word), "Booked", "Delivered" }, volumeRows, top.Count == 0,
                top.Count > 6 ? "lc-span-2 lc-tall" : "lc-span-2"));

            // Share of bookings: the four largest groups, the rest as "Other" (a donut stays readable with five slices).
            var shareLabels = new List<string>();
            var shareValues = new List<decimal>();
            var shareTones = new List<string>();
            int other = 0;
            foreach (GroupFigures g in groups)
            {
                if (g.Booked == 0) continue;
                if (shareLabels.Count < 4) { shareLabels.Add(g.Name); shareValues.Add(g.Booked); shareTones.Add(null); }
                else other += g.Booked;
            }
            if (other > 0) { shareLabels.Add("Other"); shareValues.Add(other); shareTones.Add("neutral"); }
            var shareRows = new List<string[]>();
            for (int i = 0; i < shareLabels.Count; i++)
            {
                shareRows.Add(new[] { shareLabels[i], ReportHtml.Count(shareValues[i]), Delta.FormatRate(Delta.Rate(shareValues[i], totalBooked)) });
            }
            html.Append(ReportHtml.ChartCard("chGroupShare", "Share of shipments booked",
                totalBooked == 0 ? "No shipments were booked in this period." : ReportHtml.Count(totalBooked) + " shipments booked, by " + word + ".",
                "donut", "number",
                new { labels = shareLabels, values = shareValues, tones = shareTones },
                new[] { Capital(word), "Booked", "Share" }, shareRows, totalBooked == 0));

            var rateNames = new List<string>();
            var rates = new List<decimal>();
            var rateRows = new List<string[]>();
            foreach (GroupFigures g in groups)
            {
                if (g.Delivered + g.Failed < PerformanceInsights.MinAttempts || g.SuccessRate == null) continue;
                rateNames.Add(g.Name);
                rates.Add(g.SuccessRate.Value);
                rateRows.Add(new[] { g.Name, Delta.FormatRate(g.SuccessRate), ReportHtml.Count(g.Delivered + g.Failed) });
            }
            html.Append(ReportHtml.ChartCard("chGroupSuccess", "Delivery attempt success by " + word,
                rateNames.Count == 0
                    ? "Not enough delivery attempts in this period to compare success rates."
                    : "Share of delivery attempts that succeeded, for " + word + "s with at least " + PerformanceInsights.MinAttempts +
                      " attempts. The target is " + Target() + "; bars below it need attention.",
                "hbar", "percent",
                new { labels = rateNames, series = new object[] { new { label = "Attempt success", data = rates, tone = "success" } } },
                new[] { Capital(word), "Attempt success", "Attempts" }, rateRows, rateNames.Count == 0,
                rateNames.Count > 6 ? "lc-tall" : null));
            html.Append("</div>");

            html.Append(ReportHtml.SectionTitle("table_chart", "By " + word));
            html.Append("<div class='lc-chart-card'>").Append(GroupsTable(groups, by, totalBooked)).Append("</div>");
            litBody.Text = html.ToString();
        }

        private void ShowGroupsError(string problem, string presetKey)
        {
            ShowError(problem + " Showing " + ReportQuery.PresetLabel(presetKey).ToLowerInvariant() + " instead.");
            string rawFrom = Request.QueryString["from"], rawTo = Request.QueryString["to"];
            if (string.IsNullOrEmpty(rawFrom) && string.IsNullOrEmpty(rawTo)) return;
            MarkInvalid(txtFrom, rawFrom, true);
            MarkInvalid(txtTo, rawTo, true);
        }

        private static string GroupsTable(IList<GroupFigures> groups, CompareDimension by, int totalBooked)
        {
            bool money = by != CompareDimension.Driver;
            var headers = new List<string>
            {
                Capital(DimensionWord(by)),
                by == CompareDimension.Driver ? "Booked (currently assigned)" : "Booked",
                "Booked, previous period", "Change in booked", "Share of booked",
                "Delivered", "Failed attempts", "Attempt success (target " + Target() + ")"
            };
            if (money) headers.Add("Invoiced");

            var rows = new List<string[]>();
            int delivered = 0, failed = 0, previous = 0;
            decimal invoiced = 0m;
            foreach (GroupFigures g in groups)
            {
                delivered += g.Delivered;
                failed += g.Failed;
                previous += g.PreviousBooked ?? 0;
                invoiced += g.Invoiced ?? 0m;
                var row = new List<string>
                {
                    g.Name, ReportHtml.Count(g.Booked),
                    g.PreviousBooked == null ? "n/a" : ReportHtml.Count(g.PreviousBooked.Value),
                    g.PreviousBooked == null ? "n/a" : ShortChange(g.Booked, g.PreviousBooked.Value),
                    Delta.FormatRate(Delta.Rate(g.Booked, totalBooked)),
                    ReportHtml.Count(g.Delivered), ReportHtml.Count(g.Failed),
                    g.Delivered + g.Failed < PerformanceInsights.MinAttempts && g.Delivered + g.Failed > 0
                        ? "n/a (few attempts)" : Delta.FormatRate(g.SuccessRate)
                };
                if (money) row.Add(ReportHtml.Money(g.Invoiced ?? 0m));
                rows.Add(row.ToArray());
            }
            var total = new List<string>
            {
                "Total", ReportHtml.Count(totalBooked), ReportHtml.Count(previous), ShortChange(totalBooked, previous),
                totalBooked == 0 ? "n/a" : "100.0%",
                ReportHtml.Count(delivered), ReportHtml.Count(failed), Delta.FormatRate(Delta.Rate(delivered, delivered + failed))
            };
            if (money) total.Add(ReportHtml.Money(invoiced));
            rows.Add(total.ToArray());

            return ReportHtml.Table(headers.ToArray(), rows, "Figures by " + DimensionWord(by) + ", with bookings in the previous period", true, true);
        }

        /// <summary>A compact change for a table cell: "up 20.0%", "down 5.0%", "new", "stopped", "no change".</summary>
        private static string ShortChange(int current, int previous)
        {
            if (current == previous) return "no change";
            if (previous == 0) return "new";
            if (current == 0) return "stopped";
            decimal pct = Delta.PercentChange(current, previous).Value;
            if (pct == 0m) return "about the same";
            return (pct > 0 ? "up " : "down ") + Math.Abs(pct).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        private static string Target()
        {
            return PerformanceInsights.SuccessTarget.ToString("0", CultureInfo.InvariantCulture) + "%";
        }

        private static DataTable GroupsCsv(IList<GroupFigures> groups, CompareDimension by, ReportPeriod period, ReportPeriod previous)
        {
            var dt = new DataTable();
            dt.Columns.Add(Capital(DimensionWord(by)), typeof(string));
            dt.Columns.Add("Booked " + ReportQuery.IsoDate(period.From) + " to " + ReportQuery.IsoDate(period.To) + " (count)", typeof(string));
            dt.Columns.Add("Booked " + ReportQuery.IsoDate(previous.From) + " to " + ReportQuery.IsoDate(previous.To) + " (count)", typeof(string));
            dt.Columns.Add("Delivered (count)", typeof(string));
            dt.Columns.Add("Failed attempts (count)", typeof(string));
            dt.Columns.Add("Attempt success (percent)", typeof(string));
            dt.Columns.Add("Invoiced (rand)", typeof(string));
            foreach (GroupFigures g in groups)
            {
                dt.Rows.Add(g.Name, g.Booked.ToString(CultureInfo.InvariantCulture),
                    g.PreviousBooked == null ? string.Empty : g.PreviousBooked.Value.ToString(CultureInfo.InvariantCulture),
                    g.Delivered.ToString(CultureInfo.InvariantCulture), g.Failed.ToString(CultureInfo.InvariantCulture),
                    Plain(g.SuccessRate, MetricKind.Rate),
                    g.Invoiced == null ? string.Empty : g.Invoiced.Value.ToString("0.00", CultureInfo.InvariantCulture));
            }
            return dt;
        }

        // ------------------------------------------------------------------ helpers

        private static string Highlights(IList<string> items, string empty)
        {
            if (items == null || items.Count == 0)
            {
                return "<p class='lc-note'>" + HttpUtility.HtmlEncode(empty) + "</p>";
            }
            var sb = new StringBuilder("<ul class='lc-highlights' role='list'>");
            foreach (string item in items)
            {
                sb.Append("<li><span class='material-symbols-outlined' aria-hidden='true'>insights</span><span>")
                  .Append(HttpUtility.HtmlEncode(item)).Append("</span></li>");
            }
            return sb.Append("</ul>").ToString();
        }

        private static CompareDimension ParseDimension(string key)
        {
            switch ((key ?? string.Empty).ToLowerInvariant())
            {
                case "client": return CompareDimension.ClientType;
                case "driver": return CompareDimension.Driver;
                default: return CompareDimension.ServiceArea;
            }
        }

        private static string DimensionKey(CompareDimension d)
        {
            return d == CompareDimension.ClientType ? "client" : d == CompareDimension.Driver ? "driver" : "area";
        }

        private static string DimensionWord(CompareDimension d)
        {
            return d == CompareDimension.ClientType ? "customer type" : d == CompareDimension.Driver ? "driver" : "service area";
        }

        private static string BucketWord(Granularity g)
        {
            return g == Granularity.Week ? "week" : g == Granularity.Month ? "month" : "day";
        }

        private static string Capital(string s)
        {
            return string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        private static string Days(int n)
        {
            return n + (n == 1 ? " day" : " days");
        }

        private static string Range(ReportPeriod p)
        {
            return p.From.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) + " to " +
                   p.To.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) + " (" + Days(p.Days) + ")";
        }

        /// <summary>"01 Aug to 30 Aug 2026" - the year once when both ends share it.</summary>
        private static string ShortRange(ReportPeriod p)
        {
            string from = p.From.Year == p.To.Year
                ? p.From.ToString("dd MMM", CultureInfo.InvariantCulture)
                : p.From.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
            return from + " to " + p.To.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
        }
    }
}
