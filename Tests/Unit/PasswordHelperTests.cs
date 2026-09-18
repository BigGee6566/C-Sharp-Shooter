using ForteLogisticsApp.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ForteLogisticsApp.Tests.Unit
{
    [TestClass]
    public class PasswordHelperTests
    {
        [TestMethod]
        public void Hash_ThenVerify_RoundTripsCorrectly()
        {
            string hash = PasswordHelper.Hash("Correct Horse Battery Staple");
            Assert.IsTrue(PasswordHelper.Verify("Correct Horse Battery Staple", hash));
        }

        [TestMethod]
        public void Verify_WrongPassword_Fails()
        {
            string hash = PasswordHelper.Hash("the-real-password");
            Assert.IsFalse(PasswordHelper.Verify("a-guessed-password", hash));
        }

        [TestMethod]
        public void Hash_SamePasswordTwice_ProducesDifferentHashes()
        {
            // Distinct random salts per call - two users choosing the same password must not
            // be visibly identical in storage (the pre-PBKDF2 scheme's exact flaw).
            string a = PasswordHelper.Hash("same-password");
            string b = PasswordHelper.Hash("same-password");
            Assert.AreNotEqual(a, b);
            Assert.IsTrue(PasswordHelper.Verify("same-password", a));
            Assert.IsTrue(PasswordHelper.Verify("same-password", b));
        }

        [TestMethod]
        public void GenerateTemporary_ProducesANonEmptyPasswordOfRequestedLength()
        {
            string temp = PasswordHelper.GenerateTemporary(12);
            Assert.AreEqual(12, temp.Length);
        }
    }
}
