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
