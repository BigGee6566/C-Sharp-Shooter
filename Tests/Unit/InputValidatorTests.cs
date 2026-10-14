using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ForteLogisticsApp.Security;

namespace ForteLogisticsApp.Tests.Unit
{
    /// <summary>
    /// Pure checks of the shared input normalisation/validation. No HttpContext or database
    /// is needed - these are the rules the pages lean on, exercised directly.
    /// </summary>
    [TestClass]
    public class InputValidatorTests
    {
        [DataTestMethod]
        [DataRow("  hello   world  ", "hello world")]
        [DataRow("line1\r\nline2\tcol", "line1 line2 col")]
        [DataRow("", "")]
        [DataRow("   ", "")]
        public void NormalizeText_collapses_whitespace_and_trims(string input, string expected)
        {
            Assert.AreEqual(expected, InputValidator.NormalizeText(input));
        }

        [TestMethod]
        public void NormalizeText_caps_length_when_asked()
        {
            Assert.AreEqual("abcde", InputValidator.NormalizeText("abcdefghij", 5));
            Assert.AreEqual("abc", InputValidator.NormalizeText("abc", 10));
        }

        [DataTestMethod]
        [DataRow("  Foo@Bar.CO.ZA ", "foo@bar.co.za")]
        public void NormalizeEmail_trims_and_lowercases(string input, string expected)
        {
            Assert.AreEqual(expected, InputValidator.NormalizeEmail(input));
        }

        [DataTestMethod]
        [DataRow("a@b.co", true)]
        [DataRow("first.last@sub.example.com", true)]
        [DataRow("no-at-sign", false)]
        [DataRow("two@@at.com", false)]
        [DataRow("@nodomain.com", false)]
        [DataRow("trailing@", false)]
        [DataRow("space in@domain.com", true)]      // local part may contain a space pre-@, domain is clean
        [DataRow("nodot@domaincom", false)]
        [DataRow("has space@do main.com", false)]    // space in the domain is rejected
        public void IsValidEmail_accepts_structurally_sane_addresses(string input, bool expected)
        {
            Assert.AreEqual(expected, InputValidator.IsValidEmail(input));
        }

        [DataTestMethod]
        [DataRow("071 234 5678", "0712345678")]
        [DataRow("+27 71 234 5678", "+27712345678")]
        [DataRow("(011) 555-1234", "0115551234")]
        public void NormalizePhone_keeps_leading_plus_and_digits_only(string input, string expected)
        {
            Assert.AreEqual(expected, InputValidator.NormalizePhone(input));
        }

        [DataTestMethod]
        [DataRow("071 234 5678", true)]
        [DataRow("12345", false)]                     // too short
        [DataRow("+1234567890123456", false)]         // 16 digits, too long
        [DataRow("", false)]
        public void IsValidPhone_enforces_digit_count(string input, bool expected)
        {
            Assert.AreEqual(expected, InputValidator.IsValidPhone(input));
        }

        [DataTestMethod]
        [DataRow(" fcl-2000 0001 ", "FCL-20000001")]
        [DataRow("FCL-20000150", "FCL-20000150")]
        [DataRow("  ", "")]
        public void NormalizeTrackingNumber_strips_space_and_uppercases(string input, string expected)
        {
            Assert.AreEqual(expected, InputValidator.NormalizeTrackingNumber(input));
        }

        [DataTestMethod]
        [DataRow("5", true, 5)]
        [DataRow("2,5", true, 0)]       // en-ZA comma decimal: 2.5 (exact value checked separately)
        [DataRow("2.5", true, 0)]       // invariant dot decimal
        [DataRow("0", false, 0)]
        [DataRow("-3", false, 0)]
        [DataRow("abc", false, 0)]
        [DataRow("", false, 0)]
        public void TryParseWeight_accepts_only_positive_bounded_numbers(string input, bool expectedOk, int expectedWholeKg)
        {
            decimal kg;
            bool ok = InputValidator.TryParseWeight(input, out kg);
            Assert.AreEqual(expectedOk, ok);
            if (expectedOk && expectedWholeKg > 0) Assert.AreEqual((decimal)expectedWholeKg, kg);
        }

        [TestMethod]
        public void TryParseWeight_parses_both_decimal_separators_to_the_same_value()
        {
            decimal comma, dot;
            Assert.IsTrue(InputValidator.TryParseWeight("2,5", out comma));
            Assert.IsTrue(InputValidator.TryParseWeight("2.5", out dot));
            Assert.AreEqual(2.5m, comma);
            Assert.AreEqual(2.5m, dot);
        }

        [TestMethod]
        public void TryParseWeight_rejects_over_the_limit()
        {
            decimal kg;
            Assert.IsFalse(InputValidator.TryParseWeight("1001", out kg));
            Assert.IsTrue(InputValidator.TryParseWeight("1000", out kg));
        }

        [TestMethod]
        public void TryParseDate_accepts_real_dates_and_rejects_rubbish()
        {
            DateTime d;
            Assert.IsTrue(InputValidator.TryParseDate("2026-10-14", out d));
            Assert.AreEqual(new DateTime(2026, 10, 14), d.Date);
            Assert.IsFalse(InputValidator.TryParseDate("not-a-date", out d));
            Assert.IsFalse(InputValidator.TryParseDate("2026-13-40", out d));
        }
    }
}
