using System.Data;
using System.Text;
using System.Web;

namespace ForteLogisticsApp.Services
{
    public static class ReportExport
    {
        /// <summary>Streams a DataTable to the browser as a downloadable CSV file.</summary>
        public static void ToCsv(HttpResponse response, DataTable dt, string fileName)
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

            response.Clear();
            response.ContentType = "text/csv";
            response.AddHeader("Content-Disposition", "attachment; filename=" + fileName);
            response.Write(sb.ToString());
            response.End();
        }

        private static string Escape(string value)
        {
            if (value == null) return "";
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }
    }
}
