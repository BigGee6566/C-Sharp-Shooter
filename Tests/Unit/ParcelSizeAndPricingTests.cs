using ForteLogisticsApp.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ForteLogisticsApp.Tests.Unit
{
    /// <summary>
    /// Covers: "Quotation-card prices are retrieved/validated server-side" and
    /// "Browser-submitted prices cannot override trusted prices" - these are pure logic, so
    /// they run without a database.
    /// </summary>
    [TestClass]
    public class ParcelSizeAndPricingTests
    {
        [TestMethod]
        public void Find_UnknownCode_FallsBackToDefault_NeverThrows()
        {
            // ParcelSize.Find's own contract: "the code arrives from a hidden field, so it
            // must never be trusted to match" - a tampered/garbage code must degrade safely,
            // not fail or expose an out-of-range band.
            ParcelSize result = ParcelSize.Find("<script>DROP TABLE Shipment</script>");
            Assert.AreEqual(ParcelSize.Default.Code, result.Code);
        }

        [TestMethod]
        public void Find_NullOrEmptyCode_FallsBackToDefault()
        {
            Assert.AreEqual(ParcelSize.Default.Code, ParcelSize.Find(null).Code);
            Assert.AreEqual(ParcelSize.Default.Code, ParcelSize.Find(string.Empty).Code);
        }

        [TestMethod]
        public void Find_KnownCode_IsCaseInsensitive()
        {
            Assert.AreEqual("Large", ParcelSize.Find("large").Code);
            Assert.AreEqual("Large", ParcelSize.Find("LARGE").Code);
        }

        [TestMethod]
        public void FromPrice_MatchesCourierServiceQuote_ForTheBandsOwnWeight()
        {
            // The card price is computed, not stored, precisely so it can never drift from
            // what BookShipment charges for the same band on a local (single-city) route.
            foreach (ParcelSize size in ParcelSize.All)
            {
                decimal expected = CourierService.CalculateQuote("Local", "Local", size.MaxWeightKg);
                Assert.AreEqual(expected, size.FromPrice, size.Code + " card price must equal the trusted quote formula.");
            }
        }

        [TestMethod]
        public void CalculateQuote_SameCityIsCheaperThanCrossCity_ForTheSameWeight()
        {
            decimal local = CourierService.CalculateQuote("Johannesburg", "Johannesburg", 5m);
            decimal crossCity = CourierService.CalculateQuote("Johannesburg", "Cape Town", 5m);
            Assert.IsTrue(crossCity > local);
        }

        [TestMethod]
        public void CalculateQuote_NeverGoesBelowBaseRate_ForZeroOrNegativeWeight()
        {
            decimal quote = CourierService.CalculateQuote("Durban", "Durban", 0m);
            Assert.AreEqual(85m, quote, "Weight is floored to 1kg, and 1kg is within the base rate (no per-kg surcharge below 2kg).");
        }

        [TestMethod]
        public void TryParseWeight_AcceptsBothDecimalSeparators()
        {
            decimal a, b;
            Assert.IsTrue(CourierService.TryParseWeight("3.5", out a));
            Assert.IsTrue(CourierService.TryParseWeight("3,5", out b));
            Assert.AreEqual(3.5m, a);
            Assert.AreEqual(3.5m, b);
        }

        [TestMethod]
        public void TryParseWeight_RejectsGarbageInput()
        {
            decimal w;
            Assert.IsFalse(CourierService.TryParseWeight("not-a-weight", out w));
            Assert.IsFalse(CourierService.TryParseWeight("", out w));
            Assert.IsFalse(CourierService.TryParseWeight(null, out w));
        }

        [TestMethod]
        public void ParseWeight_FallsBackForOutOfRangeValues()
        {
            Assert.AreEqual(1m, CourierService.ParseWeight("0", 1m));
            Assert.AreEqual(1m, CourierService.ParseWeight("5000", 1m));
            Assert.AreEqual(1m, CourierService.ParseWeight("garbage", 1m));
        }
    }
}
