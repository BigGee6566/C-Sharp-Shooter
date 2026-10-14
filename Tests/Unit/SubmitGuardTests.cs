using Microsoft.VisualStudio.TestTools.UnitTesting;
using ForteLogisticsApp.Security;

namespace ForteLogisticsApp.Tests.Unit
{
    /// <summary>
    /// Pure checks of the one-time submit-token core: a token validates exactly once, and a
    /// missing/stale/mismatched token never consumes whatever was stored. No HttpContext or
    /// database is needed - the Session plumbing lives in SecurePage, this is the decision logic.
    /// </summary>
    [TestClass]
    public class SubmitGuardTests
    {
        [TestMethod]
        public void First_matching_submit_is_accepted_and_consumes_the_token()
        {
            bool ok;
            string remaining = SubmitGuard.Check("abc123", "abc123", out ok);

            Assert.IsTrue(ok, "a token matching the issued one must be accepted");
            Assert.IsNull(remaining, "a successful match must consume the token so a replay fails");
        }

        [TestMethod]
        public void Replaying_a_consumed_token_is_rejected()
        {
            bool ok;
            string afterFirst = SubmitGuard.Check("abc123", "abc123", out ok);   // consumed -> null

            bool okReplay;
            string afterReplay = SubmitGuard.Check(afterFirst, "abc123", out okReplay);

            Assert.IsFalse(okReplay, "a double-click / F5-resubmit carrying the same token must be rejected");
            Assert.IsNull(afterReplay, "nothing to consume the second time");
        }

        [TestMethod]
        public void Mismatched_token_is_rejected_without_consuming_the_live_one()
        {
            bool ok;
            string remaining = SubmitGuard.Check("live-token", "stale-token", out ok);

            Assert.IsFalse(ok, "a token that does not match the issued one must be rejected");
            Assert.AreEqual("live-token", remaining,
                "a mismatch must leave the issued token intact so the genuine submit can still land");
        }

        [TestMethod]
        public void Missing_issued_or_presented_token_is_rejected()
        {
            bool ok1, ok2, ok3;
            SubmitGuard.Check(null, "x", out ok1);
            SubmitGuard.Check("x", null, out ok2);
            SubmitGuard.Check("", "", out ok3);

            Assert.IsFalse(ok1, "no token was ever issued");
            Assert.IsFalse(ok2, "nothing was presented");
            Assert.IsFalse(ok3, "empty is never a valid token");
        }
    }
}
