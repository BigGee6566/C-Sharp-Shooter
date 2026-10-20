using System;
using System.Data.SqlClient;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;

namespace ForteLogisticsApp.Services
{
    /// <summary>A shipment about to be created - online booking or counter registration.</summary>
    public sealed class ShipmentDraft
    {
        /// <summary>The customer account sending it; null for a walk-in at the counter.</summary>
        public int? SenderId;
        public string ReceiverName;
        public string ReceiverPhone;
        public string PickupAddress;
        public string DeliveryAddress;
        public string OriginCity;
        public string DestCity;
        public decimal WeightKg;
        public decimal? LengthCm, WidthCm, HeightCm;
        public string Description;
        /// <summary>Server-calculated price; never taken from the form.</summary>
        public decimal Cost;
        public string WarehouseStatus = "Pending";
        public string PaymentGateStatus = "NotRequired";
        /// <summary>The staff member registering it at the counter, if any.</summary>
        public int? RegisteredByStaffId;
        public string TrackingLocation;
        public string TrackingRemark;
        /// <summary>Raise the invoice in the same transaction (online bookings).</summary>
        public bool CreateInvoice;
    }

    public sealed class CreatedShipment
    {
        public int ShipmentId;
        public string TrackingNumber;
        public string InvoiceNumber;
    }

    /// <summary>
    /// A problem with what the user entered. Its message is written for the user and safe to
    /// show; anything else that goes wrong is a different exception and must not be echoed.
    /// </summary>
    public sealed class ValidationFailedException : Exception
    {
        public ValidationFailedException(string message) : base(message) { }
    }

    /// <summary>
    /// The one path that creates shipments. The shipment, its first tracking event and (for an
    /// online booking) its invoice - including the invoice NUMBER - are written in ONE
    /// transaction, so a failure can never leave a parcel without its history, an unbillable
    /// booking, or a gap in the invoice sequence. Inputs are normalised and checked here
    /// (<see cref="Normalize"/> / <see cref="Validate"/>, pure and unit-tested), not only on the
    /// page; anything too long for its column is refused rather than silently cut, so an address
    /// never loses its city. A clash on the random tracking number is retried with a fresh one.
    /// Notifications are the caller's job, after this returns (i.e. after commit).
    /// </summary>
    public static class IntakeService
    {
        // Column widths in App_Data/Schema.sql.
        public const int NameMax = 150, PhoneMax = 30, AddressMax = 250, CityMax = 60, DescriptionMax = 250;
        public const decimal MaxWeightKg = 1000m;

        /// <summary>Tidies every free-text field. Only the system-written remark and location are cut to fit.</summary>
        public static void Normalize(ShipmentDraft d)
        {
            if (d == null) return;
            d.ReceiverName = InputValidator.NormalizeText(d.ReceiverName);
            d.ReceiverPhone = InputValidator.NormalizePhone(d.ReceiverPhone);
            d.PickupAddress = InputValidator.NormalizeText(d.PickupAddress);
            d.DeliveryAddress = InputValidator.NormalizeText(d.DeliveryAddress);
            d.OriginCity = InputValidator.NormalizeText(d.OriginCity);
            d.DestCity = InputValidator.NormalizeText(d.DestCity);
            d.Description = InputValidator.NormalizeText(d.Description);
            d.TrackingLocation = InputValidator.NormalizeText(d.TrackingLocation, 150);
            d.TrackingRemark = InputValidator.NormalizeText(d.TrackingRemark, 250);
            // ParcelWeight is DECIMAL(10,2): judge the value that will actually be stored.
            d.WeightKg = Math.Round(d.WeightKg, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Just the address checks, for a booking step that collects addresses before the
        /// recipient: the first problem in words, or null.
        /// </summary>
        public static string ValidateAddresses(ShipmentDraft d)
        {
            if (d == null) return "Nothing to register.";
            if (d.DeliveryAddress.Length == 0) return "Please enter the delivery address.";
            if (d.DeliveryAddress.Length > AddressMax || d.PickupAddress.Length > AddressMax)
                return "An address is too long (at most " + AddressMax + " characters). Please shorten it.";
            if (d.OriginCity.Length == 0 || d.DestCity.Length == 0) return "Please choose the collection and delivery cities.";
            if (d.OriginCity.Length > CityMax || d.DestCity.Length > CityMax)
                return "A city name is too long (at most " + CityMax + " characters).";
            return null;
        }

        /// <summary>The first problem with a normalised draft, in words for the user; null when it can be created.</summary>
        public static string Validate(ShipmentDraft d)
        {
            if (d == null) return "Nothing to register.";
            if (d.ReceiverName.Length == 0) return "Please enter the recipient's name.";
            if (d.ReceiverName.Length > NameMax) return "The recipient's name is too long (at most " + NameMax + " characters).";
            string address = ValidateAddresses(d);
            if (address != null) return address;
            if (d.Description.Length > DescriptionMax)
                return "The parcel description is too long (at most " + DescriptionMax + " characters).";
            if (d.ReceiverPhone.Length > 0 && !InputValidator.IsValidPhone(d.ReceiverPhone))
                return "Please enter the recipient's phone number with 7 to 15 digits, or leave it blank.";
            if (d.WeightKg <= 0m || d.WeightKg > MaxWeightKg)
                return "Please enter a parcel weight above 0 kg and up to " + MaxWeightKg.ToString("0") + " kg.";
            if (d.Cost <= 0m) return "The price could not be calculated for this parcel.";
            if (d.CreateInvoice && d.SenderId == null) return "An invoice needs a customer account to bill.";
            return null;
        }

        /// <summary>
        /// Normalises, validates and creates the shipment atomically. Throws
        /// <see cref="ValidationFailedException"/> (user-safe message) when the draft is invalid.
        /// </summary>
        public static CreatedShipment Create(ShipmentDraft d)
        {
            Normalize(d);
            string problem = Validate(d);
            if (problem != null) throw new ValidationFailedException(problem);

            for (int attempt = 1; ; attempt++)
            {
                string tracking = CourierService.GenerateTrackingNumber();
                try
                {
                    return CreateOnce(d, tracking);
                }
                catch (SqlException ex) when (attempt < 3 && IsUniqueViolation(ex) && ex.Message.Contains(tracking))
                {
                    // Another booking claimed the same random tracking number between the
                    // availability check and our INSERT; the whole transaction rolled back, so
                    // simply try again with a new number. Any other clash is a real error.
                }
            }
        }

        private static CreatedShipment CreateOnce(ShipmentDraft d, string tracking)
        {
            var created = new CreatedShipment { TrackingNumber = tracking };

            Db.RunInTransaction((conn, tx) =>
            {
                created.ShipmentId = Db.Insert(conn, tx,
                    @"INSERT INTO dbo.[Shipment]
                        (TrackingNumber, Barcode, SenderID, ReceiverName, ReceiverPhone,
                         PickupAddress, DeliveryAddress, OriginCity, DestCity, ParcelWeight,
                         ParcelLengthCm, ParcelWidthCm, ParcelHeightCm,
                         ParcelDescription, Cost, DeliveryStatus, WarehouseStatus, PaymentGateStatus, LastStatusChange)
                      VALUES (@t, @t, @s, @rn, @rp, @pu, @da, @o, @d, @w,
                              @len, @wid, @hgt, @desc, @c, 'Registered', @ws, @pgs, GETDATE())",
                    "@t", created.TrackingNumber,
                    "@s", (object)d.SenderId ?? DBNull.Value,
                    "@rn", d.ReceiverName,
                    "@rp", d.ReceiverPhone,
                    "@pu", d.PickupAddress,
                    "@da", d.DeliveryAddress,
                    "@o", d.OriginCity,
                    "@d", d.DestCity,
                    "@w", d.WeightKg,
                    "@len", (object)d.LengthCm ?? DBNull.Value,
                    "@wid", (object)d.WidthCm ?? DBNull.Value,
                    "@hgt", (object)d.HeightCm ?? DBNull.Value,
                    "@desc", d.Description,
                    "@c", d.Cost,
                    "@ws", d.WarehouseStatus,
                    "@pgs", d.PaymentGateStatus);

                Db.NonQuery(conn, tx,
                    @"INSERT INTO dbo.[TrackingHistory] (ShipmentID, AdminID, Location, Status, Remarks)
                      VALUES (@s, @a, @loc, 'Registered', @rm)",
                    "@s", created.ShipmentId, "@a", (object)d.RegisteredByStaffId ?? DBNull.Value,
                    "@loc", d.TrackingLocation, "@rm", d.TrackingRemark);

                if (d.CreateInvoice)
                {
                    created.InvoiceNumber = CourierService.GenerateInvoiceNumber(conn, tx);
                    Db.NonQuery(conn, tx,
                        @"INSERT INTO dbo.[Payment] (ClientID, ShipmentID, InvoiceNumber, Amount, PaymentStatus)
                          VALUES (@c, @s, @inv, @a, 'Unpaid')",
                        "@c", d.SenderId.Value, "@s", created.ShipmentId,
                        "@inv", created.InvoiceNumber, "@a", d.Cost);
                }
            });
            return created;
        }

        /// <summary>SQL Server's duplicate-key errors (unique constraint 2627, unique index 2601).</summary>
        public static bool IsUniqueViolation(SqlException ex)
        {
            return ex != null && (ex.Number == 2627 || ex.Number == 2601);
        }
    }
}
