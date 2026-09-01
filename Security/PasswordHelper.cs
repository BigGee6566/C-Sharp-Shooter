using System;
using System.Security.Cryptography;
using System.Text;

namespace ForteLogisticsApp.Security
{
    /// <summary>
    /// Password hashing.
    ///
    /// New passwords use PBKDF2-HMAC-SHA1 with a random 16-byte salt and 100,000 iterations,
    /// stored as "PBKDF2|iterations|salt|hash". The old scheme was a bare SHA-256 with no
    /// salt, which meant two users choosing the same password produced identical hashes
    /// (visible in the seed data, where both drivers share one) and the whole table could be
    /// checked against a precomputed rainbow table at GPU speed.
    ///
    /// <see cref="Verify"/> still accepts the legacy format so existing accounts keep working;
    /// <see cref="NeedsUpgrade"/> lets the login path transparently re-hash them on the next
    /// successful sign-in.
    /// </summary>
    public static class PasswordHelper
    {
        private const string Pbkdf2Prefix = "PBKDF2";
        private const int SaltBytes = 16;
        private const int HashBytes = 32;
        private const int Iterations = 100000;

        /// <summary>Hashes a password for storage using PBKDF2 with a fresh random salt.</summary>
        public static string Hash(string password)
        {
            if (password == null) password = string.Empty;

            var salt = new byte[SaltBytes];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations))
            {
                byte[] hash = pbkdf2.GetBytes(HashBytes);
                return Pbkdf2Prefix + "|" + Iterations + "|" +
                       Convert.ToBase64String(salt) + "|" + Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Verifies a password against either the PBKDF2 format or the legacy SHA-256 format.
        /// Comparison is constant-time so timing cannot reveal how much of a hash matched.
        /// </summary>
        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(stored)) return false;
            if (password == null) password = string.Empty;

            if (stored.StartsWith(Pbkdf2Prefix + "|", StringComparison.Ordinal))
            {
                string[] parts = stored.Split('|');
                if (parts.Length != 4) return false;

                int iterations;
                if (!int.TryParse(parts[1], out iterations) || iterations < 1000) return false;

                byte[] salt, expected;
                try
                {
                    salt = Convert.FromBase64String(parts[2]);
                    expected = Convert.FromBase64String(parts[3]);
                }
                catch (FormatException)
                {
                    return false;
                }

                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
                {
                    return FixedTimeEquals(pbkdf2.GetBytes(expected.Length), expected);
                }
            }

            return FixedTimeEquals(
                Encoding.UTF8.GetBytes(LegacyHash(password)), Encoding.UTF8.GetBytes(stored));
        }

        /// <summary>True when a stored hash still uses the old unsalted SHA-256 format.</summary>
        public static bool NeedsUpgrade(string stored)
        {
            return !string.IsNullOrEmpty(stored) &&
                   !stored.StartsWith(Pbkdf2Prefix + "|", StringComparison.Ordinal);
        }

        /// <summary>The original unsalted SHA-256 scheme, kept only to verify existing rows.</summary>
        private static string LegacyHash(string password)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password ?? string.Empty));
                return Convert.ToBase64String(bytes);
            }
        }

        /// <summary>Length-independent, content-constant-time comparison.</summary>
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null) return false;

            int diff = a.Length ^ b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= a[i] ^ b[i];
            }
            return diff == 0;
        }

        // No 0/O/1/l/I - a temporary password gets read off a screen and typed by hand.
        private const string TempAlphabet =
            "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";

        /// <summary>
        /// Generates a random temporary password so an administrator never has to
        /// invent (or type) one when creating an account.
        /// </summary>
        public static string GenerateTemporary(int length = 10)
        {
            if (length < 6) length = 6;
            var bytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            var chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = TempAlphabet[bytes[i] % TempAlphabet.Length];
            }
            return new string(chars);
        }
    }
}
