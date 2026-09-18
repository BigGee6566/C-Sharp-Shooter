using System;
using ForteLogisticsApp.Data;

namespace ForteLogisticsApp.Services
{
    /// <summary>
    /// Server-trusted payment application. Every amount here comes from the database, never
    /// from the browser; every write happens inside one transaction so a payment can never
    /// leave Payment, Shipment and the tracking/audit trail disagreeing with each other.
    /// </summary>
    public static class PaymentService
    {
        public class PaymentOutcome
        {
            public bool Success;
            public string Message;
            public string ReferenceNumber;
            public string InvoiceNumber;
            public decimal Amount;
        }

        /// <summary>
        /// Individual-customer payment-before-dispatch.
        ///
        /// Re-validates ownership (the shipment must belong to <paramref name="clientUserId"/>)
        /// and eligibility (Shipment.PaymentGateStatus must be PaymentRequired, and there must
        /// be exactly one Unpaid Payment row) before charging the sandbox gateway. A repeated
        /// call carrying the same <paramref name="idempotencyKey"/> for the same shipment
        /// returns the original outcome instead of charging a second time - the caller should
        /// mint a fresh key per genuine attempt (e.g. one per page load / per Retry click).
        /// </summary>
        public static PaymentOutcome PayForShipment(
            int shipmentId, int clientUserId, string method, string testNote, string idempotencyKey)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                throw new ArgumentException("An idempotency key is required.", "idempotencyKey");

            PaymentOutcome outcome = null;

            Db.RunInTransaction((conn, tx) =>
            {
                // Duplicate submission (double-click, resubmit, retried callback) of the exact
                // same attempt: return the original result rather than charging again.
                var existing = Db.Query(conn, tx,
                    @"SELECT TOP 1 PaymentStatus, ReferenceNumber, InvoiceNumber, Amount
                      FROM dbo.[Payment] WHERE ShipmentID = @s AND IdempotencyKey = @k",
                    "@s", shipmentId, "@k", idempotencyKey);
                if (existing.Rows.Count > 0)
                {
                    var r = existing.Rows[0];
                    bool wasSuccess = Db.ToStr(r["PaymentStatus"]) == "Paid";
                    outcome = new PaymentOutcome
                    {
                        Success = wasSuccess,
                        Message = wasSuccess
                            ? "Payment already completed."
                            : "This payment attempt already failed; please retry.",
                        ReferenceNumber = Db.ToStr(r["ReferenceNumber"]),
                        InvoiceNumber = Db.ToStr(r["InvoiceNumber"]),
                        Amount = Db.ToDecimal(r["Amount"])
                    };
                    return;
                }

                var ship = Db.Query(conn, tx,
                    @"SELECT ShipmentID, SenderID, PaymentGateStatus
                      FROM dbo.[Shipment] WITH (UPDLOCK, ROWLOCK) WHERE ShipmentID = @s",
                    "@s", shipmentId);
                if (ship.Rows.Count == 0)
                {
                    outcome = Fail("Shipment not found.");
                    return;
                }

                var shipRow = ship.Rows[0];
                if (Db.ToInt(shipRow["SenderID"]) != clientUserId)
                {
                    // IDOR guard: never let a user pay for someone else's shipment, even if
                    // they craft the request with another shipment's ID.
                    outcome = Fail("You may only pay for your own shipment.");
                    return;
                }

                string gateStatus = Db.ToStr(shipRow["PaymentGateStatus"]);
                if (gateStatus != "PaymentRequired")
                {
                    outcome = Fail(gateStatus == "PaymentSuccessful"
                        ? "This shipment is already paid."
                        : "This shipment is not awaiting payment.");
                    return;
                }

                var pay = Db.Query(conn, tx,
                    @"SELECT PaymentID, Amount, InvoiceNumber FROM dbo.[Payment] WITH (UPDLOCK, ROWLOCK)
                      WHERE ShipmentID = @s AND PaymentStatus = 'Unpaid'",
                    "@s", shipmentId);
                if (pay.Rows.Count == 0)
                {
                    outcome = Fail("No unpaid invoice found for this shipment.");
                    return;
                }

                var payRow = pay.Rows[0];
                int paymentId = Db.ToInt(payRow["PaymentID"]);
                decimal amount = Db.ToDecimal(payRow["Amount"]);      // server-trusted, never client-sent
                string invoiceNumber = Db.ToStr(payRow["InvoiceNumber"]);

                Db.NonQuery(conn, tx,
                    "UPDATE dbo.[Shipment] SET PaymentGateStatus = 'PaymentPending' WHERE ShipmentID = @s",
                    "@s", shipmentId);

                var result = PaymentGateway.Charge(method, amount, testNote);

                if (result.Success)
                {
                    Db.NonQuery(conn, tx,
                        @"UPDATE dbo.[Payment]
                          SET PaymentStatus = 'Paid', PaymentMethod = @m, ReferenceNumber = @r, IdempotencyKey = @k
                          WHERE PaymentID = @id",
                        "@m", method, "@r", result.ReferenceNumber, "@k", idempotencyKey, "@id", paymentId);

                    Db.NonQuery(conn, tx,
                        "UPDATE dbo.[Shipment] SET PaymentGateStatus = 'PaymentSuccessful' WHERE ShipmentID = @s",
                        "@s", shipmentId);

                    Db.NonQuery(conn, tx,
                        @"INSERT INTO dbo.[TrackingHistory] (ShipmentID, Location, Status, Remarks)
                          VALUES (@s, 'Online Payment', 'PaymentSuccessful', @rm)",
                        "@s", shipmentId,
                        "@rm", "Payment received via sandbox gateway (" + method + "), ref " + result.ReferenceNumber);

                    Db.NonQuery(conn, tx,
                        @"INSERT INTO dbo.AuditLog (UserID, Action, Entity, EntityID, Details)
                          VALUES (@u, 'Payment.Success', 'Shipment', @e, @d)",
                        "@u", clientUserId, "@e", shipmentId.ToString(),
                        "@d", "Invoice " + invoiceNumber + " paid, ref " + result.ReferenceNumber);

                    outcome = new PaymentOutcome
                    {
                        Success = true,
                        Message = "Payment successful.",
                        ReferenceNumber = result.ReferenceNumber,
                        InvoiceNumber = invoiceNumber,
                        Amount = amount
                    };
                }
                else
                {
                    Db.NonQuery(conn, tx,
                        @"UPDATE dbo.[Payment]
                          SET PaymentStatus = 'Failed', PaymentMethod = @m, IdempotencyKey = @k
                          WHERE PaymentID = @id",
                        "@m", method, "@k", idempotencyKey, "@id", paymentId);

                    // Revert the gate so the customer can retry - a failed sandbox charge must
                    // never leave the shipment stuck in PaymentPending, unable to be paid again.
                    Db.NonQuery(conn, tx,
                        "UPDATE dbo.[Shipment] SET PaymentGateStatus = 'PaymentRequired' WHERE ShipmentID = @s",
                        "@s", shipmentId);

                    Db.NonQuery(conn, tx,
                        @"INSERT INTO dbo.[TrackingHistory] (ShipmentID, Location, Status, Remarks)
                          VALUES (@s, 'Online Payment', 'PaymentFailed', @rm)",
                        "@s", shipmentId, "@rm", result.FailureReason ?? "Payment declined.");

                    Db.NonQuery(conn, tx,
                        @"INSERT INTO dbo.AuditLog (UserID, Action, Entity, EntityID, Details)
                          VALUES (@u, 'Payment.Failed', 'Shipment', @e, @d)",
                        "@u", clientUserId, "@e", shipmentId.ToString(), "@d", result.FailureReason ?? "Declined");

                    outcome = new PaymentOutcome
                    {
                        Success = false,
                        Message = result.FailureReason ?? "Payment declined. Please try again.",
                        InvoiceNumber = invoiceNumber,
                        Amount = amount
                    };
                }
            });

            return outcome;
        }

        private static PaymentOutcome Fail(string message)
        {
            return new PaymentOutcome { Success = false, Message = message };
        }
    }
}
