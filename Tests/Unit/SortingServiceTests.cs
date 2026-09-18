using ForteLogisticsApp.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ForteLogisticsApp.Tests.Unit
{
    /// <summary>
    /// Covers: "East London parcels are not assigned to Cape Town routes" at the level this
    /// codebase actually represents destinations - a sort lane derived from the city name.
    /// East London itself is not one of the five named service cities, so it (like any other
    /// unlisted town) falls into the generic "Regional-Outlying" lane, distinct from
    /// "Metro-CPT" - which is exactly what stops the mix-up once Ops\RoutePlanning.aspx.cs
    /// checks a parcel's SortCategory against its route's ServiceArea (see
    /// Ops\RoutePlanning.aspx.cs btnAssign_Click).
    /// </summary>
    [TestClass]
    public class SortingServiceTests
    {
        [TestMethod]
        public void CategoryFor_NamedMetro_MapsToItsOwnLane()
        {
            Assert.AreEqual("Metro-CPT", SortingService.CategoryFor("Cape Town"));
            Assert.AreEqual("Metro-JHB", SortingService.CategoryFor("Johannesburg"));
            Assert.AreEqual("Metro-DBN", SortingService.CategoryFor("Durban"));
            Assert.AreEqual("Metro-PE", SortingService.CategoryFor("Gqeberha"));
        }

        [TestMethod]
        public void CategoryFor_IsCaseAndWhitespaceInsensitive()
        {
            Assert.AreEqual("Metro-CPT", SortingService.CategoryFor("  cape town  "));
            Assert.AreEqual("Metro-CPT", SortingService.CategoryFor("CAPE TOWN"));
        }

        [TestMethod]
        public void CategoryFor_EastLondon_DoesNotShareCapeTownsLane()
        {
            string eastLondonLane = SortingService.CategoryFor("East London");
            string capeTownLane = SortingService.CategoryFor("Cape Town");

            Assert.AreEqual("Regional-Outlying", eastLondonLane);
            Assert.AreNotEqual(capeTownLane, eastLondonLane,
                "An East London parcel must never be sorted into the same lane as a Cape Town route.");
        }

        [TestMethod]
        public void CategoryFor_UnknownOrMissingCity_FallsBackToRegionalOutlying_NeverNullOrEmpty()
        {
            Assert.AreEqual("Regional-Outlying", SortingService.CategoryFor("Nowhereville"));
            Assert.AreEqual("Regional-Outlying", SortingService.CategoryFor(null));
            Assert.AreEqual("Regional-Outlying", SortingService.CategoryFor(string.Empty));
        }
    }
}
