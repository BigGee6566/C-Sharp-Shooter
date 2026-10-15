using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web;

namespace ForteLogisticsApp.Services
{
    /// <summary>
    /// Server-rendered building blocks shared by every report and dashboard: section headings,
    /// KPI cards, chart cards, tables, the exceptions list and the delivery-pipeline schematic.
    /// Built here, like Site1.Master's navigation, so the markup and its accessibility rules
    /// live in one place. Every text value is HTML-encoded on the way out; numbers are
    /// formatted server-side.
    ///
    /// Accessibility rules applied throughout:
    ///   - a change is described in words ("up 12.5% ... (favourable)"), never by colour alone;
    ///   - every chart has a visible title, a one-line summary and a data table that starts
    ///     OPEN and is only collapsed by Scripts/lc-charts.js once the chart has drawn - so with
    ///     no JavaScript, or a failed chart, the numbers are still on the page;
    ///   - the pipeline schematic is real text, so it reads correctly with no JavaScript.
    ///
    /// Pages put this markup into Literals with EnableViewState="false" - it is rebuilt on
    /// every request and must not ride along in __VIEWSTATE.
    /// </summary>
    public static class ReportHtml
    {
        public static string Count(decimal value)
        {
            return value.ToString("N0", CultureInfo.CurrentCulture);
        }

        public static string Money(decimal value)
        {
            return "R " + value.ToString("N2", CultureInfo.CurrentCulture);
        }

        /// <summary>An h2 section heading with a decorative icon.</summary>
        public static string SectionTitle(string icon, string text)
        {
            return "<h2 class='lc-section-title'><span class='material-symbols-outlined' aria-hidden='true'>" +
                   Enc(icon) + "</span>" + Enc(text) + "</h2>";
        }

        /// <summary>
        /// A KPI with a period-over-period change line. The verdict ("favourable" /
        /// "unfavourable") is spelled out, because "up 12%" is good news for revenue and bad
        /// news for failed deliveries and colour alone must not be what tells them apart.
        /// </summary>
        public static string Kpi(string icon, string label, string value, decimal current, decimal previous,
            bool higherIsBetter, string hint = null)
        {
            int dir = Delta.Direction(current, previous);
            string tone = dir == 0 ? "flat" : ((dir > 0) == higherIsBetter ? "good" : "bad");
            string arrow = dir > 0 ? "arrow_upward" : dir < 0 ? "arrow_downward" : "remove";
            string verdict = tone == "good" ? " (favourable)" : tone == "bad" ? " (unfavourable)" : string.Empty;

            var sb = new StringBuilder();
            sb.Append("<div class='lc-kpi'>")
              .Append("<div class='lc-kpi-head'><span class='material-symbols-outlined' aria-hidden='true'>")
              .Append(Enc(icon)).Append("</span><span class='lc-kpi-label'>").Append(Enc(label)).Append("</span></div>")
              .Append("<div class='lc-kpi-value'>").Append(Enc(value)).Append("</div>")
              .Append("<div class='lc-kpi-delta lc-tone-").Append(tone).Append("'>")
              .Append("<span class='material-symbols-outlined' aria-hidden='true'>").Append(arrow).Append("</span>")
              .Append(Enc(Delta.Describe(current, previous) + verdict)).Append("</div>");
            if (!string.IsNullOrEmpty(hint))
            {
                sb.Append("<div class='lc-kpi-hint'>").Append(Enc(hint)).Append("</div>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        /// <summary>A rate KPI (e.g. delivery success) whose change is stated in percentage points.</summary>
        public static string KpiRate(string icon, string label, decimal? current, decimal? previous,
            bool higherIsBetter, string hint = null)
        {
            int dir = (current == null || previous == null) ? 0 : Delta.Direction(current.Value, previous.Value);
            string tone = dir == 0 ? "flat" : ((dir > 0) == higherIsBetter ? "good" : "bad");
            string arrow = dir > 0 ? "arrow_upward" : dir < 0 ? "arrow_downward" : "remove";
            string verdict = tone == "good" ? " (favourable)" : tone == "bad" ? " (unfavourable)" : string.Empty;

            var sb = new StringBuilder();
            sb.Append("<div class='lc-kpi'>")
              .Append("<div class='lc-kpi-head'><span class='material-symbols-outlined' aria-hidden='true'>")
              .Append(Enc(icon)).Append("</span><span class='lc-kpi-label'>").Append(Enc(label)).Append("</span></div>")
              .Append("<div class='lc-kpi-value'>").Append(Enc(Delta.FormatRate(current))).Append("</div>")
              .Append("<div class='lc-kpi-delta lc-tone-").Append(tone).Append("'>")
              .Append("<span class='material-symbols-outlined' aria-hidden='true'>").Append(arrow).Append("</span>")
              .Append(Enc(Delta.DescribePoints(current, previous) + verdict)).Append("</div>");
            if (!string.IsNullOrEmpty(hint))
            {
                sb.Append("<div class='lc-kpi-hint'>").Append(Enc(hint)).Append("</div>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        /// <summary>A point-in-time figure with no comparison (e.g. drivers on route right now).</summary>
        public static string Stat(string icon, string label, string value, string hint = null)
        {
            var sb = new StringBuilder();
            sb.Append("<div class='lc-kpi'>")
              .Append("<div class='lc-kpi-head'><span class='material-symbols-outlined' aria-hidden='true'>")
              .Append(Enc(icon)).Append("</span><span class='lc-kpi-label'>").Append(Enc(label)).Append("</span></div>")
              .Append("<div class='lc-kpi-value'>").Append(Enc(value)).Append("</div>");
            if (!string.IsNullOrEmpty(hint))
            {
                sb.Append("<div class='lc-kpi-hint'>").Append(Enc(hint)).Append("</div>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        /// <summary>
        /// A chart card: title, one-line summary, the canvas Scripts/lc-charts.js draws into,
        /// the inert JSON it reads, and the data table that is the non-visual fallback.
        /// <paramref name="kind"/>: line | bar | hbar | donut. <paramref name="format"/>:
        /// number | currency | percent. When <paramref name="isEmpty"/> the canvas is replaced
        /// by a plain "nothing in this period" message rather than an empty axis.
        /// </summary>
        public static string ChartCard(string id, string title, string summary, string kind, string format,
            object data, string[] tableHeaders, IList<string[]> tableRows, bool isEmpty,
            string extraClass = null, int headingLevel = 3)
        {
            string h = "h" + Math.Max(2, Math.Min(headingLevel, 4));
            string summaryId = id + "-summary";

            var sb = new StringBuilder();
            sb.Append("<figure class='lc-chart-card").Append(string.IsNullOrEmpty(extraClass) ? "" : " " + Enc(extraClass)).Append("'>")
              .Append("<figcaption><").Append(h).Append(" class='lc-chart-title'>").Append(Enc(title)).Append("</").Append(h).Append(">")
              .Append("<p class='lc-chart-summary' id='").Append(Enc(summaryId)).Append("'>").Append(Enc(summary)).Append("</p></figcaption>");

            if (isEmpty)
            {
                sb.Append("<div class='lc-chart-empty'><span class='material-symbols-outlined' aria-hidden='true'>insights</span>")
                  .Append("No activity in this period.</div>");
            }
            else
            {
                sb.Append("<div class='lc-chart-box lc-chart-").Append(Enc(kind)).Append("'>")
                  .Append("<canvas id='").Append(Enc(id)).Append("' role='img' aria-label='")
                  .Append(Enc(KindName(kind) + ": " + title)).Append("' aria-describedby='").Append(Enc(summaryId))
                  .Append("' data-lc-chart='").Append(Enc(kind))
                  .Append("' data-lc-source='").Append(Enc(id + "-data")).Append("' data-lc-format='")
                  .Append(Enc(format)).Append("'></canvas></div>")
                  .Append(ChartJson.ScriptBlock(id + "-data", data));
            }

            if (!isEmpty && tableHeaders != null && tableRows != null && tableRows.Count > 0)
            {
                sb.Append("<details class='lc-chart-table' open><summary>Show the data<span class='lc-sr-only'> for ")
                  .Append(Enc(title)).Append("</span></summary>")
                  .Append(Table(tableHeaders, tableRows, title)).Append("</details>");
            }
            sb.Append("</figure>");
            return sb.ToString();
        }

        /// <summary>
        /// An accessible table in the app's report-table style: columns scoped, an (off-screen)
        /// caption, and a horizontally scrollable region that keyboard users can focus.
        /// </summary>
        public static string Table(string[] headers, IList<string[]> rows, string caption = null)
        {
            string label = string.IsNullOrEmpty(caption) ? "Data table" : caption;
            var sb = new StringBuilder();
            sb.Append("<div class='lc-table-scroll' role='region' tabindex='0' aria-label='")
              .Append(Enc(label + " (table)")).Append("'><table class='report-table'>")
              .Append("<caption class='lc-sr-only'>").Append(Enc(label)).Append("</caption><thead><tr>");
            foreach (string h in headers) sb.Append("<th scope='col'>").Append(Enc(h)).Append("</th>");
            sb.Append("</tr></thead><tbody>");
            foreach (string[] row in rows)
            {
                sb.Append("<tr>");
                foreach (string cell in row) sb.Append("<td>").Append(Enc(cell)).Append("</td>");
                sb.Append("</tr>");
            }
            sb.Append("</tbody></table></div>");
            return sb.ToString();
        }

        /// <summary>
        /// The "flagged for attention" list. Each item gets a warning icon and an amber edge;
        /// with nothing to flag, a clear "all clear" line is shown instead of an empty box.
        /// </summary>
        public static string Exceptions(IList<string> items, string allClearText)
        {
            if (items == null || items.Count == 0)
            {
                return "<p class='lc-ok'><span class='material-symbols-outlined' aria-hidden='true'>check_circle</span>" +
                       Enc(allClearText) + "</p>";
            }

            var sb = new StringBuilder();
            sb.Append("<ul class='lc-exceptions' role='list'>");
            foreach (string item in items)
            {
                sb.Append("<li><span class='material-symbols-outlined' aria-hidden='true'>warning</span><span>")
                  .Append(Enc(item)).Append("</span></li>");
            }
            sb.Append("</ul>");
            return sb.ToString();
        }

        /// <summary>
        /// The delivery-pipeline schematic for a booked cohort: one box per stage, each showing
        /// its count, a bar scaled to the first stage, and the share of the previous stage that
        /// made it this far. The stage that loses the most parcels is outlined and labelled
        /// "Biggest drop-off", and parcels that ended Failed are shown as a branch off "Out for
        /// delivery". Both the stages and the failed branch come from the same cohort, so the
        /// branch percentage can never exceed 100%.
        /// </summary>
        public static string Pipeline(IList<PipelineStage> stages, CohortSummary cohort, string caption, int headingLevel = 3)
        {
            if (stages == null || stages.Count == 0) return string.Empty;
            int failed = cohort == null ? 0 : cohort.Failed;
            string h = "h" + Math.Max(2, Math.Min(headingLevel, 4));

            int first = Math.Max(stages[0].Value, 1);
            var sb = new StringBuilder();
            sb.Append("<figure class='lc-chart-card lc-pipeline-card'>")
              .Append("<figcaption><").Append(h).Append(" class='lc-chart-title'>Delivery pipeline</").Append(h).Append(">")
              .Append("<p class='lc-chart-summary'>").Append(Enc(caption)).Append("</p></figcaption>")
              .Append("<ol class='lc-pipeline' role='list'>");

            for (int i = 0; i < stages.Count; i++)
            {
                PipelineStage s = stages[i];
                int width = (int)Math.Round(s.Value * 100.0 / first);
                if (s.Value > 0 && width < 2) width = 2;

                sb.Append("<li class='lc-pipe-stage").Append(s.IsBiggestDrop ? " lc-pipe-drop" : "").Append("'>");
                if (i > 0)
                {
                    sb.Append("<span class='lc-pipe-arrow material-symbols-outlined' aria-hidden='true'>arrow_forward</span>");
                }
                sb.Append("<div class='lc-pipe-box'>")
                  .Append("<div class='lc-pipe-label'>").Append(Enc(s.Label)).Append("</div>")
                  .Append("<div class='lc-pipe-value'>").Append(Count(s.Value)).Append("</div>")
                  .Append("<div class='lc-pipe-bar' aria-hidden='true'><span style='width:").Append(width).Append("%'></span></div>")
                  .Append("<div class='lc-pipe-conv'>");
                if (i == 0) sb.Append("Starting point");
                else if (s.ConversionFromPrevious == null) sb.Append("No parcels at the previous stage");
                else sb.Append(Enc(Delta.FormatRate(s.ConversionFromPrevious))).Append(" of previous stage");
                sb.Append("</div>");
                if (s.IsBiggestDrop)
                {
                    sb.Append("<div class='lc-pipe-flag'><span class='material-symbols-outlined' aria-hidden='true'>warning</span>Biggest drop-off</div>");
                }
                sb.Append("</div></li>");
            }
            sb.Append("</ol>");

            int outForDelivery = stages.Count >= 4 ? stages[3].Value : 0;
            sb.Append("<p class='lc-pipe-branch'><span class='material-symbols-outlined' aria-hidden='true'>call_split</span>")
              .Append("Ended as failed deliveries: <strong>").Append(Count(failed)).Append("</strong>");
            if (outForDelivery > 0)
            {
                sb.Append(" (").Append(Enc(Delta.FormatRate(Delta.Rate(failed, outForDelivery)))).Append(" of parcels sent out for delivery)");
            }
            sb.Append("</p></figure>");
            return sb.ToString();
        }

        private static string KindName(string kind)
        {
            switch (kind)
            {
                case "line": return "Line chart";
                case "bar": return "Bar chart";
                case "hbar": return "Horizontal bar chart";
                case "donut": return "Donut chart";
                default: return "Chart";
            }
        }

        private static string Enc(string value)
        {
            return HttpUtility.HtmlEncode(value ?? string.Empty);
        }
    }
}
