using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;

namespace ForteLogisticsApp.Services
{
    /// <summary>
    /// The shared period picker. A report's period lives in its URL - either a named quick
    /// range (?preset=30d) or explicit dates (?from=2026-09-01&amp;to=2026-09-30&amp;group=week) - so
    /// every report view can be bookmarked or shared, and "Apply" is a redirect to a plain GET
    /// (a refresh never re-posts a form). Parsing is pure and unit-tested; nothing here
    /// trusts the request beyond turning text into a validated ReportPeriod.
    /// </summary>
    public static class ReportQuery
    {
        public const string CustomPreset = "custom";

        private static readonly string[][] Presets =
        {
            new[] { "today", "Today" },
            new[] { "7d", "Last 7 days" },
            new[] { "30d", "Last 30 days" },
            new[] { "thismonth", "This month" },
            new[] { "lastmonth", "Last month" },
            new[] { "90d", "Last 90 days" },
            new[] { "12m", "Last 12 months" },
            new[] { "ytd", "Year to date" },
            new[] { CustomPreset, "Custom dates" }
        };

        public static void BindPresets(DropDownList ddl)
        {
            ddl.Items.Clear();
            foreach (string[] p in Presets) ddl.Items.Add(new ListItem(p[1], p[0]));
        }

        public static void BindGroups(DropDownList ddl)
        {
            ddl.Items.Clear();
            ddl.Items.Add(new ListItem("Day", "day"));
            ddl.Items.Add(new ListItem("Week (Mon-Sun)", "week"));
            ddl.Items.Add(new ListItem("Month", "month"));
        }

        public static bool IsKnownPreset(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            foreach (string[] p in Presets)
            {
                if (p[0] == key && key != CustomPreset) return true;
            }
            return false;
        }

        public static string PresetLabel(string key)
        {
            foreach (string[] p in Presets) if (p[0] == key) return p[1];
            return "Custom dates";
        }

        /// <summary>
        /// Reads the period from a query string. Explicit dates win over a preset; anything
        /// unreadable or out of range falls back to <paramref name="defaultPreset"/> and sets
        /// <paramref name="problem"/> to a message the page can show.
        /// </summary>
        public static ReportPeriod Read(NameValueCollection query, DateTime today, string defaultPreset,
            out string presetKey, out string problem)
        {
            problem = null;
            string from = query == null ? null : query["from"];
            string to = query == null ? null : query["to"];
            string group = query == null ? null : query["group"];
            string preset = query == null ? null : query["preset"];

            if (!string.IsNullOrEmpty(from) || !string.IsNullOrEmpty(to))
            {
                DateTime f, t;
                if (TryDate(from, out f) && TryDate(to, out t))
                {
                    Granularity g = ParseGroup(group, Math.Abs((t - f).Days) + 1);
                    string invalid = ReportPeriod.Validate(f, t, g);
                    if (invalid == null)
                    {
                        presetKey = CustomPreset;
                        return new ReportPeriod(f, t, g);
                    }
                    problem = invalid;
                }
                else
                {
                    problem = "Please enter both dates as valid calendar dates.";
                }
            }

            presetKey = IsKnownPreset(preset) ? preset : defaultPreset;
            ReportPeriod period = ReportPeriod.Preset(presetKey, today);

            // A preset may be regrouped (e.g. last 90 days by month) if the result is still valid.
            Granularity requested;
            if (TryGroup(group, out requested) && requested != period.Granularity &&
                ReportPeriod.Validate(period.From, period.To, requested) == null)
            {
                period = new ReportPeriod(period.From, period.To, requested);
            }
            return period;
        }

        /// <summary>The query string that reproduces <paramref name="period"/>.</summary>
        public static string ToQuery(ReportPeriod period, string presetKey)
        {
            string group = GroupKey(period.Granularity);
            if (IsKnownPreset(presetKey))
            {
                return "?preset=" + HttpUtility.UrlEncode(presetKey) + "&group=" + group;
            }
            return "?from=" + period.From.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) +
                   "&to=" + period.To.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) +
                   "&group=" + group;
        }

        public static string GroupKey(Granularity g)
        {
            return g == Granularity.Week ? "week" : g == Granularity.Month ? "month" : "day";
        }

        public static string GroupLabel(Granularity g)
        {
            return g == Granularity.Week ? "Week (Mon-Sun)" : g == Granularity.Month ? "Month" : "Day";
        }

        /// <summary>The bucket size to use when none is asked for: roughly 30 points or fewer.</summary>
        public static Granularity ParseGroup(string group, int days)
        {
            Granularity g;
            if (TryGroup(group, out g)) return g;
            return days <= 45 ? Granularity.Day : days <= 200 ? Granularity.Week : Granularity.Month;
        }

        public static bool TryGroup(string group, out Granularity g)
        {
            switch ((group ?? string.Empty).ToLowerInvariant())
            {
                case "day": g = Granularity.Day; return true;
                case "week": g = Granularity.Week; return true;
                case "month": g = Granularity.Month; return true;
                default: g = Granularity.Day; return false;
            }
        }

        /// <summary>yyyy-MM-dd only - the format an HTML date input submits.</summary>
        public static bool TryDate(string value, out DateTime date)
        {
            return DateTime.TryParseExact((value ?? string.Empty).Trim(), "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        public static string IsoDate(DateTime d)
        {
            return d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
    }
}
