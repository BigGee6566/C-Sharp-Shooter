using ForteLogisticsApp.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ForteLogisticsApp.Tests.Unit
{
    /// <summary>IsSafeReturnUrl is a static, side-effect-free method, so it needs no HttpContext.</summary>
    [TestClass]
    public class SecurePageTests
    {
        [TestMethod]
        public void IsSafeReturnUrl_AcceptsOrdinarySameSitePath()
        {
            Assert.IsTrue(SecurePage.IsSafeReturnUrl("/Customer/BookShipment.aspx?size=Large"));
        }

        [TestMethod]
        public void IsSafeReturnUrl_RejectsProtocolRelativeUrl()
        {
            Assert.IsFalse(SecurePage.IsSafeReturnUrl("//evil.com/phish"));
        }

        [TestMethod]
        public void IsSafeReturnUrl_RejectsBackslashTrick()
        {
            Assert.IsFalse(SecurePage.IsSafeReturnUrl("/\\evil.com"));
        }

        [TestMethod]
        public void IsSafeReturnUrl_RejectsSchemeUrls()
        {
            Assert.IsFalse(SecurePage.IsSafeReturnUrl("javascript:alert(1)"));
            Assert.IsFalse(SecurePage.IsSafeReturnUrl("https://evil.com"));
        }

        [TestMethod]
        public void IsSafeReturnUrl_RejectsNullOrEmptyOrRelativeWithoutLeadingSlash()
        {
            Assert.IsFalse(SecurePage.IsSafeReturnUrl(null));
            Assert.IsFalse(SecurePage.IsSafeReturnUrl(string.Empty));
            Assert.IsFalse(SecurePage.IsSafeReturnUrl("Login.aspx"));
        }
    }
}
