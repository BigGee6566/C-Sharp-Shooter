namespace ForteLogisticsApp.Security
{
    /// <summary>
    /// Pure, storage-agnostic core of the one-time submit-token check that makes a
    /// non-idempotent POST (for example creating a booking) safe against a double-click or a
    /// refresh-and-resubmit. The page layer (<see cref="SecurePage"/>) holds the issued token
    /// in Session and calls <see cref="Check"/> once per submit; a token validates at most once.
    ///
    /// This is the deliberate alternative to blanket Post-Redirect-Get. Web Forms is built
    /// around in-place postbacks, so a per-action one-time token closes the double-submit
    /// window without rewriting every page's lifecycle. Money and status changes are already
    /// idempotent at the service/DB layer (idempotency keys, @@ROWCOUNT-guarded transitions);
    /// this covers the remaining creation POSTs that have no natural idempotency key.
    /// </summary>
    public static class SubmitGuard
    {
        /// <summary>
        /// Validates a presented token against the one issued for this action and returns the
        /// token value that should remain stored afterwards. A successful match consumes the
        /// token (returns null) so a replay of the same token fails the second time. An absent
        /// issued token, an absent presented token, or a mismatch all fail <paramref name="ok"/>
        /// without consuming anything, so a genuine first submit can still be retried after a
        /// validation error that returned before the token was checked.
        /// </summary>
        public static string Check(string issued, string presented, out bool ok)
        {
            if (string.IsNullOrEmpty(issued) || string.IsNullOrEmpty(presented) || issued != presented)
            {
                ok = false;
                return issued;   // leave whatever was stored untouched
            }

            ok = true;
            return null;         // consumed: a second submit carrying the same token fails
        }
    }
}
