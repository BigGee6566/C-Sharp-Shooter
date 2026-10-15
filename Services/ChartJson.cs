using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.UI;

namespace ForteLogisticsApp.Services
{
    /// <summary>
    /// Embeds server-computed chart data in a page as inert JSON that Scripts/lc-charts.js
    /// reads with JSON.parse. The data is never spliced into executable script, so a value
    /// such as a client or city name can only ever be data.
    ///
    /// The JSON sits inside a &lt;script type="application/json"&gt; element, which the HTML
    /// parser still ends at the first "&lt;/script". Escaping &lt; &gt; &amp; and the two
    /// JavaScript line terminators (U+2028, U+2029) as \u escapes makes that impossible
    /// regardless of what the serializer itself chooses to escape.
    /// </summary>
    public static class ChartJson
    {
        // Numeric code points rather than escape literals, so the source file itself never
        // contains a raw line-terminator character.
        private const char LineSeparator = (char)0x2028;
        private const char ParagraphSeparator = (char)0x2029;

        public static string Serialize(object data)
        {
            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            return EscapeForHtml(serializer.Serialize(data));
        }

        public static string EscapeForHtml(string json)
        {
            if (string.IsNullOrEmpty(json)) return "null";

            var sb = new StringBuilder(json.Length + 16);
            foreach (char c in json)
            {
                switch (c)
                {
                    case '<': sb.Append("\\u003c"); break;
                    case '>': sb.Append("\\u003e"); break;
                    case '&': sb.Append("\\u0026"); break;
                    case LineSeparator: sb.Append("\\u2028"); break;
                    case ParagraphSeparator: sb.Append("\\u2029"); break;
                    default: sb.Append(c); break;
                }
            }
            return sb.ToString();
        }

        /// <summary>The complete inert data element, ready to write into a Literal.</summary>
        public static string ScriptBlock(string id, object data)
        {
            return "<script type=\"application/json\" id=\"" + HttpUtility.HtmlAttributeEncode(id) + "\">" +
                   Serialize(data) + "</script>";
        }
    }

    /// <summary>
    /// Loads the charting scripts on the pages that need them only - Chart.js is ~200 KB and
    /// has no business on the login page. Chart.js is vendored in Scripts/vendor (verified
    /// against its published SRI hash) so charts still render with no internet connection.
    /// The small Styles/reports.css is linked from Site1.Master instead: the master's head
    /// contains &lt;%= %&gt; code blocks, so adding a control to Page.Header here would throw.
    /// </summary>
    public static class ChartAssets
    {
        public static void Register(Page page)
        {
            ClientScriptManager cs = page.ClientScript;
            cs.RegisterClientScriptInclude(typeof(ChartAssets), "chartjs",
                Versioned(page, "~/Scripts/vendor/chart.umd.min.js"));
            cs.RegisterClientScriptInclude(typeof(ChartAssets), "lccharts",
                Versioned(page, "~/Scripts/lc-charts.js"));
        }

        /// <summary>
        /// Appends the file's last-write time as a query string so a browser never keeps an
        /// old copy of a script after the app is updated.
        /// </summary>
        public static string Versioned(Page page, string virtualPath)
        {
            string url = page.ResolveUrl(virtualPath);
            try
            {
                string physical = System.Web.Hosting.HostingEnvironment.MapPath(virtualPath);
                if (physical != null && System.IO.File.Exists(physical))
                {
                    url += "?v=" + System.IO.File.GetLastWriteTimeUtc(physical).Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            catch (System.Exception)
            {
                // Versioning is a cache nicety; never let it stop the page rendering.
            }
            return url;
        }
    }
}
