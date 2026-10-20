using System;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ForteLogisticsApp.Tests.Integration
{
    /// <summary>
    /// The creation paths end to end against a real database: a shipment is created with its
    /// first tracking event (and invoice) or not at all; an account is created with its role
    /// records or not at all, and a duplicate email is answered, not crashed on.
    /// </summary>
    [TestClass]
    public class IntakeAndAccountTests
    {
        private static ShipmentDraft Draft(int? senderId, bool invoice, string receiver)
        {
            return new ShipmentDraft
            {
                SenderId = senderId,
                ReceiverName = receiver,
                PickupAddress = "1 Test Rd",
                DeliveryAddress = "2 Test St",
                OriginCity = "Johannesburg",
                DestCity = "Cape Town",
                WeightKg = 3m,
                Cost = 163m,
                Description = "Integration test parcel",
                TrackingLocation = "Johannesburg (Booking)",
                TrackingRemark = "Integration test",
                CreateInvoice = invoice,
                PaymentGateStatus = invoice ? "PaymentRequired" : "NotRequired"
            };
        }

        private static void DeleteShipment(int shipmentId)
        {
            Db.NonQuery("DELETE FROM dbo.[TrackingHistory] WHERE ShipmentID = @s", "@s", shipmentId);
            Db.NonQuery("DELETE FROM dbo.[Payment] WHERE ShipmentID = @s", "@s", shipmentId);
            Db.NonQuery("DELETE FROM dbo.[Shipment] WHERE ShipmentID = @s", "@s", shipmentId);
        }

        private static void DeleteAccount(int userId)
        {
            Db.NonQuery("DELETE FROM dbo.AuditLog WHERE UserID = @u OR (Entity = 'User' AND EntityID = @e)", "@u", userId, "@e", userId.ToString());
            Db.NonQuery("DELETE FROM dbo.Notification WHERE UserID = @u", "@u", userId);
            Db.NonQuery("DELETE FROM dbo.[Driver] WHERE UserID = @u", "@u", userId);
            Db.NonQuery("DELETE FROM dbo.[Admin] WHERE UserID = @u", "@u", userId);
            Db.NonQuery("DELETE FROM dbo.[Client] WHERE UserID = @u", "@u", userId);
            Db.NonQuery("DELETE FROM dbo.[User] WHERE UserID = @u", "@u", userId);
        }

        [TestMethod]
        public void An_online_booking_creates_the_shipment_its_history_and_its_invoice_together()
        {
            DbTestHelper.EnsureDatabaseAvailable();
            int customer = DbTestHelper.CreateTestClient("intake", "Individual");
            CreatedShipment created = null;
            try
            {
                created = IntakeService.Create(Draft(customer, true, "Receiver One"));

                Assert.AreEqual(1, Db.ToInt(Db.Scalar("SELECT COUNT(*) FROM dbo.[Shipment] WHERE ShipmentID = @s AND SenderID = @c AND Cost = 163",
                    "@s", created.ShipmentId, "@c", customer)));
                Assert.AreEqual(1, Db.ToInt(Db.Scalar("SELECT COUNT(*) FROM dbo.[TrackingHistory] WHERE ShipmentID = @s AND Status = 'Registered'",
                    "@s", created.ShipmentId)));
                Assert.AreEqual(1, Db.ToInt(Db.Scalar(
                    "SELECT COUNT(*) FROM dbo.[Payment] WHERE ShipmentID = @s AND ClientID = @c AND Amount = 163 AND PaymentStatus = 'Unpaid' AND InvoiceNumber = @i",
                    "@s", created.ShipmentId, "@c", customer, "@i", created.InvoiceNumber)));
            }
            finally
            {
                if (created != null) DeleteShipment(created.ShipmentId);
                DbTestHelper.DeleteClient(customer);
            }
        }

        [TestMethod]
        public void A_walk_in_counter_parcel_gets_history_but_no_invoice()
        {
            DbTestHelper.EnsureDatabaseAvailable();
            int staff = DbTestHelper.CreateTestDriver("counter");
            CreatedShipment created = null;
            try
            {
                ShipmentDraft d = Draft(null, false, "Walk In Receiver");
                d.RegisteredByStaffId = staff;
                d.WarehouseStatus = "Received";
                created = IntakeService.Create(d);

                Assert.IsNull(created.InvoiceNumber);
                Assert.AreEqual(0, Db.ToInt(Db.Scalar("SELECT COUNT(*) FROM dbo.[Payment] WHERE ShipmentID = @s", "@s", created.ShipmentId)));
                Assert.AreEqual(1, Db.ToInt(Db.Scalar("SELECT COUNT(*) FROM dbo.[TrackingHistory] WHERE ShipmentID = @s AND AdminID = @a",
                    "@s", created.ShipmentId, "@a", staff)), "the counter clerk is recorded");
            }
            finally
            {
                if (created != null) DeleteShipment(created.ShipmentId);
                DbTestHelper.DeleteDriver(staff);
            }
        }

        [TestMethod]
        public void An_invalid_draft_writes_nothing()
        {
            DbTestHelper.EnsureDatabaseAvailable();
            string receiver = "Nobody " + Guid.NewGuid().ToString("N").Substring(0, 8);
            ShipmentDraft d = Draft(null, false, receiver);
            d.WeightKg = 0m;

            try
            {
                IntakeService.Create(d);
                Assert.Fail("a zero-weight parcel must be refused");
            }
            catch (ValidationFailedException ex)
            {
                StringAssert.Contains(ex.Message, "weight");
            }
            Assert.AreEqual(0, Db.ToInt(Db.Scalar("SELECT COUNT(*) FROM dbo.[Shipment] WHERE ReceiverName = @r", "@r", receiver)));
        }

        [TestMethod]
        public void A_new_customer_gets_their_client_record_and_a_duplicate_email_is_answered()
        {
            DbTestHelper.EnsureDatabaseAvailable();
            string email = "acct-" + Guid.NewGuid().ToString("N").Substring(0, 8) + "@test.invalid";
            AccountResult first = AccountService.Create(new NewAccount
            {
                FullName = "Account Test", Email = email, Password = "secret123", Role = "Customer", ClientType = "Business"
            }, null);
            try
            {
                Assert.IsTrue(first.Success, first.Message);
                Assert.AreEqual("Business", Db.ToStr(Db.Scalar("SELECT ClientType FROM dbo.[Client] WHERE UserID = @u", "@u", first.UserId)));

                AccountResult again = AccountService.Create(new NewAccount
                {
                    FullName = "Account Test Two", Email = "  " + email.ToUpperInvariant() + " ", Password = "secret123"
                }, null);
                Assert.IsFalse(again.Success);
                Assert.AreEqual(AccountService.EmailTaken, again.Message, "the same address in other case is the same account");
            }
            finally
            {
                if (first.Success) DeleteAccount(first.UserId);
            }
        }

        private static int CreateTestAdmin()
        {
            return Db.Insert(
                "INSERT INTO dbo.[User] (FullName, Password, Email, Role) VALUES ('Test Admin', 'x', @e, 'Admin')",
                "@e", "adm-" + Guid.NewGuid().ToString("N").Substring(0, 8) + "@test.invalid");
        }

        [TestMethod]
        public void A_failure_part_way_through_leaves_no_shipment_behind()
        {
            DbTestHelper.EnsureDatabaseAvailable();
            int customer = DbTestHelper.CreateTestClient("atomic", "Individual");
            string receiver = "Atomic " + Guid.NewGuid().ToString("N").Substring(0, 8);
            string seq = "INV-" + DateTime.Now.Year;
            // Occupy the invoice number the booking is about to claim, so the Payment INSERT
            // (after the Shipment and TrackingHistory rows) hits the unique constraint.
            int next = Db.ToInt(Db.Scalar("SELECT ISNULL(MAX(NextValue), 0) + 1 FROM dbo.NumberSequence WHERE SeqName = @n", "@n", seq));
            int blocker = Db.Insert(
                "INSERT INTO dbo.[Payment] (ClientID, InvoiceNumber, Amount, PaymentStatus) VALUES (@c, @i, 1, 'Unpaid')",
                "@c", customer, "@i", seq + next.ToString("D4"));
            try
            {
                try
                {
                    IntakeService.Create(Draft(customer, true, receiver));
                    Assert.Fail("the invoice clash should have failed the booking");
                }
                catch (System.Data.SqlClient.SqlException ex)
                {
                    Assert.IsTrue(IntakeService.IsUniqueViolation(ex), ex.Message);
                }
                Assert.AreEqual(0, Db.ToInt(Db.Scalar("SELECT COUNT(*) FROM dbo.[Shipment] WHERE ReceiverName = @r", "@r", receiver)),
                    "the shipment row was rolled back with the failed invoice");
                Assert.AreEqual(0, Db.ToInt(Db.Scalar(
                    "SELECT COUNT(*) FROM dbo.[TrackingHistory] th JOIN dbo.[Shipment] s ON s.ShipmentID = th.ShipmentID WHERE s.ReceiverName = @r",
                    "@r", receiver)));
                Assert.AreEqual(next - 1, Db.ToInt(Db.Scalar("SELECT ISNULL(MAX(NextValue), 0) FROM dbo.NumberSequence WHERE SeqName = @n", "@n", seq)),
                    "the claimed invoice number was rolled back too - no gap in the sequence");
            }
            finally
            {
                Db.NonQuery("DELETE FROM dbo.[Payment] WHERE PaymentID = @p", "@p", blocker);
                DbTestHelper.DeleteClient(customer);
            }
        }

        [TestMethod]
        public void Only_an_admin_can_create_a_staff_account()
        {
            DbTestHelper.EnsureDatabaseAvailable();
            int notAdmin = DbTestHelper.CreateTestDriver("notadmin");
            try
            {
                AccountResult viaPublic = AccountService.Create(new NewAccount
                {
                    FullName = "Sneaky", Email = "sneaky-" + Guid.NewGuid().ToString("N").Substring(0, 8) + "@test.invalid",
                    Password = "secret123", Role = "Admin"
                }, null);
                Assert.IsFalse(viaPublic.Success, "no creator");
                AccountResult viaStaff = AccountService.Create(new NewAccount
                {
                    FullName = "Sneaky", Email = "sneaky-" + Guid.NewGuid().ToString("N").Substring(0, 8) + "@test.invalid",
                    Password = "secret123", Role = "Staff"
                }, notAdmin);
                Assert.IsFalse(viaStaff.Success, "a staff member is not an admin");
                StringAssert.Contains(viaStaff.Message, "administrator");
            }
            finally
            {
                DbTestHelper.DeleteDriver(notAdmin);
            }
        }

        [TestMethod]
        public void An_admin_created_driver_gets_staff_and_driver_records_and_an_audit_entry()
        {
            DbTestHelper.EnsureDatabaseAvailable();
            int admin = CreateTestAdmin();
            string email = "drv-" + Guid.NewGuid().ToString("N").Substring(0, 8) + "@test.invalid";
            AccountResult created = AccountService.Create(new NewAccount
            {
                FullName = "New Driver", Email = email, Password = "secret123", Role = "Staff", IsDriver = true
            }, admin);
            try
            {
                Assert.IsTrue(created.Success, created.Message);
                Assert.AreEqual(1, Db.ToInt(Db.Scalar("SELECT COUNT(*) FROM dbo.[Admin] WHERE UserID = @u", "@u", created.UserId)));
                Assert.AreEqual(1, Db.ToInt(Db.Scalar("SELECT COUNT(*) FROM dbo.[Driver] WHERE UserID = @u", "@u", created.UserId)));
                Assert.AreEqual(0, Db.ToInt(Db.Scalar("SELECT COUNT(*) FROM dbo.[Client] WHERE UserID = @u", "@u", created.UserId)));
                Assert.AreEqual(1, Db.ToInt(Db.Scalar(
                    "SELECT COUNT(*) FROM dbo.AuditLog WHERE UserID = @a AND Action = 'User.Create' AND EntityID = @e",
                    "@a", admin, "@e", created.UserId.ToString())));
            }
            finally
            {
                if (created.Success) DeleteAccount(created.UserId);
                DeleteAccount(admin);
            }
        }
    }
}
