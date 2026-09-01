using System;

namespace ForteLogisticsApp.Services
{
    /// <summary>
    /// One preset parcel band offered to customers in step 1 of the booking wizard.
    ///
    /// This type is the single source of truth for the bands. The billed weight, the
    /// dimensions printed on the waybill and the "from" price on the card all read from
    /// here, so a change to a band moves all three together. Before this existed the
    /// weights lived in a switch in the code-behind while the weights, dimensions and
    /// prices were also re-typed as literal text in four near-identical markup blocks -
    /// and the prices had already drifted away from what the customer was actually charged.
    /// </summary>
    public sealed class ParcelSize
    {
        private ParcelSize(string code, string name, string icon, string examples,
                           decimal maxWeightKg, decimal lengthCm, decimal widthCm, decimal heightCm)
        {
            Code = code;
            Name = name;
            Icon = icon;
            Examples = examples;
            MaxWeightKg = maxWeightKg;
            LengthCm = lengthCm;
            WidthCm = widthCm;
            HeightCm = heightCm;
        }

        /// <summary>Stored value, and what <c>hfSelectedSize</c> carries between postbacks.</summary>
        public string Code { get; private set; }

        public string Name { get; private set; }

        /// <summary>Material Symbols glyph name, matching the icon set the app already loads.</summary>
        public string Icon { get; private set; }

        public string Examples { get; private set; }

        /// <summary>
        /// The band maximum, and the weight the shipment is billed at. Billing deliberately
        /// charges the band rather than an entered weight, so the price on the card is
        /// exactly the price the band costs.
        /// </summary>
        public decimal MaxWeightKg { get; private set; }

        public decimal LengthCm { get; private set; }
        public decimal WidthCm { get; private set; }
        public decimal HeightCm { get; private set; }

        public string MaxWeightText
        {
            get { return "Up to " + MaxWeightKg.ToString("0.##") + " kg"; }
        }

        public string DimensionsText
        {
            get { return Describe(LengthCm, WidthCm, HeightCm); }
        }

        /// <summary>
        /// The cheapest this band can cost, priced through the app's one rate model.
        ///
        /// CalculateQuote charges a lower base rate when origin and destination match, so
        /// quoting the band against a single city yields its true floor. This is computed
        /// rather than stored precisely so the card can never contradict what step 3
        /// charges - the previous hardcoded figures understated Large by R69 and Extra
        /// Large by R209.
        /// </summary>
        public decimal FromPrice
        {
            get { return CourierService.CalculateQuote("Local", "Local", MaxWeightKg); }
        }

        public string FromPriceText
        {
            get { return "R " + FromPrice.ToString("N2"); }
        }

        /// <summary>
        /// Renders a dimension triple for display, e.g. "25 × 15 × 10 cm".
        /// Returns an em dash when any part is missing - shipments booked at the Ops
        /// counter have a weight but no band, so their dimensions are legitimately null.
        /// </summary>
        public static string Describe(object lengthCm, object widthCm, object heightCm)
        {
            decimal l = Data.Db.ToDecimal(lengthCm);
            decimal w = Data.Db.ToDecimal(widthCm);
            decimal h = Data.Db.ToDecimal(heightCm);

            if (l <= 0m || w <= 0m || h <= 0m) return "—";

            return l.ToString("0.##") + " × " + w.ToString("0.##") + " × " + h.ToString("0.##") + " cm";
        }

        /// <summary>The bands offered, in the order they are shown.</summary>
        public static readonly ParcelSize[] All =
        {
            new ParcelSize("Small", "Small", "markunread_mailbox",
                "e.g. Documents, books, T-shirts, small electronics", 2m, 25m, 15m, 10m),

            new ParcelSize("Medium", "Medium", "package_2",
                "e.g. Shoe box, laptop, winter jacket, small appliances", 5m, 35m, 25m, 15m),

            new ParcelSize("Large", "Large", "inventory_2",
                "e.g. Microwave, heavy boots, multi-item orders", 15m, 50m, 40m, 30m),

            new ParcelSize("XLarge", "Extra Large", "package",
                "e.g. Luggage bag, large TV box, bulky equipment", 30m, 70m, 50m, 40m)
        };

        /// <summary>The band selected when the customer has not chosen one.</summary>
        public static ParcelSize Default
        {
            get { return All[0]; }
        }

        /// <summary>
        /// Looks a band up by code, falling back to the default rather than returning null.
        /// The code arrives from a hidden field, so it must never be trusted to match.
        /// </summary>
        public static ParcelSize Find(string code)
        {
            if (!string.IsNullOrEmpty(code))
            {
                foreach (ParcelSize size in All)
                {
                    if (string.Equals(size.Code, code, StringComparison.OrdinalIgnoreCase))
                    {
                        return size;
                    }
                }
            }
            return Default;
        }
    }
}
