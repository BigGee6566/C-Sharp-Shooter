using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Tests.Unit
{
    /// <summary>
    /// CSV export rules, exercised through BuildCsv (no HttpResponse needed): formula triggers
    /// are neutralised so a crafted field cannot execute when the report is opened in a
    /// spreadsheet, genuine numbers survive, and RFC-4180 quoting still applies.
    /// </summary>
    [TestClass]
    public class ReportExportTests
    {
        private static string[] Rows(params string[] values)
        {
            var dt = new DataTable();
            dt.Columns.Add("V", typeof(string));
            foreach (string v in values) dt.Rows.Add(v);

            string csv = ReportExport.BuildCsv(dt);
            return csv.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        }

        [TestMethod]
        public void Formula_triggers_are_prefixed_with_an_apostrophe()
        {
            string[] lines = Rows("=1+2", "@SUM(A1)", "\tTAB");
            // lines[0] is the header "V"
            Assert.AreEqual("'=1+2", lines[1]);
            Assert.AreEqual("'@SUM(A1)", lines[2]);
            Assert.AreEqual("'\tTAB", lines[3]);
        }

        [TestMethod]
        public void A_formula_that_also_needs_quoting_is_neutralised_then_quoted()
        {
            string[] lines = Rows("=HYPERLINK(\"http://evil\",\"x\")");
            // apostrophe added first, then the whole cell quoted with doubled inner quotes
            Assert.AreEqual("\"'=HYPERLINK(\"\"http://evil\"\",\"\"x\"\")\"", lines[1]);
        }

        [TestMethod]
        public void Genuine_signed_numbers_are_not_mangled()
        {
            string[] lines = Rows("-42.50", "1234");
            Assert.AreEqual("-42.50", lines[1], "a negative amount must stay a number");
            Assert.AreEqual("1234", lines[2]);
        }

        [TestMethod]
        public void Phone_like_plus_value_is_treated_as_text_and_neutralised()
        {
            string[] lines = Rows("+27 71 234 5678");
            Assert.AreEqual("'+27 71 234 5678", lines[1]);
        }

        [TestMethod]
        public void Ordinary_values_pass_through_and_commas_still_force_quoting()
        {
            string[] lines = Rows("Cape Town", "Doe, John");
            Assert.AreEqual("Cape Town", lines[1]);
            Assert.AreEqual("\"Doe, John\"", lines[2]);
        }
    }
}
