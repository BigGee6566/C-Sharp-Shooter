using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ForteLogisticsApp.Tests.Unit
{
    [TestClass]
    public class ForcedPasswordChangeTests
    {
        [TestMethod]
        public void A_good_new_password_has_no_problem()
        {
            Assert.IsNull(AuthService.PasswordProblem("Chosen#456", "Chosen#456", PasswordHelper.Hash("Temporary123")));
        }

        [TestMethod]
        public void A_password_outside_the_length_rule_is_refused()
        {
            string temp = PasswordHelper.Hash("Temporary123");
            string tooLong = new string('a', AccountService.MaxPasswordLength + 1);

            Assert.IsNotNull(AuthService.PasswordProblem("short", "short", temp));
            Assert.IsNotNull(AuthService.PasswordProblem(tooLong, tooLong, temp));
        }

        [TestMethod]
        public void Passwords_that_differ_are_refused()
        {
            Assert.IsNotNull(AuthService.PasswordProblem("Chosen#456", "Chosen#457", PasswordHelper.Hash("Temporary123")));
        }

        [TestMethod]
        public void The_temporary_password_cannot_become_the_chosen_one()
        {
            Assert.IsNotNull(AuthService.PasswordProblem("Temporary123", "Temporary123", PasswordHelper.Hash("Temporary123")));
        }

        [TestMethod]
        public void Only_the_change_page_is_open_during_a_forced_change()
        {
            Assert.IsTrue(AuthService.IsAllowedDuringForcedChange("~/ChangePassword.aspx"));
            Assert.IsTrue(AuthService.IsAllowedDuringForcedChange("~/changepassword.ASPX"));

            Assert.IsFalse(AuthService.IsAllowedDuringForcedChange("~/Customer/Dashboard.aspx"));
            Assert.IsFalse(AuthService.IsAllowedDuringForcedChange("~/Default.aspx"));
            Assert.IsFalse(AuthService.IsAllowedDuringForcedChange("~/TrackApi.aspx"));
        }
    }
}
