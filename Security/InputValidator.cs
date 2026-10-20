using System;
using System.Globalization;
using System.Text;

namespace ForteLogisticsApp.Security
{
    /// <summary>
    /// Shared, culture-stable input normalisation and validation. Every method is pure and
    /// side-effect free, so it is unit-testable without a page or a database.
    ///
    /// The principle is normalise-on-input, encode-on-output: this type owns the input half
    /// (trim, collapse whitespace, canonical casing, range-check), while UiHelper.Enc owns the
    /// output half. Normalising here does NOT make a value safe to emit as HTML - callers must
    /// still encode on the way out. It is also not a SQL-safety layer: every query in this
    /// codebase is already parameterised.
    /// </summary>
    public static class InputValidator
    {
        /// <summary>
        /// Trims, and collapses every run of whitespace (including tabs and newlines) to a
        /// single space. Invisible characters are dropped: control characters, and format
        /// characters such as zero-width spaces and right-to-left overrides, which can make a
        /// stored name or address display as something other than what it is. Never returns
        /// null. When <paramref name="maxLength"/> is positive the result is cut to that many
        /// characters, so an over-long paste cannot blow past a column width or a log line.
        /// </summary>
        public static string NormalizeText(string value, int maxLength = 0)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var sb = new StringBuilder(value.Length);
            bool inWhitespace = false;
            foreach (char c in value)
            {
                if (char.IsWhiteSpace(c))
                {
                    inWhitespace = true;
                    continue;
                }
                if (IsInvisible(c)) continue;
                if (inWhitespace && sb.Length > 0) sb.Append(' ');
                inWhitespace = false;
                sb.Append(c);
            }

            string result = sb.ToString();
            if (maxLength > 0 && result.Length > maxLength) result = result.Substring(0, maxLength);
            return result;
        }

        /// <summary>
        /// For free-form messages: keeps line breaks and tabs, drops other invisible characters,
        /// trims the ends. Never returns null. Does not cut - callers refuse what is too long.
        /// </summary>
        public static string NormalizeMultiline(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var sb = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (c == '\n' || c == '\r' || c == '\t' || !IsInvisible(c)) sb.Append(c);
            }
            return sb.ToString().Trim();
        }

        private static bool IsInvisible(char c)
        {
            UnicodeCategory cat = char.GetUnicodeCategory(c);
            return cat == UnicodeCategory.Control || cat == UnicodeCategory.Format || cat == UnicodeCategory.PrivateUse;
        }

        /// <summary>The width of User.Email; a longer address could not be stored.</summary>
        public const int EmailMaxLength = 150;

        /// <summary>Trim and lower-case (invariant) so the same address compares equal regardless of how it was typed.</summary>
        public static string NormalizeEmail(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Trim().ToLowerInvariant();
        }

        /// <summary>
        /// A deliberately conservative structural check: printable ASCII only with no spaces
        /// (so look-alike Unicode letters cannot impersonate another address), at most
        /// <see cref="EmailMaxLength"/> characters, exactly one @, a non-empty local part, and a
        /// domain that contains a dot with something either side. It does not try to be RFC 5322
        /// complete - the goal is to reject rubbish before it is stored, not to prove deliverability.
        /// </summary>
        public static bool IsValidEmail(string value)
        {
            string email = NormalizeEmail(value);
            if (email.Length == 0 || email.Length > EmailMaxLength) return false;
            foreach (char c in email)
            {
                if (c <= ' ' || c > '~') return false;
            }

            int at = email.IndexOf('@');
            if (at <= 0 || at != email.LastIndexOf('@') || at == email.Length - 1) return false;

            string domain = email.Substring(at + 1);
            int dot = domain.IndexOf('.');
            return dot > 0 && dot < domain.Length - 1 && domain.IndexOf(' ') < 0;
        }

        /// <summary>
        /// Keeps a single leading + (international prefix) and strips everything that is not a
        /// digit, so "071 234 5678" and "+27 71 234 5678" normalise to a comparable form. Not a
        /// phone-number-validity oracle; see <see cref="IsValidPhone"/> for the length gate.
        /// </summary>
        public static string NormalizePhone(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            string trimmed = value.Trim();
            bool plus = trimmed.StartsWith("+");

            var sb = new StringBuilder(trimmed.Length);
            if (plus) sb.Append('+');
            foreach (char c in trimmed)
                if (c >= '0' && c <= '9') sb.Append(c);

            return sb.ToString();
        }

        /// <summary>True when the normalised number carries 7 to 15 digits (ITU E.164 upper bound).</summary>
        public static bool IsValidPhone(string value)
        {
            string normalized = NormalizePhone(value);
            int digits = 0;
            foreach (char c in normalized) if (c >= '0' && c <= '9') digits++;
            return digits >= 7 && digits <= 15;
        }

        /// <summary>
        /// A city is free text the customer may type, so this only tidies whitespace and caps the
        /// length - at the 60-character width of Shipment.OriginCity/DestCity, so a long paste is
        /// cut rather than failing the INSERT.
        /// </summary>
        public static string NormalizeCity(string value)
        {
            return NormalizeText(value, 60);
        }

        /// <summary>
        /// Canonical form of a waybill: no surrounding or internal whitespace, upper-cased, so a
        /// number pasted as "fcl-2000 0001" or " FCL-20000001 " still finds its shipment.
        /// </summary>
        public static string NormalizeTrackingNumber(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var sb = new StringBuilder(value.Length);
            foreach (char c in value)
                if (!char.IsWhiteSpace(c)) sb.Append(c);

            return sb.ToString().ToUpperInvariant();
        }

        /// <summary>
        /// Parses a parcel weight tolerant of both the en-ZA comma and the invariant dot as the
        /// decimal separator, and accepts it only when it is a positive value no greater than
        /// <paramref name="maxKg"/>. A blank, non-numeric, zero, negative or over-limit entry
        /// returns false so the caller can reject it rather than silently booking a 0 kg parcel.
        /// </summary>
        public static bool TryParseWeight(string value, out decimal kg, decimal maxKg = 1000m)
        {
            kg = 0m;
            if (string.IsNullOrWhiteSpace(value)) return false;

            string trimmed = value.Trim();
            decimal parsed;
            bool ok =
                decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.GetCultureInfo("en-ZA"), out parsed) ||
                decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed);

            if (!ok || parsed <= 0m || parsed > maxKg) return false;

            kg = parsed;
            return true;
        }

        /// <summary>
        /// Parses a date using the application culture (en-ZA, day-first) then the invariant
        /// culture, rejecting anything that is not a real calendar date. Returns false rather
        /// than throwing so a bad filter value cannot crash a report.
        /// </summary>
        public static bool TryParseDate(string value, out DateTime date)
        {
            date = default(DateTime);
            if (string.IsNullOrWhiteSpace(value)) return false;

            string trimmed = value.Trim();
            return
                DateTime.TryParse(trimmed, CultureInfo.GetCultureInfo("en-ZA"), DateTimeStyles.None, out date) ||
                DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }
    }
}
