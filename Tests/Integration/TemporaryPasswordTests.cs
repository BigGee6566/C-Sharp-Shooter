using System;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ForteLogisticsApp.Tests.Integration
{
    /// <summary>The forgot-password journey against a real database, driven through AuthService.</summary>
    [TestClass]
    public class TemporaryPasswordTests
    {
        private const string RealPassword = "Remembered#123";

        private int _user;
        private string _email;

        private void CreateUser(string role)
        {
            DbTestHelper.EnsureDatabaseAvailable();
            _email = "forgot-" + Guid.NewGuid().ToString("N").Substring(0, 8) + "@test.invalid";
            _user = Db.Insert(
                "INSERT INTO dbo.[User] (FullName, Password, Email, Role) VALUES ('Forgot Test', @p, @e, @r)",
                "@p", PasswordHelper.Hash(RealPassword), "@e", _email, "@r", role);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_user == 0) return;
            RequestThrottle.Reset("login:" + _email);
            Db.NonQuery("DELETE FROM dbo.AuditLog WHERE UserID = @u", "@u", _user);
            Db.NonQuery("DELETE FROM dbo.[User] WHERE UserID = @u", "@u", _user);
        }

        private PasswordMatch SignIn(string password)
        {
            PasswordMatch status;
            AuthService.Login(_email, password, null, out status);
            return status;
        }

        private TemporaryPassword Issue()
        {
            return AuthService.IssueTemporaryPassword(_email, "127.0.0.1");
        }

        private int AuditCount(string action)
        {
            return Db.ToInt(Db.Scalar(
                "SELECT COUNT(*) FROM dbo.AuditLog WHERE UserID = @u AND Action = @a", "@u", _user, "@a", action));
        }

        [TestMethod]
        public void A_temporary_password_signs_in_once_and_never_again()
        {
            CreateUser("Customer");
            TemporaryPassword temp = Issue();

            Assert.AreEqual(PasswordMatch.Temporary, SignIn(temp.Password), "first use");
            Assert.AreEqual(PasswordMatch.Rejected, SignIn(temp.Password), "second use");
            Assert.AreEqual(1, AuditCount("Password.Temporary.Used"));
        }

        [TestMethod]
        public void A_temporary_password_lasts_twenty_minutes()
        {
            CreateUser("Customer");
            TemporaryPassword temp = Issue();

            double minutes = (temp.ExpiresUtc - DateTime.UtcNow).TotalMinutes;
            Assert.IsTrue(minutes > 19.5 && minutes <= 20.5, "expires in " + minutes + " minutes");
            Assert.AreEqual(1, AuditCount("Password.Temporary.Issued"));
        }

        [TestMethod]
        public void An_expired_temporary_password_is_refused_and_says_so()
        {
            CreateUser("Customer");
            TemporaryPassword temp = Issue();
            Db.NonQuery("UPDATE dbo.[User] SET TempPasswordExpires = DATEADD(SECOND, -1, GETUTCDATE()) WHERE UserID = @u", "@u", _user);

            string role = AuthService.Login(_email, temp.Password, null);

            Assert.IsNull(role, "no sign-in");
            Assert.AreEqual(PasswordMatch.TemporaryExpired, SignIn(temp.Password));
        }

        [TestMethod]
        public void The_temporary_password_is_stored_only_as_a_hash()
        {
            CreateUser("Customer");
            TemporaryPassword temp = Issue();

            string stored = Db.ToStr(Db.Scalar("SELECT TempPassword FROM dbo.[User] WHERE UserID = @u", "@u", _user));

            Assert.AreNotEqual(temp.Password, stored);
            StringAssert.StartsWith(stored, "PBKDF2|");
        }

        [TestMethod]
        public void A_new_request_replaces_the_earlier_temporary_password()
        {
            CreateUser("Customer");
            TemporaryPassword first = Issue();
            TemporaryPassword second = Issue();

            Assert.AreEqual(PasswordMatch.Rejected, SignIn(first.Password));
            Assert.AreEqual(PasswordMatch.Temporary, SignIn(second.Password));
        }

        [TestMethod]
        public void Someone_elses_request_does_not_lock_the_owner_out()
        {
            CreateUser("Customer");
            TemporaryPassword temp = Issue();

            Assert.AreEqual(PasswordMatch.Permanent, SignIn(RealPassword));
            Assert.AreEqual(PasswordMatch.Rejected, SignIn(temp.Password), "the owner signing in cancels the request");
        }

        [TestMethod]
        public void Choosing_a_password_replaces_the_old_one()
        {
            CreateUser("Customer");
            TemporaryPassword temp = Issue();
            SignIn(temp.Password);

            string hash = AuthService.SetChosenPassword(_user, "Chosen#456789");

            Assert.AreEqual(hash, Db.ToStr(Db.Scalar("SELECT Password FROM dbo.[User] WHERE UserID = @u", "@u", _user)));
            Assert.AreEqual(PasswordMatch.Permanent, SignIn("Chosen#456789"));
            Assert.AreEqual(PasswordMatch.Rejected, SignIn(RealPassword), "the forgotten password is gone");
            Assert.AreEqual(PasswordMatch.Rejected, SignIn(temp.Password));
            Assert.AreEqual(1, AuditCount("Password.Changed.AfterReset"));
        }

        [TestMethod]
        public void Staff_and_admin_accounts_cannot_get_a_temporary_password()
        {
            foreach (string role in new[] { "Staff", "Admin" })
            {
                CreateUser(role);

                Assert.IsNull(Issue(), role);
                Assert.IsNull(Db.Scalar("SELECT TempPassword FROM dbo.[User] WHERE UserID = @u", "@u", _user) as string, role);

                Cleanup();
            }
            _user = 0;
        }

        [TestMethod]
        public void An_unknown_email_gets_nothing()
        {
            DbTestHelper.EnsureDatabaseAvailable();

            Assert.IsNull(AuthService.IssueTemporaryPassword("nobody-" + Guid.NewGuid().ToString("N") + "@test.invalid", "127.0.0.1"));
        }
    }
}
