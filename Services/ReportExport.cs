using System.Data;
using System.Globalization;
using System.Text;
using System.Web;

namespace ForteLogisticsApp.Services
{
    public static class ReportExport
    {
        /// <summary>Streams a DataTable to the browser as a downloadable CSV file.</summary>
        public static void ToCsv(HttpResponse response, DataTable dt, string fileName)
        {
            string csv = BuildCsv(dt);

            response.Clear();
            response.ContentType = "text/csv";
            // Quote the filename and strip CR/LF so a value can never break out of the header
            // into an injected one (defensive - today's callers pass static literals).
            string safeName = SanitizeFileName(fileName);
            response.AddHeader("Content-Disposition", "attachment; filename=\"" + safeName + "\"");
            response.Write(csv);
            response.End();
        }

        /// <summary>
        /// Renders the DataTable as RFC-4180 CSV text with formula-injection neutralisation.
        /// Separated from <see cref="ToCsv"/> (which only adds the HTTP streaming) so the
        /// escaping rules can be unit-tested without an HttpResponse.
        /// </summary>
        public static string BuildCsv(DataTable dt)
        {
            var sb = new StringBuilder();
            for (int c = 0; c < dt.Columns.Count; c++)
            {
                if (c > 0) sb.Append(',');
                sb.Append(Escape(dt.Columns[c].ColumnName));
            }
            sb.AppendLine();

            foreach (DataRow row in dt.Rows)
            {
                for (int c = 0; c < dt.Columns.Count; c++)
                {
                    if (c > 0) sb.Append(',');
                    sb.Append(Escape(row[c] == null ? "" : row[c].ToString()));
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string Escape(string value)
        {
            if (value == null) return "";

            // Neutralise CSV/formula injection BEFORE RFC-4180 quoting: a spreadsheet treats a
            // cell beginning with = + - @ (or a tab / carriage return) as a formula, so a value
            // like "=HYPERLINK(...)" or "=cmd|..." pasted into a tracked field would execute on
            // open. Genuine signed numbers (amounts, counts) are left intact so numeric columns
            // are not corrupted; only non-numeric trigger values are prefixed with an apostrophe.
            string neutralised = NeutralizeFormula(value);

            if (neutralised.Contains(",") || neutralised.Contains("\"") ||
                neutralised.Contains("\n") || neutralised.Contains("\r"))
                return "\"" + neutralised.Replace("\"", "\"\"") + "\"";
            return neutralised;
        }

        private static string NeutralizeFormula(string value)
        {
            if (value.Length == 0) return value;

            char first = value[0];
            bool trigger = first == '=' || first == '+' || first == '-' ||
                           first == '@' || first == '\t' || first == '\r';
            if (!trigger) return value;

            // A plain signed number (e.g. -42.50) is harmless and must stay numeric.
            decimal _;
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo("en-ZA"), out _) ||
                decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                return value;

            return "'" + value;
        }

        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return "export.csv";
            return fileName.Replace("\r", "").Replace("\n", "").Replace("\"", "");
        }
    }
}
