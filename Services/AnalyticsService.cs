using System;
using System.Collections.Generic;
using System.Data;
using ForteLogisticsApp.Data;

namespace ForteLogisticsApp.Services
{
    public enum ScopeKind
    {
        /// <summary>Never valid. Zero on purpose, so a default/uninitialised value grants nothing.</summary>
        None = 0,
        /// <summary>Every shipment and invoice - Admin / operations staff only.</summary>
        Company = 1,
        /// <summary>Only shipments the customer sent and invoices billed to them.</summary>
        Customer = 2,
        /// <summary>Only shipments and delivery attempts that belong to the driver. No revenue.</summary>
        Driver = 3
    }

    /// <summary>
    /// Whose data an analytics query may see. Every AnalyticsService method requires one, and
    /// the WHERE clause is derived from it inside the SQL - a page cannot "forget" to filter.
    /// The page still chooses the scope from the signed-in identity after RequireRole, never
    /// from a request value. Company-only figures (headcounts, the driver league table) refuse
    /// any other scope outright.
    /// </summary>
    public sealed class AnalyticsScope
    {
        public ScopeKind Kind { get; private set; }
        public int UserId { get; private set; }

        private AnalyticsScope(ScopeKind kind, int userId)
        {
            Kind = kind;
            UserId = userId;
        }

        public static AnalyticsScope Company()
        {
            return new AnalyticsScope(ScopeKind.Company, 0);
        }

        public static AnalyticsScope ForCustomer(int customerUserId)
        {
            if (customerUserId <= 0) throw new ArgumentException("A signed-in customer is required.", "customerUserId");
            return new AnalyticsScope(ScopeKind.Customer, customerUserId);
        }

        public static AnalyticsScope ForDriver(int driverUserId)
        {
            if (driverUserId <= 0) throw new ArgumentException("A signed-in driver is required.", "driverUserId");
            return new AnalyticsScope(ScopeKind.Driver, driverUserId);
        }
    }

    /// <summary>Totals for one period, every figure computed under the shared metric definitions.</summary>
    public sealed class PeriodTotals
    {
        public decimal Booked;
        public decimal Delivered;
        /// <summary>Unsuccessful delivery attempts (see the definitions on AnalyticsService).</summary>
        public decimal Failed;
        public decimal Invoiced;
        public decimal Collected;
        public decimal NewCustomers;
        public decimal NewDrivers;
        public decimal ActiveDrivers;

        /// <summary>Mean hours from booking to delivery for parcels delivered in the period; null when none were.</summary>
        public decimal? AvgDeliveryHours;

        /// <summary>Invoiced / Booked; null when nothing was booked.</summary>
        public decimal? RevenuePerShipment
        {
            get { return Booked == 0m ? (decimal?)null : Math.Round(Invoiced / Booked, 2, MidpointRounding.AwayFromZero); }
        }

        /// <summary>Delivered / (Delivered + Failed attempts); null when neither happened in the period.</summary>
        public decimal? SuccessRate
        {
            get { return Delta.Rate(Delivered, Delivered + Failed); }
        }

        /// <summary>Collected / Invoiced for invoices issued in the period; null when nothing was invoiced.</summary>
        public decimal? CollectionRate
        {
            get { return Delta.Rate(Collected, Invoiced); }
        }
    }

    public sealed class NamedValue
    {
        public string Name;
        public decimal Value;

        public NamedValue(string name, decimal value)
        {
            Name = name;
            Value = value;
        }
    }

    /// <summary>One box in the delivery-pipeline schematic.</summary>
    public sealed class PipelineStage
    {
        public string Label;
        public int Value;
        /// <summary>Share of the previous stage that reached this one; null for the first stage or an empty previous stage.</summary>
        public decimal? ConversionFromPrevious;
        public bool IsBiggestDrop;
    }

    public sealed class DriverStanding
    {
        public int DriverId;
        public string Name;
        public int Delivered;
        public int Failed;
        public decimal? AverageRating;
        /// <summary>Ratings received in the period (an average from one or two ratings means little).</summary>
        public int RatingCount;

        public decimal? SuccessRate
        {
            get { return Delta.Rate(Delivered, Delivered + Failed); }
        }
    }

    /// <summary>What happened to the shipments BOOKED in a period (a cohort, not an event count).</summary>
    public sealed class CohortSummary
    {
        public int Total;
        public int Delivered;
        public int Failed;
        public int InProgress;
        public int Lost;
        public int Damaged;
        public IDictionary<string, int> ByStatus = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    }

    public sealed class CompanyCounts
    {
        public int Users;
        public int Customers;
        public int Staff;
        public int Drivers;
        public int DriversAvailable;
        public int DriversOnRoute;
    }

    /// <summary>
    /// Unpaid money billed in a period, split by whether it is already past its due date. An
    /// invoice's due date is Payment.DueDate, or IssuedDate + DefaultPaymentTermsDays when the
    /// invoice was raised without one (online bookings leave it empty).
    /// </summary>
    public sealed class Receivables
    {
        public decimal Outstanding;
        public decimal Overdue;
        public int OverdueInvoices;
        public int OldestOverdueDays;

        public decimal NotYetDue
        {
            get { return Outstanding - Overdue; }
        }
    }

    /// <summary>How the comparison report can split one period into groups.</summary>
    public enum CompareDimension
    {
        /// <summary>Shipment.SortCategory - the service area / sort lane.</summary>
        ServiceArea,
        /// <summary>Client.ClientType of the sender - Business vs Individual.</summary>
        ClientType,
        /// <summary>The driver (attempts are credited to the driver who made them).</summary>
        Driver
    }

    /// <summary>One group's figures for a period (company view only).</summary>
    public sealed class GroupFigures
    {
        /// <summary>Stable identity of the group (a driver's ID, otherwise the name), for matching periods.</summary>
        public string Key;
        public string Name;
        /// <summary>True for catch-all groups such as "Unassigned" or "Unknown", which are never named as findings.</summary>
        public bool Placeholder;
        /// <summary>Shipments booked by the same group in the previous period of equal length, when attached.</summary>
        public int? PreviousBooked;
        public int Booked;
        public int Delivered;
        public int Failed;
        /// <summary>Money invoiced for the group; null where the split does not apply (drivers).</summary>
        public decimal? Invoiced;

        public decimal? SuccessRate
        {
            get { return Delta.Rate(Delivered, Delivered + Failed); }
        }
    }

    /// <summary>Parcels sitting at one delivery stage for longer than the "stuck" threshold.</summary>
    public sealed class StuckStage
    {
        public string Status;
        public int Count;
        public int OldestDays;
    }

    /// <summary>Everything one report or dashboard needs for a period, plus the previous period for comparison.</summary>
    public sealed class PerformanceSnapshot
    {
        public ReportPeriod Period;
        public ReportPeriod PreviousPeriod;
        public string[] Labels;

        public decimal[] Booked, Delivered, Failed, Invoiced, Collected, NewCustomers;
        /// <summary>
        /// Previous period's series, aligned by bucket POSITION for a dashed overlay line. The two
        /// windows can hold a different number of week/month buckets, so the overlay is an
        /// approximate shape guide; the exact previous figures are in <see cref="Previous"/>.
        /// </summary>
        public decimal[] PrevBooked, PrevDelivered, PrevInvoiced;

        public PeriodTotals Current;
        public PeriodTotals Previous;
        public CohortSummary Cohort;
        public IList<PipelineStage> Pipeline;
        public IList<NamedValue> TopDestinations;
    }

    /// <summary>
    /// Pre-aggregated, parameterised reporting queries - the one place every report and
    /// dashboard metric is defined, so two screens can never disagree about a number.
    ///
    /// Metric definitions (events are counted WHEN THEY HAPPEN):
    ///   Booked     - a shipment counted on its ShipmentDate.
    ///   Delivered  - DeliveryStatus 'Delivered', counted on DeliveryDate (a successful delivery).
    ///   Failed     - every UNSUCCESSFUL DELIVERY ATTEMPT, from the TrackingHistory event log:
    ///                AutomationService writes a 'Failed' (final) or 'Requeued' (will retry) row
    ///                with the attempting driver for each one. A retried parcel goes back to
    ///                'Sorted' and loses its driver, so the Shipment row alone would forget the
    ///                attempt - the event log does not. Shipments marked 'Failed' with no such
    ///                event (older or directly-entered data) are counted once on their last
    ///                status change, so nothing is lost either way.
    ///   Success    - Delivered / (Delivered + Failed attempts); n/a when neither occurred.
    ///   Invoiced   - Payment.Amount counted on IssuedDate, excluding Cancelled invoices
    ///                (Unpaid, Pending and Failed-payment invoices are still money billed).
    ///   Collected  - the part of the period's invoiced amount that is now Paid. The schema
    ///                records no paid-on date, so this is a cohort figure ("of what we billed in
    ///                this period, how much has come in"), not cash received in the period.
    ///   New users  - User.CreatedDate in the period (company scope only).
    ///   Active drivers - distinct drivers with a delivery or a failed attempt in the period.
    /// A shipment booked in March and delivered in April is booked in March, delivered in April.
    /// Drivers never see money: the driver scope returns R0 for every revenue figure.
    ///
    /// All SQL is fixed text. The scope predicates are constant fragments driven by parameters
    /// (@sk, @su), never by concatenated values; OPTION (RECOMPILE) stops a plan compiled for
    /// one scope (e.g. the whole company) being reused for another (one customer).
    /// </summary>
    public static class AnalyticsService
    {
        private const string ShipScope =
            "(@sk = 1 OR (@sk = 2 AND s.SenderID = @su) OR (@sk = 3 AND s.DriverID = @su))";

        // A delivery attempt belongs to the driver who MADE it (th.DriverID), not to whoever
        // holds the parcel now - after a requeue Shipment.DriverID is cleared.
        private const string AttemptScope =
            "(@sk = 1 OR (@sk = 2 AND s.SenderID = @su) OR (@sk = 3 AND th.DriverID = @su))";

        // Customers see their own invoices; drivers see no revenue at all.
        private const string PayScope =
            "(@sk = 1 OR (@sk = 2 AND p.ClientID = @su))";

        private const string AttemptEvent = "th.Status IN ('Failed', 'Requeued')";

        // A 'Failed' shipment with no attempt event, dated on its last status change. Written
        // as two ranges rather than ISNULL(...) >= @f so an index on either column can be used.
        private const string LegacyFailed =
            "s.DeliveryStatus = 'Failed' AND " +
            "((s.LastStatusChange >= @f AND s.LastStatusChange < @t) OR " +
            " (s.LastStatusChange IS NULL AND s.ShipmentDate >= @f AND s.ShipmentDate < @t)) AND " +
            "NOT EXISTS (SELECT 1 FROM dbo.[TrackingHistory] h WHERE h.ShipmentID = s.ShipmentID AND h.Status IN ('Failed', 'Requeued'))";

        private const string Recompile = " OPTION (RECOMPILE)";

        private const string BookedSql =
            "SELECT CAST(s.ShipmentDate AS DATE) AS D, COUNT(*) AS V FROM dbo.[Shipment] s " +
            "WHERE s.ShipmentDate >= @f AND s.ShipmentDate < @t AND " + ShipScope +
            " GROUP BY CAST(s.ShipmentDate AS DATE)" + Recompile;

        private const string DeliveredSql =
            "SELECT CAST(s.DeliveryDate AS DATE) AS D, COUNT(*) AS V FROM dbo.[Shipment] s " +
            "WHERE s.DeliveryStatus = 'Delivered' AND s.DeliveryDate >= @f AND s.DeliveryDate < @t AND " + ShipScope +
            " GROUP BY CAST(s.DeliveryDate AS DATE)" + Recompile;

        private const string FailedSql =
            "SELECT CAST(x.D AS DATE) AS D, COUNT(*) AS V FROM (" +
            "  SELECT th.[Timestamp] AS D FROM dbo.[TrackingHistory] th " +
            "  JOIN dbo.[Shipment] s ON s.ShipmentID = th.ShipmentID " +
            "  WHERE " + AttemptEvent + " AND th.[Timestamp] >= @f AND th.[Timestamp] < @t AND " + AttemptScope +
            "  UNION ALL " +
            "  SELECT ISNULL(s.LastStatusChange, s.ShipmentDate) FROM dbo.[Shipment] s " +
            "  WHERE " + LegacyFailed + " AND " + ShipScope +
            ") x GROUP BY CAST(x.D AS DATE)" + Recompile;

        private const string InvoicedSql =
            "SELECT CAST(p.IssuedDate AS DATE) AS D, SUM(p.Amount) AS V FROM dbo.[Payment] p " +
            "WHERE p.PaymentStatus <> 'Cancelled' AND p.IssuedDate >= @f AND p.IssuedDate < @t AND " + PayScope +
            " GROUP BY CAST(p.IssuedDate AS DATE)" + Recompile;

        private const string CollectedSql =
            "SELECT CAST(p.IssuedDate AS DATE) AS D, SUM(p.Amount) AS V FROM dbo.[Payment] p " +
            "WHERE p.PaymentStatus = 'Paid' AND p.IssuedDate >= @f AND p.IssuedDate < @t AND " + PayScope +
            " GROUP BY CAST(p.IssuedDate AS DATE)" + Recompile;

        // New users are a company-level fact: the @sk = 1 guard returns nothing for a scoped view.
        private const string NewCustomersSql =
            "SELECT CAST(u.CreatedDate AS DATE) AS D, COUNT(*) AS V FROM dbo.[User] u " +
            "WHERE @sk = 1 AND u.Role = 'Customer' AND u.CreatedDate >= @f AND u.CreatedDate < @t " +
            "GROUP BY CAST(u.CreatedDate AS DATE)";

        private const string NewDriversSql =
            "SELECT CAST(u.CreatedDate AS DATE) AS D, COUNT(*) AS V FROM dbo.[User] u " +
            "JOIN dbo.[Driver] d ON d.UserID = u.UserID " +
            "WHERE @sk = 1 AND u.CreatedDate >= @f AND u.CreatedDate < @t " +
            "GROUP BY CAST(u.CreatedDate AS DATE)";

        private const string ActiveDriversSql =
            "SELECT COUNT(DISTINCT x.DriverID) FROM (" +
            "  SELECT s.DriverID FROM dbo.[Shipment] s " +
            "  WHERE s.DriverID IS NOT NULL AND s.DeliveryStatus = 'Delivered' " +
            "    AND s.DeliveryDate >= @f AND s.DeliveryDate < @t AND " + ShipScope +
            "  UNION ALL " +
            "  SELECT th.DriverID FROM dbo.[TrackingHistory] th JOIN dbo.[Shipment] s ON s.ShipmentID = th.ShipmentID " +
            "  WHERE th.DriverID IS NOT NULL AND " + AttemptEvent +
            "    AND th.[Timestamp] >= @f AND th.[Timestamp] < @t AND " + AttemptScope +
            "  UNION ALL " +
            "  SELECT s.DriverID FROM dbo.[Shipment] s WHERE s.DriverID IS NOT NULL AND " + LegacyFailed + " AND " + ShipScope +
            ") x" + Recompile;

        /// <summary>Payment terms assumed for an invoice raised without a due date (the demo data uses 7 days).</summary>
        public const int DefaultPaymentTermsDays = 7;

        // Keep the literal 7 in step with DefaultPaymentTermsDays (SQL text must stay constant).
        private const string DueOn = "ISNULL(p.DueDate, DATEADD(day, 7, p.IssuedDate))";

        private const string AvgDeliverySql =
            "SELECT AVG(CAST(DATEDIFF(MINUTE, s.ShipmentDate, s.DeliveryDate) AS DECIMAL(18,2))) / 60.0 " +
            "FROM dbo.[Shipment] s " +
            "WHERE s.DeliveryStatus = 'Delivered' AND s.DeliveryDate >= @f AND s.DeliveryDate < @t " +
            "AND s.DeliveryDate >= s.ShipmentDate AND " + ShipScope + Recompile;

        private const string ReceivablesSql =
            "SELECT ISNULL(SUM(p.Amount), 0) AS Outstanding, " +
            "ISNULL(SUM(CASE WHEN " + DueOn + " < @asOf THEN p.Amount ELSE 0 END), 0) AS Overdue, " +
            "ISNULL(SUM(CASE WHEN " + DueOn + " < @asOf THEN 1 ELSE 0 END), 0) AS OverdueCount, " +
            "ISNULL(MAX(CASE WHEN " + DueOn + " < @asOf THEN DATEDIFF(day, " + DueOn + ", @asOf) END), 0) AS Oldest " +
            "FROM dbo.[Payment] p " +
            "WHERE p.PaymentStatus NOT IN ('Paid', 'Cancelled') AND p.IssuedDate >= @f AND p.IssuedDate < @t AND " + PayScope + Recompile;

        private const string StuckSql =
            "SELECT s.DeliveryStatus AS St, COUNT(*) AS N, " +
            "MAX(DATEDIFF(day, ISNULL(s.LastStatusChange, s.ShipmentDate), @asOf)) AS Oldest " +
            "FROM dbo.[Shipment] s " +
            "WHERE s.DeliveryStatus NOT IN ('Delivered', 'Failed') " +
            "AND ISNULL(s.LastStatusChange, s.ShipmentDate) < @cut AND " + ShipScope +
            " GROUP BY s.DeliveryStatus ORDER BY N DESC" + Recompile;

        private const string TopCustomersSql =
            "SELECT TOP (@n) ISNULL(u.FullName, 'Unknown customer') AS Name, SUM(p.Amount) AS V " +
            "FROM dbo.[Payment] p LEFT JOIN dbo.[User] u ON u.UserID = p.ClientID " +
            "WHERE p.PaymentStatus <> 'Cancelled' AND p.IssuedDate >= @f AND p.IssuedDate < @t " +
            "GROUP BY p.ClientID, u.FullName ORDER BY V DESC, Name";

        // ---- Group comparison: one constant key expression (and join) per dimension ----
        private const string LaneKey = "ISNULL(NULLIF(LTRIM(RTRIM(s.SortCategory)), ''), 'Not yet sorted')";
        private const string ClientKey = "ISNULL(c.ClientType, 'Unknown')";
        private const string ClientJoin = " LEFT JOIN dbo.[Client] c ON c.UserID = s.SenderID";
        // Drivers are keyed "<UserID><US><FullName>" so two drivers with the same name stay apart.
        private const string DriverKey =
            "CASE WHEN du.UserID IS NULL THEN 'Unassigned' ELSE CAST(du.UserID AS varchar(12)) + CHAR(31) + ISNULL(du.FullName, '') END";
        private const string DriverJoin = " LEFT JOIN dbo.[User] du ON du.UserID = s.DriverID";
        private const string AttemptDriverKey =
            "CASE WHEN au.UserID IS NULL THEN 'Unknown driver' ELSE CAST(au.UserID AS varchar(12)) + CHAR(31) + ISNULL(au.FullName, '') END";
        private const char KeySeparator = (char)31;

        /// <summary>The catch-all group names the queries above produce.</summary>
        private static readonly HashSet<string> PlaceholderGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Not yet sorted", "Unknown", "Unassigned", "Unknown driver", "No shipment"
        };
        private const string AttemptDriverJoin = " LEFT JOIN dbo.[User] au ON au.UserID = th.DriverID";

        private static readonly string LaneGroupSql = GroupSql(LaneKey, string.Empty, LaneKey, string.Empty);
        private static readonly string ClientGroupSql = GroupSql(ClientKey, ClientJoin, ClientKey, ClientJoin);
        private static readonly string DriverGroupSql = GroupSql(DriverKey, DriverJoin, AttemptDriverKey, AttemptDriverJoin);

        private const string LaneInvoicedSql =
            "SELECT CASE WHEN s.ShipmentID IS NULL THEN 'No shipment' ELSE " + LaneKey + " END AS Name, SUM(p.Amount) AS V " +
            "FROM dbo.[Payment] p LEFT JOIN dbo.[Shipment] s ON s.ShipmentID = p.ShipmentID " +
            "WHERE p.PaymentStatus <> 'Cancelled' AND p.IssuedDate >= @f AND p.IssuedDate < @t " +
            "GROUP BY CASE WHEN s.ShipmentID IS NULL THEN 'No shipment' ELSE " + LaneKey + " END";

        private const string ClientInvoicedSql =
            "SELECT ISNULL(c.ClientType, 'Unknown') AS Name, SUM(p.Amount) AS V " +
            "FROM dbo.[Payment] p LEFT JOIN dbo.[Client] c ON c.UserID = p.ClientID " +
            "WHERE p.PaymentStatus <> 'Cancelled' AND p.IssuedDate >= @f AND p.IssuedDate < @t " +
            "GROUP BY ISNULL(c.ClientType, 'Unknown')";

        /// <summary>
        /// Builds the per-group booked / delivered / failed-attempt query from constant fragments
        /// only (called once, at type initialisation, with the constants above - never with
        /// request data). Same metric definitions as the period totals.
        /// </summary>
        private static string GroupSql(string key, string joins, string attemptKey, string attemptJoins)
        {
            return
                "SELECT x.K AS Name, SUM(x.B) AS Booked, SUM(x.D) AS Delivered, SUM(x.F) AS Failed FROM (" +
                "  SELECT " + key + " AS K, 1 AS B, 0 AS D, 0 AS F FROM dbo.[Shipment] s" + joins +
                "  WHERE s.ShipmentDate >= @f AND s.ShipmentDate < @t" +
                "  UNION ALL" +
                "  SELECT " + key + ", 0, 1, 0 FROM dbo.[Shipment] s" + joins +
                "  WHERE s.DeliveryStatus = 'Delivered' AND s.DeliveryDate >= @f AND s.DeliveryDate < @t" +
                "  UNION ALL" +
                "  SELECT " + attemptKey + ", 0, 0, 1 FROM dbo.[TrackingHistory] th" +
                "  JOIN dbo.[Shipment] s ON s.ShipmentID = th.ShipmentID" + attemptJoins +
                "  WHERE " + AttemptEvent + " AND th.[Timestamp] >= @f AND th.[Timestamp] < @t" +
                "  UNION ALL" +
                "  SELECT " + key + ", 0, 0, 1 FROM dbo.[Shipment] s" + joins + " WHERE " + LegacyFailed +
                ") x GROUP BY x.K";
        }

        private const string CohortSql =
            "SELECT s.DeliveryStatus AS St, COUNT(*) AS N, " +
            "SUM(CASE WHEN s.ExceptionType = 'Lost' THEN 1 ELSE 0 END) AS Lost, " +
            "SUM(CASE WHEN s.ExceptionType = 'Damaged' THEN 1 ELSE 0 END) AS Damaged " +
            "FROM dbo.[Shipment] s WHERE s.ShipmentDate >= @f AND s.ShipmentDate < @t AND " + ShipScope +
            " GROUP BY s.DeliveryStatus" + Recompile;

        private const string TopDestinationsSql =
            "SELECT TOP (@n) ISNULL(NULLIF(LTRIM(RTRIM(s.DestCity)), ''), 'Unknown') AS Name, COUNT(*) AS V " +
            "FROM dbo.[Shipment] s WHERE s.ShipmentDate >= @f AND s.ShipmentDate < @t AND " + ShipScope +
            " GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(s.DestCity)), ''), 'Unknown') ORDER BY V DESC, Name" + Recompile;

        private const string DriverStandingsSql =
            "SELECT d.UserID, u.FullName, ISNULL(dl.N, 0) AS Delivered, ISNULL(fa.N, 0) AS Failed, rt.AvgRating, ISNULL(rt.N, 0) AS RatingCount " +
            "FROM dbo.[Driver] d JOIN dbo.[User] u ON u.UserID = d.UserID " +
            "LEFT JOIN (SELECT s.DriverID, COUNT(*) AS N FROM dbo.[Shipment] s " +
            "           WHERE s.DeliveryStatus = 'Delivered' AND s.DeliveryDate >= @f AND s.DeliveryDate < @t " +
            "           GROUP BY s.DriverID) dl ON dl.DriverID = d.UserID " +
            "LEFT JOIN (SELECT x.DriverID, COUNT(*) AS N FROM (" +
            "             SELECT th.DriverID FROM dbo.[TrackingHistory] th " +
            "             WHERE " + AttemptEvent + " AND th.[Timestamp] >= @f AND th.[Timestamp] < @t " +
            "             UNION ALL " +
            "             SELECT s.DriverID FROM dbo.[Shipment] s WHERE " + LegacyFailed +
            "           ) x GROUP BY x.DriverID) fa ON fa.DriverID = d.UserID " +
            "LEFT JOIN (SELECT r.DriverID, AVG(CAST(r.RatingScore AS DECIMAL(5,2))) AS AvgRating, COUNT(*) AS N FROM dbo.[Rating] r " +
            "           WHERE r.CreatedDate >= @f AND r.CreatedDate < @t GROUP BY r.DriverID) rt ON rt.DriverID = d.UserID";

        /// <summary>
        /// The full picture for <paramref name="period"/>: bucketed series, totals for this and
        /// the previous period, the booked cohort, the pipeline and the top destinations.
        /// The daily queries run once over [previous.From, period.To] and are split in memory.
        /// </summary>
        public static PerformanceSnapshot Build(AnalyticsScope scope, ReportPeriod period)
        {
            RequireScope(scope);
            RequireValid(period);

            ReportPeriod prev = period.Previous();
            DateTime from = prev.From, toEx = period.ToExclusive;

            Dictionary<DateTime, decimal> booked = Daily(BookedSql, scope, from, toEx);
            Dictionary<DateTime, decimal> delivered = Daily(DeliveredSql, scope, from, toEx);
            Dictionary<DateTime, decimal> failed = Daily(FailedSql, scope, from, toEx);
            Dictionary<DateTime, decimal> invoiced = Daily(InvoicedSql, scope, from, toEx);
            Dictionary<DateTime, decimal> collected = Daily(CollectedSql, scope, from, toEx);
            Dictionary<DateTime, decimal> newCustomers = Daily(NewCustomersSql, scope, from, toEx);
            Dictionary<DateTime, decimal> newDrivers = Daily(NewDriversSql, scope, from, toEx);

            var snap = new PerformanceSnapshot
            {
                Period = period,
                PreviousPeriod = prev,
                Labels = period.Labels(),
                Booked = period.Rollup(booked),
                Delivered = period.Rollup(delivered),
                Failed = period.Rollup(failed),
                Invoiced = period.Rollup(invoiced),
                Collected = period.Rollup(collected),
                NewCustomers = period.Rollup(newCustomers)
            };

            int n = snap.Labels.Length;
            snap.PrevBooked = Align(prev.Rollup(booked), n);
            snap.PrevDelivered = Align(prev.Rollup(delivered), n);
            snap.PrevInvoiced = Align(prev.Rollup(invoiced), n);

            snap.Current = Totals(period, booked, delivered, failed, invoiced, collected, newCustomers, newDrivers,
                ActiveDrivers(scope, period.From, period.ToExclusive), AvgDeliveryHours(scope, period.From, period.ToExclusive));
            snap.Previous = Totals(prev, booked, delivered, failed, invoiced, collected, newCustomers, newDrivers,
                ActiveDrivers(scope, prev.From, prev.ToExclusive), AvgDeliveryHours(scope, prev.From, prev.ToExclusive));

            snap.Cohort = Cohort(scope, period.From, period.ToExclusive);
            snap.Pipeline = BuildPipeline(snap.Cohort.ByStatus);
            snap.TopDestinations = TopDestinations(scope, period.From, period.ToExclusive, 6);
            return snap;
        }

        /// <summary>Just the totals for one period - used by the comparison report for each side.</summary>
        public static PeriodTotals TotalsFor(AnalyticsScope scope, ReportPeriod period)
        {
            RequireScope(scope);
            RequireValid(period);

            DateTime f = period.From, t = period.ToExclusive;
            return Totals(period,
                Daily(BookedSql, scope, f, t), Daily(DeliveredSql, scope, f, t), Daily(FailedSql, scope, f, t),
                Daily(InvoicedSql, scope, f, t), Daily(CollectedSql, scope, f, t),
                Daily(NewCustomersSql, scope, f, t), Daily(NewDriversSql, scope, f, t),
                ActiveDrivers(scope, f, t), AvgDeliveryHours(scope, f, t));
        }

        public static CohortSummary Cohort(AnalyticsScope scope, DateTime from, DateTime toExclusive)
        {
            RequireScope(scope);
            DataTable dt = Db.Query(CohortSql, Params(scope, from, toExclusive));
            var c = new CohortSummary();
            foreach (DataRow r in dt.Rows)
            {
                string status = Db.ToStr(r["St"]);
                int count = Db.ToInt(r["N"]);
                c.ByStatus[status] = count;
                c.Total += count;
                c.Lost += Db.ToInt(r["Lost"]);
                c.Damaged += Db.ToInt(r["Damaged"]);
                if (string.Equals(status, "Delivered", StringComparison.OrdinalIgnoreCase)) c.Delivered += count;
                else if (string.Equals(status, "Failed", StringComparison.OrdinalIgnoreCase)) c.Failed += count;
            }
            c.InProgress = c.Total - c.Delivered - c.Failed;
            return c;
        }

        public static IList<NamedValue> TopDestinations(AnalyticsScope scope, DateTime from, DateTime toExclusive, int top)
        {
            RequireScope(scope);
            object[] p = Params(scope, from, toExclusive);
            var withTop = new object[p.Length + 2];
            p.CopyTo(withTop, 0);
            withTop[p.Length] = "@n";
            withTop[p.Length + 1] = Math.Max(1, Math.Min(top, 50));

            var list = new List<NamedValue>();
            foreach (DataRow r in Db.Query(TopDestinationsSql, withTop).Rows)
            {
                list.Add(new NamedValue(Db.ToStr(r["Name"]), Db.ToDecimal(r["V"])));
            }
            return list;
        }

        /// <summary>
        /// Per-driver deliveries, failed attempts and average rating in the period - company view
        /// only. Every registered driver appears, including idle ones; most deliveries first.
        /// </summary>
        public static IList<DriverStanding> DriverStandings(AnalyticsScope scope, DateTime from, DateTime toExclusive)
        {
            RequireCompany(scope);
            DataTable dt = Db.Query(DriverStandingsSql, "@f", from, "@t", toExclusive);

            var list = new List<DriverStanding>();
            foreach (DataRow r in dt.Rows)
            {
                list.Add(new DriverStanding
                {
                    DriverId = Db.ToInt(r["UserID"]),
                    Name = Db.ToStr(r["FullName"]),
                    Delivered = Db.ToInt(r["Delivered"]),
                    Failed = Db.ToInt(r["Failed"]),
                    RatingCount = Db.ToInt(r["RatingCount"]),
                    AverageRating = r["AvgRating"] == DBNull.Value
                        ? (decimal?)null
                        : Math.Round(Db.ToDecimal(r["AvgRating"]), 1, MidpointRounding.AwayFromZero)
                });
            }

            list.Sort((a, b) =>
            {
                int byDelivered = b.Delivered.CompareTo(a.Delivered);
                return byDelivered != 0 ? byDelivered : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
            return list;
        }

        /// <summary>Point-in-time headcounts - company view only.</summary>
        public static CompanyCounts Counts(AnalyticsScope scope)
        {
            RequireCompany(scope);
            DataTable dt = Db.Query(
                @"SELECT (SELECT COUNT(*) FROM dbo.[User]) AS Users,
                         (SELECT COUNT(*) FROM dbo.[User] WHERE Role = 'Customer') AS Customers,
                         (SELECT COUNT(*) FROM dbo.[User] WHERE Role = 'Staff') AS Staff,
                         (SELECT COUNT(*) FROM dbo.[Driver]) AS Drivers,
                         (SELECT COUNT(*) FROM dbo.[Driver] WHERE AvailabilityStatus = 'Available') AS Available,
                         (SELECT COUNT(*) FROM dbo.[Driver] WHERE AvailabilityStatus = 'On Route') AS OnRoute");
            var c = new CompanyCounts();
            if (dt.Rows.Count == 0) return c;
            DataRow r = dt.Rows[0];
            c.Users = Db.ToInt(r["Users"]);
            c.Customers = Db.ToInt(r["Customers"]);
            c.Staff = Db.ToInt(r["Staff"]);
            c.Drivers = Db.ToInt(r["Drivers"]);
            c.DriversAvailable = Db.ToInt(r["Available"]);
            c.DriversOnRoute = Db.ToInt(r["OnRoute"]);
            return c;
        }

        /// <summary>
        /// Unpaid, uncancelled money billed in the period, and how much of it is past due as at
        /// <paramref name="asOf"/>. Customers see their own; drivers see none.
        /// </summary>
        public static Receivables ReceivablesFor(AnalyticsScope scope, DateTime from, DateTime toExclusive, DateTime asOf)
        {
            RequireScope(scope);
            DataTable dt = Db.Query(ReceivablesSql, With(Params(scope, from, toExclusive), "@asOf", asOf));
            var r = new Receivables();
            if (dt.Rows.Count == 0) return r;
            DataRow row = dt.Rows[0];
            r.Outstanding = Db.ToDecimal(row["Outstanding"]);
            r.Overdue = Db.ToDecimal(row["Overdue"]);
            r.OverdueInvoices = Db.ToInt(row["OverdueCount"]);
            r.OldestOverdueDays = Db.ToInt(row["Oldest"]);
            return r;
        }

        /// <summary>
        /// Parcels that are neither delivered nor failed and have not changed status for more than
        /// <paramref name="olderThanDays"/> days as at <paramref name="asOf"/>, grouped by the stage
        /// they are stuck at (busiest first). A point-in-time check, not tied to a period.
        /// </summary>
        public static IList<StuckStage> StuckParcels(AnalyticsScope scope, DateTime asOf, int olderThanDays)
        {
            RequireScope(scope);
            DataTable dt = Db.Query(StuckSql,
                "@asOf", asOf, "@cut", asOf.AddDays(-Math.Max(0, olderThanDays)),
                "@sk", (int)scope.Kind, "@su", scope.UserId);

            var list = new List<StuckStage>();
            foreach (DataRow r in dt.Rows)
            {
                list.Add(new StuckStage
                {
                    Status = Db.ToStr(r["St"]),
                    Count = Db.ToInt(r["N"]),
                    OldestDays = Db.ToInt(r["Oldest"])
                });
            }
            return list;
        }

        /// <summary>
        /// One period split by <paramref name="by"/> (company view only): booked, delivered and
        /// failed attempts per group, plus money invoiced where that split applies. Largest
        /// groups first.
        /// </summary>
        public static IList<GroupFigures> BreakdownBy(AnalyticsScope scope, CompareDimension by, DateTime from, DateTime toExclusive)
        {
            RequireCompany(scope);
            string sql = by == CompareDimension.ClientType ? ClientGroupSql
                       : by == CompareDimension.Driver ? DriverGroupSql : LaneGroupSql;

            var groups = new Dictionary<string, GroupFigures>(StringComparer.OrdinalIgnoreCase);
            var nameCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow r in Db.Query(sql, "@f", from, "@t", toExclusive).Rows)
            {
                string key = Db.ToStr(r["Name"]);
                int cut = key.IndexOf(KeySeparator);
                string name = cut < 0 ? key : key.Substring(cut + 1);
                if (name.Length == 0) name = "Driver #" + key.Substring(0, cut);
                groups[key] = new GroupFigures
                {
                    Key = key,
                    Name = name,
                    Placeholder = cut < 0 && PlaceholderGroups.Contains(name),
                    Booked = Db.ToInt(r["Booked"]),
                    Delivered = Db.ToInt(r["Delivered"]),
                    Failed = Db.ToInt(r["Failed"])
                };
                int seen;
                nameCount.TryGetValue(name, out seen);
                nameCount[name] = seen + 1;
            }
            // Two drivers who share a name are told apart by their ID.
            foreach (GroupFigures g in groups.Values)
            {
                int cut = g.Key.IndexOf(KeySeparator);
                if (cut > 0 && nameCount[g.Name] > 1) g.Name += " (driver #" + g.Key.Substring(0, cut) + ")";
            }

            if (by != CompareDimension.Driver)
            {
                string moneySql = by == CompareDimension.ClientType ? ClientInvoicedSql : LaneInvoicedSql;
                foreach (DataRow r in Db.Query(moneySql, "@f", from, "@t", toExclusive).Rows)
                {
                    string name = Db.ToStr(r["Name"]);
                    GroupFigures g;
                    if (!groups.TryGetValue(name, out g))
                    {
                        g = new GroupFigures { Key = name, Name = name, Placeholder = PlaceholderGroups.Contains(name) };
                        groups[name] = g;
                    }
                    g.Invoiced = Db.ToDecimal(r["V"]);
                }
                foreach (GroupFigures g in groups.Values) if (g.Invoiced == null) g.Invoiced = 0m;
            }

            var list = new List<GroupFigures>(groups.Values);
            list.Sort((a, b) =>
            {
                int byBooked = b.Booked.CompareTo(a.Booked);
                return byBooked != 0 ? byBooked : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
            return list;
        }

        /// <summary>Customers ranked by money invoiced in the period (company view only).</summary>
        public static IList<NamedValue> TopCustomers(AnalyticsScope scope, DateTime from, DateTime toExclusive, int top)
        {
            RequireCompany(scope);
            var list = new List<NamedValue>();
            foreach (DataRow r in Db.Query(TopCustomersSql, "@f", from, "@t", toExclusive,
                         "@n", Math.Max(1, Math.Min(top, 50))).Rows)
            {
                list.Add(new NamedValue(Db.ToStr(r["Name"]), Db.ToDecimal(r["V"])));
            }
            return list;
        }

        /// <summary>
        /// How far the period's booked shipments got. A stage counts every shipment that has
        /// reached it OR gone past it, so the funnel only ever narrows. Parcels that ended Failed
        /// went out for delivery, so they count up to that stage and are reported as a side
        /// branch (CohortSummary.Failed). Pure - unit tested without a database.
        /// </summary>
        public static IList<PipelineStage> BuildPipeline(IDictionary<string, int> byStatus)
        {
            string[] labels = { "Booked", "Sorted", "Assigned / in transit", "Out for delivery", "Delivered" };
            var reached = new int[labels.Length];

            if (byStatus != null)
            {
                foreach (KeyValuePair<string, int> kv in byStatus)
                {
                    int rank = StageRank(kv.Key);
                    for (int i = 0; i <= rank; i++) reached[i] += kv.Value;
                }
            }

            var stages = new List<PipelineStage>();
            int biggestDropIndex = -1;
            int biggestDrop = 0;
            for (int i = 0; i < labels.Length; i++)
            {
                var stage = new PipelineStage { Label = labels[i], Value = reached[i] };
                if (i > 0)
                {
                    stage.ConversionFromPrevious = Delta.Rate(reached[i], reached[i - 1]);
                    int drop = reached[i - 1] - reached[i];
                    if (drop > biggestDrop)
                    {
                        biggestDrop = drop;
                        biggestDropIndex = i;
                    }
                }
                stages.Add(stage);
            }
            if (biggestDropIndex > 0) stages[biggestDropIndex].IsBiggestDrop = true;
            return stages;
        }

        /// <summary>Furthest pipeline stage (0-4) a DeliveryStatus implies. Unknown statuses count as booked only.</summary>
        public static int StageRank(string deliveryStatus)
        {
            switch ((deliveryStatus ?? string.Empty).ToLowerInvariant())
            {
                case "sorted": return 1;
                case "assigned":
                case "intransit": return 2;
                case "outfordelivery":
                case "failed": return 3;
                case "delivered": return 4;
                default: return 0;
            }
        }

        private static void RequireScope(AnalyticsScope scope)
        {
            if (scope == null) throw new ArgumentNullException("scope");
            if (scope.Kind == ScopeKind.None) throw new UnauthorizedAccessException("An analytics scope is required.");
        }

        private static void RequireCompany(AnalyticsScope scope)
        {
            RequireScope(scope);
            if (scope.Kind != ScopeKind.Company)
                throw new UnauthorizedAccessException("This figure is only available in the company-wide view.");
        }

        private static void RequireValid(ReportPeriod period)
        {
            if (period == null) throw new ArgumentNullException("period");
            string problem = ReportPeriod.Validate(period.From, period.To, period.Granularity);
            if (problem != null) throw new ArgumentException(problem, "period");
        }

        private static PeriodTotals Totals(ReportPeriod p,
            Dictionary<DateTime, decimal> booked, Dictionary<DateTime, decimal> delivered,
            Dictionary<DateTime, decimal> failed, Dictionary<DateTime, decimal> invoiced,
            Dictionary<DateTime, decimal> collected, Dictionary<DateTime, decimal> newCustomers,
            Dictionary<DateTime, decimal> newDrivers, int activeDrivers, decimal? avgDeliveryHours)
        {
            return new PeriodTotals
            {
                Booked = p.Total(booked),
                Delivered = p.Total(delivered),
                Failed = p.Total(failed),
                Invoiced = p.Total(invoiced),
                Collected = p.Total(collected),
                NewCustomers = p.Total(newCustomers),
                NewDrivers = p.Total(newDrivers),
                ActiveDrivers = activeDrivers,
                AvgDeliveryHours = avgDeliveryHours
            };
        }

        private static int ActiveDrivers(AnalyticsScope scope, DateTime from, DateTime toExclusive)
        {
            return Db.ToInt(Db.Scalar(ActiveDriversSql, Params(scope, from, toExclusive)));
        }

        private static decimal? AvgDeliveryHours(AnalyticsScope scope, DateTime from, DateTime toExclusive)
        {
            object v = Db.Scalar(AvgDeliverySql, Params(scope, from, toExclusive));
            if (v == null || v == DBNull.Value) return null;
            return Math.Round(Db.ToDecimal(v), 1, MidpointRounding.AwayFromZero);
        }

        private static object[] With(object[] pairs, string name, object value)
        {
            var result = new object[pairs.Length + 2];
            pairs.CopyTo(result, 0);
            result[pairs.Length] = name;
            result[pairs.Length + 1] = value;
            return result;
        }

        private static Dictionary<DateTime, decimal> Daily(string sql, AnalyticsScope scope, DateTime from, DateTime toExclusive)
        {
            var result = new Dictionary<DateTime, decimal>();
            foreach (DataRow r in Db.Query(sql, Params(scope, from, toExclusive)).Rows)
            {
                if (r["D"] == DBNull.Value) continue;
                result[Convert.ToDateTime(r["D"]).Date] = Db.ToDecimal(r["V"]);
            }
            return result;
        }

        private static object[] Params(AnalyticsScope scope, DateTime from, DateTime toExclusive)
        {
            return new object[] { "@f", from, "@t", toExclusive, "@sk", (int)scope.Kind, "@su", scope.UserId };
        }

        /// <summary>Pads or trims a series to <paramref name="length"/> so two periods overlay bucket-for-bucket.</summary>
        private static decimal[] Align(decimal[] series, int length)
        {
            var aligned = new decimal[length];
            Array.Copy(series, aligned, Math.Min(series.Length, length));
            return aligned;
        }
    }
}
