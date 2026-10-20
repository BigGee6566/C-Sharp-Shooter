using System;
using System.Globalization;
using ForteLogisticsApp.Data;

namespace ForteLogisticsApp.Services
{
    /// <summary>Shared business helpers: tracking numbers, quotes and invoice numbers.</summary>
    public static class CourierService
    {
        private static readonly Random Rng = new Random();

        /// <summary>Generates a unique tracking/barcode number, e.g. FCL-48210453.</summary>
        public static string GenerateTrackingNumber()
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                string candidate = "FCL-" + Rng.Next(10000000, 99999999).ToString();
                object exists = Db.Scalar(
                    "SELECT COUNT(*) FROM dbo.[Shipment] WHERE TrackingNumber = @t", "@t", candidate);
                if (Db.ToInt(exists) == 0) return candidate;
            }
            return "FCL-" + DateTime.Now.Ticks.ToString().Substring(10);
        }

        /// <summary>
        /// Parses a weight entered by a user, accepting either decimal separator.
        ///
        /// This server runs under en-ZA, where the decimal separator is a comma, so a plain
        /// decimal.TryParse("3.5") returns false and the caller silently fell back to 1 kg -
        /// quoting and invoicing a 3.5 kg parcel as if it weighed 1 kg. Browsers also submit
        /// &lt;input type="number"&gt; values with a dot regardless of the user's locale, so
        /// both forms have to be understood.
        /// </summary>
        public static bool TryParseWeight(string text, out decimal weight)
        {
            weight = 0m;
            if (string.IsNullOrEmpty(text)) return false;

            text = text.Trim();

            // Try the user's culture first, then the invariant form, then the opposite separator.
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out weight)) return true;
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out weight)) return true;

            string swapped = text.IndexOf(',') >= 0 ? text.Replace(',', '.') : text.Replace('.', ',');
            return decimal.TryParse(swapped, NumberStyles.Number, CultureInfo.CurrentCulture, out weight)
                || decimal.TryParse(swapped, NumberStyles.Number, CultureInfo.InvariantCulture, out weight);
        }

        /// <summary>Parses a weight, falling back to <paramref name="fallback"/> when unusable.</summary>
        public static decimal ParseWeight(string text, decimal fallback = 1m)
        {
            decimal w;
            if (!TryParseWeight(text, out w)) return fallback;
            return (w <= 0m || w > 1000m) ? fallback : w;
        }

        /// <summary>Same rate model used by the public quote calculator on the home page.</summary>
        public static decimal CalculateQuote(string origin, string destination, decimal weightKg)
        {
            if (weightKg < 1) weightKg = 1;
            decimal baseRate = string.Equals(origin, destination, StringComparison.OrdinalIgnoreCase) ? 85m : 145m;
            decimal extra = weightKg > 2 ? (weightKg - 2) * 18m : 0m;
            return baseRate + extra;
        }

        /// <summary>
        /// Generates a sequential invoice number, e.g. INV-20260007.
        /// The counter is claimed atomically from dbo.NumberSequence so two deliveries
        /// confirmed at the same moment cannot produce the same number, and it restarts
        /// per year in step with the prefix.
        /// </summary>
        public static string GenerateInvoiceNumber()
        {
            string seq = "INV-" + DateTime.Now.Year.ToString();
            object next = Db.Scalar(ClaimSql, "@n", seq);
            return seq + Db.ToInt(next, 1).ToString("D4");
        }

        /// <summary>
        /// As <see cref="GenerateInvoiceNumber()"/>, inside the caller's transaction: if the
        /// invoice is rolled back, so is the number, so the sequence has no gaps.
        /// </summary>
        public static string GenerateInvoiceNumber(System.Data.SqlClient.SqlConnection conn, System.Data.SqlClient.SqlTransaction tx)
        {
            string seq = "INV-" + DateTime.Now.Year.ToString();
            object next = Db.Scalar(conn, tx, ClaimSql, "@n", seq);
            return seq + Db.ToInt(next, 1).ToString("D4");
        }

        private const string ClaimSql =
            @"MERGE dbo.NumberSequence WITH (HOLDLOCK) AS t
              USING (SELECT @n AS SeqName) AS s ON t.SeqName = s.SeqName
              WHEN MATCHED THEN UPDATE SET t.NextValue = t.NextValue + 1
              WHEN NOT MATCHED THEN INSERT (SeqName, NextValue) VALUES (@n, 1)
              OUTPUT inserted.NextValue;";
    }
}
