using System;
using System.Collections.Generic;
using System.Globalization;

namespace ForteLogisticsApp.Services
{
    /// <summary>How a report period is sliced into chart buckets.</summary>
    public enum Granularity
    {
        Day,
        Week,
        Month
    }

    /// <summary>
    /// A reporting window plus the bucket size used to chart it. Pure date maths with no
    /// database access, so every rule below is unit-tested directly.
    ///
    /// The definitions every report and dashboard share:
    ///   - From and To are both INCLUSIVE calendar dates in server local time (SAST - the
    ///     database stamps rows with GETDATE()). Queries use From &lt;= x &lt; To + 1 day.
    ///   - Weeks start on Monday. A week bucket is keyed by its Monday, so the first and last
    ///     week of a range may be partial - only dates inside the range are ever counted.
    ///   - Month buckets are keyed by the 1st of the month.
    ///   - The previous period is the same number of days immediately before From.
    /// </summary>
    public sealed class ReportPeriod
    {
        /// <summary>Longest range per bucket size, so a chart never carries an unreadable number of points.</summary>
        public const int MaxDailyDays = 93;
        public const int MaxWeeklyDays = 742;     // 106 weeks
        public const int MaxMonthlyDays = 1830;   // ~60 months

        private static readonly CultureInfo LabelCulture = CultureInfo.InvariantCulture;

        public DateTime From { get; private set; }
        public DateTime To { get; private set; }
        public Granularity Granularity { get; private set; }

        public ReportPeriod(DateTime from, DateTime to, Granularity granularity)
        {
            DateTime a = from.Date, b = to.Date;
            // A reversed range is a user slip, not an error worth failing a report over.
            From = a <= b ? a : b;
            To = a <= b ? b : a;
            Granularity = granularity;
        }

        /// <summary>The exclusive upper bound to use in SQL: midnight after To.</summary>
        public DateTime ToExclusive
        {
            get { return To.AddDays(1); }
        }

        public int Days
        {
            get { return (To - From).Days + 1; }
        }

        /// <summary>The same-length window immediately before this one.</summary>
        public ReportPeriod Previous()
        {
            DateTime prevTo = From.AddDays(-1);
            return new ReportPeriod(prevTo.AddDays(-(Days - 1)), prevTo, Granularity);
        }

        /// <summary>The start of the bucket <paramref name="date"/> falls in.</summary>
        public static DateTime BucketStart(DateTime date, Granularity granularity)
        {
            DateTime d = date.Date;
            switch (granularity)
            {
                case Granularity.Week:
                    // DayOfWeek: Sunday = 0 ... Saturday = 6. Monday-based offset: Mon 0 .. Sun 6.
                    int offset = ((int)d.DayOfWeek + 6) % 7;
                    return d.AddDays(-offset);
                case Granularity.Month:
                    return new DateTime(d.Year, d.Month, 1);
                default:
                    return d;
            }
        }

        public static DateTime NextBucket(DateTime bucketStart, Granularity granularity)
        {
            switch (granularity)
            {
                case Granularity.Week: return bucketStart.AddDays(7);
                case Granularity.Month: return bucketStart.AddMonths(1);
                default: return bucketStart.AddDays(1);
            }
        }

        /// <summary>Every bucket start covering the range, in order - including empty ones, so a chart never skips a gap.</summary>
        public IList<DateTime> Buckets()
        {
            var list = new List<DateTime>();
            for (DateTime b = BucketStart(From, Granularity); b <= To; b = NextBucket(b, Granularity))
            {
                list.Add(b);
            }
            return list;
        }

        public static string Label(DateTime bucketStart, Granularity granularity)
        {
            switch (granularity)
            {
                case Granularity.Week: return "Wk " + bucketStart.ToString("dd MMM", LabelCulture);
                case Granularity.Month: return bucketStart.ToString("MMM yyyy", LabelCulture);
                default: return bucketStart.ToString("dd MMM", LabelCulture);
            }
        }

        public string[] Labels()
        {
            IList<DateTime> buckets = Buckets();
            var labels = new string[buckets.Count];
            for (int i = 0; i < buckets.Count; i++) labels[i] = Label(buckets[i], Granularity);
            return labels;
        }

        /// <summary>
        /// Sums per-day values into this period's buckets. Days outside the range are ignored,
        /// so a daily series fetched for a wider window (e.g. current + previous period at once)
        /// can be rolled up for either period without double counting.
        /// </summary>
        public decimal[] Rollup(IDictionary<DateTime, decimal> daily)
        {
            IList<DateTime> buckets = Buckets();
            var index = new Dictionary<DateTime, int>();
            for (int i = 0; i < buckets.Count; i++) index[buckets[i]] = i;

            var result = new decimal[buckets.Count];
            if (daily == null) return result;

            foreach (KeyValuePair<DateTime, decimal> kv in daily)
            {
                DateTime day = kv.Key.Date;
                if (day < From || day > To) continue;

                int i;
                if (index.TryGetValue(BucketStart(day, Granularity), out i)) result[i] += kv.Value;
            }
            return result;
        }

        /// <summary>Sum of the per-day values that fall inside the range.</summary>
        public decimal Total(IDictionary<DateTime, decimal> daily)
        {
            decimal total = 0m;
            if (daily == null) return total;
            foreach (KeyValuePair<DateTime, decimal> kv in daily)
            {
                DateTime day = kv.Key.Date;
                if (day >= From && day <= To) total += kv.Value;
            }
            return total;
        }

        public string Describe()
        {
            string size = Granularity == Granularity.Day ? "daily"
                        : Granularity == Granularity.Week ? "weekly" : "monthly";
            return From.ToString("dd MMM yyyy", LabelCulture) + " to " +
                   To.ToString("dd MMM yyyy", LabelCulture) + " (" + size + ")";
        }

        /// <summary>
        /// A sentence saying which week/month buckets are only partly inside the period, so a
        /// short first or last bar is not mistaken for a drop. Null for daily periods and when
        /// every bucket is whole.
        /// </summary>
        public string PartialNote()
        {
            if (Granularity == Granularity.Day) return null;
            IList<DateTime> buckets = Buckets();
            if (buckets.Count == 0) return null;

            string unit = Granularity == Granularity.Week ? "week" : "month";
            bool firstPartial = buckets[0] < From;
            bool lastPartial = NextBucket(buckets[buckets.Count - 1], Granularity).AddDays(-1) > To;

            if (firstPartial && lastPartial)
                return "The first and last " + unit + "s are partial: only days inside the period are counted.";
            if (firstPartial)
                return "The first " + unit + " is partial: it starts on " + From.ToString("dd MMM", LabelCulture) + ".";
            if (lastPartial)
                return "The last " + unit + " is partial: it runs only to " + To.ToString("dd MMM", LabelCulture) + ".";
            return null;
        }

        /// <summary>Null when the range is acceptable, otherwise a user-facing reason.</summary>
        public static string Validate(DateTime from, DateTime to, Granularity granularity)
        {
            return Validate(from, to, granularity, DateTime.Today);
        }

        /// <summary>As <see cref="Validate(DateTime, DateTime, Granularity)"/>, against an explicit "today" (testable).</summary>
        public static string Validate(DateTime from, DateTime to, Granularity granularity, DateTime today)
        {
            var p = new ReportPeriod(from, to, granularity);
            if (p.From.Year < 2000) return "Please choose a start date from the year 2000 onwards.";
            if (p.To > today.Date.AddYears(1)) return "The end date is too far in the future.";

            int max = granularity == Granularity.Day ? MaxDailyDays
                    : granularity == Granularity.Week ? MaxWeeklyDays : MaxMonthlyDays;
            if (p.Days > max)
            {
                return granularity == Granularity.Day
                    ? "Daily view covers at most " + MaxDailyDays + " days - choose weekly or monthly for a longer range."
                    : granularity == Granularity.Week
                        ? "Weekly view covers at most 106 weeks - choose monthly for a longer range."
                        : "Monthly view covers at most 5 years.";
            }
            return null;
        }

        /// <summary>
        /// Named quick ranges for the period picker. Unknown keys fall back to the last 30 days.
        /// </summary>
        public static ReportPeriod Preset(string key, DateTime today)
        {
            DateTime t = today.Date;
            switch ((key ?? string.Empty).ToLowerInvariant())
            {
                case "today": return new ReportPeriod(t, t, Granularity.Day);
                case "7d": return new ReportPeriod(t.AddDays(-6), t, Granularity.Day);
                case "thismonth": return new ReportPeriod(new DateTime(t.Year, t.Month, 1), t, Granularity.Day);
                case "lastmonth":
                    DateTime firstThis = new DateTime(t.Year, t.Month, 1);
                    return new ReportPeriod(firstThis.AddMonths(-1), firstThis.AddDays(-1), Granularity.Day);
                case "90d": return new ReportPeriod(t.AddDays(-89), t, Granularity.Week);
                case "12m": return new ReportPeriod(new DateTime(t.Year, t.Month, 1).AddMonths(-11), t, Granularity.Month);
                case "ytd": return new ReportPeriod(new DateTime(t.Year, 1, 1), t, Granularity.Month);
                default: return new ReportPeriod(t.AddDays(-29), t, Granularity.Day);
            }
        }
    }

    /// <summary>
    /// Period-over-period change and rates, with the divide-by-zero cases decided once.
    /// Rounding is half-up (6.25 -> 6.3), matching how the chart tooltips round, so a table
    /// and a chart can never show the same figure differently.
    /// </summary>
    public static class Delta
    {
        /// <summary>Percentage change from previous to current; null when there is no previous base.</summary>
        public static decimal? PercentChange(decimal current, decimal previous)
        {
            if (previous == 0m) return null;
            return Math.Round((current - previous) / previous * 100m, 1, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Short wording for a KPI change line. Words, not just colour, carry the meaning:
        /// "up 12.5%", "down 3.0%", "new this period" (previous was zero), "no change", and
        /// "about the same" when the change rounds to 0.0%.
        /// </summary>
        public static string Describe(decimal current, decimal previous)
        {
            if (current == previous) return "no change vs previous period";
            decimal? pct = PercentChange(current, previous);
            if (pct == null) return "new this period (none previously)";
            if (pct.Value == 0m) return "about the same as previous period (under 0.1% change)";
            string mag = Math.Abs(pct.Value).ToString("0.0", CultureInfo.InvariantCulture) + "%";
            return (pct.Value > 0 ? "up " : "down ") + mag + " vs previous period";
        }

        /// <summary>+1 when current is higher, -1 when lower, 0 when equal.</summary>
        public static int Direction(decimal current, decimal previous)
        {
            return current > previous ? 1 : current < previous ? -1 : 0;
        }

        /// <summary>numerator / denominator as a percentage, or null when the denominator is zero.</summary>
        public static decimal? Rate(decimal numerator, decimal denominator)
        {
            if (denominator == 0m) return null;
            return Math.Round(numerator / denominator * 100m, 1, MidpointRounding.AwayFromZero);
        }

        public static string FormatRate(decimal? rate)
        {
            return rate == null ? "n/a" : rate.Value.ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        /// <summary>
        /// Change between two RATES, in percentage points ("up 4.2 points"). A relative change
        /// of a percentage ("down 4.3%") is ambiguous, so rates are always compared this way.
        /// </summary>
        public static string DescribePoints(decimal? current, decimal? previous)
        {
            if (current == null) return "nothing to measure this period";
            if (previous == null) return "no previous period to compare";
            decimal diff = Math.Round(current.Value - previous.Value, 1, MidpointRounding.AwayFromZero);
            if (diff == 0m) return "no change vs previous period";
            return (diff > 0 ? "up " : "down ") +
                   Math.Abs(diff).ToString("0.0", CultureInfo.InvariantCulture) + " points vs previous period";
        }
    }
}
