using System;

namespace ForteLogisticsApp.Services
{
    /// <summary>
    /// SANDBOX / TEST payment processor.
    ///
    /// This is a clearly-labelled simulation - no card network, bank or real payment
    /// provider is contacted, and no real money ever moves. It exists so the
    /// payment-before-dispatch and bulk-invoice-payment flows can be built and tested end to
    /// end now, and swapped for a real provider (e.g. PayFast, Stripe, PayGate) later without
    /// touching PaymentService's transaction, ownership or idempotency logic - only this
    /// class's Charge method would be replaced with a real API call.
    ///
    /// No raw card number or CVV is ever accepted here or anywhere upstream of it; the
    /// optional testNote lets a tester force the decline path without simulating card
    /// capture at all.
    /// </summary>
    public static class PaymentGateway
    {
        public class GatewayResult
        {
            public bool Success;
            public string ReferenceNumber;
            public string FailureReason;
        }

        /// <summary>
        /// Simulates charging <paramref name="amount"/> via <paramref name="method"/>.
        /// Include the text "FAIL" anywhere in <paramref name="testNote"/> to exercise the
        /// decline path during testing; every other input succeeds.
        /// </summary>
        public static GatewayResult Charge(string method, decimal amount, string testNote)
        {
            if (amount <= 0m)
            {
                return new GatewayResult { Success = false, FailureReason = "Invalid amount." };
            }

            bool simulateFailure = !string.IsNullOrEmpty(testNote) &&
                testNote.IndexOf("FAIL", StringComparison.OrdinalIgnoreCase) >= 0;

            if (simulateFailure)
            {
                return new GatewayResult { Success = false, FailureReason = "Payment declined (sandbox test failure)." };
            }

            string reference = "SANDBOX-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" +
                                Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
            return new GatewayResult { Success = true, ReferenceNumber = reference };
        }
    }
}
