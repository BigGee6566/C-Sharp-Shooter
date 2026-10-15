using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Controls;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Reports
{
    /// <summary>
    /// The shared plumbing of every period-based report page, so each report only says what it
    /// shows. The period lives in the URL (ReportQuery): Apply and Export redirect to a GET built
    /// from the form, so a report can be bookmarked, shared and refreshed safely. A bad period
    /// falls back to the default with an error summary that keeps what the user typed and marks
    /// the fields. Pages declare the standard controls (same IDs as Reports/Performance.aspx):
    /// rptHeader, rptFooter, pnlError/litError, pnlFilters, ddlPreset, txtFrom, txtTo, ddlGroup,
    /// btnApply, btnExport, hfShownFrom, hfShownTo and litBody.
    ///
    /// These pages have no .designer.cs on purpose: the controls are the protected fields below,
    /// which the compiled page assigns by ID. A regenerated designer file would declare shadowing
    /// fields and leave these null - delete it rather than keep it.
    /// </summary>
    public abstract class PeriodReportPage : SecurePage
    {
        /// <summary>Detail tables stop at this many rows on screen and in print; the CSV export has them all.</summary>
        public const int MaxDetailRows = 500;

        protected ReportHeader rptHeader;
        protected ReportFooter rptFooter;
        protected DropDownList ddlPreset, ddlGroup;
        protected TextBox txtFrom, txtTo;
        protected Button btnApply, btnExport;
        protected HiddenField hfShownFrom, hfShownTo;
        protected Panel pnlError, pnlFilters;
        protected Literal litError, litBody;

        protected ReportPeriod Period;
        protected string PresetKey;

        /// <summary>"~/Reports/X.aspx".</summary>
        protected abstract string PageUrl { get; }

        /// <summary>The preset shown when the URL names no period.</summary>
        protected virtual string DefaultPreset
        {
            get { return "30d"; }
        }

        /// <summary>Role check; runs before anything is read.</summary>
        protected abstract void Authorize();

        /// <summary>The CSV for the current period.</summary>
        protected abstract DataTable ExportTable(out string fileName);

        /// <summary>Adds header filters and returns the report body's HTML.</summary>
        protected abstract string RenderBody();

        protected override void OnLoad(EventArgs e)
        {
            Authorize();
            // RequireRole redirects; never read data for a request that is already being turned away.
            if (Response.IsRequestBeingRedirected) return;

            string problem;
            Period = ReportQuery.Read(Request.QueryString, DateTime.Today, DefaultPreset, out PresetKey, out problem);
            base.OnLoad(e);
            if (IsPostBack) return;

            if (problem == null && string.Equals(Request.QueryString["export"], "csv", StringComparison.OrdinalIgnoreCase))
            {
                string fileName;
                DataTable table = ExportTable(out fileName);
                ReportExport.ToCsv(Response, table, fileName);
                return;
            }

            ReportQuery.BindPresets(ddlPreset);
            ReportQuery.BindGroups(ddlGroup);
            ddlPreset.SelectedValue = PresetKey;
            ddlGroup.SelectedValue = ReportQuery.GroupSelection(Request.QueryString);
            txtFrom.Text = ReportQuery.IsoDate(Period.From);
            txtTo.Text = ReportQuery.IsoDate(Period.To);
            // What the boxes showed, so Apply can tell whether the user edited the dates.
            hfShownFrom.Value = txtFrom.Text;
            hfShownTo.Value = txtTo.Text;
            if (problem != null) ShowError(problem);

            ChartAssets.Register(this);
            litBody.Text = RenderBody();
        }

        protected void btnApply_Click(object sender, EventArgs e)
        {
            Response.Redirect(ResolveUrl(PageUrl) + FormQuery());
        }

        /// <summary>Exports exactly what the form says - the same path as Apply - as a GET.</summary>
        protected void btnExport_Click(object sender, EventArgs e)
        {
            Response.Redirect(ResolveUrl(PageUrl) + FormQuery() + "&export=csv");
        }

        private string FormQuery()
        {
            return ReportQuery.FromForm(ddlPreset.SelectedValue, txtFrom.Text, txtTo.Text, ddlGroup.SelectedValue,
                hfShownFrom.Value, hfShownTo.Value);
        }

        private void ShowError(string problem)
        {
            pnlError.Visible = true;
            litError.Text = HttpUtility.HtmlEncode(problem + " Showing " +
                ReportQuery.PresetLabel(PresetKey).ToLowerInvariant() + " instead.");

            // When the dates were the problem, keep what the user typed and mark the fields.
            string rawFrom = Request.QueryString["from"], rawTo = Request.QueryString["to"];
            if (string.IsNullOrEmpty(rawFrom) && string.IsNullOrEmpty(rawTo)) return;
            txtFrom.Text = rawFrom ?? string.Empty;
            txtTo.Text = rawTo ?? string.Empty;
            foreach (TextBox box in new[] { txtFrom, txtTo })
            {
                box.Attributes["aria-invalid"] = "true";
                box.Attributes["aria-describedby"] = pnlError.ClientID;
            }
        }

        // ------------------------------------------------------------------ helpers for reports

        /// <summary>The standard header filters: the period, its grouping and what it is compared with.</summary>
        protected void AddPeriodFilters(bool comparedWithPrevious)
        {
            rptHeader.AddFilter("Period", DescribeRange(Period));
            rptHeader.AddFilter("Grouped by", ReportQuery.GroupLabel(Period.Granularity));
            if (comparedWithPrevious) rptHeader.AddFilter("Compared with", DescribeRange(Period.Previous()));
        }

        protected static string DescribeRange(ReportPeriod p)
        {
            return p.From.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) + " to " +
                   p.To.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) + " (" + p.Days + (p.Days == 1 ? " day)" : " days)");
        }

        /// <summary>The viewer's own dashboard, for the breadcrumb's "Home".</summary>
        protected string HomeUrl
        {
            get
            {
                if (string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase)) return ResolveUrl("~/Admin/Dashboard.aspx");
                return ResolveUrl(IsDriver(UserId) ? "~/Driver/Dashboard.aspx" : "~/Ops/Dashboard.aspx");
            }
        }

        /// <summary>For ReportHtml.Attention links.</summary>
        protected string Href(string appRelative)
        {
            return ResolveUrl(appRelative);
        }

        /// <summary>See <see cref="ReportHtml.DetailTable"/>; <see cref="MaxDetailRows"/> rows at most.</summary>
        protected static string DetailTable(string[] headers, IList<string[]> rows, string caption, string emptyText, bool numeric,
            int? totalRows = null)
        {
            return ReportHtml.DetailTable(headers, rows, caption, emptyText, numeric, MaxDetailRows, totalRows);
        }

        /// <summary>Day-by-day totals from rows with a date column, ready for <see cref="ReportPeriod.Rollup"/>.</summary>
        protected static Dictionary<DateTime, decimal> Daily(DataTable dt, string dateColumn, Func<DataRow, bool> include)
        {
            var daily = new Dictionary<DateTime, decimal>();
            foreach (DataRow r in dt.Rows)
            {
                if (include != null && !include(r)) continue;
                object v = r[dateColumn];
                if (v == null || v == DBNull.Value) continue;
                DateTime day = Convert.ToDateTime(v, CultureInfo.InvariantCulture).Date;
                decimal n;
                daily.TryGetValue(day, out n);
                daily[day] = n + 1m;
            }
            return daily;
        }

        protected static string Lower(string s)
        {
            return string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s.Substring(1);
        }
    }
}
