using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Web;

namespace ForteLogisticsApp.Services
{
    public enum MetricKind { Count, Decimal, Money, Rate, Hours }

    /// <summary>One line of a side-by-side comparison: the metric, both values, and the change in words.</summary>
    public sealed class ComparisonRow
    {
        public string Metric;
        public MetricKind Kind;
        public decimal? A;
        public decimal? B;
        /// <summary>True when a rise is good news; null when no verdict is given (neutral, unequal lengths, or too few cases).</summary>
        public bool? HigherIsBetter;
        public string Change;
        /// <summary>What Period A's figure rests on (a total, or the number of cases behind an average), used to skip tiny bases.</summary>
        public decimal? BaseA;
        /// <summary>True when <see cref="BaseA"/> is a number of cases rather than an amount of money.</summary>
        public bool BaseIsCount;
        /// <summary>True for the per-day rows that put unequal periods on the same footing.</summary>
        public bool PerDay;
        /// <summary>Optional sample note shown with the measure, e.g. "12 vs 140 attempts".</summary>
        public string Detail;

        public string Format(decimal? v)
        {
            if (v == null) return "n/a";
            switch (Kind)
            {
                case MetricKind.Money: return ReportHtml.Money(v.Value);
                case MetricKind.Rate: return Delta.FormatRate(v);
                case MetricKind.Hours: return ReportHtml.Duration(v);
                case MetricKind.Decimal: return v.Value.ToString("N2", ReportHtml.Numbers);
                default: return ReportHtml.Count(v.Value);
            }
        }

        /// <summary>The unit, spelled out for the CSV export.</summary>
        public string Unit
        {
            get
            {
                switch (Kind)
                {
                    case MetricKind.Money: return PerDay ? "rand per day" : "rand";
                    case MetricKind.Rate: return "percent";
                    case MetricKind.Hours: return "hours";
                    case MetricKind.Decimal: return "per day";
                    default: return "count";
                }
            }
        }
    }

    /// <summary>
    /// Pure comparison logic for the Comparison report: the side-by-side rows (B measured
    /// against A, the baseline), the "biggest movements" highlights, and group highlights.
    /// No database, no page - unit-tested.
    /// </summary>
    public static class ComparisonInsights
    {
        public const string BaselineName = "Period A";

        /// <summary>Counts below this in Period A are too small for a percentage change to mean much.</summary>
        public const int MinCountBase = 5;

        /// <summary>Money below this in Period A is too small for a percentage change to mean much.</summary>
        public const decimal MinMoneyBase = 1000m;

        /// <summary>A delivery-success move this large (percentage points) is worth a highlight.</summary>
        public const decimal RateHighlightPoints = 2m;

        /// <summary>
        /// Days of the period that have started by <paramref name="today"/>. Future days have
        /// no activity yet, so per-day figures must not divide by them.
        /// </summary>
        public static int ElapsedDays(ReportPeriod p, DateTime today)
        {
            DateTime end = p.To < today.Date ? p.To : today.Date;
            return end < p.From ? 0 : (end - p.From).Days + 1;
        }

        /// <summary>
        /// The side-by-side rows. When the periods differ in length (<paramref name="daysA"/> vs
        /// <paramref name="daysB"/>, elapsed days) the raw totals carry no verdict - only the
        /// per-day rows, rates and averages are judged. Rates and averages resting on fewer than
        /// <see cref="PerformanceInsights.MinAttempts"/> cases are shown but not judged.
        /// </summary>
        public static List<ComparisonRow> PeriodRows(PeriodTotals a, PeriodTotals b, int daysA, int daysB)
        {
            bool same = daysA == daysB;
            bool? moreIsBetter = same ? (bool?)true : null;
            bool? fewerIsBetter = same ? (bool?)false : null;
            decimal attemptsA = a.Delivered + a.Failed, attemptsB = b.Delivered + b.Failed;
            bool enoughAttempts = attemptsA >= PerformanceInsights.MinAttempts && attemptsB >= PerformanceInsights.MinAttempts;
            bool enoughDeliveries = a.Delivered >= PerformanceInsights.MinAttempts && b.Delivered >= PerformanceInsights.MinAttempts;

            var rows = new List<ComparisonRow>();
            Add(rows, "Shipments booked", MetricKind.Count, a.Booked, b.Booked, moreIsBetter);
            AddPerDay(rows, "Shipments booked per day", MetricKind.Decimal, a.Booked, b.Booked, daysA, daysB, true);
            Add(rows, "Successful deliveries", MetricKind.Count, a.Delivered, b.Delivered, moreIsBetter);
            if (!same) AddPerDay(rows, "Successful deliveries per day", MetricKind.Decimal, a.Delivered, b.Delivered, daysA, daysB, true);
            Add(rows, "Failed delivery attempts", MetricKind.Count, a.Failed, b.Failed, fewerIsBetter);
            if (!same) AddPerDay(rows, "Failed delivery attempts per day", MetricKind.Decimal, a.Failed, b.Failed, daysA, daysB, false);

            ComparisonRow success = Add(rows, "Delivery attempt success", MetricKind.Rate, a.SuccessRate, b.SuccessRate,
                enoughAttempts ? (bool?)true : null);
            success.Detail = Sample(attemptsA, attemptsB, "attempts", enoughAttempts);
            success.BaseA = attemptsA;
            success.BaseIsCount = true;

            ComparisonRow speed = Add(rows, "Average delivery time", MetricKind.Hours, a.AvgDeliveryHours, b.AvgDeliveryHours,
                enoughDeliveries ? (bool?)false : null);
            speed.Detail = Sample(a.Delivered, b.Delivered, "deliveries", enoughDeliveries);
            speed.BaseA = a.Delivered;
            speed.BaseIsCount = true;

            Add(rows, "Revenue invoiced", MetricKind.Money, a.Invoiced, b.Invoiced, moreIsBetter);
            AddPerDay(rows, "Revenue invoiced per day", MetricKind.Money, a.Invoiced, b.Invoiced, daysA, daysB, true);
            // No verdict: an older period has simply had longer to be paid.
            Add(rows, "Invoices paid so far", MetricKind.Money, a.Collected, b.Collected, null);
            Add(rows, "Share of invoiced revenue paid so far", MetricKind.Rate, a.CollectionRate, b.CollectionRate, null);

            ComparisonRow perShipment = Add(rows, "Revenue per shipment", MetricKind.Money, a.RevenuePerShipment, b.RevenuePerShipment, true);
            perShipment.BaseA = a.Booked;
            perShipment.BaseIsCount = true;

            Add(rows, "New customers", MetricKind.Count, a.NewCustomers, b.NewCustomers, moreIsBetter);
            Add(rows, "New drivers", MetricKind.Count, a.NewDrivers, b.NewDrivers, null);
            Add(rows, "Active drivers", MetricKind.Count, a.ActiveDrivers, b.ActiveDrivers, null);
            return rows;
        }

        private static string Sample(decimal a, decimal b, string noun, bool enough)
        {
            string text = ReportHtml.Count(a) + " vs " + ReportHtml.Count(b) + " " + noun;
            return enough ? text : text + "; too few to judge";
        }

        private static void AddPerDay(List<ComparisonRow> rows, string metric, MetricKind kind, decimal a, decimal b,
            int daysA, int daysB, bool higherIsBetter)
        {
            ComparisonRow row = Add(rows, metric, kind, PerDayValue(a, daysA), PerDayValue(b, daysB), higherIsBetter);
            row.PerDay = true;
            row.BaseA = a;
            row.BaseIsCount = kind != MetricKind.Money;
        }

        /// <summary>Unrounded: rounding happens only when the value is shown, so small rates keep their true change.</summary>
        private static decimal? PerDayValue(decimal total, int days)
        {
            if (days <= 0) return null;
            return total / days;
        }

        private static ComparisonRow Add(List<ComparisonRow> rows, string metric, MetricKind kind, decimal? a, decimal? b, bool? higherIsBetter)
        {
            var row = new ComparisonRow
            {
                Metric = metric, Kind = kind, A = a, B = b, HigherIsBetter = higherIsBetter,
                BaseA = a, BaseIsCount = kind != MetricKind.Money
            };

            string change;
            int dir = 0;
            if (kind == MetricKind.Rate)
            {
                change = Delta.DescribePointsVersus(b, a, BaselineName);
                if (a != null && b != null)
                {
                    decimal diff = Math.Round(b.Value - a.Value, 1, MidpointRounding.AwayFromZero);
                    dir = diff > 0 ? 1 : diff < 0 ? -1 : 0;
                }
            }
            else if (a == null || b == null)
            {
                change = "not comparable (nothing to measure in one period)";
            }
            else
            {
                change = Delta.DescribeVersus(b.Value, a.Value, BaselineName);
                decimal? pct = Delta.PercentChange(b.Value, a.Value);
                bool negligible = pct != null && pct.Value == 0m;
                // From nothing to something still has a direction: 0 -> 5 failed attempts is bad news.
                dir = negligible ? 0 : Delta.Direction(b.Value, a.Value);
            }

            if (dir != 0 && higherIsBetter != null)
            {
                change += (dir > 0) == higherIsBetter.Value ? " (favourable)" : " (unfavourable)";
            }
            row.Change = change;
            rows.Add(row);
            return row;
        }

        /// <summary>
        /// Up to three movements worth acting on, worded as sentences with their verdict.
        /// A delivery-success move of at least <see cref="RateHighlightPoints"/> points comes
        /// first (only judged rates qualify, so both periods have enough attempts); then the
        /// largest relative changes among judged rows with a meaningful base. When the periods
        /// differ in length only the per-day rows compete; when they match, the per-day rows
        /// repeat the totals and are skipped.
        /// </summary>
        public static List<string> PeriodHighlights(IList<ComparisonRow> rows, bool sameLength)
        {
            var result = new List<string>();
            var candidates = new List<KeyValuePair<decimal, ComparisonRow>>();
            foreach (ComparisonRow r in rows)
            {
                if (r.HigherIsBetter == null || r.A == null || r.B == null) continue;
                if (r.Kind == MetricKind.Rate)
                {
                    decimal diff = Math.Round(r.B.Value - r.A.Value, 1, MidpointRounding.AwayFromZero);
                    if (Math.Abs(diff) >= RateHighlightPoints && result.Count == 0)
                    {
                        result.Add(r.Metric + (diff > 0 ? " rose " : " fell ") +
                                   Math.Abs(diff).ToString("0.0", CultureInfo.InvariantCulture) + " percentage points (" +
                                   r.Format(r.A) + " to " + r.Format(r.B) + ")" + Verdict(r, diff > 0) + ".");
                    }
                    continue;
                }
                if (r.PerDay == sameLength) continue;
                if (r.A.Value == 0m || !MeaningfulBase(r)) continue;
                decimal? pct = Delta.PercentChange(r.B.Value, r.A.Value);
                if (pct == null || pct.Value == 0m) continue;
                candidates.Add(new KeyValuePair<decimal, ComparisonRow>(Math.Abs(pct.Value), r));
            }
            // Largest first; ties keep table order (List.Sort is not stable, so compare positions).
            candidates.Sort((x, y) =>
            {
                int byPct = y.Key.CompareTo(x.Key);
                return byPct != 0 ? byPct : rows.IndexOf(x.Value).CompareTo(rows.IndexOf(y.Value));
            });

            for (int i = 0; i < candidates.Count && result.Count < 3; i++)
            {
                ComparisonRow r = candidates[i].Value;
                decimal pct = Delta.PercentChange(r.B.Value, r.A.Value).Value;
                result.Add(r.Metric + (pct > 0 ? " rose " : " fell ") +
                           Math.Abs(pct).ToString("0.0", CultureInfo.InvariantCulture) + "% (" +
                           r.Format(r.A) + " to " + r.Format(r.B) + ")" + Verdict(r, pct > 0) + ".");
            }
            return result;
        }

        private static bool MeaningfulBase(ComparisonRow r)
        {
            decimal baseA = r.BaseA ?? 0m;
            return r.BaseIsCount ? baseA >= MinCountBase : baseA >= MinMoneyBase;
        }

        private static string Verdict(ComparisonRow r, bool rose)
        {
            return rose == r.HigherIsBetter.Value ? " - favourable" : " - unfavourable";
        }

        /// <summary>
        /// Plain-language findings for a group comparison: the busiest group, the weakest and
        /// strongest delivery success, the most revenue, and the biggest rise and fall in
        /// bookings against the previous period. Placeholder groups ("Unassigned", "Unknown",
        /// ...) are never named as findings.
        /// </summary>
        public static List<string> GroupHighlights(IList<GroupFigures> groups, string groupWord)
        {
            var result = new List<string>();
            if (groups == null || groups.Count == 0) return result;

            int totalBooked = 0;
            foreach (GroupFigures g in groups) totalBooked += g.Booked;
            GroupFigures largest = null;
            foreach (GroupFigures g in groups)
            {
                if (!g.Placeholder && (largest == null || g.Booked > largest.Booked)) largest = g;
            }
            if (largest != null && largest.Booked > 0)
            {
                result.Add(largest.Name + " is the busiest " + groupWord + ": " + PerformanceInsights.Plural(largest.Booked, "shipment") +
                           " booked (" + Delta.FormatRate(Delta.Rate(largest.Booked, totalBooked)) + " of all).");
            }

            GroupFigures weakest = null, strongest = null;
            int qualifying = 0;
            foreach (GroupFigures g in groups)
            {
                if (g.Placeholder || g.Delivered + g.Failed < PerformanceInsights.MinAttempts || g.SuccessRate == null) continue;
                qualifying++;
                if (weakest == null || g.SuccessRate.Value < weakest.SuccessRate.Value) weakest = g;
                if (strongest == null || g.SuccessRate.Value > strongest.SuccessRate.Value) strongest = g;
            }
            if (qualifying == 1)
            {
                result.Add("Only " + strongest.Name + " had enough delivery attempts (at least " + PerformanceInsights.MinAttempts +
                           ") to judge success: " + Delta.FormatRate(strongest.SuccessRate) + ".");
            }
            else if (qualifying > 1 && weakest.SuccessRate.Value < strongest.SuccessRate.Value)
            {
                result.Add("Lowest delivery success: " + weakest.Name + " at " + Delta.FormatRate(weakest.SuccessRate) +
                           " (" + PerformanceInsights.Plural(weakest.Delivered + weakest.Failed, "attempt") + "). Highest: " +
                           strongest.Name + " at " + Delta.FormatRate(strongest.SuccessRate) + ".");
            }
            else if (qualifying > 1)
            {
                result.Add("Delivery success is level across the " + qualifying + " " + groupWord + "s with enough attempts (" +
                           Delta.FormatRate(strongest.SuccessRate) + ").");
            }

            GroupFigures topMoney = null;
            foreach (GroupFigures g in groups)
            {
                if (!g.Placeholder && g.Invoiced != null && g.Invoiced.Value > 0 &&
                    (topMoney == null || g.Invoiced.Value > topMoney.Invoiced.Value)) topMoney = g;
            }
            if (topMoney != null)
            {
                result.Add("Most revenue: " + topMoney.Name + " (" + ReportHtml.Money(topMoney.Invoiced.Value) + " invoiced).");
            }

            GroupFigures rise = null, fall = null;
            decimal risePct = 0m, fallPct = 0m;
            foreach (GroupFigures g in groups)
            {
                if (g.Placeholder || g.PreviousBooked == null || g.PreviousBooked.Value < MinCountBase) continue;
                decimal? pct = Delta.PercentChange(g.Booked, g.PreviousBooked.Value);
                if (pct == null) continue;
                if (pct.Value > risePct) { risePct = pct.Value; rise = g; }
                if (pct.Value < fallPct) { fallPct = pct.Value; fall = g; }
            }
            if (rise != null) result.Add("Fastest growth: " + rise.Name + ", " + Movement(rise));
            if (fall != null) result.Add("Biggest drop: " + fall.Name + ", " + Movement(fall));
            return result;
        }

        private static string Movement(GroupFigures g)
        {
            return ReportHtml.Count(g.PreviousBooked.Value) + " to " + PerformanceInsights.Plural(g.Booked, "shipment") +
                   " booked (" + Delta.Describe(g.Booked, g.PreviousBooked.Value) + ").";
        }

        /// <summary>
        /// Copies each group's bookings in the previous period onto the current figures,
        /// matched by group key. A group that was active before but is silent now is added
        /// with zeros, so a lane or driver that stopped is not hidden.
        /// </summary>
        public static void AttachPrevious(List<GroupFigures> current, IList<GroupFigures> previous)
        {
            var byKey = new Dictionary<string, GroupFigures>(StringComparer.Ordinal);
            foreach (GroupFigures g in current)
            {
                g.PreviousBooked = 0;
                byKey[g.Key ?? g.Name] = g;
            }
            foreach (GroupFigures p in previous)
            {
                GroupFigures g;
                if (byKey.TryGetValue(p.Key ?? p.Name, out g))
                {
                    g.PreviousBooked = p.Booked;
                }
                else if (p.Booked > 0)
                {
                    current.Add(new GroupFigures
                    {
                        Key = p.Key, Name = p.Name, Placeholder = p.Placeholder, PreviousBooked = p.Booked,
                        Invoiced = p.Invoiced == null ? (decimal?)null : 0m
                    });
                }
            }
        }
    }

    /// <summary>
    /// Reads and writes the two periods of a period-vs-period comparison in the URL. Named
    /// pairs (?pair=30d) or explicit dates (?aFrom=..&amp;aTo=..&amp;bFrom=..&amp;bTo=..); Period A is
    /// the baseline. Pure and unit-tested.
    /// </summary>
    public static class ComparisonQuery
    {
        private static readonly string[][] Pairs =
        {
            new[] { "7d", "Last 7 days vs the 7 before" },
            new[] { "30d", "Last 30 days vs the 30 before" },
            new[] { "month", "Last month vs the month before" },
            new[] { "ytd", "Year to date vs same days last year" },
            new[] { ReportQuery.CustomPreset, "Custom dates" }
        };

        public static IList<string[]> PairOptions
        {
            get { return Pairs; }
        }

        public static bool IsKnownPair(string key)
        {
            if (string.IsNullOrEmpty(key) || key == ReportQuery.CustomPreset) return false;
            foreach (string[] p in Pairs) if (p[0] == key) return true;
            return false;
        }

        /// <summary>The two periods of a named pair, as at <paramref name="today"/>. Unknown keys mean "30d".</summary>
        public static void PairPeriods(string key, DateTime today, out ReportPeriod a, out ReportPeriod b)
        {
            DateTime t = today.Date;
            switch (key)
            {
                case "7d":
                    b = new ReportPeriod(t.AddDays(-6), t, Granularity.Day);
                    break;
                case "month":
                    DateTime firstThis = new DateTime(t.Year, t.Month, 1);
                    b = new ReportPeriod(firstThis.AddMonths(-1), firstThis.AddDays(-1), Granularity.Day);
                    a = new ReportPeriod(firstThis.AddMonths(-2), firstThis.AddMonths(-1).AddDays(-1), Granularity.Day);
                    return;
                case "ytd":
                    b = new ReportPeriod(new DateTime(t.Year, 1, 1), t, Granularity.Month);
                    a = new ReportPeriod(new DateTime(t.Year - 1, 1, 1), SameDayLastYear(t), Granularity.Month);
                    return;
                default:
                    b = new ReportPeriod(t.AddDays(-29), t, Granularity.Day);
                    break;
            }
            a = b.Previous();
        }

        private static DateTime SameDayLastYear(DateTime t)
        {
            int day = Math.Min(t.Day, DateTime.DaysInMonth(t.Year - 1, t.Month));
            return new DateTime(t.Year - 1, t.Month, day);
        }

        public static void Read(NameValueCollection q, DateTime today, out ReportPeriod a, out ReportPeriod b,
            out string pairKey, out string problem)
        {
            problem = null;
            string aFrom = q == null ? null : q["aFrom"], aTo = q == null ? null : q["aTo"];
            string bFrom = q == null ? null : q["bFrom"], bTo = q == null ? null : q["bTo"];
            string pair = q == null ? null : q["pair"];

            if (!string.IsNullOrEmpty(aFrom) || !string.IsNullOrEmpty(aTo) || !string.IsNullOrEmpty(bFrom) || !string.IsNullOrEmpty(bTo))
            {
                DateTime af, at, bf, bt;
                if (ReportQuery.TryDate(aFrom, out af) && ReportQuery.TryDate(aTo, out at) &&
                    ReportQuery.TryDate(bFrom, out bf) && ReportQuery.TryDate(bTo, out bt))
                {
                    string invalidA = ReportPeriod.Validate(af, at, Granularity.Month, today);
                    string invalidB = ReportPeriod.Validate(bf, bt, Granularity.Month, today);
                    string invalid = invalidA != null ? "Period A: " + invalidA : invalidB != null ? "Period B: " + invalidB : null;
                    if (invalid == null)
                    {
                        a = new ReportPeriod(af, at, ReportQuery.ParseGroup(null, Math.Abs((at - af).Days) + 1));
                        b = new ReportPeriod(bf, bt, ReportQuery.ParseGroup(null, Math.Abs((bt - bf).Days) + 1));
                        pairKey = ReportQuery.CustomPreset;
                        return;
                    }
                    problem = invalid;
                }
                else
                {
                    problem = "Please enter all four dates (both periods) as real calendar dates (yyyy-mm-dd).";
                }
            }

            pairKey = IsKnownPair(pair) ? pair : "30d";
            PairPeriods(pairKey, today, out a, out b);
        }

        /// <summary>
        /// The query string for the submitted form: the named pair when no date was edited,
        /// otherwise the four dates (canonical when valid, encoded as typed when not).
        /// </summary>
        public static string FromForm(string pair, string aFrom, string aTo, string bFrom, string bTo, string shown)
        {
            string typed = string.Join("|", Trim(aFrom), Trim(aTo), Trim(bFrom), Trim(bTo));
            if (IsKnownPair(pair) && string.Equals(typed, shown ?? string.Empty, StringComparison.Ordinal))
            {
                return "?mode=periods&pair=" + HttpUtility.UrlEncode(pair);
            }
            return "?mode=periods&aFrom=" + Canon(aFrom) + "&aTo=" + Canon(aTo) + "&bFrom=" + Canon(bFrom) + "&bTo=" + Canon(bTo);
        }

        public static string Shown(ReportPeriod a, ReportPeriod b)
        {
            return string.Join("|", ReportQuery.IsoDate(a.From), ReportQuery.IsoDate(a.To), ReportQuery.IsoDate(b.From), ReportQuery.IsoDate(b.To));
        }

        private static string Trim(string s)
        {
            return (s ?? string.Empty).Trim();
        }

        private static string Canon(string value)
        {
            DateTime d;
            return ReportQuery.TryDate(value, out d) ? ReportQuery.IsoDate(d) : HttpUtility.UrlEncode(Trim(value));
        }
    }
}
