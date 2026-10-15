using System;
using System.Collections.Generic;

namespace ForteLogisticsApp.Services
{
    /// <summary>
    /// Turns the numbers into decisions: the "Needs attention" rules and the table tidy-up the
    /// performance views share. Pure - no database, no page - so every rule is unit-tested.
    ///
    /// Each finding says what was found, how urgent it is, and where to go to act on it.
    /// Rules are deliberately quiet on routine data: money only when it is OVERDUE (not merely
    /// not-yet-paid), parcels only when they have NOT MOVED for several days (not merely
    /// in progress), so the list stays short enough to act on.
    /// </summary>
    public static class PerformanceInsights
    {
        /// <summary>Delivery-attempt success below which a day/week/month is flagged.</summary>
        public const decimal SuccessTarget = 90m;

        /// <summary>Driver success below which a driver is flagged (with at least MinAttempts attempts).</summary>
        public const decimal DriverAlert = 80m;

        public const int MinAttempts = 3;

        /// <summary>A parcel that has not changed status for this many days counts as stuck.</summary>
        public const int StuckDays = 3;

        private const int MaxBucketFlags = 5;
        private const int MaxStuckStages = 3;

        public static List<AttentionItem> Build(PerformanceSnapshot s, IList<DriverStanding> drivers,
            Receivables receivables, IList<StuckStage> stuck, string bucketWord)
        {
            var high = new List<AttentionItem>();
            var medium = new List<AttentionItem>();
            if (s == null) return high;

            if (receivables != null && receivables.Overdue > 0)
            {
                high.Add(new AttentionItem(AttentionSeverity.High,
                    ReportHtml.Money(receivables.Overdue) + " is overdue across " + Plural(receivables.OverdueInvoices, "invoice") +
                    " issued in this period; the oldest is " + Plural(receivables.OldestOverdueDays, "day") + " past due.",
                    "~/Finance/Invoices.aspx", "Open billing"));
            }

            if (stuck != null)
            {
                for (int i = 0; i < stuck.Count && i < MaxStuckStages; i++)
                {
                    StuckStage st = stuck[i];
                    high.Add(new AttentionItem(AttentionSeverity.High,
                        Plural(st.Count, "parcel") + " at \"" + StatusLabel(st.Status) + "\" have not moved for more than " +
                        StuckDays + " days (the oldest for " + Plural(st.OldestDays, "day") + ").",
                        "~/Admin/Shipments.aspx", "Review shipments"));
                }
            }

            if (s.Cohort != null && (s.Cohort.Lost > 0 || s.Cohort.Damaged > 0))
            {
                high.Add(new AttentionItem(AttentionSeverity.High,
                    Plural(s.Cohort.Lost, "parcel") + " lost and " + Plural(s.Cohort.Damaged, "parcel") +
                    " damaged among this period's shipments.",
                    "~/Reports/LostDamaged.aspx", "Lost & Damaged report"));
            }

            PeriodTotals c = s.Current, p = s.Previous;
            if (c != null && p != null && c.Failed > p.Failed && (p.Failed > 0 || c.Failed >= MinAttempts))
            {
                string text = p.Failed == 0
                    ? Plural(c.Failed, "failed delivery attempt") + " this period, against none in the previous period."
                    : "Failed delivery attempts rose from " + ReportHtml.Count(p.Failed) + " to " + ReportHtml.Count(c.Failed) +
                      " (" + Delta.Describe(c.Failed, p.Failed) + ").";
                medium.Add(new AttentionItem(AttentionSeverity.Medium, text, "~/Reports/DriverPerformance.aspx", "Driver performance"));
            }

            AddWeakBuckets(s, medium, bucketWord);
            AddWeakDrivers(drivers, medium);

            high.AddRange(medium);
            return high;
        }

        private static void AddWeakBuckets(PerformanceSnapshot s, List<AttentionItem> into, string bucketWord)
        {
            if (s.Labels == null || s.Delivered == null || s.Failed == null) return;

            var weak = new List<KeyValuePair<int, decimal>>();
            for (int i = 0; i < s.Labels.Length; i++)
            {
                decimal resolved = s.Delivered[i] + s.Failed[i];
                decimal? rate = Delta.Rate(s.Delivered[i], resolved);
                if (resolved >= MinAttempts && rate != null && rate.Value < SuccessTarget)
                {
                    weak.Add(new KeyValuePair<int, decimal>(i, rate.Value));
                }
            }

            // Worst first, so the truncated list still shows the buckets that matter most.
            weak.Sort((a, b) => a.Value != b.Value ? a.Value.CompareTo(b.Value) : a.Key.CompareTo(b.Key));
            for (int k = 0; k < weak.Count && k < MaxBucketFlags; k++)
            {
                int i = weak[k].Key;
                decimal resolved = s.Delivered[i] + s.Failed[i];
                into.Add(new AttentionItem(AttentionSeverity.Medium,
                    s.Labels[i] + ": delivery success " + Delta.FormatRate(weak[k].Value) + " (" +
                    ReportHtml.Count(s.Delivered[i]) + " of " + ReportHtml.Count(resolved) + " attempts), below the " +
                    SuccessTarget.ToString("0") + "% target."));
            }
            if (weak.Count > MaxBucketFlags)
            {
                int more = weak.Count - MaxBucketFlags;
                into.Add(new AttentionItem(AttentionSeverity.Medium,
                    "...and " + Plural(more, string.IsNullOrEmpty(bucketWord) ? "period" : bucketWord) +
                    " more below the success target (see the breakdown table)."));
            }
        }

        private static void AddWeakDrivers(IList<DriverStanding> drivers, List<AttentionItem> into)
        {
            if (drivers == null) return;
            var weak = new List<DriverStanding>();
            foreach (DriverStanding d in drivers)
            {
                if (d.Delivered + d.Failed >= MinAttempts && d.SuccessRate != null && d.SuccessRate.Value < DriverAlert)
                {
                    weak.Add(d);
                }
            }
            weak.Sort((a, b) => a.SuccessRate.Value.CompareTo(b.SuccessRate.Value));
            foreach (DriverStanding d in weak)
            {
                into.Add(new AttentionItem(AttentionSeverity.Medium,
                    d.Name + ": " + Delta.FormatRate(d.SuccessRate) + " delivery success over " +
                    Plural(d.Delivered + d.Failed, "attempt") + " (below " + DriverAlert.ToString("0") + "%).",
                    "~/Reports/DriverPerformance.aspx", "Driver performance"));
            }
        }

        /// <summary>A delivery status in words, saying what the parcel is waiting for.</summary>
        public static string StatusLabel(string status)
        {
            switch ((status ?? string.Empty).ToLowerInvariant())
            {
                case "registered": return "Booked - awaiting sorting";
                case "sorted": return "Sorted - awaiting a driver";
                case "assigned":
                case "intransit": return "Assigned - awaiting collection";
                case "outfordelivery": return "Out for delivery";
                default: return string.IsNullOrEmpty(status) ? "Unknown" : status;
            }
        }

        /// <summary>
        /// Replaces every run of at least <paramref name="minRun"/> consecutive empty rows with
        /// one summary row ("Nov 2025 to Jul 2026 | No activity (9 months)"), keeping the column
        /// count. Shorter runs are kept as they are, so isolated quiet days still show.
        /// </summary>
        public static List<string[]> CollapseEmptyRuns(IList<string[]> rows, IList<bool> empty, int minRun, string unitSingular)
        {
            var result = new List<string[]>();
            if (rows == null) return result;

            int i = 0;
            while (i < rows.Count)
            {
                if (!empty[i])
                {
                    result.Add(rows[i]);
                    i++;
                    continue;
                }

                int j = i;
                while (j < rows.Count && empty[j]) j++;
                int run = j - i;

                if (run >= minRun)
                {
                    var summary = new string[rows[i].Length];
                    summary[0] = rows[i][0] + " to " + rows[j - 1][0];
                    if (summary.Length > 1) summary[1] = "No activity (" + Plural(run, unitSingular) + ")";
                    for (int k = 2; k < summary.Length; k++) summary[k] = string.Empty;
                    result.Add(summary);
                }
                else
                {
                    for (int k = i; k < j; k++) result.Add(rows[k]);
                }
                i = j;
            }
            return result;
        }

        /// <summary>"1 day", "3 days" - with the number formatted the report way.</summary>
        public static string Plural(decimal count, string singular)
        {
            return ReportHtml.Count(count) + " " + singular + (count == 1m ? string.Empty : "s");
        }
    }
}
