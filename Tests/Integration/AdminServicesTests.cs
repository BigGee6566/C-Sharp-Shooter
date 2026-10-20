using System;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ForteLogisticsApp.Tests.Integration
{
    /// <summary>
    /// Admin -> Users and Fleet abuse cases: the role rules (pure) and the guarded, atomic
    /// updates against a real database.
    /// </summary>
    [TestClass]
    public class AdminServicesTests
    {
        // ---------------------------------------------------------------- pure rules

        [TestMethod]
        public void An_admin_cannot_remove_their_own_admin_access()
        {
            StringAssert.Contains(UserAdminRules.CheckRoleChange(1, 1, "Admin", "Staff", 5), "your own");
            Assert.IsNull(UserAdminRules.CheckRoleChange(1, 1, "Admin", "Admin", 5), "keeping Admin is fine");
        }

        [TestMethod]
        public void The_last_admin_cannot_be_demoted_but_others_can()
        {
            StringAssert.Contains(UserAdminRules.CheckRoleChange(1, 2, "Admin", "Customer", 1), "last administrator");
            Assert.IsNull(UserAdminRules.CheckRoleChange(1, 2, "Admin", "Staff", 2));
            Assert.IsNull(UserAdminRules.CheckRoleChange(1, 2, "Staff", "Customer", 1), "non-admins are not limited");
        }

        [TestMethod]
        public void Vehicle_registrations_have_one_canonical_form()
        {
            Assert.AreEqual("CA 123-456", FleetService.NormalizeRegistration("  ca   123-456 "));
            Assert.IsTrue(FleetService.IsValidRegistration("CA 123-456"));
            Assert.IsFalse(FleetService.IsValidRegistration("C"), "too short");
            Assert.IsFalse(FleetService.IsValidRegistration(new string('A', 31)), "longer than the column");
            Assert.IsFalse(FleetService.IsValidRegistration("CA<123>"), "only letters, digits, spaces and hyphens");
        }

        // ---------------------------------------------------------------- database

        private static int CreateUser(string role)
        {
            return Db.Insert(
                "INSERT INTO dbo.[User] (FullName, Password, Email, Role) VALUES ('Admin Test', 'x', @e, @r)",
                "@e", "admtest-" + Guid.NewGuid().ToString("N").Substring(0, 8) + "@test.invalid", "@r", role);
        }

        private static void DeleteUser(int userId)
        {
            Db.NonQuery("DELETE FROM dbo.AuditLog WHERE UserID = @u OR (Entity = 'User' AND EntityID = @e)", "@u", userId, "@e", userId.ToString());
            Db.NonQuery("DELETE FROM dbo.[Notification] WHERE UserID = @u", "@u", userId);
            Db.NonQuery("DELETE FROM dbo.[Driver] WHERE UserID = @u", "@u", userId);
            Db.NonQuery("DELETE FROM dbo.[Admin] WHERE UserID = @u", "@u", userId);
            Db.NonQuery("DELETE FROM dbo.[Client] WHERE UserID = @u", "@u", userId);
            Db.NonQuery("DELETE FROM dbo.[User] WHERE UserID = @u", "@u", userId);
        }

        private static UserEdit Edit(int userId, string role, bool driver = false)
        {
            return new UserEdit { UserId = userId, FullName = "Admin Test", Role = role, IsDriver = driver, ClientType = "Individual" };
        }

        [TestMethod]
        public void An_admin_demoting_themselves_is_refused_and_nothing_changes()
        {
            DbTestHelper.EnsureDatabaseAvailable();
            int admin = CreateUser("Admin");
            try
            {
                AccountResult r = UserAdminService.Update(Edit(admin, "Customer"), admin);
                Assert.IsFalse(r.Success);
                Assert.AreEqual("Admin", Db.ToStr(Db.Scalar("SELECT Role FROM dbo.[User] WHERE UserID = @u", "@u", admin)));
            }
            finally
            {
                DeleteUser(admin);
            }
        }

        [TestMethod]
        public void A_role_change_by_another_admin_is_applied_and_audited()
        {
            DbTestHelper.EnsureDatabaseAvailable();
            int actor = CreateUser("Admin");
            int target = CreateUser("Admin");
            try
            {
                AccountResult r = UserAdminService.Update(Edit(target, "Staff"), actor);
                Assert.IsTrue(r.Success, r.Message);
                Assert.AreEqual("Staff", Db.ToStr(Db.Scalar("SELECT Role FROM dbo.[User] WHERE UserID = @u", "@u", target)));
                Assert.AreEqual(1, Db.ToInt(Db.Scalar(
                    "SELECT COUNT(*) FROM dbo.AuditLog WHERE UserID = @a AND Action = 'User.Update' AND EntityID = @t AND Details LIKE 'role Admin -> Staff%'",
                    "@a", actor, "@t", target.ToString())));
                Assert.AreEqual(1, Db.ToInt(Db.Scalar("SELECT COUNT(*) FROM dbo.[Admin] WHERE UserID = @u", "@u", target)),
                    "staff keep their staff record");
            }
            finally
            {
                DeleteUser(target);
                DeleteUser(actor);
            }
        }

        [TestMethod]
        public void A_driver_holding_parcels_cannot_be_unenrolled_and_nothing_else_changes()
        {
            DbTestHelper.EnsureDatabaseAvailable();
            int admin = CreateUser("Admin");
            int driver = DbTestHelper.CreateTestDriver("holding");
            int customer = DbTestHelper.CreateTestClient("holding", "Individual");
            int paymentId;
            int shipment = DbTestHelper.CreateTestShipmentWithInvoice(customer, 100m, "NotRequired", "Unpaid", out paymentId);
            Db.NonQuery("UPDATE dbo.[Shipment] SET DriverID = @d, DeliveryStatus = 'Assigned' WHERE ShipmentID = @s", "@d", driver, "@s", shipment);
            try
            {
                UserEdit e = Edit(driver, "Customer");
                e.FullName = "Renamed Driver";
                AccountResult r = UserAdminService.Update(e, admin);

                Assert.IsFalse(r.Success);
                StringAssert.Contains(r.Message, "driver record must stay");
                Assert.AreEqual("Staff", Db.ToStr(Db.Scalar("SELECT Role FROM dbo.[User] WHERE UserID = @u", "@u", driver)));
                Assert.AreNotEqual("Renamed Driver", Db.ToStr(Db.Scalar("SELECT FullName FROM dbo.[User] WHERE UserID = @u", "@u", driver)),
                    "the refused save changed nothing at all");
                Assert.AreEqual(1, Db.ToInt(Db.Scalar("SELECT COUNT(*) FROM dbo.[Driver] WHERE UserID = @u", "@u", driver)));
            }
            finally
            {
                DbTestHelper.DeleteShipmentAndPayment(shipment, paymentId);
                DbTestHelper.DeleteClient(customer);
                DbTestHelper.DeleteDriver(driver);
                DeleteUser(admin);
            }
        }

        [TestMethod]
        public void A_duplicate_vehicle_registration_is_refused()
        {
            DbTestHelper.EnsureDatabaseAvailable();
            int admin = CreateUser("Admin");
            string plate = "TST " + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
            try
            {
                Assert.IsTrue(FleetService.AddVehicle(plate, "Panel Van", null, admin).Success);
                AccountResult again = FleetService.AddVehicle("  " + plate.ToLowerInvariant() + " ", "Motorbike", null, admin);
                Assert.IsFalse(again.Success, "the same plate typed differently is the same vehicle");
                StringAssert.Contains(again.Message, "already in the fleet");
                Assert.IsFalse(FleetService.AddVehicle("TST 999", "Spaceship", null, admin).Success, "unknown vehicle type");
                Assert.AreEqual(1, Db.ToInt(Db.Scalar("SELECT COUNT(*) FROM dbo.[Vehicle] WHERE RegistrationNumber = @r", "@r", plate)));
            }
            finally
            {
                Db.NonQuery("DELETE FROM dbo.AuditLog WHERE Entity = 'Vehicle' AND EntityID = @r", "@r", plate);
                Db.NonQuery("DELETE FROM dbo.[Vehicle] WHERE RegistrationNumber = @r", "@r", plate);
                DeleteUser(admin);
            }
        }

        // ---------------------------------------------------------------- sessions and sign-in

        [TestMethod]
        public void A_session_stamp_stops_matching_when_the_role_or_password_changes()
        {
            string stamp = ForteLogisticsApp.Security.SessionGuard.Stamp("Admin", "hash-1");
            Assert.IsTrue(ForteLogisticsApp.Security.SessionGuard.StampMatches(stamp, "Admin", "hash-1"));
            Assert.IsFalse(ForteLogisticsApp.Security.SessionGuard.StampMatches(stamp, "Staff", "hash-1"), "demoted");
            Assert.IsFalse(ForteLogisticsApp.Security.SessionGuard.StampMatches(stamp, "Admin", "hash-2"), "password reset");
            Assert.IsFalse(ForteLogisticsApp.Security.SessionGuard.StampMatches(null, "Admin", "hash-1"), "a session from before stamps existed");
            Assert.IsFalse(stamp.Contains("hash-1"), "the stamp never carries the hash");
        }

        [TestMethod]
        public void A_demoted_admins_old_session_can_no_longer_edit_users()
        {
            DbTestHelper.EnsureDatabaseAvailable();
            int formerAdmin = CreateUser("Staff");   // demoted since signing in
            int target = CreateUser("Customer");
            try
            {
                UserEdit e = Edit(target, "Admin");
                AccountResult r = UserAdminService.Update(e, formerAdmin);
                Assert.IsFalse(r.Success, "self-promotion of someone else through a stale admin session");
                StringAssert.Contains(r.Message, "sign in again");
                Assert.AreEqual("Customer", Db.ToStr(Db.Scalar("SELECT Role FROM dbo.[User] WHERE UserID = @u", "@u", target)));
                Assert.AreEqual(1, Db.ToInt(Db.Scalar(
                    "SELECT COUNT(*) FROM dbo.AuditLog WHERE UserID = @a AND Action = 'User.Update.Denied' AND EntityID = @t",
                    "@a", formerAdmin, "@t", target.ToString())), "the refusal is on the record");
            }
            finally
            {
                DeleteUser(target);
                DeleteUser(formerAdmin);
            }
        }

        [TestMethod]
        public void Billing_is_left_alone_when_its_fields_were_not_on_screen()
        {
            DbTestHelper.EnsureDatabaseAvailable();
            int admin = CreateUser("Admin");
            int business = DbTestHelper.CreateTestBusiness("keep");
            int customer = DbTestHelper.CreateTestClient("keep", "Business", business);
            try
            {
                UserEdit e = Edit(customer, "Customer");   // UpdateBilling false, ClientType default
                Assert.IsTrue(UserAdminService.Update(e, admin).Success);
                Assert.AreEqual(business, Db.ToInt(Db.Scalar("SELECT BusinessID FROM dbo.[Client] WHERE UserID = @u", "@u", customer)));
                Assert.AreEqual("Business", Db.ToStr(Db.Scalar("SELECT ClientType FROM dbo.[Client] WHERE UserID = @u", "@u", customer)));
            }
            finally
            {
                DeleteUser(customer);
                DbTestHelper.DeleteBusiness(business);
                DeleteUser(admin);
            }
        }

        [TestMethod]
        public void Plates_written_with_or_without_separators_are_the_same_vehicle()
        {
            DbTestHelper.EnsureDatabaseAvailable();
            int admin = CreateUser("Admin");
            string core = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
            string plate = "SP " + core.Substring(0, 3) + "-" + core.Substring(3);
            try
            {
                Assert.IsTrue(FleetService.AddVehicle(plate, "Panel Van", null, admin).Success);
                Assert.IsFalse(FleetService.AddVehicle("SP" + core, "Panel Van", null, admin).Success);
            }
            finally
            {
                Db.NonQuery("DELETE FROM dbo.AuditLog WHERE Entity = 'Vehicle' AND EntityID = @r", "@r", plate);
                Db.NonQuery("DELETE FROM dbo.[Vehicle] WHERE RegistrationNumber = @r", "@r", plate);
                DeleteUser(admin);
            }
        }

        [TestMethod]
        public void Repeated_wrong_passwords_lock_the_account_whatever_the_session()
        {
            DbTestHelper.EnsureDatabaseAvailable();
            string email = "lock-" + Guid.NewGuid().ToString("N").Substring(0, 8) + "@test.invalid";
            int user = Db.Insert(
                "INSERT INTO dbo.[User] (FullName, Password, Email, Role) VALUES ('Lock Test', @p, @e, 'Customer')",
                "@p", ForteLogisticsApp.Security.PasswordHelper.Hash("Correct#123"), "@e", email);
            try
            {
                Assert.AreEqual("Customer", AuthService.Login(email, "Correct#123", null), "a good password works at first");
                // Each failure as if from a fresh session (no cookie): the account still counts them.
                for (int i = 0; i < AuthService.MaxAccountFailures; i++) Assert.IsNull(AuthService.Login(email, "wrong" + i, null));
                Assert.IsNull(AuthService.Login(email, "Correct#123", null), "locked even with the right password");
            }
            finally
            {
                ForteLogisticsApp.Security.RequestThrottle.Reset("login:" + email);
                DeleteUser(user);
            }
        }
    }
}
