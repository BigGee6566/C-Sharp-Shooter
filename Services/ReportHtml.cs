using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web;

namespace ForteLogisticsApp.Services
{
    /// <summary>
    /// Server-rendered building blocks shared by every report and dashboard: section headings,
    /// KPI cards, chart cards, tables, the attention list and the delivery-pipeline schematic.
    /// Built here, like Site1.Master's navigation, so the markup and its accessibility rules
    /// live in one place. Every text value is HTML-encoded on the way out.
    ///
    /// Accessibility rules applied throughout:
    ///   - a change is described in words ("up 12.5% ... (favourable)"), never by colour alone;
    ///   - every chart has a visible title, a one-line summary and a data table that starts
    ///     OPEN and is only collapsed by Scripts/lc-charts.js once the chart has drawn - so with
    ///     no JavaScript, or a failed chart, the numbers are still on the page;
    ///   - tables give each row a row header, so a screen reader names the row it is reading;
    ///   - the pipeline schematic is real text, so it reads correctly with no JavaScript.
    ///
    /// Numbers use ONE fixed format on every machine (space thousands separator, "." decimal
    /// point): a report must read the same on any PC, and Windows versions disagree about the
    /// en-ZA decimal separator. Pages put this markup into Literals with EnableViewState="false".
    /// </summary>
    public static class ReportHtml
    {
        /// <summary>The report number format: "1 234 567.89" (non-breaking space between thousands).</summary>
        public static readonly NumberFormatInfo Numbers = CreateNumbers();

        private static NumberFormatInfo CreateNumbers()
        {
            var nf = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            nf.NumberGroupSeparator = ((char)0xA0).ToString();
            nf.NumberDecimalSeparator = ".";
            return nf;
        }

        public static string Count(decimal value)
        {
            return value.ToString("N0", Numbers);
        }

        /// <summary>"R 58 924.50" - the space after R is non-breaking too, so an amount never wraps.</summary>
        public static string Money(decimal value)
        {
            return "R" + Numbers.NumberGroupSeparator + value.ToString("N2", Numbers);
        }

        /// <summary>A duration in hours, shown in hours under two days and in days after that.</summary>
        public static string Duration(decimal? hours)
        {
            if (hours == null) return "n/a";
            decimal h = hours.Value;
            return h < 48m
                ? h.ToString("0.0", Numbers) + " hours"
                : (h / 24m).ToString("0.0", Numbers) + " days";
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
        /// news for failed deliveries and colour alone must not tell them apart. There is no
        /// verdict when there was nothing to compare against, or when the change rounds to 0.
        /// </summary>
        public static string Kpi(string icon, string label, string value, decimal current, decimal previous,
            bool higherIsBetter, string hint = null)
        {
            int dir = Delta.Direction(current, previous);
            bool noBase = previous == 0m && current != 0m;
            decimal? pct = Delta.PercentChange(current, previous);
            bool negligible = pct != null && pct.Value == 0m;

            string tone = dir == 0 || noBase || negligible ? "flat" : ((dir > 0) == higherIsBetter ? "good" : "bad");
            string arrow = noBase ? "fiber_new" : negligible || dir == 0 ? "remove" : dir > 0 ? "arrow_upward" : "arrow_downward";
            return Card(icon, label, value, tone, arrow, Delta.Describe(current, previous) + Verdict(tone), hint);
        }

        /// <summary>A rate KPI (e.g. delivery success) whose change is stated in percentage points.</summary>
        public static string KpiRate(string icon, string label, decimal? current, decimal? previous,
            bool higherIsBetter, string hint = null)
        {
            decimal? diff = (current == null || previous == null)
                ? (decimal?)null
                : Math.Round(current.Value - previous.Value, 1, MidpointRounding.AwayFromZero);
            int dir = diff == null || diff.Value == 0m ? 0 : diff.Value > 0 ? 1 : -1;
            string tone = dir == 0 ? "flat" : ((dir > 0) == higherIsBetter ? "good" : "bad");
            string arrow = dir > 0 ? "arrow_upward" : dir < 0 ? "arrow_downward" : "remove";
            return Card(icon, label, Delta.FormatRate(current), tone, arrow,
                Delta.DescribePoints(current, previous) + Verdict(tone), hint);
        }

        /// <summary>A KPI for a quantity where any change is described by a caller-supplied phrase.</summary>
        public static string KpiCustom(string icon, string label, string value, int direction, bool higherIsBetter,
            string changeText, string hint = null)
        {
            string tone = direction == 0 ? "flat" : ((direction > 0) == higherIsBetter ? "good" : "bad");
            string arrow = direction > 0 ? "arrow_upward" : direction < 0 ? "arrow_downward" : "remove";
            return Card(icon, label, value, tone, arrow, changeText + Verdict(tone), hint);
        }

        /// <summary>A point-in-time or uncompared figure (e.g. drivers on route right now).</summary>
        public static string Stat(string icon, string label, string value, string hint = null)
        {
            var sb = new StringBuilder();
            sb.Append("<div class='lc-kpi' role='group' aria-label='").Append(Enc(label)).Append("'>")
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

        private static string Card(string icon, string label, string value, string tone, string arrow,
            string changeText, string hint)
        {
            var sb = new StringBuilder();
            sb.Append("<div class='lc-kpi' role='group' aria-label='").Append(Enc(label)).Append("'>")
              .Append("<div class='lc-kpi-head'><span class='material-symbols-outlined' aria-hidden='true'>")
              .Append(Enc(icon)).Append("</span><span class='lc-kpi-label'>").Append(Enc(label)).Append("</span></div>")
              .Append("<div class='lc-kpi-value'>").Append(Enc(value)).Append("</div>")
              .Append("<div class='lc-kpi-delta lc-tone-").Append(tone).Append("'>")
              .Append("<span class='material-symbols-outlined' aria-hidden='true'>").Append(arrow).Append("</span>")
              .Append(Enc(changeText)).Append("</div>");
            if (!string.IsNullOrEmpty(hint))
            {
                sb.Append("<div class='lc-kpi-hint'>").Append(Enc(hint)).Append("</div>");
            }
            sb.Append("</div>");
            return sb.ToString();
        }

        private static string Verdict(string tone)
        {
            return tone == "good" ? " (favourable)" : tone == "bad" ? " (unfavourable)" : string.Empty;
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
        /// A detail table showing at most <paramref name="maxRows"/> rows, with a note naming how
        /// many were left out (the CSV export has them all). <paramref name="totalRows"/> is the true
        /// row count when the caller fetched only the first rows; otherwise rows.Count is used.
        /// </summary>
        public static string DetailTable(string[] headers, IList<string[]> rows, string caption, string emptyText, bool numeric,
            int maxRows, int? totalRows = null)
        {
            int total = Math.Max(totalRows ?? rows.Count, rows.Count);
            if (total == 0)
            {
                return "<p class='lc-note'>" + Enc(emptyText) + "</p>";
            }
            IList<string[]> shown = rows;
            string note = string.Empty;
            if (rows.Count > maxRows)
            {
                var first = new List<string[]>();
                for (int i = 0; i < maxRows; i++) first.Add(rows[i]);
                shown = first;
            }
            if (total > shown.Count)
            {
                note = "<p class='lc-note'>Showing the first " + Count(shown.Count) + " of " + Count(total) +
                       " rows. Export the CSV for all of them.</p>";
            }
            return note + "<div class='lc-chart-card'>" + Table(headers, shown, caption, false, numeric) + "</div>";
        }

        /// <summary>
        /// A donut of the five largest categories with the rest as a grey "Other" slice (a donut
        /// stops being readable beyond about six slices). <paramref name="format"/>: number | currency.
        /// </summary>
        public static string CategoryDonut(string id, string title, string summary, IDictionary<string, decimal> values,
            string column, string format = "number", string extraClass = null)
        {
            List<KeyValuePair<string, decimal>> sorted = Largest(values);
            var labels = new List<string>();
            var data = new List<decimal>();
            var tones = new List<string>();
            decimal total = 0m, other = 0m;
            foreach (KeyValuePair<string, decimal> kv in sorted) total += kv.Value;
            foreach (KeyValuePair<string, decimal> kv in sorted)
            {
                if (labels.Count < 5) { labels.Add(kv.Key); data.Add(kv.Value); tones.Add(null); }
                else other += kv.Value;
            }
            if (other > 0m) { labels.Add("Other"); data.Add(other); tones.Add("neutral"); }

            var rows = new List<string[]>();
            for (int i = 0; i < labels.Count; i++)
            {
                rows.Add(new[] { labels[i], FormatValue(data[i], format), Delta.FormatRate(Delta.Rate(data[i], total)) });
            }
            return ChartCard(id, title, summary, "donut", format, new { labels = labels, values = data, tones = tones },
                new[] { column, format == "currency" ? "Amount" : "Count", "Share" }, rows, total == 0m, extraClass);
        }

        /// <summary>A horizontal bar chart of the <paramref name="top"/> largest categories, largest first.</summary>
        public static string CategoryBars(string id, string title, string summary, IDictionary<string, decimal> values,
            string column, int top, string format = "number", string tone = "primary", string extraClass = null)
        {
            var labels = new List<string>();
            var data = new List<decimal>();
            var rows = new List<string[]>();
            string valueName = format == "currency" ? "Amount" : "Count";
            foreach (KeyValuePair<string, decimal> kv in Largest(values))
            {
                if (labels.Count >= top || kv.Value == 0m) break;
                labels.Add(kv.Key);
                data.Add(kv.Value);
                rows.Add(new[] { kv.Key, FormatValue(kv.Value, format) });
            }
            return ChartCard(id, title, summary, "hbar", format,
                new { labels = labels, series = new object[] { new { label = valueName, data = data, tone = tone } } },
                new[] { column, valueName }, rows, labels.Count == 0, extraClass);
        }

        private static List<KeyValuePair<string, decimal>> Largest(IDictionary<string, decimal> values)
        {
            var sorted = new List<KeyValuePair<string, decimal>>(values ?? new Dictionary<string, decimal>());
            sorted.Sort((a, b) => b.Value != a.Value ? b.Value.CompareTo(a.Value) : string.CompareOrdinal(a.Key, b.Key));
            return sorted;
        }

        private static string FormatValue(decimal v, string format)
        {
            return format == "currency" ? Money(v) : format == "percent" ? Delta.FormatRate(v) : Count(v);
        }

        /// <summary>
        /// An accessible table in the app's report-table style: an (off-screen) caption, a row
        /// header in the first column of every row, and a horizontally scrollable region that
        /// keyboard users can focus. <paramref name="lastRowIsTotal"/> moves the final row into
        /// a bold, ruled footer; <paramref name="numeric"/> right-aligns every column after the
        /// first so figures line up.
        /// </summary>
        public static string Table(string[] headers, IList<string[]> rows, string caption = null,
            bool lastRowIsTotal = false, bool numeric = false)
        {
            string label = string.IsNullOrEmpty(caption) ? "Data table" : caption;
            int bodyRows = lastRowIsTotal && rows.Count > 0 ? rows.Count - 1 : rows.Count;

            var sb = new StringBuilder();
            sb.Append("<div class='lc-table-scroll' role='region' tabindex='0' aria-label='")
              .Append(Enc(label + " (table)")).Append("'><table class='report-table")
              .Append(numeric ? " lc-table-num" : "").Append("'>")
              .Append("<caption class='lc-sr-only'>").Append(Enc(label)).Append("</caption><thead><tr>");
            foreach (string h in headers) sb.Append("<th scope='col'>").Append(Enc(h)).Append("</th>");
            sb.Append("</tr></thead><tbody>");
            for (int i = 0; i < bodyRows; i++)
            {
                AppendRow(sb, rows[i], null);
            }
            sb.Append("</tbody>");

            if (bodyRows < rows.Count)
            {
                sb.Append("<tfoot>");
                AppendRow(sb, rows[rows.Count - 1], "lc-total");
                sb.Append("</tfoot>");
            }
            sb.Append("</table></div>");
            return sb.ToString();
        }

        private static void AppendRow(StringBuilder sb, string[] row, string cssClass)
        {
            sb.Append(cssClass == null ? "<tr>" : "<tr class='" + cssClass + "'>");
            for (int c = 0; c < row.Length; c++)
            {
                if (c == 0) sb.Append("<th scope='row'>").Append(Enc(row[c])).Append("</th>");
                else sb.Append("<td>").Append(Enc(row[c])).Append("</td>");
            }
            sb.Append("</tr>");
        }

        /// <summary>
        /// The "Needs attention" list. Each item states its priority in words, what was found,
        /// and where to go to act on it. <paramref name="resolveUrl"/> turns an app-relative
        /// "~/..." link into a real URL (the page's ResolveUrl). With nothing to flag, a clear
        /// "all clear" line is shown instead of an empty box.
        /// </summary>
        public static string Attention(IList<AttentionItem> items, string allClearText, Func<string, string> resolveUrl)
        {
            if (items == null || items.Count == 0)
            {
                return "<p class='lc-ok'><span class='material-symbols-outlined' aria-hidden='true'>check_circle</span>" +
                       Enc(allClearText) + "</p>";
            }

            var sb = new StringBuilder();
            sb.Append("<ul class='lc-exceptions' role='list'>");
            foreach (AttentionItem item in items)
            {
                string level = item.Severity == AttentionSeverity.High ? "high"
                             : item.Severity == AttentionSeverity.Medium ? "medium" : "info";
                string word = item.Severity == AttentionSeverity.High ? "Act now"
                            : item.Severity == AttentionSeverity.Medium ? "Check" : "Note";
                string icon = item.Severity == AttentionSeverity.High ? "error" : item.Severity == AttentionSeverity.Medium ? "warning" : "info";

                sb.Append("<li class='lc-att-").Append(level).Append("'>")
                  .Append("<span class='material-symbols-outlined' aria-hidden='true'>").Append(icon).Append("</span>")
                  .Append("<div><strong class='lc-att-level'>").Append(word).Append(":</strong> ")
                  .Append(Enc(item.Text));
                if (!string.IsNullOrEmpty(item.Href) && resolveUrl != null)
                {
                    sb.Append(" <a class='fcl-link lc-att-link' href='").Append(Enc(resolveUrl(item.Href))).Append("'>")
                      .Append(Enc(string.IsNullOrEmpty(item.LinkText) ? "Open" : item.LinkText))
                      .Append("<span class='material-symbols-outlined' aria-hidden='true'>arrow_forward</span></a>");
                }
                sb.Append("</div></li>");
            }
            sb.Append("</ul>");
            return sb.ToString();
        }

        /// <summary>Plain-text list version of <see cref="Attention"/> (kept for simple callers).</summary>
        public static string Exceptions(IList<string> items, string allClearText)
        {
            var converted = new List<AttentionItem>();
            if (items != null)
            {
                foreach (string s in items) converted.Add(new AttentionItem(AttentionSeverity.Medium, s));
            }
            return Attention(converted, allClearText, null);
        }

        /// <summary>
        /// The delivery-pipeline schematic for a booked cohort: one box per stage, each showing
        /// its count, a bar scaled to the first stage, and the share of the previous stage that
        /// made it this far. The stage that loses the most parcels is outlined and labelled
        /// "Biggest drop-off", and parcels that ended Failed are shown as a branch off "Out for
        /// delivery". Both come from the same cohort, so the branch can never exceed 100%.
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
              .Append("Parcels that ended as failed deliveries: <strong>").Append(Count(failed)).Append("</strong>");
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

    public enum AttentionSeverity
    {
        High = 0,
        Medium = 1,
        Info = 2
    }

    /// <summary>One "Needs attention" finding: what was found, how urgent, and where to act on it.</summary>
    public sealed class AttentionItem
    {
        public AttentionSeverity Severity;
        public string Text;
        /// <summary>App-relative link ("~/Finance/Invoices.aspx") to the page where it can be acted on.</summary>
        public string Href;
        public string LinkText;

        public AttentionItem(AttentionSeverity severity, string text, string href = null, string linkText = null)
        {
            Severity = severity;
            Text = text;
            Href = href;
            LinkText = linkText;
        }
    }
}
