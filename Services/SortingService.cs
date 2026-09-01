using System;
using System.Web.UI.WebControls;

namespace ForteLogisticsApp.Services
{
    /// <summary>
    /// Single source of truth for the service cities and warehouse sort lanes.
    /// Previously these lists were hardcoded as &lt;asp:ListItem&gt; blocks in three
    /// separate pages; adding a city now means editing one array here.
    /// </summary>
    public static class SortingService
    {
        public static readonly string[] Cities =
        {
            "Johannesburg", "Cape Town", "Durban", "Gqeberha", "Bloemfontein"
        };

        public static readonly string[] Categories =
        {
            "Metro-JHB", "Metro-CPT", "Metro-DBN", "Metro-PE", "Regional-Outlying", "Express-Priority"
        };

        /// <summary>
        /// Derives the warehouse sort lane from the destination city. The mapping is
        /// deterministic, so the operator never has to pick it by hand.
        /// Never returns null or an empty string.
        /// </summary>
        public static string CategoryFor(string destCity)
        {
            switch ((destCity ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "johannesburg": return "Metro-JHB";
                case "cape town":    return "Metro-CPT";
                case "durban":       return "Metro-DBN";
                case "gqeberha":     return "Metro-PE";
                default:             return "Regional-Outlying";
            }
        }

        public static void BindCities(ListControl list, string selected = null)
        {
            Bind(list, Cities, selected);
        }

        public static void BindCategories(ListControl list, string selected = null)
        {
            Bind(list, Categories, selected);
        }

        private static void Bind(ListControl list, string[] values, string selected)
        {
            if (list == null) return;

            list.Items.Clear();
            foreach (string value in values)
            {
                list.Items.Add(new ListItem(value, value));
            }
            SelectValue(list, selected);
        }

        /// <summary>Selects a value if it exists, otherwise falls back to the first item.</summary>
        public static void SelectValue(ListControl list, string value)
        {
            if (list == null || list.Items.Count == 0) return;

            list.ClearSelection();
            ListItem item = string.IsNullOrEmpty(value) ? null : list.Items.FindByValue(value);
            if (item != null) item.Selected = true;
            else list.SelectedIndex = 0;
        }

        /// <summary>Suggests a route name, e.g. "Metro-CPT · 05 Aug · Run 2".</summary>
        public static string SuggestRouteName(string category, DateTime date, int sequenceForDay)
        {
            if (string.IsNullOrEmpty(category)) category = "General";
            if (sequenceForDay < 1) sequenceForDay = 1;
            return category + " · " + date.ToString("dd MMM") + " · Run " + sequenceForDay;
        }
    }
}
