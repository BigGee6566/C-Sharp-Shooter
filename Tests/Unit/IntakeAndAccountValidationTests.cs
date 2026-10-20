using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.Tests.Unit
{
    /// <summary>Server-side validation for the creation flows (bookings, counter parcels, accounts).</summary>
    [TestClass]
    public class IntakeAndAccountValidationTests
    {
        private static ShipmentDraft Draft()
        {
            return new ShipmentDraft
            {
                SenderId = 7,
                ReceiverName = "  Thandi   Mokoena ",
                ReceiverPhone = "071 234 5678",
                PickupAddress = "1 Main Rd",
                DeliveryAddress = "2 Long St",
                OriginCity = "Johannesburg",
                DestCity = "Cape Town",
                WeightKg = 2.5m,
                Description = "Books",
                Cost = 145m,
                TrackingLocation = "Johannesburg Depot",
                TrackingRemark = "Registered"
            };
        }

        private static string Problem(ShipmentDraft d)
        {
            IntakeService.Normalize(d);
            return IntakeService.Validate(d);
        }

        [TestMethod]
        public void A_good_draft_is_tidied_and_accepted()
        {
            ShipmentDraft d = Draft();
            Assert.IsNull(Problem(d));
            Assert.AreEqual("Thandi Mokoena", d.ReceiverName);
            Assert.AreEqual("0712345678", d.ReceiverPhone);
        }

        [TestMethod]
        public void Over_long_addresses_are_refused_not_silently_cut()
        {
            ShipmentDraft d = Draft();
            d.DeliveryAddress = new string('y', 400);
            StringAssert.Contains(Problem(d), "too long", "cutting would drop the city and postal code");
            Assert.AreEqual(400, d.DeliveryAddress.Length, "the address is left whole for the user to fix");

            d = Draft(); d.DestCity = new string('x', 61);
            StringAssert.Contains(Problem(d), "too long");
            d = Draft(); d.ReceiverName = new string('n', 151);
            StringAssert.Contains(Problem(d), "too long");
        }

        [TestMethod]
        public void The_address_step_checks_addresses_only()
        {
            ShipmentDraft d = Draft();
            d.ReceiverName = string.Empty;  // collected on the next step
            IntakeService.Normalize(d);
            Assert.IsNull(IntakeService.ValidateAddresses(d));
            d.PickupAddress = new string('p', 251);
            StringAssert.Contains(IntakeService.ValidateAddresses(d), "too long");
        }

        [TestMethod]
        public void Bad_drafts_are_refused_with_a_reason()
        {
            ShipmentDraft d = Draft(); d.ReceiverName = "   ";
            StringAssert.Contains(Problem(d), "recipient");

            d = Draft(); d.WeightKg = 0m;
            StringAssert.Contains(Problem(d), "weight", "never a silent 1 kg default");

            d = Draft(); d.WeightKg = 0.004m;
            StringAssert.Contains(Problem(d), "weight", "0.004 kg would be stored as 0.00");

            d = Draft(); d.WeightKg = 1001m;
            StringAssert.Contains(Problem(d), "weight");

            d = Draft(); d.ReceiverPhone = "12";
            StringAssert.Contains(Problem(d), "phone");

            d = Draft(); d.Cost = 0m;
            StringAssert.Contains(Problem(d), "price");

            d = Draft(); d.SenderId = null; d.CreateInvoice = true;
            StringAssert.Contains(Problem(d), "invoice", "an invoice needs someone to bill");
        }

        [TestMethod]
        public void Invisible_characters_never_reach_the_database()
        {
            // Zero-width space, right-to-left override, NUL.
            string sneaky = "Jo​hn ‮Smith\0";
            Assert.AreEqual("John Smith", InputValidator.NormalizeText(sneaky));
        }

        private static NewAccount Account()
        {
            return new NewAccount
            {
                FullName = " Sipho  Dlamini ",
                Email = "  Sipho@Example.COM ",
                Phone = "+27 71 234 5678",
                Address = "3 Park Ave",
                Password = "secret123",
                Role = "Customer",
                ClientType = "business"
            };
        }

        private static string Problem(NewAccount a)
        {
            AccountService.Normalize(a);
            return AccountService.Validate(a);
        }

        [TestMethod]
        public void A_good_account_is_tidied_and_accepted()
        {
            NewAccount a = Account();
            Assert.IsNull(Problem(a));
            Assert.AreEqual("sipho@example.com", a.Email, "one canonical form, so duplicates are caught");
            Assert.AreEqual("Business", a.ClientType);
            Assert.AreEqual("Sipho Dlamini", a.FullName);
        }

        [TestMethod]
        public void Bad_accounts_are_refused_with_a_reason()
        {
            NewAccount a = Account(); a.Email = "not-an-email";
            StringAssert.Contains(Problem(a), "email");

            a = Account(); a.Email = new string('a', 140) + "@example.com";
            StringAssert.Contains(Problem(a), "email", "longer than the 150-character column");

            a = Account(); a.Email = "sіpho@example.com";   // Cyrillic i
            StringAssert.Contains(Problem(a), "email", "look-alike letters are refused");

            a = Account(); a.Password = "1234567";
            StringAssert.Contains(Problem(a), "password");

            a = Account(); a.Password = new string('p', 129);
            StringAssert.Contains(Problem(a), "password", "no unbounded hashing work");

            a = Account(); a.Password = "Sipho@Example.com";
            StringAssert.Contains(Problem(a), "same as the email");

            a = Account(); a.Role = "SuperUser";
            StringAssert.Contains(Problem(a), "account type", "a forged role is refused");

            a = Account(); a.IsDriver = true;
            StringAssert.Contains(Problem(a), "drivers", "only staff can drive");

            a = Account(); a.ClientType = "Platinum";
            Assert.IsNull(Problem(a));
            Assert.AreEqual("Individual", a.ClientType, "unknown client types fall back to Individual");
        }

        [TestMethod]
        public void Sign_ups_are_throttled_per_key_within_the_window()
        {
            string key = "test:" + Guid.NewGuid().ToString("N");
            for (int i = 0; i < 3; i++) Assert.IsTrue(RequestThrottle.Allow(key, 3, TimeSpan.FromMinutes(10)), "attempt " + (i + 1));
            Assert.IsFalse(RequestThrottle.Allow(key, 3, TimeSpan.FromMinutes(10)), "the fourth is refused");
            Assert.IsTrue(RequestThrottle.Allow(key + "-other", 3, TimeSpan.FromMinutes(10)), "other callers are unaffected");
        }
    }
}
