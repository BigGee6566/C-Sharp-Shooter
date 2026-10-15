using System;
using System.Collections.Generic;
using System.Globalization;

namespace ForteLogisticsApp.Services
{
    /// <summary>
    /// The movement stages a tracking event belongs to (Parcel Movement report). Pure and
    /// unit-tested; the SQL CASE in that report mirrors <see cref="Of"/>.
    /// </summary>
    public static class MovementStages
    {
        public const int Booked = 0, Sorted = 1, Dispatched = 2, Delivered = 3, FailedAttempt = 4;

        /// <summary>The stage of a TrackingHistory status, or -1 for statuses outside the delivery flow.</summary>
        public static int Of(string status)
        {
            switch ((status ?? string.Empty).ToLowerInvariant())
            {
                case "registered": return Booked;
                case "sorted": return Sorted;
                case "assigned":
                case "intransit":
                case "outfordelivery": return Dispatched;
                case "delivered": return Delivered;
                // Both are unsuccessful delivery attempts (AnalyticsService.AttemptEvent).
                case "failed":
                case "requeued": return FailedAttempt;
                default: return -1;
            }
        }

        /// <summary>A tracking event in words.</summary>
        public static string EventLabel(string status)
        {
            switch ((status ?? string.Empty).ToLowerInvariant())
            {
                case "registered": return "Booked";
                case "sorted": return "Sorted";
                case "assigned": return "Assigned to a driver";
                case "intransit": return "In transit";
                case "outfordelivery": return "Out for delivery";
                case "delivered": return "Delivered";
                case "failed": return "Delivery attempt failed";
                case "requeued": return "Attempt failed - re-queued";
                default: return string.IsNullOrEmpty(status) ? "Unknown" : status;
            }
        }
    }

    /// <summary>How a driver's period is judged (Driver Performance report). Pure and unit-tested.</summary>
    public static class DriverRules
    {
        /// <summary>A rating alert needs at least this many ratings, so one bad review does not flag a driver.</summary>
        public const int MinRatings = 5;

        /// <summary>A rating below this (out of 5) is worth a conversation.</summary>
        public const decimal RatingAlert = 3m;

        public const string NoAttempts = "No attempts";
        public const string TooFew = "Too few attempts to judge";
        public const string BelowAlert = "Below alert level";
        public const string BelowTarget = "Below target";
        public const string OnTarget = "On target";

        public static string Classify(int delivered, int failed)
        {
            int attempts = delivered + failed;
            if (attempts == 0) return NoAttempts;
            if (attempts < PerformanceInsights.MinAttempts) return TooFew;
            decimal rate = Delta.Rate(delivered, attempts).Value;
            if (rate < PerformanceInsights.DriverAlert) return BelowAlert;
            if (rate < PerformanceInsights.SuccessTarget) return BelowTarget;
            return OnTarget;
        }

        /// <summary>Order for tables and lists: worst standing first.</summary>
        public static int Rank(string standing)
        {
            switch (standing)
            {
                case BelowAlert: return 0;
                case BelowTarget: return 1;
                case OnTarget: return 2;
                case TooFew: return 3;
                default: return 4;
            }
        }

        /// <summary>The change in success rate against the previous period, in points; "n/a" unless both periods can be judged.</summary>
        public static string ChangeInPoints(int delivered, int failed, int deliveredBefore, int failedBefore)
        {
            if (delivered + failed < PerformanceInsights.MinAttempts || deliveredBefore + failedBefore < PerformanceInsights.MinAttempts) return "n/a";
            decimal diff = Math.Round(Delta.Rate(delivered, delivered + failed).Value - Delta.Rate(deliveredBefore, deliveredBefore + failedBefore).Value,
                1, MidpointRounding.AwayFromZero);
            if (diff == 0m) return "no change";
            return (diff > 0 ? "up " : "down ") + Math.Abs(diff).ToString("0.0", CultureInfo.InvariantCulture) + " points";
        }

        public static bool RatingNeedsAttention(decimal? average, int count)
        {
            return average != null && count >= MinRatings && average.Value < RatingAlert;
        }
    }

    /// <summary>When the Revenue report raises an alert. Pure and unit-tested.</summary>
    public static class RevenueRules
    {
        /// <summary>Below this share paid, once the period's invoices are all due, collection needs attention.</summary>
        public const decimal CollectionAlertRate = 70m;

        /// <summary>A fall in revenue invoiced at least this large (percent) against the previous period is noted.</summary>
        public const decimal RevenueFallNote = -10m;

        /// <summary>
        /// True once every invoice issued in a period ending <paramref name="periodTo"/> is past its
        /// standard due date - before that, a low share paid is just invoices not yet due.
        /// </summary>
        public static bool AllInvoicesDue(DateTime periodTo, DateTime today)
        {
            return periodTo.Date.AddDays(AnalyticsService.DefaultPaymentTermsDays) < today.Date;
        }
    }

    /// <summary>One group's exception figures (Lost and Damaged report).</summary>
    public sealed class ExceptionGroup
    {
        public string Name;
        public int Booked;
        public int Exceptions;

        public decimal? Rate
        {
            get { return Delta.Rate(Exceptions, Booked); }
        }
    }

    /// <summary>How the Lost and Damaged report compares groups and periods. Pure and unit-tested.</summary>
    public static class ExceptionRules
    {
        /// <summary>
        /// Lost and damaged parcels are often reported days after booking, so a period is only
        /// compared once its last booking is this old.
        /// </summary>
        public const int MaturityDays = 14;

        /// <summary>A destination or driver needs at least this many bookings for its rate to be compared.</summary>
        public const int MinBookings = 10;

        public static bool IsMature(DateTime periodTo, DateTime today)
        {
            return periodTo.Date.AddDays(MaturityDays) <= today.Date;
        }

        /// <summary>Groups with enough bookings and at least one exception, highest rate first.</summary>
        public static List<ExceptionGroup> Ranked(IEnumerable<ExceptionGroup> groups)
        {
            var list = new List<ExceptionGroup>();
            foreach (ExceptionGroup g in groups) if (g.Booked >= MinBookings && g.Exceptions > 0) list.Add(g);
            list.Sort((a, b) =>
            {
                int byRate = b.Rate.Value.CompareTo(a.Rate.Value);
                if (byRate != 0) return byRate;
                int byCount = b.Exceptions.CompareTo(a.Exceptions);
                return byCount != 0 ? byCount : string.CompareOrdinal(a.Name, b.Name);
            });
            return list;
        }

        /// <summary>Ranked groups whose rate is at least double the overall rate (and that have 2+ exceptions), at most three.</summary>
        public static List<ExceptionGroup> Hotspots(IEnumerable<ExceptionGroup> groups, decimal? overallRate)
        {
            var result = new List<ExceptionGroup>();
            if (overallRate == null || overallRate.Value == 0m) return result;
            foreach (ExceptionGroup g in Ranked(groups))
            {
                if (result.Count == 3) break;
                if (g.Exceptions >= 2 && g.Rate.Value >= 2m * overallRate.Value) result.Add(g);
            }
            return result;
        }
    }

    /// <summary>Display names that stay unique when two people share a name.</summary>
    public static class DisplayNames
    {
        /// <summary>Returns id -> name, adding " (#id)" to every name that appears more than once.</summary>
        public static Dictionary<int, string> Unique(IEnumerable<KeyValuePair<int, string>> people)
        {
            var count = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var all = new List<KeyValuePair<int, string>>(people);
            foreach (KeyValuePair<int, string> p in all)
            {
                int n;
                count.TryGetValue(p.Value ?? string.Empty, out n);
                count[p.Value ?? string.Empty] = n + 1;
            }
            var result = new Dictionary<int, string>();
            foreach (KeyValuePair<int, string> p in all)
            {
                string name = p.Value ?? string.Empty;
                result[p.Key] = count[name] > 1 ? name + " (#" + p.Key.ToString(CultureInfo.InvariantCulture) + ")" : name;
            }
            return result;
        }
    }
}
