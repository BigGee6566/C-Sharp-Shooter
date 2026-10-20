using System;
using System.Data;
using System.Text;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;

namespace ForteLogisticsApp.Services
{
    /// <summary>An admin's edit of an existing user (Admin -> Users).</summary>
    public sealed class UserEdit
    {
        public int UserId;
        public string FullName;
        public string Phone;
        public string Address;
        public string Role;
        /// <summary>New password, or empty to leave it unchanged.</summary>
        public string ResetPassword;
        public bool IsDriver;
        public string ClientType;
        public string BusinessName;
        /// <summary>True when the billing fields were shown and may be applied.</summary>
        public bool UpdateBilling;
    }

    /// <summary>The rules for changing who holds what role. Pure and unit-tested.</summary>
    public static class UserAdminRules
    {
        /// <summary>
        /// Why this role change must not happen, or null. An admin cannot take away their own
        /// admin access (they would lock themselves out mid-session), and the last admin can
        /// never be demoted (nobody would be left to manage users).
        /// </summary>
        public static string CheckRoleChange(int actorId, int targetId, string currentRole, string newRole, int adminCount)
        {
            bool losesAdmin = currentRole == "Admin" && newRole != "Admin";
            if (!losesAdmin) return null;
            if (actorId == targetId) return "You cannot remove your own administrator access. Ask another administrator to do it.";
            if (adminCount <= 1) return "This is the last administrator account, so it must stay an administrator.";
            return null;
        }
    }

    /// <summary>
    /// The one path for an admin's edit of a user. The profile, role, role records, billing
    /// type, driver enrolment and any password reset are applied in ONE transaction, so a
    /// refused part leaves nothing half-changed. Role changes are serialised by an application
    /// lock, so two admins demoting each other at once can neither both succeed nor deadlock;
    /// the acting admin is re-checked inside it. Changes and refusals go to the audit log, and
    /// the user is told (without any secret) when their password or role changes. A role change
    /// or password reset also ends that user's other sessions (SessionGuard).
    /// </summary>
    public static class UserAdminService
    {
        private static readonly string[] Roles = { "Customer", "Staff", "Admin" };

        public static AccountResult Update(UserEdit e, int actorId)
        {
            if (e == null) return Fail("Nothing to save.");
            e.FullName = InputValidator.NormalizeText(e.FullName);
            e.Phone = InputValidator.NormalizePhone(e.Phone);
            e.Address = InputValidator.NormalizeText(e.Address);
            e.BusinessName = InputValidator.NormalizeText(e.BusinessName);
            e.ClientType = string.Equals(e.ClientType, "Business", StringComparison.OrdinalIgnoreCase) ? "Business" : "Individual";

            if (Array.IndexOf(Roles, e.Role) < 0) return Fail("Choose a valid role.");
            if (e.FullName.Length == 0) return Fail("Full name is required.");
            if (e.FullName.Length > AccountService.NameMax) return Fail("The name is too long (at most " + AccountService.NameMax + " characters).");
            if (e.Address.Length > AccountService.AddressMax) return Fail("The address is too long (at most " + AccountService.AddressMax + " characters).");
            if (e.BusinessName.Length > 150) return Fail("The business name is too long (at most 150 characters).");
            bool reset = !string.IsNullOrEmpty(e.ResetPassword);
            if (reset && (e.ResetPassword.Length < AccountService.MinPasswordLength || e.ResetPassword.Length > AccountService.MaxPasswordLength))
                return Fail("A new password must be " + AccountService.MinPasswordLength + " to " + AccountService.MaxPasswordLength + " characters long.");
            bool wantsDriver = e.IsDriver && e.Role == "Staff";
            string hash = reset ? PasswordHelper.Hash(e.ResetPassword) : null;

            string refusal = null;
            string currentRole = null;
            bool wasDriver = false;
            Db.RunInTransaction((conn, tx) =>
            {
                // One admin edit at a time: no two-row lock ordering, so no deadlock, and the
                // admin count below cannot change under us.
                int got = Db.ToInt(Db.Scalar(conn, tx,
                    @"DECLARE @r INT;
                      EXEC @r = sp_getapplock @Resource = 'UserAdmin', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
                      SELECT @r;"));
                if (got < 0) { refusal = "Another administrator is saving a change right now. Please try again."; return; }

                // The acting admin must still be one (their session may predate a demotion).
                if (Db.ToInt(Db.Scalar(conn, tx, "SELECT COUNT(*) FROM dbo.[User] WHERE UserID = @a AND Role = 'Admin'", "@a", actorId)) != 1)
                {
                    refusal = "Your administrator access has changed. Please sign in again.";
                    return;
                }

                DataTable cur = Db.Query(conn, tx, "SELECT Role, PhoneNumber FROM dbo.[User] WHERE UserID = @u", "@u", e.UserId);
                if (cur.Rows.Count == 0) { refusal = "That user no longer exists."; return; }
                currentRole = Db.ToStr(cur.Rows[0]["Role"]);

                // Only a phone that is being changed has to pass today's rules; an old stored
                // number must not block an unrelated edit.
                if (e.Phone.Length > 0 && e.Phone != InputValidator.NormalizePhone(Db.ToStr(cur.Rows[0]["PhoneNumber"])) &&
                    !InputValidator.IsValidPhone(e.Phone))
                {
                    refusal = "Please enter a phone number with 7 to 15 digits, or leave it blank.";
                    return;
                }

                int admins = currentRole == "Admin" && e.Role != "Admin"
                    ? Db.ToInt(Db.Scalar(conn, tx, "SELECT COUNT(*) FROM dbo.[User] WHERE Role = 'Admin'"))
                    : 0;
                refusal = UserAdminRules.CheckRoleChange(actorId, e.UserId, currentRole, e.Role, admins);
                if (refusal != null) return;

                wasDriver = Db.ToInt(Db.Scalar(conn, tx, "SELECT COUNT(*) FROM dbo.[Driver] WHERE UserID = @u", "@u", e.UserId)) > 0;
                if (wasDriver && !wantsDriver)
                {
                    // Shipments, vehicles and routes all point at the driver record; it can only
                    // go once nothing does. (Delivered history keeps it for good - set the driver
                    // Off Duty instead.)
                    int holds = Db.ToInt(Db.Scalar(conn, tx,
                        @"SELECT (SELECT COUNT(*) FROM dbo.[Shipment] WHERE DriverID = @u)
                               + (SELECT COUNT(*) FROM dbo.[Vehicle] WHERE DriverID = @u)
                               + (SELECT COUNT(*) FROM dbo.[Route] WHERE DriverID = @u)", "@u", e.UserId));
                    if (holds > 0)
                    {
                        refusal = "This user still has parcels, a vehicle or a route on record as a driver, so their driver " +
                                  "record must stay (reassign them, or keep them as Staff and mark them Off Duty). Nothing was changed.";
                        return;
                    }
                }

                Db.NonQuery(conn, tx,
                    "UPDATE dbo.[User] SET FullName = @n, PhoneNumber = @ph, Address = @a, Role = @r WHERE UserID = @u",
                    "@n", e.FullName, "@ph", e.Phone, "@a", e.Address, "@r", e.Role, "@u", e.UserId);
                if (reset)
                {
                    Db.NonQuery(conn, tx, "UPDATE dbo.[User] SET Password = @p WHERE UserID = @u", "@p", hash, "@u", e.UserId);
                }

                // Role records for the (possibly new) role. Old role rows are kept: billing and
                // staff history hang off them, and no page treats them as authority.
                if (e.Role == "Customer")
                {
                    bool hasClient = Db.ToInt(Db.Scalar(conn, tx, "SELECT COUNT(*) FROM dbo.[Client] WHERE UserID = @u", "@u", e.UserId)) > 0;
                    if (!hasClient)
                        Db.NonQuery(conn, tx, "INSERT INTO dbo.[Client] (UserID, ClientType) VALUES (@u, 'Individual')", "@u", e.UserId);

                    // Billing is only changed when its fields were on screen, so switching a
                    // Staff account to Customer cannot silently wipe an existing business link.
                    if (e.UpdateBilling)
                    {
                        object businessId = DBNull.Value;
                        if (e.ClientType == "Business" && e.BusinessName.Length > 0)
                        {
                            object existing = Db.Scalar(conn, tx, "SELECT TOP 1 BusinessID FROM dbo.Business WHERE BusinessName = @n", "@n", e.BusinessName);
                            businessId = existing != null && existing != DBNull.Value
                                ? existing
                                : (object)Db.Insert(conn, tx, "INSERT INTO dbo.Business (BusinessName) VALUES (@n)", "@n", e.BusinessName);
                        }
                        Db.NonQuery(conn, tx, "UPDATE dbo.[Client] SET ClientType = @t, BusinessID = @b WHERE UserID = @u",
                            "@t", e.ClientType, "@b", businessId, "@u", e.UserId);
                    }
                }
                else if (Db.ToInt(Db.Scalar(conn, tx, "SELECT COUNT(*) FROM dbo.[Admin] WHERE UserID = @u", "@u", e.UserId)) == 0)
                {
                    Db.NonQuery(conn, tx, "INSERT INTO dbo.[Admin] (UserID, Department) VALUES (@u, @d)",
                        "@u", e.UserId, "@d", e.Role == "Admin" ? "IT / Systems" : "Operations");
                }

                if (wantsDriver && !wasDriver)
                    Db.NonQuery(conn, tx, "INSERT INTO dbo.[Driver] (UserID, AvailabilityStatus) VALUES (@u, 'Available')", "@u", e.UserId);
                else if (!wantsDriver && wasDriver)
                    Db.NonQuery(conn, tx, "DELETE FROM dbo.[Driver] WHERE UserID = @u", "@u", e.UserId);

                var changes = new StringBuilder();
                if (currentRole != e.Role) changes.Append("role ").Append(currentRole).Append(" -> ").Append(e.Role).Append("; ");
                if (reset) changes.Append("password reset; ");
                if (wantsDriver != wasDriver) changes.Append(wantsDriver ? "enrolled as driver; " : "removed as driver; ");
                Db.NonQuery(conn, tx,
                    @"INSERT INTO dbo.AuditLog (UserID, Action, Entity, EntityID, Details)
                      VALUES (@by, 'User.Update', 'User', @e, @d)",
                    "@by", actorId, "@e", e.UserId.ToString(),
                    "@d", changes.Length == 0 ? "profile details" : changes.ToString().TrimEnd(' ', ';'));
            });

            if (refusal != null)
            {
                Audit(actorId, "User.Update.Denied", e.UserId, refusal);
                return Fail(refusal);
            }

            // After commit, best effort: the user learns of changes to their own account.
            if (reset)
                NotificationService.TrySend(e.UserId, null, "An administrator reset your password. If you did not ask for this, contact the office.", "System");
            if (currentRole != e.Role)
                NotificationService.TrySend(e.UserId, null, "Your account type was changed from " + currentRole + " to " + e.Role + " by an administrator.", "System");

            string note = e.IsDriver && !wantsDriver
                ? " Only Staff accounts can be drivers, so this account is not enrolled as a driver."
                : string.Empty;
            return new AccountResult { Success = true, UserId = e.UserId, Message = "User updated." + note };
        }

        /// <summary>Writes an audit row on its own (used for refusals, whose transaction wrote nothing).</summary>
        private static void Audit(int actorId, string action, int targetId, string details)
        {
            try
            {
                Db.NonQuery(@"INSERT INTO dbo.AuditLog (UserID, Action, Entity, EntityID, Details)
                              VALUES (@by, @act, 'User', @e, @d)",
                    "@by", actorId, "@act", action, "@e", targetId.ToString(), "@d", details.Length > 1000 ? details.Substring(0, 1000) : details);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Audit write failed: " + ex);
            }
        }

        private static AccountResult Fail(string message)
        {
            return new AccountResult { Message = message };
        }
    }

    /// <summary>Vehicle registration rules and the guarded insert behind Admin -> Fleet.</summary>
    public static class FleetService
    {
        public static readonly string[] VehicleTypes = { "Panel Van", "1-Ton Truck", "3-Ton Truck", "Motorbike" };
        public const int RegistrationMax = 30;

        /// <summary>One canonical form - upper case, single spaces - so "ca 123-456" and "CA 123-456" are the same plate.</summary>
        public static string NormalizeRegistration(string value)
        {
            return InputValidator.NormalizeText(value).ToUpperInvariant();
        }

        /// <summary>The plate with spaces and hyphens removed, for comparing two registrations.</summary>
        public static string RegistrationKey(string normalized)
        {
            return (normalized ?? string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty);
        }

        /// <summary>Letters, digits, spaces and hyphens only, 2 to 30 characters.</summary>
        public static bool IsValidRegistration(string normalized)
        {
            if (string.IsNullOrEmpty(normalized) || normalized.Length < 2 || normalized.Length > RegistrationMax) return false;
            foreach (char c in normalized)
            {
                if (!((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == ' ' || c == '-')) return false;
            }
            return true;
        }

        /// <summary>Adds a vehicle, or says why not. Duplicate plates are refused atomically.</summary>
        public static AccountResult AddVehicle(string registration, string vehicleType, int? driverId, int actorId)
        {
            string reg = NormalizeRegistration(registration);
            if (!IsValidRegistration(reg))
                return new AccountResult { Message = "Enter a registration of 2 to " + RegistrationMax + " letters, digits, spaces or hyphens." };
            if (Array.IndexOf(VehicleTypes, vehicleType) < 0)
                return new AccountResult { Message = "Choose a vehicle type from the list." };
            if (driverId != null &&
                Db.ToInt(Db.Scalar("SELECT COUNT(*) FROM dbo.[Driver] WHERE UserID = @d", "@d", driverId.Value)) != 1)
                return new AccountResult { Message = "That driver is no longer registered. Reload the page and choose again." };

            int added = 0;
            Db.RunInTransaction((conn, tx) =>
            {
                // Check-and-insert in one locked statement: two admins adding the same plate at
                // once cannot both succeed.
                added = Db.NonQuery(conn, tx,
                    @"INSERT INTO dbo.[Vehicle] (DriverID, VehicleType, RegistrationNumber)
                      SELECT @d, @t, @r
                      WHERE NOT EXISTS (SELECT 1 FROM dbo.[Vehicle] WITH (UPDLOCK, HOLDLOCK)
                                        WHERE REPLACE(REPLACE(UPPER(RegistrationNumber), ' ', ''), '-', '') = @k)",
                    "@d", (object)driverId ?? DBNull.Value, "@t", vehicleType, "@r", reg, "@k", RegistrationKey(reg));
                if (added == 1)
                {
                    Db.NonQuery(conn, tx,
                        @"INSERT INTO dbo.AuditLog (UserID, Action, Entity, EntityID, Details)
                          VALUES (@by, 'Vehicle.Create', 'Vehicle', @r, @t)",
                        "@by", actorId, "@r", reg, "@t", vehicleType);
                }
            });
            return added == 1
                ? new AccountResult { Success = true }
                : new AccountResult { Message = "A vehicle with registration " + reg + " is already in the fleet." };
        }
    }
}
