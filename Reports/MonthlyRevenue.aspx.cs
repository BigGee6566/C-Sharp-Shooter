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
    /// Revenue Report - for a month (or any period): money invoiced and how much of it has been
    /// paid, against the previous period; what is still owed and how much of that is overdue;
    /// the mix of invoice statuses, the biggest customers, and every invoice.
    ///
    /// Admin-only: with Finance folded into Staff, revenue would otherwise be visible to every
    /// warehouse and dispatch user. "Invoiced" excludes cancelled invoices and is counted on the
    /// issue date, exactly as on every other report (AnalyticsService).
    /// </summary>
    public partial class MonthlyRevenue : PeriodReportPage
    {
        protected override string PageUrl
        {
            get { return "~/Reports/MonthlyRevenue.aspx"; }
        }

        protected override string DefaultPreset
        {
            get { return "thismonth"; }
        }

        protected override void Authorize()
        {
            RequireRole("Admin");
        }

        // Newest first; the screen asks for one row more than it shows, the CSV for all of them.
        private const string InvoicesSql =
            "SELECT TOP (@n) p.InvoiceNumber, ISNULL(u.FullName, 'Unknown customer') AS Client, ISNULL(s.TrackingNumber, '') AS Waybill, " +
            "       p.Amount, p.PaymentStatus, p.IssuedDate, ISNULL(p.DueDate, DATEADD(day, @terms, p.IssuedDate)) AS DueOn " +
            "FROM dbo.[Payment] p " +
            "LEFT JOIN dbo.[User] u ON u.UserID = p.ClientID " +
            "LEFT JOIN dbo.[Shipment] s ON s.ShipmentID = p.ShipmentID " +
            "WHERE p.IssuedDate >= @from AND p.IssuedDate < @to " +
            "ORDER BY p.IssuedDate DESC, p.InvoiceNumber DESC";

        private const string StatusSql =
            "SELECT p.PaymentStatus, COUNT(*) AS N, SUM(p.Amount) AS V FROM dbo.[Payment] p " +
            "WHERE p.IssuedDate >= @from AND p.IssuedDate < @to GROUP BY p.PaymentStatus";

        private static string StatusLabel(string status)
        {
            switch ((status ?? string.Empty).ToLowerInvariant())
            {
                case "paid": return "Paid";
                case "unpaid": return "Unpaid";
                case "pending": return "Payment pending";
                case "failed": return "Payment failed";
                case "cancelled": return "Cancelled";
                default: return string.IsNullOrEmpty(status) ? "Unknown" : status;
            }
        }

        private DataTable LoadInvoices(int top)
        {
            return Db.Query(InvoicesSql, "@n", top, "@terms", AnalyticsService.DefaultPaymentTermsDays,
                "@from", Period.From, "@to", Period.ToExclusive);
        }

        /// <summary>
        /// Invoices issued in a period that were not cancelled; the value of those by status (for the
        /// donut, so it adds up to "Revenue invoiced"); and the count of every invoice by raw status.
        /// </summary>
        private static void StatusTotals(DateTime from, DateTime toExclusive, out int invoices, out Dictionary<string, decimal> amounts,
            out Dictionary<string, int> counts)
        {
            invoices = 0;
            amounts = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in Db.Query(StatusSql, "@from", from, "@to", toExclusive).Rows)
            {
                string status = Db.ToStr(r["PaymentStatus"]);
                int n = Db.ToInt(r["N"]);
                counts[status] = n;
                if (string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase)) continue;
                amounts[StatusLabel(status)] = Db.ToDecimal(r["V"]);
                invoices += n;
            }
        }

        protected override DataTable ExportTable(out string fileName)
        {
            fileName = "Revenue_" + ReportQuery.IsoDate(Period.From) + "_to_" + ReportQuery.IsoDate(Period.To) + ".csv";
            var dt = new DataTable();
            foreach (string c in new[] { "Invoice", "Customer", "Waybill", "Amount (rand)", "Status", "Issued", "Due" }) dt.Columns.Add(c, typeof(string));
            foreach (DataRow r in LoadInvoices(int.MaxValue).Rows)
            {
                dt.Rows.Add(Db.ToStr(r["InvoiceNumber"]), Db.ToStr(r["Client"]), Db.ToStr(r["Waybill"]),
                    Db.ToDecimal(r["Amount"]).ToString("0.00", CultureInfo.InvariantCulture), StatusLabel(Db.ToStr(r["PaymentStatus"])),
                    Day(r["IssuedDate"], "yyyy-MM-dd"), Day(r["DueOn"], "yyyy-MM-dd"));
            }
            return dt;
        }

        protected override string RenderBody()
        {
            AnalyticsScope scope = AnalyticsScope.Company();
            DateTime now = DateTime.Now;
            PerformanceSnapshot s = AnalyticsService.Build(scope, Period);
            Receivables owed = AnalyticsService.ReceivablesFor(scope, Period.From, Period.ToExclusive, now);
            IList<NamedValue> customers = AnalyticsService.TopCustomers(scope, Period.From, Period.ToExclusive, 8);
            ReportPeriod previous = Period.Previous();

            int invoices, invoicesBefore;
            Dictionary<string, decimal> amounts, amountsBefore;
            Dictionary<string, int> counts, countsBefore;
            StatusTotals(Period.From, Period.ToExclusive, out invoices, out amounts, out counts);
            StatusTotals(previous.From, previous.ToExclusive, out invoicesBefore, out amountsBefore, out countsBefore);
            bool compare = s.Previous.Invoiced > 0m || invoicesBefore > 0;

            rptHeader.Description = "Money invoiced in the chosen period and how much of it has been paid, what is still owed and overdue, " +
                                    "and which customers bring in the most.";
            AddPeriodFilters(compare);
            rptHeader.AddFilter("Paid and overdue as at", now.ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture));
            rptHeader.AddFilter("Scope", "Whole company");

            var html = new StringBuilder();
            html.Append(ReportHtml.SectionTitle("payments", "Key figures"));
            html.Append("<div class='lc-kpis'>");
            html.Append(compare
                ? ReportHtml.Kpi("receipt_long", "Revenue invoiced", ReportHtml.Money(s.Current.Invoiced), s.Current.Invoiced, s.Previous.Invoiced, true,
                    "Cancelled invoices excluded")
                : ReportHtml.Stat("receipt_long", "Revenue invoiced", ReportHtml.Money(s.Current.Invoiced), "Cancelled invoices excluded"));
            html.Append(ReportHtml.Stat("account_balance_wallet", "Paid so far", ReportHtml.Money(s.Current.Collected),
                "Of this period's invoices, as at today"));
            html.Append(ReportHtml.Stat("percent", "Share paid", Delta.FormatRate(s.Current.CollectionRate),
                "Paid so far / invoiced"));
            html.Append(compare
                ? ReportHtml.Kpi("description", "Invoices issued", ReportHtml.Count(invoices), invoices, invoicesBefore, true)
                : ReportHtml.Stat("description", "Invoices issued", ReportHtml.Count(invoices)));
            html.Append(ReportHtml.Stat("functions", "Average invoice",
                invoices == 0 ? "n/a" : ReportHtml.Money(Math.Round(s.Current.Invoiced / invoices, 2, MidpointRounding.AwayFromZero))));
            html.Append(ReportHtml.Stat("hourglass_top", "Still owed", ReportHtml.Money(owed.Outstanding),
                ReportHtml.Money(owed.NotYetDue) + " not yet due"));
            html.Append(ReportHtml.Stat("assignment_late", "Overdue", ReportHtml.Money(owed.Overdue),
                owed.OverdueInvoices == 0 ? "Nothing overdue"
                    : PerformanceInsights.Plural(owed.OverdueInvoices, "invoice") + ", oldest " + PerformanceInsights.Plural(owed.OldestOverdueDays, "day") + " late"));
            html.Append("</div>");

            // ---- Charts
            string bucket = Period.Granularity == Granularity.Week ? "week" : Period.Granularity == Granularity.Month ? "month" : "day";
            html.Append(ReportHtml.SectionTitle("show_chart", "Charts"));
            html.Append("<div class='lc-chart-grid'>");

            var trendRows = new List<string[]>();
            for (int i = 0; i < s.Labels.Length; i++)
            {
                trendRows.Add(new[] { s.Labels[i], ReportHtml.Money(s.Invoiced[i]), ReportHtml.Money(s.Collected[i]) });
            }
            html.Append(ReportHtml.ChartCard("chRevenueTrend", "Revenue invoiced and paid so far, per " + bucket,
                ReportHtml.Money(s.Current.Invoiced) + " invoiced, of which " + ReportHtml.Money(s.Current.Collected) +
                " has been paid. Each " + bucket + " shows the invoices issued in it.",
                "line", "currency",
                new
                {
                    labels = s.Labels,
                    series = new object[]
                    {
                        new { label = "Invoiced", data = s.Invoiced, tone = "primary" },
                        new { label = "Paid so far", data = s.Collected, tone = "success", dashed = true }
                    }
                },
                new[] { Capital(bucket), "Invoiced", "Paid so far" }, trendRows, s.Current.Invoiced == 0m, "lc-span-2"));

            int cancelled;
            counts.TryGetValue("Cancelled", out cancelled);
            html.Append(ReportHtml.CategoryDonut("chStatusMix", "Invoice value by status",
                "Where the " + ReportHtml.Money(s.Current.Invoiced) + " invoiced in the period stands now" +
                (cancelled > 0 ? "; " + PerformanceInsights.Plural(cancelled, "cancelled invoice") + " left out." : "."),
                amounts, "Status", "currency"));

            var customerValues = new Dictionary<string, decimal>();
            foreach (NamedValue c in customers)
            {
                // Customers are grouped by account; two accounts can share a name (or have none).
                decimal v;
                customerValues.TryGetValue(c.Name, out v);
                customerValues[c.Name] = v + c.Value;
            }
            html.Append(ReportHtml.CategoryBars("chTopCustomers", "Top customers by revenue",
                "The customers invoiced the most in the period.", customerValues, "Customer", 8, "currency", "primary",
                customerValues.Count > 6 ? "lc-tall" : null));

            var owedValues = new Dictionary<string, decimal>
            {
                { "Overdue", owed.Overdue },
                { "Not yet due", owed.NotYetDue },
                { "Paid so far", s.Current.Collected }
            };
            html.Append(ReportHtml.ChartCard("chOwed", "Paid, owed and overdue",
                "Of the money invoiced in the period: what has been paid, what is owed but not yet due, and what is overdue.",
                "bar", "currency",
                new
                {
                    labels = new[] { "Paid so far", "Not yet due", "Overdue" },
                    series = new object[]
                    {
                        new { label = "Amount", data = new[] { s.Current.Collected, owed.NotYetDue, owed.Overdue }, tone = "info" }
                    }
                },
                new[] { "Position", "Amount" },
                new List<string[]>
                {
                    new[] { "Paid so far", ReportHtml.Money(s.Current.Collected) },
                    new[] { "Not yet due", ReportHtml.Money(owed.NotYetDue) },
                    new[] { "Overdue", ReportHtml.Money(owed.Overdue) }
                },
                s.Current.Collected + owed.Outstanding == 0m));
            html.Append("</div>");

            // ---- Needs attention
            html.Append(ReportHtml.SectionTitle("notification_important", "Needs attention"));
            html.Append(ReportHtml.Attention(Attention(s, owed, counts, RevenueRules.AllInvoicesDue(Period.To, now), Period.ToExclusive <= now.Date),
                "Nothing needs attention: no overdue invoices or failed payments.", Href));

            // ---- Detail
            html.Append(ReportHtml.SectionTitle("table_rows", "Invoices issued in the period (newest first)"));
            var detail = new List<string[]>();
            foreach (DataRow r in LoadInvoices(MaxDetailRows + 1).Rows)
            {
                detail.Add(new[]
                {
                    Db.ToStr(r["InvoiceNumber"]), Db.ToStr(r["Client"]), Db.ToStr(r["Waybill"]),
                    ReportHtml.Money(Db.ToDecimal(r["Amount"])), StatusLabel(Db.ToStr(r["PaymentStatus"])),
                    Day(r["IssuedDate"], "dd MMM yyyy"), Day(r["DueOn"], "dd MMM yyyy")
                });
            }
            int allInvoices = 0;
            foreach (int n in counts.Values) allInvoices += n;
            html.Append(DetailTable(new[] { "Invoice", "Customer", "Waybill", "Amount", "Status", "Issued", "Due" }, detail,
                "Invoices issued in the period, newest first", "No invoices were issued in this period.", false, allInvoices));
            return html.ToString();
        }

        private static List<AttentionItem> Attention(PerformanceSnapshot s, Receivables owed, IDictionary<string, int> counts,
            bool allDue, bool periodOver)
        {
            var items = new List<AttentionItem>();
            if (owed.OverdueInvoices > 0)
            {
                items.Add(new AttentionItem(AttentionSeverity.High,
                    ReportHtml.Money(owed.Overdue) + " is overdue across " + PerformanceInsights.Plural(owed.OverdueInvoices, "invoice") +
                    "; the oldest is " + PerformanceInsights.Plural(owed.OldestOverdueDays, "day") + " late.",
                    "~/Finance/Invoices.aspx", "Open invoices"));
            }
            int failed;
            if (counts.TryGetValue("Failed", out failed) && failed > 0)
            {
                items.Add(new AttentionItem(AttentionSeverity.Medium,
                    PerformanceInsights.Plural(failed, "payment") + " failed - the customers need to pay another way.",
                    "~/Finance/Invoices.aspx", "Open invoices"));
            }
            // Only once every invoice is past its due date - before that, unpaid is simply not yet due.
            if (allDue && s.Current.Invoiced > 0m && s.Current.CollectionRate != null &&
                s.Current.CollectionRate.Value < RevenueRules.CollectionAlertRate)
            {
                items.Add(new AttentionItem(AttentionSeverity.Medium,
                    "Only " + Delta.FormatRate(s.Current.CollectionRate) + " of the money invoiced in this period has been paid so far."));
            }
            if (s.Previous.Invoiced > 0m && s.Current.Invoiced < s.Previous.Invoiced && periodOver)
            {
                decimal pct = Delta.PercentChange(s.Current.Invoiced, s.Previous.Invoiced) ?? 0m;
                if (pct <= RevenueRules.RevenueFallNote)
                {
                    items.Add(new AttentionItem(AttentionSeverity.Info,
                        "Revenue invoiced fell " + Math.Abs(pct).ToString("0.0", CultureInfo.InvariantCulture) + "% against the previous period (" +
                        ReportHtml.Money(s.Previous.Invoiced) + " to " + ReportHtml.Money(s.Current.Invoiced) + ")."));
                }
            }
            return items;
        }

        private static string Day(object value, string format)
        {
            return value == null || value == DBNull.Value
                ? string.Empty
                : Convert.ToDateTime(value, CultureInfo.InvariantCulture).ToString(format, CultureInfo.InvariantCulture);
        }

        private static string Capital(string s)
        {
            return string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }
    }
}
