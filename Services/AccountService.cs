using System;
using System.Data.SqlClient;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;

namespace ForteLogisticsApp.Services
{
    /// <summary>An account about to be created (self-registration or Admin -> Users).</summary>
    public sealed class NewAccount
    {
        public string FullName;
        public string Email;
        public string Phone;
        public string Address;
        public string Password;
        /// <summary>Customer, Staff or Admin. Self-registration always passes Customer.</summary>
        public string Role = "Customer";
        /// <summary>Individual or Business (customers only).</summary>
        public string ClientType = "Individual";
        /// <summary>Enrol a Staff account as a driver.</summary>
        public bool IsDriver;
    }

    public sealed class AccountResult
    {
        public bool Success;
        public int UserId;
        public string Message;
    }

    /// <summary>
    /// The one path that creates user accounts. The User row and its role records (Client for a
    /// customer; Admin, and Driver when enrolled, for staff) are written in ONE transaction, so a
    /// failure can never leave a half-made account that can sign in but has no profile.
    ///
    /// Rules enforced here, not only on the page:
    ///   - inputs are normalised and validated (<see cref="Normalize"/> / <see cref="Validate"/>,
    ///     pure and unit-tested), including the width of every column;
    ///   - only a signed-in Admin (verified in the database) may create a Staff or Admin account,
    ///     so a public page can never be turned into an admin factory by a missed role check;
    ///   - two sign-ups racing for one email get the friendly "already registered" answer from the
    ///     database's unique constraint instead of an error page, and the duplicate path does the
    ///     same password-hashing work as a real sign-up, so its timing does not give it away.
    /// </summary>
    public static class AccountService
    {
        public const int NameMax = 150, AddressMax = 250, MinPasswordLength = 8, MaxPasswordLength = 128;
        public const string EmailTaken = "An account with that email already exists.";

        private static readonly string[] Roles = { "Customer", "Staff", "Admin" };

        public static void Normalize(NewAccount a)
        {
            if (a == null) return;
            a.FullName = InputValidator.NormalizeText(a.FullName);
            a.Email = InputValidator.NormalizeEmail(a.Email);
            a.Phone = InputValidator.NormalizePhone(a.Phone);
            a.Address = InputValidator.NormalizeText(a.Address);
            a.ClientType = string.Equals(a.ClientType, "Business", StringComparison.OrdinalIgnoreCase) ? "Business" : "Individual";
        }

        /// <summary>The first problem with a normalised account, in words for the user; null when it can be created.</summary>
        public static string Validate(NewAccount a)
        {
            if (a == null) return "Nothing to create.";
            if (Array.IndexOf(Roles, a.Role) < 0) return "Choose a valid account type.";
            if (a.FullName.Length == 0) return "Please enter a full name.";
            if (a.FullName.Length > NameMax) return "The name is too long (at most " + NameMax + " characters).";
            if (!InputValidator.IsValidEmail(a.Email))
                return "Please enter a valid email address (at most " + InputValidator.EmailMaxLength + " characters, no spaces).";
            if (a.Phone.Length > 0 && !InputValidator.IsValidPhone(a.Phone))
                return "Please enter a phone number with 7 to 15 digits, or leave it blank.";
            if (a.Address.Length > AddressMax) return "The address is too long (at most " + AddressMax + " characters).";
            if (string.IsNullOrEmpty(a.Password) || a.Password.Length < MinPasswordLength || a.Password.Length > MaxPasswordLength)
                return "The password must be " + MinPasswordLength + " to " + MaxPasswordLength + " characters long.";
            if (string.Equals(a.Password.Trim(), a.Email, StringComparison.OrdinalIgnoreCase))
                return "The password must not be the same as the email address.";
            if (a.IsDriver && a.Role != "Staff") return "Only staff accounts can be enrolled as drivers.";
            return null;
        }

        /// <summary>
        /// Normalises, validates and creates the account and its role records atomically.
        /// <paramref name="createdBy"/> is the admin creating it (audited), or null for self-registration.
        /// </summary>
        public static AccountResult Create(NewAccount a, int? createdBy)
        {
            Normalize(a);
            string problem = Validate(a);
            if (problem != null) return new AccountResult { Message = problem };

            if (a.Role != "Customer" && !IsAdmin(createdBy))
                return new AccountResult { Message = "Only an administrator can create staff or admin accounts." };

            // Hashed before the duplicate check so both answers cost the same time.
            string hash = PasswordHelper.Hash(a.Password);
            if (Db.ToInt(Db.Scalar("SELECT COUNT(*) FROM dbo.[User] WHERE Email = @e", "@e", a.Email)) > 0)
                return new AccountResult { Message = EmailTaken };

            int userId = 0;
            try
            {
                Db.RunInTransaction((conn, tx) =>
                {
                    userId = Db.Insert(conn, tx,
                        @"INSERT INTO dbo.[User] (FullName, Email, Password, Role, PhoneNumber, Address)
                          VALUES (@n, @e, @p, @r, @ph, @a)",
                        "@n", a.FullName, "@e", a.Email, "@p", hash,
                        "@r", a.Role, "@ph", a.Phone, "@a", a.Address);

                    if (a.Role == "Customer")
                    {
                        Db.NonQuery(conn, tx, "INSERT INTO dbo.[Client] (UserID, ClientType) VALUES (@u, @t)",
                            "@u", userId, "@t", a.ClientType);
                    }
                    else
                    {
                        Db.NonQuery(conn, tx, "INSERT INTO dbo.[Admin] (UserID, Department) VALUES (@u, @d)",
                            "@u", userId, "@d", a.Role == "Admin" ? "IT / Systems" : "Operations");
                        if (a.IsDriver)
                        {
                            Db.NonQuery(conn, tx, "INSERT INTO dbo.[Driver] (UserID, AvailabilityStatus) VALUES (@u, 'Available')",
                                "@u", userId);
                        }
                    }

                    Db.NonQuery(conn, tx,
                        @"INSERT INTO dbo.AuditLog (UserID, Action, Entity, EntityID, Details)
                          VALUES (@by, @act, 'User', @e, @d)",
                        "@by", (object)createdBy ?? userId,
                        "@act", createdBy == null ? "User.Register" : "User.Create",
                        "@e", userId.ToString(),
                        "@d", a.Role + (a.IsDriver ? " (driver)" : string.Empty) + " account for " + a.Email);
                });
            }
            catch (SqlException ex) when (IntakeService.IsUniqueViolation(ex) && ex.Message.IndexOf(a.Email, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Another sign-up claimed the email between the check above and the INSERT.
                return new AccountResult { Message = EmailTaken };
            }
            return new AccountResult { Success = true, UserId = userId };
        }

        private static bool IsAdmin(int? userId)
        {
            return userId != null &&
                   Db.ToInt(Db.Scalar("SELECT COUNT(*) FROM dbo.[User] WHERE UserID = @u AND Role = 'Admin'", "@u", userId.Value)) == 1;
        }
    }
}
