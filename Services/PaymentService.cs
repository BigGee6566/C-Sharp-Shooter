using System;
using System.Data;
using System.Text;
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

        public class BulkPaymentOutcome
        {
            public bool Success;
            public string Message;
            public string ReferenceNumber;
            public int InvoiceCount;
            public decimal TotalAmount;
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

        /// <summary>
        /// Admin/Finance recording of a payment received outside the system (e.g. an EFT that
        /// landed in the bank account) - Finance\Invoices.aspx's "Mark Paid".
        ///
        /// Unlike PayForShipment/PayInvoices this never calls PaymentGateway: there is no
        /// charge to make, only a receipt to record, so it is not "sandbox payment" in the
        /// same sense. It still shares their transactional, audited, race-safe shape: the
        /// UPDLOCK'd read-then-conditional-update means two concurrent clicks (or a
        /// resubmitted form) can only ever mark the invoice paid once - the second caller
        /// simply finds it already Paid, so no separate idempotency key is needed here.
        /// Authorization (Admin/Staff-only) is the caller's responsibility, exactly as every
        /// other page-level action in this codebase enforces its own RequireRole.
        /// </summary>
        public static PaymentOutcome MarkInvoicePaidByAdmin(int paymentId, int adminUserId, string method)
        {
            PaymentOutcome outcome = null;

            Db.RunInTransaction((conn, tx) =>
            {
                DataTable pay = Db.Query(conn, tx,
                    @"SELECT PaymentStatus, InvoiceNumber, Amount, ShipmentID
                      FROM dbo.[Payment] WITH (UPDLOCK, ROWLOCK) WHERE PaymentID = @id",
                    "@id", paymentId);
                if (pay.Rows.Count == 0)
                {
                    outcome = Fail("Invoice not found.");
                    return;
                }

                DataRow row = pay.Rows[0];
                if (Db.ToStr(row["PaymentStatus"]) != "Unpaid")
                {
                    outcome = Fail("That invoice has already been settled.");
                    return;
                }

                decimal amount = Db.ToDecimal(row["Amount"]);
                string invoiceNumber = Db.ToStr(row["InvoiceNumber"]);
                string reference = "EFT-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

                Db.NonQuery(conn, tx,
                    @"UPDATE dbo.[Payment]
                      SET PaymentStatus = 'Paid', PaymentMethod = @m, ReferenceNumber = @r
                      WHERE PaymentID = @id",
                    "@m", method, "@r", reference, "@id", paymentId);

                if (row["ShipmentID"] != DBNull.Value)
                {
                    Db.NonQuery(conn, tx,
                        @"INSERT INTO dbo.[TrackingHistory] (ShipmentID, Location, Status, Remarks)
                          VALUES (@s, 'Finance', 'InvoicePaid', @rm)",
                        "@s", Db.ToInt(row["ShipmentID"]),
                        "@rm", "Payment recorded by Finance (" + method + "), ref " + reference);
                }

                Db.NonQuery(conn, tx,
                    @"INSERT INTO dbo.AuditLog (UserID, Action, Entity, EntityID, Details)
                      VALUES (@u, 'Payment.AdminRecorded', 'Payment', @e, @d)",
                    "@u", adminUserId, "@e", paymentId.ToString(),
                    "@d", "Invoice " + invoiceNumber + " marked paid, ref " + reference);

                outcome = new PaymentOutcome
                {
                    Success = true,
                    Message = "Payment recorded.",
                    ReferenceNumber = reference,
                    InvoiceNumber = invoiceNumber,
                    Amount = amount
                };
            });

            return outcome;
        }

        /// <summary>
        /// Business (or self-service Individual) bulk invoice payment: pays one, several, or
        /// all of the selected invoices in a single charge.
        ///
        /// The caller's billing scope - every Client sharing their Business, or just
        /// themself if they belong to no Business - is derived here, server-side, and is
        /// what actually authorizes each invoice; the posted <paramref name="paymentIds"/>
        /// list only says which of those eligible invoices to include. Any id outside the
        /// caller's scope, already paid, or simply not found fails the whole batch rather
        /// than silently dropping it, so a client can never learn which other PaymentIDs
        /// exist by probing. The total charged is always SUM(Amount) read back from the
        /// database, never a browser-supplied figure.
        /// </summary>
        public static BulkPaymentOutcome PayInvoices(
            int[] paymentIds, int payingUserId, string method, string idempotencyKey)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                throw new ArgumentException("An idempotency key is required.", "idempotencyKey");
            if (paymentIds == null || paymentIds.Length == 0)
                return new BulkPaymentOutcome { Success = false, Message = "Select at least one invoice to pay." };

            BulkPaymentOutcome outcome = null;

            Db.RunInTransaction((conn, tx) =>
            {
                // Duplicate submission of the exact same batch: return the original result
                // instead of charging again. Every allocation in one batch shares the same
                // gateway reference (one charge for the whole batch), so any row's is correct.
                DataTable already = Db.Query(conn, tx,
                    @"SELECT COUNT(*) AS Cnt, SUM(pa.AmountApplied) AS Total, MAX(p.ReferenceNumber) AS ReferenceNumber
                      FROM dbo.PaymentAllocation pa
                      JOIN dbo.[Payment] p ON p.PaymentID = pa.PaymentID
                      WHERE pa.BatchKey = @k", "@k", idempotencyKey);
                if (already.Rows.Count > 0 && Db.ToInt(already.Rows[0]["Cnt"]) > 0)
                {
                    outcome = new BulkPaymentOutcome
                    {
                        Success = true,
                        Message = "Payment already completed.",
                        ReferenceNumber = Db.ToStr(already.Rows[0]["ReferenceNumber"]),
                        InvoiceCount = Db.ToInt(already.Rows[0]["Cnt"]),
                        TotalAmount = Db.ToDecimal(already.Rows[0]["Total"])
                    };
                    return;
                }

                DataTable scope = Db.Query(conn, tx,
                    "SELECT BusinessID FROM dbo.[Client] WHERE UserID = @u", "@u", payingUserId);
                if (scope.Rows.Count == 0)
                {
                    outcome = new BulkPaymentOutcome { Success = false, Message = "No billing account found." };
                    return;
                }
                object businessId = scope.Rows[0]["BusinessID"];

                // paymentIds are ints already parsed by the caller (never raw request text),
                // so building the IN(...) list this way cannot inject SQL.
                var idList = new StringBuilder();
                for (int i = 0; i < paymentIds.Length; i++)
                {
                    if (i > 0) idList.Append(',');
                    idList.Append(paymentIds[i]);
                }

                DataTable rows = Db.Query(conn, tx,
                    @"SELECT p.PaymentID, p.Amount, p.PaymentStatus, p.ShipmentID
                      FROM dbo.[Payment] p WITH (UPDLOCK, ROWLOCK)
                      JOIN dbo.[Client] c ON c.UserID = p.ClientID
                      WHERE p.PaymentID IN (" + idList + @")
                        AND ((@b IS NOT NULL AND c.BusinessID = @b) OR (@b IS NULL AND p.ClientID = @u))",
                    "@b", businessId, "@u", payingUserId);

                if (rows.Rows.Count != paymentIds.Length)
                {
                    outcome = new BulkPaymentOutcome
                    {
                        Success = false,
                        Message = "One or more selected invoices are invalid, not yours, or belong to another business."
                    };
                    return;
                }

                decimal total = 0m;
                foreach (DataRow r in rows.Rows)
                {
                    if (Db.ToStr(r["PaymentStatus"]) != "Unpaid")
                    {
                        outcome = new BulkPaymentOutcome
                        {
                            Success = false,
                            Message = "One or more selected invoices are no longer unpaid. Refresh and try again."
                        };
                        return;
                    }
                    total += Db.ToDecimal(r["Amount"]);
                }

                var result = PaymentGateway.Charge(method, total, null);

                if (!result.Success)
                {
                    Db.NonQuery(conn, tx,
                        @"INSERT INTO dbo.AuditLog (UserID, Action, Entity, EntityID, Details)
                          VALUES (@u, 'Payment.BulkFailed', 'PaymentBatch', @e, @d)",
                        "@u", payingUserId, "@e", idempotencyKey,
                        "@d", (result.FailureReason ?? "Declined") + " (" + rows.Rows.Count + " invoice(s), R " + total.ToString("N2") + ")");

                    outcome = new BulkPaymentOutcome
                    {
                        Success = false,
                        Message = result.FailureReason ?? "Payment declined. Please try again.",
                        InvoiceCount = rows.Rows.Count,
                        TotalAmount = total
                    };
                    return;
                }

                foreach (DataRow r in rows.Rows)
                {
                    int paymentId = Db.ToInt(r["PaymentID"]);
                    decimal amount = Db.ToDecimal(r["Amount"]);

                    Db.NonQuery(conn, tx,
                        @"UPDATE dbo.[Payment]
                          SET PaymentStatus = 'Paid', PaymentMethod = @m, ReferenceNumber = @r
                          WHERE PaymentID = @id",
                        "@m", method, "@r", result.ReferenceNumber, "@id", paymentId);

                    Db.NonQuery(conn, tx,
                        @"INSERT INTO dbo.PaymentAllocation (PaymentID, AmountApplied, AppliedBy, BatchKey)
                          VALUES (@p, @a, @u, @k)",
                        "@p", paymentId, "@a", amount, "@u", payingUserId, "@k", idempotencyKey);

                    if (r["ShipmentID"] != DBNull.Value)
                    {
                        Db.NonQuery(conn, tx,
                            @"INSERT INTO dbo.[TrackingHistory] (ShipmentID, Location, Status, Remarks)
                              VALUES (@s, 'Online Payment', 'InvoicePaid', @rm)",
                            "@s", Db.ToInt(r["ShipmentID"]),
                            "@rm", "Invoice paid via sandbox gateway (" + method + "), ref " + result.ReferenceNumber);
                    }
                }

                Db.NonQuery(conn, tx,
                    @"INSERT INTO dbo.AuditLog (UserID, Action, Entity, EntityID, Details)
                      VALUES (@u, 'Payment.BulkSuccess', 'PaymentBatch', @e, @d)",
                    "@u", payingUserId, "@e", idempotencyKey,
                    "@d", rows.Rows.Count + " invoice(s) paid, total R " + total.ToString("N2") + ", ref " + result.ReferenceNumber);

                outcome = new BulkPaymentOutcome
                {
                    Success = true,
                    Message = "Payment successful.",
                    ReferenceNumber = result.ReferenceNumber,
                    InvoiceCount = rows.Rows.Count,
                    TotalAmount = total
                };
            });

            return outcome;
        }
    }
}
