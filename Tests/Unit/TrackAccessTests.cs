using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ForteLogisticsApp.Security;

namespace ForteLogisticsApp.Tests.Unit
{
    /// <summary>Abuse cases for the live-tracking endpoint's access rules (TrackAccess).</summary>
    [TestClass]
    public class TrackAccessTests
    {
        private const int Sender = 10, Driver = 20, Stranger = 30, Office = 40, OtherDriver = 50;

        [TestMethod]
        public void Only_the_parties_and_office_staff_see_live_positions()
        {
            Assert.IsTrue(TrackAccess.CanSeeLivePositions(Sender, "Customer", false, Sender, Driver), "the sender");
            Assert.IsTrue(TrackAccess.CanSeeLivePositions(Driver, "Staff", true, Sender, Driver), "the assigned driver");
            Assert.IsTrue(TrackAccess.CanSeeLivePositions(Office, "Staff", false, Sender, Driver), "office staff");
            Assert.IsTrue(TrackAccess.CanSeeLivePositions(Office, "Admin", false, Sender, Driver), "an admin");

            Assert.IsFalse(TrackAccess.CanSeeLivePositions(Stranger, "Customer", false, Sender, Driver),
                "another customer who only knows the waybill");
            Assert.IsFalse(TrackAccess.CanSeeLivePositions(OtherDriver, "Staff", true, Sender, Driver),
                "a different driver");
            Assert.IsFalse(TrackAccess.CanSeeLivePositions(0, "Admin", false, Sender, Driver), "not signed in");
        }

        [TestMethod]
        public void A_walk_in_parcel_has_no_sender_to_match()
        {
            Assert.IsFalse(TrackAccess.CanSeeLivePositions(0, "Customer", false, 0, Driver));
            Assert.IsFalse(TrackAccess.CanPing("Customer", 0, 0, Driver, "Registered"));
        }

        [TestMethod]
        public void A_ping_is_only_accepted_for_the_role_the_caller_holds()
        {
            Assert.IsTrue(TrackAccess.CanPing("Customer", Sender, Sender, Driver, "Registered"));
            Assert.IsTrue(TrackAccess.CanPing("Driver", Driver, Sender, Driver, "OutForDelivery"));

            Assert.IsFalse(TrackAccess.CanPing("Driver", Sender, Sender, Driver, "OutForDelivery"), "the sender posing as the driver");
            Assert.IsFalse(TrackAccess.CanPing("Customer", Stranger, Sender, Driver, "Registered"), "a stranger posing as the sender");
            Assert.IsFalse(TrackAccess.CanPing("Admin", Office, Sender, Driver, "Registered"), "an unknown ping role");
        }

        [TestMethod]
        public void Nothing_is_tracked_once_the_parcel_is_finished_and_a_driver_only_while_carrying_it()
        {
            Assert.IsFalse(TrackAccess.CanPing("Customer", Sender, Sender, Driver, "Delivered"));
            Assert.IsFalse(TrackAccess.CanPing("Driver", Driver, Sender, Driver, "Failed"));
            Assert.IsFalse(TrackAccess.CanPing("Driver", Driver, Sender, Driver, "Registered"), "not yet with the driver");
            Assert.IsTrue(TrackAccess.CanPing("Driver", Driver, Sender, Driver, "Assigned"));
        }

        [TestMethod]
        public void Pings_must_come_from_this_site()
        {
            var url = new Uri("https://logisticore.example/TrackApi.aspx");
            Assert.IsTrue(TrackAccess.IsSameOrigin("https://logisticore.example", null, null, url));
            Assert.IsTrue(TrackAccess.IsSameOrigin("https://logisticore.example/", null, null, url), "trailing slash");
            Assert.IsTrue(TrackAccess.IsSameOrigin(null, "https://logisticore.example/Customer/Track.aspx?wb=FCL-1", null, url),
                "no Origin, matching Referer");

            Assert.IsFalse(TrackAccess.IsSameOrigin("https://evil.example", null, null, url), "cross-site form post");
            Assert.IsFalse(TrackAccess.IsSameOrigin("null", "https://logisticore.example/x", null, url), "sandboxed frame");
            Assert.IsFalse(TrackAccess.IsSameOrigin(null, "https://logisticore.example.evil.example/", null, url), "look-alike host");
            Assert.IsFalse(TrackAccess.IsSameOrigin(null, "not a url", null, url), "malformed Referer");
            Assert.IsFalse(TrackAccess.IsSameOrigin(string.Empty, null, null, url), "empty Origin and nothing else");
            Assert.IsFalse(TrackAccess.IsSameOrigin(null, null, null, url), "no evidence at all");
        }

        [TestMethod]
        public void Pings_work_behind_a_tls_terminating_proxy()
        {
            // The app sees plain http on an internal port; the browser's origin is https.
            var behindProxy = new Uri("http://logisticore.example:8080/TrackApi.aspx");
            Assert.IsTrue(TrackAccess.IsSameOrigin("https://logisticore.example", null, null, behindProxy));
            Assert.IsTrue(TrackAccess.IsSameOrigin(null, "https://logisticore.example/Driver/Map.aspx", null, behindProxy));
        }

        [TestMethod]
        public void The_browsers_own_fetch_metadata_decides_when_present()
        {
            var url = new Uri("https://logisticore.example/TrackApi.aspx");
            Assert.IsTrue(TrackAccess.IsSameOrigin(null, null, "same-origin", url));
            Assert.IsFalse(TrackAccess.IsSameOrigin("https://logisticore.example", null, "cross-site", url),
                "a forged Origin cannot outvote the browser");
            Assert.IsFalse(TrackAccess.IsSameOrigin(null, null, "same-site", url), "a sibling subdomain is not this site");
        }

        [TestMethod]
        public void Role_checks_ignore_case_and_an_unassigned_parcel_has_no_driver()
        {
            Assert.IsTrue(TrackAccess.CanSeeLivePositions(Office, "admin", false, Sender, Driver));
            Assert.IsFalse(TrackAccess.CanSeeLivePositions(OtherDriver, "Staff", true, Sender, 0), "no driver assigned yet");
            Assert.IsFalse(TrackAccess.CanPing("Driver", Driver, Sender, 0, "Assigned"));
        }
    }
}
