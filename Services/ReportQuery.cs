using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;

namespace ForteLogisticsApp.Services
{
    /// <summary>
    /// The shared period picker. A report's period lives in its URL - either a named quick
    /// range (?preset=30d) or explicit dates (?from=2026-09-01&amp;to=2026-09-30), optionally with
    /// a bucket size (&amp;group=week) - so every report view can be bookmarked or shared, and
    /// "Apply"/"Export" redirect to a plain GET (a refresh never re-posts a form). Parsing is pure
    /// and unit-tested; nothing here trusts the request beyond turning text into a validated
    /// ReportPeriod, and every problem comes back as a message the page can show.
    /// </summary>
    public static class ReportQuery
    {
        public const string CustomPreset = "custom";

        /// <summary>The group-by choice meaning "pick a sensible bucket size for the range".</summary>
        public const string AutoGroup = "auto";

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
            ddl.Items.Add(new ListItem("Automatic", AutoGroup));
            ddl.Items.Add(new ListItem("Day", "day"));
            ddl.Items.Add(new ListItem("Week (Mon-Sun)", "week"));
            ddl.Items.Add(new ListItem("Month", "month"));
        }

        public static bool IsKnownPreset(string key)
        {
            if (string.IsNullOrEmpty(key) || key == CustomPreset) return false;
            foreach (string[] p in Presets) if (p[0] == key) return true;
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
                    string invalid = ReportPeriod.Validate(f, t, g, today);
                    if (invalid == null)
                    {
                        presetKey = CustomPreset;
                        return new ReportPeriod(f, t, g);
                    }
                    problem = invalid;
                }
                else
                {
                    problem = "Please enter both the From and To dates as real calendar dates (yyyy-mm-dd).";
                }
            }
            else if (preset == CustomPreset)
            {
                problem = "Choose both a From and a To date for a custom range.";
            }

            presetKey = IsKnownPreset(preset) ? preset : defaultPreset;
            ReportPeriod period = ReportPeriod.Preset(presetKey, today);

            // A preset may be regrouped (e.g. last 90 days by month) when the result is valid;
            // otherwise say so rather than silently keeping the preset's own bucket size.
            Granularity requested;
            if (TryGroup(group, out requested) && requested != period.Granularity)
            {
                if (ReportPeriod.Validate(period.From, period.To, requested, today) == null)
                {
                    period = new ReportPeriod(period.From, period.To, requested);
                }
                else if (problem == null)
                {
                    problem = "Grouping by " + GroupKey(requested) + " is not available for " +
                              PresetLabel(presetKey).ToLowerInvariant() + ", so it is shown by " +
                              GroupKey(period.Granularity) + ".";
                }
            }
            return period;
        }

        /// <summary>The group-by option to show as selected: the explicit choice in the URL, or Automatic.</summary>
        public static string GroupSelection(NameValueCollection query)
        {
            Granularity g;
            return TryGroup(query == null ? null : query["group"], out g) ? GroupKey(g) : AutoGroup;
        }

        /// <summary>
        /// The query string for what the user submitted. Dates the user edited (compared with the
        /// dates the page was showing) mean a custom range even when the Quick range still says a
        /// preset - so this works with JavaScript off. Valid dates are re-written in canonical
        /// form; unreadable ones are passed through encoded so the next GET can explain them.
        /// </summary>
        public static string FromForm(string preset, string from, string to, string group,
            string shownFrom, string shownTo)
        {
            string f = (from ?? string.Empty).Trim();
            string t = (to ?? string.Empty).Trim();
            bool datesEdited = !string.Equals(f, (shownFrom ?? string.Empty).Trim(), StringComparison.Ordinal) ||
                               !string.Equals(t, (shownTo ?? string.Empty).Trim(), StringComparison.Ordinal);

            Granularity g;
            string groupPart = TryGroup(group, out g) ? "&group=" + GroupKey(g) : string.Empty;

            if (IsKnownPreset(preset) && !datesEdited)
            {
                return "?preset=" + HttpUtility.UrlEncode(preset) + groupPart;
            }

            DateTime fd, td;
            if (TryDate(f, out fd) && TryDate(t, out td))
            {
                return "?from=" + IsoDate(fd) + "&to=" + IsoDate(td) + groupPart;
            }
            return "?from=" + HttpUtility.UrlEncode(f) + "&to=" + HttpUtility.UrlEncode(t) + groupPart;
        }

        /// <summary>The query string that reproduces <paramref name="period"/> exactly.</summary>
        public static string ToQuery(ReportPeriod period, string presetKey)
        {
            string group = GroupKey(period.Granularity);
            if (IsKnownPreset(presetKey))
            {
                return "?preset=" + HttpUtility.UrlEncode(presetKey) + "&group=" + group;
            }
            return "?from=" + IsoDate(period.From) + "&to=" + IsoDate(period.To) + "&group=" + group;
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
            switch ((group ?? string.Empty).Trim().ToLowerInvariant())
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
