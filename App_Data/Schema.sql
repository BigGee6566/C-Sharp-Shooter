/* ============================================================
   Logisticore Courier Service - Database schema + seed
   Target: SQL Server Express  (Data Source = .\SQLEXPRESS)

   HOW TO CREATE THE DATABASE MANUALLY:
     1. Open this file in SQL Server Management Studio (or Visual
        Studio SQL Server Object Explorer) connected to .\SQLEXPRESS.
     2. Execute the whole script (F5). It creates the "Logisticore"
        database, all tables, and seed data (including working logins).

   Table names are singular to match the ERD. Each table lists the
   ERD attributes first, followed by application extension columns.

   Seed login accounts (email / password):
     admin@fcl.co.za      / Admin@123
     manager@fcl.co.za    / Manager@123
     dispatch@fcl.co.za   / Dispatch@123
     warehouse@fcl.co.za  / Warehouse@123
     driver@fcl.co.za     / Driver@123
     finance@fcl.co.za    / Finance@123
     customer@fcl.co.za   / Customer@123
   ============================================================ */

IF DB_ID('Logisticore') IS NULL
    CREATE DATABASE Logisticore;
GO

USE Logisticore;
GO

/* ---------- User (ERD: UserID, FullName, Address, PhoneNumber, IDNumber, Password, Email) ---------- */
IF OBJECT_ID('dbo.[User]', 'U') IS NULL
CREATE TABLE dbo.[User] (
    UserID       INT IDENTITY(1,1) PRIMARY KEY,
    FullName     NVARCHAR(150) NOT NULL,
    Address      NVARCHAR(250) NULL,
    PhoneNumber  NVARCHAR(30)  NULL,
    IDNumber     NVARCHAR(30)  NULL,
    Password     NVARCHAR(200) NOT NULL,
    Email        NVARCHAR(150) NOT NULL UNIQUE,
    -- extensions
    -- Three roles only. "Driver" is NOT a role: a driver is a Staff user who also has a
    -- row in dbo.[Driver], which is what every assignment query joins on.
    Role         NVARCHAR(30)  NOT NULL
        CONSTRAINT CK_User_Role CHECK (Role IN ('Customer', 'Staff', 'Admin')),
    CreatedDate  DATETIME      NOT NULL DEFAULT GETDATE()
);
GO

/* ---------- Admin (ERD: EmployeeID, UserID, Department) ---------- */
IF OBJECT_ID('dbo.[Admin]', 'U') IS NULL
CREATE TABLE dbo.[Admin] (
    EmployeeID  INT IDENTITY(1,1) PRIMARY KEY,
    UserID      INT NOT NULL REFERENCES dbo.[User](UserID),
    Department  NVARCHAR(100) NULL
);
GO

/* ---------- Client (ERD: UserID (PK/FK), ClientType) ---------- */
IF OBJECT_ID('dbo.[Client]', 'U') IS NULL
CREATE TABLE dbo.[Client] (
    UserID      INT PRIMARY KEY REFERENCES dbo.[User](UserID),
    ClientType  NVARCHAR(30) NOT NULL DEFAULT 'Individual'   -- Individual, Business
);
GO

/* ---------- Driver (ERD: LicenseNumber, UserID (PK/FK), EmployeeNumber, AvailabilityStatus) ---------- */
IF OBJECT_ID('dbo.[Driver]', 'U') IS NULL
CREATE TABLE dbo.[Driver] (
    LicenseNumber      NVARCHAR(50) NULL,
    UserID             INT PRIMARY KEY REFERENCES dbo.[User](UserID),
    EmployeeNumber     NVARCHAR(50) NULL,
    AvailabilityStatus NVARCHAR(30) NOT NULL DEFAULT 'Available'   -- Available, On Route, Off Duty
);
GO

/* ---------- Vehicle (ERD: VehicleID, DriverID, VehicleType, RegistrationNumber) ---------- */
IF OBJECT_ID('dbo.[Vehicle]', 'U') IS NULL
CREATE TABLE dbo.[Vehicle] (
    VehicleID          INT IDENTITY(1,1) PRIMARY KEY,
    DriverID           INT NULL REFERENCES dbo.[Driver](UserID),
    VehicleType        NVARCHAR(50) NULL,
    RegistrationNumber NVARCHAR(30) NULL
);
GO

/* ---------- Route (extension - not in ERD) ---------- */
IF OBJECT_ID('dbo.[Route]', 'U') IS NULL
CREATE TABLE dbo.[Route] (
    RouteID    INT IDENTITY(1,1) PRIMARY KEY,
    RouteName  NVARCHAR(120) NOT NULL,
    DriverID   INT NULL REFERENCES dbo.[Driver](UserID),
    VehicleID  INT NULL REFERENCES dbo.[Vehicle](VehicleID),
    RouteDate  DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    Status     NVARCHAR(30) NOT NULL DEFAULT 'Planned'   -- Planned, Active, Completed
);
GO

/* ---------- Shipment (ERD: ShipmentID, SenderID, ReceiverID, DriverID, PickupAddress,
                         DeliveryAddress, ParcelWeight, ParcelDescription, Barcode,
                         TrackingNumber, ShipmentDate, DeliveryDate, DeliveryStatus) ---------- */
IF OBJECT_ID('dbo.[Shipment]', 'U') IS NULL
CREATE TABLE dbo.[Shipment] (
    ShipmentID        INT IDENTITY(1,1) PRIMARY KEY,
    SenderID          INT NULL REFERENCES dbo.[User](UserID),
    ReceiverID        INT NULL REFERENCES dbo.[User](UserID),
    DriverID          INT NULL REFERENCES dbo.[Driver](UserID),
    PickupAddress     NVARCHAR(250) NULL,
    DeliveryAddress   NVARCHAR(250) NULL,
    ParcelWeight      DECIMAL(10,2) NOT NULL DEFAULT 1,
    ParcelDescription NVARCHAR(250) NULL,
    Barcode           NVARCHAR(30)  NULL,
    TrackingNumber    NVARCHAR(30)  NOT NULL UNIQUE,
    ShipmentDate      DATETIME      NOT NULL DEFAULT GETDATE(),
    DeliveryDate      DATETIME      NULL,
    CollectedDate     DATETIME      NULL,
    DeliveryStatus    NVARCHAR(30)  NOT NULL DEFAULT 'Registered', -- Registered, Sorted, Assigned, OutForDelivery, Delivered, Failed
    -- extensions
    ReceiverName      NVARCHAR(150) NULL,
    ReceiverPhone     NVARCHAR(30)  NULL,
    OriginCity        NVARCHAR(60)  NULL,
    DestCity          NVARCHAR(60)  NULL,
    Cost              DECIMAL(10,2) NOT NULL DEFAULT 0,
    RouteID           INT NULL REFERENCES dbo.[Route](RouteID),
    SortCategory      NVARCHAR(60)  NULL,
    WarehouseStatus   NVARCHAR(30)  NOT NULL DEFAULT 'Pending',    -- Pending, Received, Sorted, Dispatched
    ExceptionType     NVARCHAR(20)  NOT NULL DEFAULT 'None'        -- None, Lost, Damaged
);
GO

/* ---------- TrackingHistory (ERD: TrackingID, DriverID, ShipmentID, AdminID, VehicleID,
                               Location, Timestamp, Remarks, Status) ---------- */
IF OBJECT_ID('dbo.[TrackingHistory]', 'U') IS NULL
CREATE TABLE dbo.[TrackingHistory] (
    TrackingID  INT IDENTITY(1,1) PRIMARY KEY,
    DriverID    INT NULL,
    ShipmentID  INT NOT NULL REFERENCES dbo.[Shipment](ShipmentID),
    AdminID     INT NULL,
    VehicleID   INT NULL,
    Location    NVARCHAR(150) NULL,
    [Timestamp] DATETIME NOT NULL DEFAULT GETDATE(),
    Remarks     NVARCHAR(250) NULL,
    Status      NVARCHAR(40)  NULL
);
GO

/* ---------- Rating (ERD: RatingID, ClientID, DriverID, Comment, RatingScore) ---------- */
IF OBJECT_ID('dbo.[Rating]', 'U') IS NULL
CREATE TABLE dbo.[Rating] (
    RatingID    INT IDENTITY(1,1) PRIMARY KEY,
    ClientID    INT NULL REFERENCES dbo.[User](UserID),
    DriverID    INT NULL REFERENCES dbo.[Driver](UserID),
    Comment     NVARCHAR(250) NULL,
    RatingScore INT NOT NULL DEFAULT 5,
    -- extension
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
);
GO

/* ---------- Payment (ERD: PaymentID, ClientID, PaymentStatus, PaymentMethod, ReferenceNumber) ---------- */
IF OBJECT_ID('dbo.[Payment]', 'U') IS NULL
CREATE TABLE dbo.[Payment] (
    PaymentID       INT IDENTITY(1,1) PRIMARY KEY,
    ClientID        INT NULL REFERENCES dbo.[User](UserID),
    PaymentStatus   NVARCHAR(30) NOT NULL DEFAULT 'Unpaid',   -- Unpaid, Paid
    PaymentMethod   NVARCHAR(30) NULL,
    ReferenceNumber NVARCHAR(50) NULL,
    -- extensions
    InvoiceNumber   NVARCHAR(30) NOT NULL UNIQUE,
    ShipmentID      INT NULL REFERENCES dbo.[Shipment](ShipmentID),
    Amount          DECIMAL(10,2) NOT NULL DEFAULT 0,
    IssuedDate      DATETIME NOT NULL DEFAULT GETDATE()
);
GO

/* ---------- Notification (extension - not in ERD) ---------- */
IF OBJECT_ID('dbo.[Notification]', 'U') IS NULL
CREATE TABLE dbo.[Notification] (
    NotificationID INT IDENTITY(1,1) PRIMARY KEY,
    UserID         INT NOT NULL REFERENCES dbo.[User](UserID),
    ShipmentID     INT NULL REFERENCES dbo.[Shipment](ShipmentID),
    Message        NVARCHAR(400) NOT NULL,
    Type           NVARCHAR(30) NULL,   -- Registration, Dispatch, Delivery, Delay, System
    IsRead         BIT NOT NULL DEFAULT 0,
    CreatedDate    DATETIME NOT NULL DEFAULT GETDATE()
);
GO

/* ============================================================
   Seed data (only if the User table is empty).
   Password hashes are SHA-256 / Base64 of the plain passwords.
   ============================================================ */
IF NOT EXISTS (SELECT 1 FROM dbo.[User])
BEGIN
    -- Five accounts across the three roles. The two drivers are Staff who also hold a
    -- dbo.[Driver] row; Route Planning needs at least two to have a choice to make.
    INSERT INTO dbo.[User] (FullName, Address, PhoneNumber, Password, Email, Role) VALUES
        ('System Administrator', 'Isando, Gauteng',    '0110000001', '6G94qKPK8LYNjnTllCqm2G3BUM08AzOK7yW30tfjrMc=', 'admin@fcl.co.za',     'Admin'),
        ('Thandi Mokoena',       'Central Depot, JHB', '0110000003', 'riOpTDpLwroHMiv7cirAaFJNHsiZdkDOmqXGBluBcMc=', 'staff@fcl.co.za',     'Staff'),
        ('Sipho Dlamini',        'Soweto, JHB',        '0110000005', 'OI/CLGhlBc6ItZ4qmx3u+T3JuFVYBXjqcEqjg9Y+4f0=', 'driver@fcl.co.za',    'Staff'),
        ('Ishmael Zakwe',        'Tembisa, JHB',       '0110000007', 'OI/CLGhlBc6ItZ4qmx3u+T3JuFVYBXjqcEqjg9Y+4f0=', 'driver2@fcl.co.za',   'Staff'),
        ('Nolwazi Khoza',        '12 Main Rd, Forte Town', '0821234567', 'mOxlSo3yj48PjwIiBIPUaRa4UBfeC3TY7HVcKMuFOag=', 'customer@fcl.co.za', 'Customer');

    DECLARE @admin INT   = (SELECT UserID FROM dbo.[User] WHERE Email = 'admin@fcl.co.za');
    DECLARE @staff INT   = (SELECT UserID FROM dbo.[User] WHERE Email = 'staff@fcl.co.za');
    DECLARE @driver INT  = (SELECT UserID FROM dbo.[User] WHERE Email = 'driver@fcl.co.za');
    DECLARE @driver2 INT = (SELECT UserID FROM dbo.[User] WHERE Email = 'driver2@fcl.co.za');
    DECLARE @cust INT    = (SELECT UserID FROM dbo.[User] WHERE Email = 'customer@fcl.co.za');

    INSERT INTO dbo.[Admin] (UserID, Department) VALUES
        (@admin, 'IT / Systems'), (@staff, 'Operations'),
        (@driver, 'Operations'), (@driver2, 'Operations');

    INSERT INTO dbo.[Driver] (UserID, LicenseNumber, EmployeeNumber, AvailabilityStatus) VALUES
        (@driver,  'DL-8841022', 'EMP-1005', 'Available'),
        (@driver2, 'DL-7712556', 'EMP-1007', 'Available');

    INSERT INTO dbo.[Client] (UserID, ClientType) VALUES (@cust, 'Business');

    INSERT INTO dbo.[Vehicle] (DriverID, VehicleType, RegistrationNumber) VALUES
        (@driver,  'Panel Van',   'FCL 001 GP'),
        (@driver2, '1-Ton Truck', 'FCL 002 GP');

    -- Sample shipments spanning the lifecycle
    INSERT INTO dbo.[Shipment]
        (SenderID, DriverID, PickupAddress, DeliveryAddress, ParcelWeight, ParcelDescription, Barcode,
         TrackingNumber, ShipmentDate, DeliveryDate, DeliveryStatus, ReceiverName, ReceiverPhone,
         OriginCity, DestCity, Cost, SortCategory, WarehouseStatus, ExceptionType)
    VALUES
        (@cust, @driver, '12 Main Rd, Forte Town', '88 Long St, Cape Town', 3.5, 'Boxed clothing order', 'FCL-10000001',
         'FCL-10000001', DATEADD(day,-4,GETDATE()), DATEADD(day,-2,GETDATE()), 'Delivered', 'Nomvula Sithole', '0837654321',
         'Johannesburg', 'Cape Town', 172.00, 'Metro-CPT', 'Dispatched', 'None'),
        (@cust, @driver2, '12 Main Rd, Forte Town', '5 Beach Rd, Durban', 8.0, 'Fragile ceramics', 'FCL-10000002',
         'FCL-10000002', DATEADD(day,-1,GETDATE()), NULL, 'InTransit', 'Peter Adams', '0845551212',
         'Johannesburg', 'Durban', 253.00, 'Metro-DBN', 'Dispatched', 'Damaged'),
        (@cust, NULL, '12 Main Rd, Forte Town', '9 Church St, Bloemfontein', 1.2, 'Documents', 'FCL-10000003',
         'FCL-10000003', GETDATE(), NULL, 'Registered', 'Lerato M', '0829998877',
         'Johannesburg', 'Bloemfontein', 145.00, NULL, 'Pending', 'None');

    DECLARE @ship1 INT = (SELECT ShipmentID FROM dbo.[Shipment] WHERE TrackingNumber = 'FCL-10000001');
    DECLARE @ship2 INT = (SELECT ShipmentID FROM dbo.[Shipment] WHERE TrackingNumber = 'FCL-10000002');
    DECLARE @ship3 INT = (SELECT ShipmentID FROM dbo.[Shipment] WHERE TrackingNumber = 'FCL-10000003');

    INSERT INTO dbo.[TrackingHistory] (ShipmentID, Location, Status, Remarks) VALUES
        (@ship1, 'Johannesburg Depot', 'Registered', 'Parcel registered at intake'),
        (@ship1, 'Cape Town',          'Delivered',  'Delivered - signed by recipient'),
        (@ship2, 'Johannesburg Depot', 'Registered', 'Parcel registered at intake'),
        (@ship2, 'Dispatch',           'InTransit',  'Allocated to delivery route and dispatched'),
        (@ship3, 'Johannesburg Depot', 'Registered', 'Parcel registered at intake');

    INSERT INTO dbo.[Payment] (ClientID, PaymentStatus, PaymentMethod, ReferenceNumber, InvoiceNumber, ShipmentID, Amount)
        VALUES (@cust, 'Paid', 'EFT', 'PAYREF001', 'INV-20260001', @ship1, 172.00);
END
GO

/* ------------------------------------------------------------
   Idempotent upgrades for existing databases (safe to re-run).
   ------------------------------------------------------------ */
IF COL_LENGTH('dbo.Shipment', 'CollectedDate') IS NULL
    ALTER TABLE dbo.[Shipment] ADD CollectedDate DATETIME NULL;
GO

/* ------------------------------------------------------------
   Automation upgrades (idempotent, safe to re-run).
   DbInitializer replays every batch in this section on startup.
   ------------------------------------------------------------ */

/* De-duplication key so a repeated notification is only ever sent once. */
IF COL_LENGTH('dbo.Notification', 'DedupeKey') IS NULL
    ALTER TABLE dbo.[Notification] ADD DedupeKey NVARCHAR(120) NULL;
GO

/* Delivery attempts, so a failed delivery can be re-queued a bounded number of times. */
IF COL_LENGTH('dbo.Shipment', 'AttemptCount') IS NULL
    ALTER TABLE dbo.[Shipment] ADD AttemptCount INT NOT NULL DEFAULT 0;
GO

/* When the delivery status last moved - the sensor for stuck-parcel detection. */
IF COL_LENGTH('dbo.Shipment', 'LastStatusChange') IS NULL
    ALTER TABLE dbo.[Shipment] ADD LastStatusChange DATETIME NULL;
GO

/* Parcel dimensions for the size band chosen at booking, printed on the waybill and the
   invoice so the record shows what was actually shipped.

   Nullable on purpose: a shipment registered at the Ops counter is captured from a
   free-text weight with no size band, so it legitimately has no dimensions. Every screen
   that displays these renders an em dash when they are absent. */
IF COL_LENGTH('dbo.Shipment', 'ParcelLengthCm') IS NULL
    ALTER TABLE dbo.[Shipment] ADD ParcelLengthCm DECIMAL(6,1) NULL;
GO

IF COL_LENGTH('dbo.Shipment', 'ParcelWidthCm') IS NULL
    ALTER TABLE dbo.[Shipment] ADD ParcelWidthCm DECIMAL(6,1) NULL;
GO

IF COL_LENGTH('dbo.Shipment', 'ParcelHeightCm') IS NULL
    ALTER TABLE dbo.[Shipment] ADD ParcelHeightCm DECIMAL(6,1) NULL;
GO

IF COL_LENGTH('dbo.Route', 'CompletedDate') IS NULL
    ALTER TABLE dbo.[Route] ADD CompletedDate DATETIME NULL;
GO

IF COL_LENGTH('dbo.Payment', 'DueDate') IS NULL
    ALTER TABLE dbo.[Payment] ADD DueDate DATETIME NULL;
GO

/* Race-free invoice numbering (replaces COUNT(*)+1, which collided under concurrency). */
IF OBJECT_ID('dbo.NumberSequence', 'U') IS NULL
CREATE TABLE dbo.NumberSequence (
    SeqName   NVARCHAR(40) NOT NULL PRIMARY KEY,
    NextValue INT          NOT NULL DEFAULT 0
);
GO

/* Seed this year's counter from existing invoices so we never collide with seeded data. */
IF OBJECT_ID('dbo.NumberSequence', 'U') IS NOT NULL
BEGIN
    DECLARE @seq NVARCHAR(40) = 'INV-' + CAST(YEAR(GETDATE()) AS NVARCHAR(4));
    IF NOT EXISTS (SELECT 1 FROM dbo.NumberSequence WHERE SeqName = @seq)
        INSERT INTO dbo.NumberSequence (SeqName, NextValue)
        SELECT @seq, ISNULL(MAX(TRY_CAST(RIGHT(InvoiceNumber, 4) AS INT)), 0)
        FROM dbo.[Payment] WHERE InvoiceNumber LIKE @seq + '%';
END
GO

/* Website enquiries from Contact.aspx (previously discarded). */
IF OBJECT_ID('dbo.ContactMessage', 'U') IS NULL
CREATE TABLE dbo.ContactMessage (
    MessageID   INT IDENTITY(1,1) PRIMARY KEY,
    FullName    NVARCHAR(150)  NOT NULL,
    Email       NVARCHAR(150)  NULL,
    Message     NVARCHAR(2000) NOT NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    HandledBy   INT NULL REFERENCES dbo.[User](UserID),
    HandledDate DATETIME NULL
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'IX_Notification_User_Unread'
                 AND object_id = OBJECT_ID('dbo.Notification'))
CREATE INDEX IX_Notification_User_Unread
    ON dbo.[Notification](UserID, IsRead, CreatedDate DESC);
GO

/* ============================================================
   Migration: seven roles -> three (Customer, Staff, Admin).

   Needed because the seed block above only runs when dbo.[User] is empty, so a
   database already carrying the old Manager/Dispatch/Warehouse/Finance/Driver rows
   is not corrected by it. Without this every one of those logins lands on
   Default.aspx and sees an empty nav.

   Drivers keep their dbo.[Driver] rows, so they remain drivers - that table, not
   the Role column, is the driver registry.

   Idempotent: re-running matches nothing.
   ============================================================ */
UPDATE dbo.[User]
   SET Role = 'Staff'
 WHERE Role IN ('Manager', 'Dispatch', 'Warehouse', 'Finance', 'Driver');
GO

/* Every internal user needs a dbo.[Admin] row; the old Driver role never created one. */
INSERT INTO dbo.[Admin] (UserID, Department)
SELECT u.UserID, 'Operations'
  FROM dbo.[User] u
 WHERE u.Role IN ('Staff', 'Admin')
   AND NOT EXISTS (SELECT 1 FROM dbo.[Admin] a WHERE a.UserID = u.UserID);
GO

/* Added separately: the CHECK above only applies to a freshly created table.
   Runs after the migration, so it cannot fail on legacy values. */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_User_Role')
ALTER TABLE dbo.[User] ADD CONSTRAINT CK_User_Role
    CHECK (Role IN ('Customer', 'Staff', 'Admin'));
GO

/* ============================================================
   Payment-before-dispatch gate (Individual customers).

   PaymentGateStatus lives on Shipment, separate from DeliveryStatus/WarehouseStatus,
   so "has this parcel cleared payment" is never conflated with "where is it in the
   warehouse/route pipeline". Values: NotRequired, PaymentRequired, PaymentPending,
   PaymentSuccessful. Counter-registered and Business-client shipments default to
   NotRequired (Business is invoiced, not gated); online Individual bookings are set to
   PaymentRequired by the booking code. Warehouse/RoutePlanning only admit shipments whose
   PaymentGateStatus is NotRequired or PaymentSuccessful - see Ops\Warehouse.aspx.cs and
   Ops\RoutePlanning.aspx.cs.
   ============================================================ */
IF COL_LENGTH('dbo.Shipment', 'PaymentGateStatus') IS NULL
    ALTER TABLE dbo.[Shipment] ADD PaymentGateStatus NVARCHAR(20) NOT NULL DEFAULT 'NotRequired';
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Shipment_PaymentGateStatus')
ALTER TABLE dbo.[Shipment] ADD CONSTRAINT CK_Shipment_PaymentGateStatus
    CHECK (PaymentGateStatus IN ('NotRequired', 'PaymentRequired', 'PaymentPending', 'PaymentSuccessful'));
GO

/* Duplicate-payment-submission guard: a client-generated idempotency key (one per payment
   attempt/click) makes a resubmitted or duplicated request a no-op instead of a second
   charge. Nullable + filtered unique index so historical rows (no key) are unaffected. */
IF COL_LENGTH('dbo.Payment', 'IdempotencyKey') IS NULL
    ALTER TABLE dbo.[Payment] ADD IdempotencyKey NVARCHAR(100) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UX_Payment_IdempotencyKey' AND object_id = OBJECT_ID('dbo.Payment'))
CREATE UNIQUE INDEX UX_Payment_IdempotencyKey
    ON dbo.[Payment](IdempotencyKey) WHERE IdempotencyKey IS NOT NULL;
GO

/* PaymentMethod recorded by the sandbox gateway; PaymentStatus gains Pending/Failed so a
   declined or in-flight sandbox charge is distinguishable from a genuinely unpaid invoice. */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Payment_PaymentStatus')
ALTER TABLE dbo.[Payment] ADD CONSTRAINT CK_Payment_PaymentStatus
    CHECK (PaymentStatus IN ('Unpaid', 'Pending', 'Paid', 'Failed', 'Cancelled'));
GO

/* Business billing: groups Client rows that share and jointly pay each other's invoices.
   A Business (Client.ClientType = 'Business') is not itself gated by PaymentGateStatus -
   see comment above - it settles via Finance\Invoices.aspx / Customer\Invoices.aspx instead. */
IF OBJECT_ID('dbo.Business', 'U') IS NULL
CREATE TABLE dbo.Business (
    BusinessID   INT IDENTITY(1,1) PRIMARY KEY,
    BusinessName NVARCHAR(150) NOT NULL,
    RegNumber    NVARCHAR(50)  NULL,
    CreatedDate  DATETIME NOT NULL DEFAULT GETDATE()
);
GO

IF COL_LENGTH('dbo.Client', 'BusinessID') IS NULL
    ALTER TABLE dbo.[Client] ADD BusinessID INT NULL REFERENCES dbo.Business(BusinessID);
GO

/* Records which invoices a single bulk-payment submission settled, and how much of each -
   the join needed to allocate one payment across several selected invoices safely. */
IF OBJECT_ID('dbo.PaymentAllocation', 'U') IS NULL
CREATE TABLE dbo.PaymentAllocation (
    AllocationID   INT IDENTITY(1,1) PRIMARY KEY,
    PaymentID      INT NOT NULL REFERENCES dbo.[Payment](PaymentID),
    AmountApplied  DECIMAL(10,2) NOT NULL,
    AppliedBy      INT NOT NULL REFERENCES dbo.[User](UserID),
    AppliedDate    DATETIME NOT NULL DEFAULT GETDATE(),
    BatchKey       NVARCHAR(100) NULL   -- ties every allocation in one bulk-pay submission together
);
GO

/* General-purpose audit trail for manual actions that need a record beyond TrackingHistory
   (bulk invoice payments, route/service-area overrides). */
IF OBJECT_ID('dbo.AuditLog', 'U') IS NULL
CREATE TABLE dbo.AuditLog (
    AuditID     INT IDENTITY(1,1) PRIMARY KEY,
    UserID      INT NULL REFERENCES dbo.[User](UserID),
    Action      NVARCHAR(60)   NOT NULL,
    Entity      NVARCHAR(60)   NOT NULL,
    EntityID    NVARCHAR(60)   NULL,
    Details     NVARCHAR(1000) NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
);
GO

/* Existing rows: counter/walk-in and any already-Delivered/InTransit/Assigned shipment
   have obviously already cleared dispatch, so backfill them to NotRequired rather than
   retroactively blocking history. Only a brand-new online booking is gated going forward. */
UPDATE dbo.[Shipment]
   SET PaymentGateStatus = 'NotRequired'
 WHERE PaymentGateStatus = 'PaymentRequired'
   AND DeliveryStatus <> 'Registered';
GO

/* ============================================================
   Route <-> destination service-area validation.

   A route previously had no destination of its own, so nothing stopped an East London
   parcel being assigned to a Cape Town route: assignment only filtered the parcel PICKER
   by sort lane as a convenience, it never checked the chosen ROUTE. ServiceArea pins a
   route to one of SortingService.Categories (the same lanes Warehouse sorting already
   derives from Shipment.DestCity via SortingService.CategoryFor), and
   Ops\RoutePlanning.aspx.cs now refuses to assign a parcel whose SortCategory does not
   match the target route's ServiceArea.

   NULL is kept legal only for routes created before this migration, so historical data is
   never invalidated; Ops\RoutePlanning.aspx now requires a Service Area on every route it
   creates going forward.
   ============================================================ */
IF COL_LENGTH('dbo.Route', 'ServiceArea') IS NULL
    ALTER TABLE dbo.[Route] ADD ServiceArea NVARCHAR(60) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Route_ServiceArea')
ALTER TABLE dbo.[Route] ADD CONSTRAINT CK_Route_ServiceArea
    CHECK (ServiceArea IN ('Metro-JHB', 'Metro-CPT', 'Metro-DBN', 'Metro-PE', 'Regional-Outlying', 'Express-Priority'));
GO

/* Route management audit trail - who created/last changed a route, and when. */
IF COL_LENGTH('dbo.Route', 'CreatedBy') IS NULL
    ALTER TABLE dbo.[Route] ADD CreatedBy INT NULL REFERENCES dbo.[User](UserID);
GO

IF COL_LENGTH('dbo.Route', 'UpdatedBy') IS NULL
    ALTER TABLE dbo.[Route] ADD UpdatedBy INT NULL REFERENCES dbo.[User](UserID);
GO

IF COL_LENGTH('dbo.Route', 'UpdatedDate') IS NULL
    ALTER TABLE dbo.[Route] ADD UpdatedDate DATETIME NULL;
GO

/* ============================================================
   Live-map position pings (driver + customer shared locations).
   The Track page and the driver map READ the latest ping per
   (ShipmentID, Role); TrackHandler.ashx WRITES them. One row per
   role per shipment (upserted by the endpoint).
   ============================================================ */
IF OBJECT_ID('dbo.LocationPing', 'U') IS NULL
CREATE TABLE dbo.LocationPing (
    PingID      INT IDENTITY(1,1) PRIMARY KEY,
    ShipmentID  INT NOT NULL REFERENCES dbo.[Shipment](ShipmentID),
    Role        NVARCHAR(20) NOT NULL,          -- Driver, Customer
    Lat         DECIMAL(9,6) NOT NULL,
    Lng         DECIMAL(9,6) NOT NULL,
    UpdatedDate DATETIME NOT NULL DEFAULT GETDATE()
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE name = 'UX_LocationPing_Shipment_Role' AND object_id = OBJECT_ID('dbo.LocationPing'))
CREATE UNIQUE INDEX UX_LocationPing_Shipment_Role ON dbo.LocationPing(ShipmentID, Role);
GO

/* ============================================================
   BULK DEMO DATA  (idempotent: keyed on FCL-20000001, runs once)

   198 shipments across the full lifecycle, each tied to a customer
   (sender), a driver, a vehicle, a route (with ServiceArea), a parcel
   size, a payment/invoice, tracking history, and - when delivered -
   a rating. Tracking numbers are fixed so they can be documented:

     150  Delivered           FCL-20000001 .. FCL-20000150
      20  Upcoming (booked)   FCL-20000151 .. FCL-20000170
      15  Awaiting collection FCL-20000171 .. FCL-20000185   (DeliveryStatus = Assigned)
      13  Out for delivery    FCL-20000186 .. FCL-20000198   (DeliveryStatus = OutForDelivery)

   All seeded logins share one password per role:
     drivers   -> Driver@123       customers -> Customer@123
   ============================================================ */
IF NOT EXISTS (SELECT 1 FROM dbo.[Shipment] WHERE TrackingNumber = 'FCL-20000001')
BEGIN
    DECLARE @custHash NVARCHAR(200) = 'mOxlSo3yj48PjwIiBIPUaRa4UBfeC3TY7HVcKMuFOag='; -- Customer@123
    DECLARE @drvHash  NVARCHAR(200) = 'OI/CLGhlBc6ItZ4qmx3u+T3JuFVYBXjqcEqjg9Y+4f0='; -- Driver@123

    /* ---- Extra drivers (driver3@ .. driver8@); driver@/driver2@ already seeded ---- */
    ;WITH D(idx, nm) AS (SELECT * FROM (VALUES
        (3,'Bongani Nkosi'),(4,'Pieter van Wyk'),(5,'Fatima Patel'),
        (6,'Lerato Modise'),(7,'Johan Botha'),(8,'Themba Zulu')) v(idx,nm))
    INSERT INTO dbo.[User](FullName, Address, PhoneNumber, Password, Email, Role)
    SELECT nm, 'Operations Depot, Johannesburg',
           '0110' + RIGHT('000000' + CAST(idx AS VARCHAR(6)), 6), @drvHash,
           'driver' + CAST(idx AS VARCHAR(6)) + '@fcl.co.za', 'Staff'
    FROM D
    WHERE NOT EXISTS (SELECT 1 FROM dbo.[User] u
                      WHERE u.Email = 'driver' + CAST(D.idx AS VARCHAR(6)) + '@fcl.co.za');

    /* Driver / Admin / Vehicle rows for every driverN that still lacks them */
    INSERT INTO dbo.[Driver](UserID, LicenseNumber, EmployeeNumber, AvailabilityStatus)
    SELECT u.UserID, 'DL-' + RIGHT('0000000' + CAST(u.UserID AS VARCHAR(10)), 7),
           'EMP-' + CAST(1000 + u.UserID AS VARCHAR(10)), 'Available'
    FROM dbo.[User] u
    WHERE u.Email LIKE 'driver%@fcl.co.za'
      AND NOT EXISTS (SELECT 1 FROM dbo.[Driver] d WHERE d.UserID = u.UserID);

    INSERT INTO dbo.[Admin](UserID, Department)
    SELECT u.UserID, 'Operations'
    FROM dbo.[User] u
    WHERE u.Email LIKE 'driver%@fcl.co.za'
      AND NOT EXISTS (SELECT 1 FROM dbo.[Admin] a WHERE a.UserID = u.UserID);

    INSERT INTO dbo.[Vehicle](DriverID, VehicleType, RegistrationNumber)
    SELECT d.UserID,
           CASE d.UserID % 3 WHEN 0 THEN 'Panel Van' WHEN 1 THEN '1-Ton Truck' ELSE 'Motorcycle' END,
           'FCL ' + RIGHT('000' + CAST(d.UserID AS VARCHAR(6)), 3) + ' GP'
    FROM dbo.[Driver] d
    WHERE NOT EXISTS (SELECT 1 FROM dbo.[Vehicle] v WHERE v.DriverID = d.UserID);

    /* ---- Customers (customer1@ .. customer30@); customer@ already seeded ---- */
    ;WITH nums AS (SELECT TOP (30) ROW_NUMBER() OVER (ORDER BY (SELECT 1)) AS i FROM sys.all_objects)
    INSERT INTO dbo.[User](FullName, Address, PhoneNumber, Password, Email, Role)
    SELECT
        CHOOSE(((i-1)%10)+1,'Sipho','Naledi','Kabelo','Zanele','Thabo','Aisha','Lwazi','Palesa','Riaan','Nandi')
          + ' ' + CHOOSE(((i-1)%8)+1,'Mahlangu','Ndlovu','Pretorius','Khumalo','Naidoo','Mokoena','Jacobs','Sithole'),
        CAST(((i*13)%95)+1 AS VARCHAR(6)) + ' '
          + CHOOSE(((i-1)%5)+1,'Oak Ave','Main Rd','Church St','Kerk St','Beach Rd') + ', Johannesburg',
        '082' + RIGHT('0000000' + CAST(i*37 AS VARCHAR(10)), 7),
        @custHash, 'customer' + CAST(i AS VARCHAR(6)) + '@fcl.co.za', 'Customer'
    FROM nums
    WHERE NOT EXISTS (SELECT 1 FROM dbo.[User] u
                      WHERE u.Email = 'customer' + CAST(nums.i AS VARCHAR(6)) + '@fcl.co.za');

    INSERT INTO dbo.[Client](UserID, ClientType)
    SELECT u.UserID, CASE WHEN u.UserID % 6 = 0 THEN 'Business' ELSE 'Individual' END
    FROM dbo.[User] u
    WHERE u.Role = 'Customer'
      AND NOT EXISTS (SELECT 1 FROM dbo.[Client] c WHERE c.UserID = u.UserID);

    /* ---- Ordered working sets for round-robin assignment ---- */
    SELECT UserID, (ROW_NUMBER() OVER (ORDER BY UserID) - 1) AS idx
      INTO #cust FROM dbo.[User] WHERE Role = 'Customer';
    DECLARE @C INT = (SELECT COUNT(*) FROM #cust);

    SELECT d.UserID,
           (ROW_NUMBER() OVER (ORDER BY d.UserID) - 1) AS idx,
           (SELECT TOP 1 v.VehicleID FROM dbo.[Vehicle] v WHERE v.DriverID = d.UserID ORDER BY v.VehicleID) AS VehicleID
      INTO #drv FROM dbo.[Driver] d;
    DECLARE @D INT = (SELECT COUNT(*) FROM #drv);

    /* ---- Reference data: cities (with coordinates) and parcel sizes ---- */
    DECLARE @cities TABLE (idx INT, City NVARCHAR(60), Area NVARCHAR(60), Lat DECIMAL(9,6), Lng DECIMAL(9,6));
    INSERT INTO @cities VALUES
        (0,'Cape Town',   'Metro-CPT',         -33.924870, 18.424055),
        (1,'Durban',      'Metro-DBN',         -29.858680, 31.021840),
        (2,'Gqeberha',    'Metro-PE',          -33.960800, 25.602200),
        (3,'Bloemfontein','Regional-Outlying', -29.085200, 26.159900),
        (4,'Pretoria',    'Regional-Outlying', -25.747900, 28.229300),
        (5,'East London', 'Regional-Outlying', -33.015300, 27.911600);

    DECLARE @sizes TABLE (idx INT, Nm NVARCHAR(30), Wt DECIMAL(10,2), L DECIMAL(6,1), W DECIMAL(6,1), H DECIMAL(6,1));
    INSERT INTO @sizes VALUES
        (0,'Small',2,25,20,10),(1,'Medium',5,40,30,20),(2,'Large',10,60,40,30),(3,'Extra-Large',25,80,60,50);

    /* ---- Routes: two per destination service area, each with a driver + vehicle ---- */
    DECLARE @areas TABLE (idx INT, Area NVARCHAR(60));
    INSERT INTO @areas VALUES (0,'Metro-CPT'),(1,'Metro-DBN'),(2,'Metro-PE'),(3,'Regional-Outlying');

    ;WITH R AS (SELECT a.idx AS aidx, a.Area, r2.rn FROM @areas a CROSS JOIN (VALUES (0),(1)) r2(rn))
    INSERT INTO dbo.[Route](RouteName, DriverID, VehicleID, RouteDate, Status, ServiceArea)
    SELECT R.Area + ' Run ' + CAST(R.rn + 1 AS VARCHAR(2)),
           dr.UserID, dr.VehicleID,
           CAST(DATEADD(day, -((R.aidx * 2 + R.rn) % 7), GETDATE()) AS DATE),
           'Active', R.Area
    FROM R
    CROSS APPLY (SELECT UserID, VehicleID FROM #drv WHERE idx = (R.aidx * 2 + R.rn) % @D) dr;

    SELECT RouteID, ServiceArea, DriverID,
           (ROW_NUMBER() OVER (PARTITION BY ServiceArea ORDER BY RouteID) - 1) AS idx
      INTO #rte FROM dbo.[Route] WHERE ServiceArea IS NOT NULL;

    /* ---- 198 shipments ---- */
    ;WITH N AS (SELECT TOP (198) ROW_NUMBER() OVER (ORDER BY (SELECT 1)) AS n FROM sys.all_objects),
    G AS (
        SELECT n,
               CASE WHEN n <= 150 THEN 'Delivered'
                    WHEN n <= 170 THEN CASE WHEN n % 2 = 0 THEN 'Sorted' ELSE 'Registered' END
                    WHEN n <= 185 THEN 'Assigned'
                    ELSE 'OutForDelivery' END AS Status,
               (n - 1) % 6 AS cidx, (n - 1) % 4 AS sidx
        FROM N
    )
    INSERT INTO dbo.[Shipment]
        (SenderID, DriverID, PickupAddress, DeliveryAddress, ParcelWeight, ParcelDescription, Barcode,
         TrackingNumber, ShipmentDate, DeliveryDate, CollectedDate, DeliveryStatus,
         ReceiverName, ReceiverPhone, OriginCity, DestCity, Cost, RouteID, SortCategory,
         WarehouseStatus, ExceptionType, ParcelLengthCm, ParcelWidthCm, ParcelHeightCm,
         LastStatusChange, PaymentGateStatus, AttemptCount)
    SELECT
        cu.UserID,
        CASE WHEN g.Status IN ('Delivered','Assigned','OutForDelivery') THEN rt.DriverID ELSE NULL END,
        '12 Main Rd, Forte Town, Johannesburg',
        CAST(((g.n * 7) % 90) + 1 AS VARCHAR(4)) + ' '
          + CHOOSE((g.n % 5) + 1,'Long St','Smith St','Govan Mbeki Ave','Maitland St','Oxford Rd') + ', ' + ci.City,
        sz.Wt,
        CHOOSE((g.n % 6) + 1,'Documents','Boxed clothing','Electronics','Books','Spare parts','Homeware'),
        'FCL-2000' + RIGHT('0000' + CAST(g.n AS VARCHAR(6)), 4),
        'FCL-2000' + RIGHT('0000' + CAST(g.n AS VARCHAR(6)), 4),
        CASE g.Status
            WHEN 'Delivered'      THEN DATEADD(day,  -((g.n % 55) + 5), GETDATE())
            WHEN 'OutForDelivery' THEN DATEADD(hour, -((g.n % 20) + 4), GETDATE())
            WHEN 'Assigned'       THEN DATEADD(hour, -((g.n % 30) + 2), GETDATE())
            ELSE                       DATEADD(hour, -((g.n % 48) + 1), GETDATE()) END,
        CASE WHEN g.Status = 'Delivered' THEN DATEADD(day, -((g.n % 55) + 2), GETDATE()) ELSE NULL END,
        CASE WHEN g.Status = 'Delivered'      THEN DATEADD(day,  -((g.n % 55) + 4), GETDATE())
             WHEN g.Status = 'OutForDelivery' THEN DATEADD(hour, -((g.n % 10) + 1), GETDATE()) ELSE NULL END,
        g.Status,
        CHOOSE((g.n % 8) + 1,'Nomvula Sithole','Peter Adams','Lerato Mphela','Kyle Daniels',
                             'Aisha Khan','Sibusiso Ngema','Megan Fourie','Tshepo Radebe'),
        '083' + RIGHT('0000000' + CAST(g.n * 53 AS VARCHAR(10)), 7),
        'Johannesburg', ci.City,
        145 + CASE WHEN sz.Wt > 2 THEN (sz.Wt - 2) * 18 ELSE 0 END,
        CASE WHEN g.Status IN ('Delivered','Assigned','OutForDelivery') THEN rt.RouteID ELSE NULL END,
        ci.Area,
        CASE g.Status WHEN 'Registered' THEN 'Pending' WHEN 'Sorted' THEN 'Sorted' ELSE 'Dispatched' END,
        CASE WHEN g.n % 37 = 0 THEN 'Damaged' WHEN g.n % 53 = 0 THEN 'Lost' ELSE 'None' END,
        sz.L, sz.W, sz.H,
        CASE WHEN g.Status = 'Delivered' THEN DATEADD(day, -((g.n % 55) + 2), GETDATE())
             ELSE DATEADD(hour, -((g.n % 12) + 1), GETDATE()) END,
        'NotRequired', 0
    FROM G
        JOIN @cities ci ON ci.idx = g.cidx
        JOIN @sizes  sz ON sz.idx = g.sidx
        CROSS APPLY (SELECT UserID FROM #cust WHERE idx = (g.n - 1) % @C) cu
        OUTER APPLY (SELECT TOP 1 RouteID, DriverID FROM #rte
                     WHERE ServiceArea = ci.Area AND idx = ((g.n - 1) / 6) % 2) rt;

    /* ---- One invoice/payment per seeded shipment ---- */
    ;WITH P AS (
        SELECT s.ShipmentID, s.SenderID, s.Cost, s.DeliveryStatus, s.ShipmentDate,
               ROW_NUMBER() OVER (ORDER BY s.ShipmentID) AS rn
        FROM dbo.[Shipment] s
        WHERE s.TrackingNumber LIKE 'FCL-2000%'
          AND NOT EXISTS (SELECT 1 FROM dbo.[Payment] p WHERE p.ShipmentID = s.ShipmentID)
    )
    INSERT INTO dbo.[Payment](ClientID, PaymentStatus, PaymentMethod, ReferenceNumber,
                              InvoiceNumber, ShipmentID, Amount, IssuedDate, DueDate)
    SELECT P.SenderID,
           CASE WHEN P.DeliveryStatus IN ('Delivered','OutForDelivery','Assigned') THEN 'Paid' ELSE 'Unpaid' END,
           CASE WHEN P.DeliveryStatus IN ('Delivered','OutForDelivery','Assigned')
                THEN CHOOSE((P.rn % 3) + 1,'Card','EFT','Wallet') ELSE NULL END,
           CASE WHEN P.DeliveryStatus IN ('Delivered','OutForDelivery','Assigned')
                THEN 'PAYSEED-' + RIGHT('0000' + CAST(P.rn AS VARCHAR(6)), 4) ELSE NULL END,
           'INV-2000' + RIGHT('0000' + CAST(P.rn AS VARCHAR(6)), 4),
           P.ShipmentID, P.Cost, P.ShipmentDate, DATEADD(day, 7, P.ShipmentDate)
    FROM P;

    /* ---- Tracking history: the lifecycle stages reached so far ---- */
    INSERT INTO dbo.[TrackingHistory](ShipmentID, DriverID, Location, Status, Remarks, [Timestamp])
    SELECT s.ShipmentID, s.DriverID, t.Loc, t.St, t.Rm, DATEADD(hour, t.Ord, s.ShipmentDate)
    FROM dbo.[Shipment] s
    CROSS APPLY (VALUES
        ('Johannesburg Depot',          'Registered',     'Parcel registered at intake',                 0),
        ('Johannesburg Warehouse',      'Sorted',         'Sorted to ' + s.SortCategory + ' lane',       2),
        ('Dispatch Bay, Johannesburg',  'Assigned',       'Allocated to a delivery route',               4),
        ('En route to ' + s.DestCity,   'OutForDelivery', 'Parcel collected - out for delivery',         6),
        (s.DestCity,                    'Delivered',      'Delivered - signed by recipient',            48)
    ) t(Loc, St, Rm, Ord)
    WHERE s.TrackingNumber LIKE 'FCL-2000%'
      AND ( t.St = 'Registered'
         OR (t.St = 'Sorted'         AND s.DeliveryStatus IN ('Sorted','Assigned','OutForDelivery','Delivered'))
         OR (t.St = 'Assigned'       AND s.DeliveryStatus IN ('Assigned','OutForDelivery','Delivered'))
         OR (t.St = 'OutForDelivery' AND s.DeliveryStatus IN ('OutForDelivery','Delivered'))
         OR (t.St = 'Delivered'      AND s.DeliveryStatus = 'Delivered') );

    /* ---- Ratings on a third of delivered shipments ---- */
    INSERT INTO dbo.[Rating](ClientID, DriverID, Comment, RatingScore, CreatedDate)
    SELECT s.SenderID, s.DriverID,
           CHOOSE((s.ShipmentID % 4) + 1,'Fast and professional.','Parcel arrived safely.',
                                         'Great service, thank you.','On time - will use again.'),
           3 + (s.ShipmentID % 3),
           s.DeliveryDate
    FROM dbo.[Shipment] s
    WHERE s.TrackingNumber LIKE 'FCL-2000%' AND s.DeliveryStatus = 'Delivered'
      AND s.DriverID IS NOT NULL AND s.ShipmentID % 3 = 0;

    /* ---- Seed a mid-route driver ping for every out-for-delivery parcel ---- */
    INSERT INTO dbo.LocationPing(ShipmentID, Role, Lat, Lng, UpdatedDate)
    SELECT s.ShipmentID, 'Driver', (-26.204100 + ci.Lat) / 2.0, (28.047300 + ci.Lng) / 2.0, GETDATE()
    FROM dbo.[Shipment] s JOIN @cities ci ON ci.City = s.DestCity
    WHERE s.TrackingNumber LIKE 'FCL-2000%' AND s.DeliveryStatus = 'OutForDelivery'
      AND NOT EXISTS (SELECT 1 FROM dbo.LocationPing lp WHERE lp.ShipmentID = s.ShipmentID AND lp.Role = 'Driver');

    /* ---- Drivers currently carrying an out-for-delivery parcel are On Route ---- */
    UPDATE dbo.[Driver] SET AvailabilityStatus = 'On Route'
    WHERE UserID IN (SELECT DISTINCT DriverID FROM dbo.[Shipment]
                     WHERE DeliveryStatus = 'OutForDelivery' AND DriverID IS NOT NULL);

    DROP TABLE #cust; DROP TABLE #drv; DROP TABLE #rte;
END
GO

