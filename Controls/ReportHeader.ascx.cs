using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ForteLogisticsApp.Controls
{
    /// <summary>
    /// The branded header every report wears, on screen and in print: the system logo and
    /// name, the report title, a report-type badge, the organisation line, a short purpose
    /// line, and the filters the report was run with - so a printed copy always says exactly
    /// what it covers.
    ///
    /// The page sets the properties and calls AddFilter during Load or a click handler; the
    /// markup is built in PreRender, after those have run.
    /// </summary>
    public partial class ReportHeader : UserControl
    {
        protected Literal litHeader;

        private readonly List<KeyValuePair<string, string>> filters = new List<KeyValuePair<string, string>>();

        public string ReportTitle { get; set; }

        /// <summary>Short category shown as a badge, e.g. "Performance report".</summary>
        public string ReportType { get; set; }

        /// <summary>One line on what decision this report supports.</summary>
        public string Description { get; set; }

        /// <summary>Records one filter the report was run with. A blank value reads as "All".</summary>
        public void AddFilter(string label, string value)
        {
            if (string.IsNullOrWhiteSpace(label)) return;
            filters.Add(new KeyValuePair<string, string>(label, string.IsNullOrWhiteSpace(value) ? "All" : value));
        }

        public void ClearFilters()
        {
            filters.Clear();
        }

        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);
            litHeader.Text = Build();
        }

        private string Build()
        {
            var sb = new StringBuilder();
            sb.Append("<header class='lc-report-header'>")
              .Append("<div class='lc-rh-top'>")
              .Append("<img class='lc-rh-logo' src='").Append(HttpUtility.HtmlAttributeEncode(ResolveUrl("~/Images/web/logo.png")))
              // Decorative: the organisation name is right beside it as text.
              .Append("' alt='' />")
              .Append("<div class='lc-rh-titles'>")
              .Append("<div class='lc-rh-system'>Logisticore Courier Service &middot; Management Information</div>")
              .Append("<h1 class='lc-rh-title'>").Append(Enc(ReportTitle)).Append("</h1>")
              .Append("<div class='lc-rh-org'>Logisticore Courier Service (Pty) Ltd &middot; Isando, Gauteng &middot; 0860 LOGISTICORE &middot; support@logisticore.co.za</div>")
              .Append("</div>");

            if (!string.IsNullOrEmpty(ReportType))
            {
                sb.Append("<div class='lc-rh-badge'>").Append(Enc(ReportType)).Append("</div>");
            }
            sb.Append("</div>");

            if (!string.IsNullOrEmpty(Description))
            {
                sb.Append("<p class='lc-rh-desc'>").Append(Enc(Description)).Append("</p>");
            }

            if (filters.Count > 0)
            {
                sb.Append("<dl class='lc-rh-filters'>");
                foreach (KeyValuePair<string, string> f in filters)
                {
                    sb.Append("<div><dt>").Append(Enc(f.Key)).Append("</dt><dd>").Append(Enc(f.Value)).Append("</dd></div>");
                }
                sb.Append("</dl>");
            }

            sb.Append("</header>");
            return sb.ToString();
        }

        private static string Enc(string value)
        {
            return HttpUtility.HtmlEncode(value ?? string.Empty);
        }
    }
}
