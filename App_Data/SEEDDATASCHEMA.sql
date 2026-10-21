/* ============================================================
   Logisticore - clean database + full demo data (for sharing as a backup)

   WHAT THIS SCRIPT DOES
     Builds the "Logisticore" database from scratch, in its final
     shape (every table, column, constraint and index the app uses),
     and loads every login listed in CREDENTIALS.md together with the
     demo shipments, invoices, tracking history, ratings and routes.
     The result is a clean database you can back up and send to the
     team, so everyone tests against the same data.

     App_Data\Schema.sql stays the app's own setup/upgrade script.
     This file is the "fresh copy for the team" version of it: same
     tables, same demo data, but every password stored as a salted
     PBKDF2 hash (the format the app writes today) instead of the
     old unsalted SHA-256 one.

   STEP 1 - BUILD THE DATABASE (you)
     1. Open this file in SQL Server Management Studio, connected to
        your instance (e.g. .\SQLEXPRESS).
     2. If you already have a "Logisticore" database, this script stops
        without touching it. To replace it, change
            DECLARE @ReplaceExisting BIT = 0;
        below to 1. That DELETES the existing Logisticore database.
     3. Press F5. The last result grid shows the row counts and
        "Seed check passed".
     All dates are relative to the moment you run the script (e.g.
     "out for delivery since 4 hours ago"), so make the backup soon
     after running it.

   STEP 2 - MAKE THE BACKUP (you)
     Use the BACKUP block at the end of this file (select the lines
     inside it and press F5), or in SSMS: right-click Logisticore >
     Tasks > Back Up... > Backup type Full, "Copy-only backup" ticked.

   STEP 3 - RESTORE IT (each team member)
     Use the RESTORE block at the end of this file, or in SSMS:
     right-click Databases > Restore Database... > Device > add the
     .bak file > OK. Then check the app's Web.config connection string
     points at their own instance (Data Source=.\SQLEXPRESS by default).

     A backup only restores on the SAME OR A NEWER SQL Server version
     than the one that made it. A backup made on SQL Server 2022 (16.x)
     will not restore on 2019 or older - those team members should run
     this script instead; it produces the same data.

   LOGINS (all passwords are case-sensitive)
     Staff Portal tab:
       admin@fcl.co.za                         Admin@123     (Admin)
       staff@fcl.co.za                         Dispatch@123  (Operations staff)
       driver@fcl.co.za, driver2..driver8@     Driver@123    (Drivers)
     Client Portal tab:
       customer@fcl.co.za                      Customer@123  (Business client)
       customer1@ .. customer30@fcl.co.za      Customer@123

   WAYBILLS
       FCL-20000001 .. FCL-20000150   Delivered
       FCL-20000151 .. FCL-20000170   Upcoming (Registered / Sorted)
       FCL-20000171 .. FCL-20000185   Awaiting collection (Assigned)
       FCL-20000186 .. FCL-20000198   Out for delivery (live map demo)
       FCL-10000001 / 02 / 03         Original samples (delivered /
                                      in transit / registered)
   ============================================================ */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;   -- needed for the filtered unique index (sqlcmd defaults it OFF)
SET NOCOUNT ON;
GO

USE master;   -- never hold a connection inside the database we may drop
GO

/* ---------- Safety guard ---------- */
DECLARE @ReplaceExisting BIT = 0;   -- 1 = delete the existing Logisticore database and rebuild it

IF DB_ID(N'Logisticore') IS NOT NULL
BEGIN
    IF @ReplaceExisting = 0
    BEGIN
        RAISERROR(N'A database named Logisticore already exists, so nothing was changed. To delete it and rebuild it with the demo data, set @ReplaceExisting = 1 near the top of this script and run it again.', 16, 1);
        SET NOEXEC ON;   -- the remaining batches are only compiled, never run
    END
    ELSE
    BEGIN
        ALTER DATABASE Logisticore SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        DROP DATABASE Logisticore;
        PRINT 'Existing Logisticore database removed.';
    END
END
GO

CREATE DATABASE Logisticore;
GO

ALTER DATABASE Logisticore SET RECOVERY SIMPLE;   -- small log, small backup
GO

/* Owned by sa rather than your Windows login, so a restored copy has no orphaned owner. */
BEGIN TRY
    ALTER AUTHORIZATION ON DATABASE::Logisticore TO [sa];
END TRY
BEGIN CATCH
    PRINT 'Note: could not make sa the database owner (' + ERROR_MESSAGE() + '). This is harmless.';
END CATCH
GO

USE Logisticore;
GO

/* ============================================================
   SCHEMA (final shape - equivalent to running App_Data\Schema.sql
   on an empty database, including every upgrade at its end)
   ============================================================ */

CREATE TABLE dbo.[User] (
    UserID       INT IDENTITY(1,1) PRIMARY KEY,
    FullName     NVARCHAR(150) NOT NULL,
    Address      NVARCHAR(250) NULL,
    PhoneNumber  NVARCHAR(30)  NULL,
    IDNumber     NVARCHAR(30)  NULL,
    Password     NVARCHAR(200) NOT NULL,
    Email        NVARCHAR(150) NOT NULL UNIQUE,
    -- Three roles. A driver is a Staff user who also has a dbo.[Driver] row.
    Role         NVARCHAR(30)  NOT NULL
        CONSTRAINT CK_User_Role CHECK (Role IN ('Customer', 'Staff', 'Admin')),
    CreatedDate  DATETIME      NOT NULL DEFAULT GETDATE()
);

CREATE TABLE dbo.[Admin] (
    EmployeeID  INT IDENTITY(1,1) PRIMARY KEY,
    UserID      INT NOT NULL REFERENCES dbo.[User](UserID),
    Department  NVARCHAR(100) NULL
);

CREATE TABLE dbo.Business (
    BusinessID   INT IDENTITY(1,1) PRIMARY KEY,
    BusinessName NVARCHAR(150) NOT NULL,
    RegNumber    NVARCHAR(50)  NULL,
    CreatedDate  DATETIME NOT NULL DEFAULT GETDATE()
);

CREATE TABLE dbo.[Client] (
    UserID      INT PRIMARY KEY REFERENCES dbo.[User](UserID),
    ClientType  NVARCHAR(30) NOT NULL DEFAULT 'Individual',   -- Individual, Business
    BusinessID  INT NULL REFERENCES dbo.Business(BusinessID)
);

CREATE TABLE dbo.[Driver] (
    LicenseNumber      NVARCHAR(50) NULL,
    UserID             INT PRIMARY KEY REFERENCES dbo.[User](UserID),
    EmployeeNumber     NVARCHAR(50) NULL,
    AvailabilityStatus NVARCHAR(30) NOT NULL DEFAULT 'Available'   -- Available, On Route, Off Duty
);

CREATE TABLE dbo.[Vehicle] (
    VehicleID          INT IDENTITY(1,1) PRIMARY KEY,
    DriverID           INT NULL REFERENCES dbo.[Driver](UserID),
    VehicleType        NVARCHAR(50) NULL,
    RegistrationNumber NVARCHAR(30) NULL
);

CREATE TABLE dbo.[Route] (
    RouteID       INT IDENTITY(1,1) PRIMARY KEY,
    RouteName     NVARCHAR(120) NOT NULL,
    DriverID      INT NULL REFERENCES dbo.[Driver](UserID),
    VehicleID     INT NULL REFERENCES dbo.[Vehicle](VehicleID),
    RouteDate     DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    Status        NVARCHAR(30) NOT NULL DEFAULT 'Planned',   -- Planned, Active, Completed
    CompletedDate DATETIME NULL,
    ServiceArea   NVARCHAR(60) NULL
        CONSTRAINT CK_Route_ServiceArea CHECK (ServiceArea IN
            ('Metro-JHB', 'Metro-CPT', 'Metro-DBN', 'Metro-PE', 'Regional-Outlying', 'Express-Priority')),
    CreatedBy     INT NULL REFERENCES dbo.[User](UserID),
    UpdatedBy     INT NULL REFERENCES dbo.[User](UserID),
    UpdatedDate   DATETIME NULL
);

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
    ReceiverName      NVARCHAR(150) NULL,
    ReceiverPhone     NVARCHAR(30)  NULL,
    OriginCity        NVARCHAR(60)  NULL,
    DestCity          NVARCHAR(60)  NULL,
    Cost              DECIMAL(10,2) NOT NULL DEFAULT 0,
    RouteID           INT NULL REFERENCES dbo.[Route](RouteID),
    SortCategory      NVARCHAR(60)  NULL,
    WarehouseStatus   NVARCHAR(30)  NOT NULL DEFAULT 'Pending',    -- Pending, Received, Sorted, Dispatched
    ExceptionType     NVARCHAR(20)  NOT NULL DEFAULT 'None',       -- None, Lost, Damaged
    AttemptCount      INT           NOT NULL DEFAULT 0,
    LastStatusChange  DATETIME      NULL,
    ParcelLengthCm    DECIMAL(6,1)  NULL,
    ParcelWidthCm     DECIMAL(6,1)  NULL,
    ParcelHeightCm    DECIMAL(6,1)  NULL,
    PaymentGateStatus NVARCHAR(20)  NOT NULL DEFAULT 'NotRequired'
        CONSTRAINT CK_Shipment_PaymentGateStatus CHECK (PaymentGateStatus IN
            ('NotRequired', 'PaymentRequired', 'PaymentPending', 'PaymentSuccessful'))
);

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

CREATE TABLE dbo.[Rating] (
    RatingID    INT IDENTITY(1,1) PRIMARY KEY,
    ClientID    INT NULL REFERENCES dbo.[User](UserID),
    DriverID    INT NULL REFERENCES dbo.[Driver](UserID),
    Comment     NVARCHAR(250) NULL,
    RatingScore INT NOT NULL DEFAULT 5,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
);

CREATE TABLE dbo.[Payment] (
    PaymentID       INT IDENTITY(1,1) PRIMARY KEY,
    ClientID        INT NULL REFERENCES dbo.[User](UserID),
    PaymentStatus   NVARCHAR(30) NOT NULL DEFAULT 'Unpaid'
        CONSTRAINT CK_Payment_PaymentStatus CHECK (PaymentStatus IN
            ('Unpaid', 'Pending', 'Paid', 'Failed', 'Cancelled')),
    PaymentMethod   NVARCHAR(30) NULL,
    ReferenceNumber NVARCHAR(50) NULL,
    InvoiceNumber   NVARCHAR(30) NOT NULL UNIQUE,
    ShipmentID      INT NULL REFERENCES dbo.[Shipment](ShipmentID),
    Amount          DECIMAL(10,2) NOT NULL DEFAULT 0,
    IssuedDate      DATETIME NOT NULL DEFAULT GETDATE(),
    DueDate         DATETIME NULL,
    IdempotencyKey  NVARCHAR(100) NULL
);

CREATE TABLE dbo.[Notification] (
    NotificationID INT IDENTITY(1,1) PRIMARY KEY,
    UserID         INT NOT NULL REFERENCES dbo.[User](UserID),
    ShipmentID     INT NULL REFERENCES dbo.[Shipment](ShipmentID),
    Message        NVARCHAR(400) NOT NULL,
    Type           NVARCHAR(30) NULL,   -- Registration, Dispatch, Delivery, Delay, System
    IsRead         BIT NOT NULL DEFAULT 0,
    CreatedDate    DATETIME NOT NULL DEFAULT GETDATE(),
    DedupeKey      NVARCHAR(120) NULL
);

/* Race-free invoice numbering. */
CREATE TABLE dbo.NumberSequence (
    SeqName   NVARCHAR(40) NOT NULL PRIMARY KEY,
    NextValue INT          NOT NULL DEFAULT 0
);

/* Website enquiries from Contact.aspx. */
CREATE TABLE dbo.ContactMessage (
    MessageID   INT IDENTITY(1,1) PRIMARY KEY,
    FullName    NVARCHAR(150)  NOT NULL,
    Email       NVARCHAR(150)  NULL,
    Message     NVARCHAR(2000) NOT NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    HandledBy   INT NULL REFERENCES dbo.[User](UserID),
    HandledDate DATETIME NULL
);

/* Which invoices one bulk payment settled, and how much of each. */
CREATE TABLE dbo.PaymentAllocation (
    AllocationID   INT IDENTITY(1,1) PRIMARY KEY,
    PaymentID      INT NOT NULL REFERENCES dbo.[Payment](PaymentID),
    AmountApplied  DECIMAL(10,2) NOT NULL,
    AppliedBy      INT NOT NULL REFERENCES dbo.[User](UserID),
    AppliedDate    DATETIME NOT NULL DEFAULT GETDATE(),
    BatchKey       NVARCHAR(100) NULL
);

/* Audit trail for manual and administrative actions. */
CREATE TABLE dbo.AuditLog (
    AuditID     INT IDENTITY(1,1) PRIMARY KEY,
    UserID      INT NULL REFERENCES dbo.[User](UserID),
    Action      NVARCHAR(60)   NOT NULL,
    Entity      NVARCHAR(60)   NOT NULL,
    EntityID    NVARCHAR(60)   NULL,
    Details     NVARCHAR(1000) NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
);

/* Live-map positions: the latest driver / customer location per shipment. */
CREATE TABLE dbo.LocationPing (
    PingID      INT IDENTITY(1,1) PRIMARY KEY,
    ShipmentID  INT NOT NULL REFERENCES dbo.[Shipment](ShipmentID),
    Role        NVARCHAR(20) NOT NULL,          -- Driver, Customer
    Lat         DECIMAL(9,6) NOT NULL,
    Lng         DECIMAL(9,6) NOT NULL,
    UpdatedDate DATETIME NOT NULL DEFAULT GETDATE()
);
GO

/* ---------- Indexes (same names as Schema.sql, so the app's start-up check finds them) ---------- */
CREATE INDEX IX_Notification_User_Unread
    ON dbo.[Notification](UserID, IsRead, CreatedDate DESC);

CREATE UNIQUE INDEX UX_Payment_IdempotencyKey
    ON dbo.[Payment](IdempotencyKey) WHERE IdempotencyKey IS NOT NULL;

CREATE UNIQUE INDEX UX_LocationPing_Shipment_Role
    ON dbo.LocationPing(ShipmentID, Role);

CREATE NONCLUSTERED INDEX IX_Shipment_ShipmentDate
    ON dbo.[Shipment](ShipmentDate) INCLUDE (DeliveryStatus, SenderID, DriverID, DestCity, ExceptionType);

CREATE NONCLUSTERED INDEX IX_Shipment_DeliveryDate
    ON dbo.[Shipment](DeliveryDate) INCLUDE (DeliveryStatus, SenderID, DriverID);

CREATE NONCLUSTERED INDEX IX_Shipment_Sender_Date
    ON dbo.[Shipment](SenderID, ShipmentDate) INCLUDE (DeliveryStatus, DeliveryDate);

CREATE NONCLUSTERED INDEX IX_Shipment_Driver_Status
    ON dbo.[Shipment](DriverID, DeliveryStatus) INCLUDE (DeliveryDate, ShipmentDate, LastStatusChange);

CREATE NONCLUSTERED INDEX IX_Payment_IssuedDate
    ON dbo.[Payment](IssuedDate) INCLUDE (Amount, PaymentStatus, ClientID, ShipmentID);

CREATE NONCLUSTERED INDEX IX_Payment_Client_Date
    ON dbo.[Payment](ClientID, IssuedDate) INCLUDE (Amount, PaymentStatus);

CREATE NONCLUSTERED INDEX IX_TrackingHistory_Status_Time
    ON dbo.[TrackingHistory](Status, [Timestamp]) INCLUDE (ShipmentID, DriverID);
GO

/* ============================================================
   DATA - one transaction: either everything loads or nothing does.
   ============================================================ */
SET XACT_ABORT ON;
BEGIN TRANSACTION;

/* ---------- 1. People: every login in CREDENTIALS.md ----------
   Passwords are PBKDF2 (100,000 iterations, a different random salt
   per user), exactly what the app's PasswordHelper writes. The plain
   password is shown in the comment on each row. */
SET IDENTITY_INSERT dbo.[User] ON;
INSERT INTO dbo.[User] (UserID, FullName, Address, PhoneNumber, Email, Role, Password) VALUES
    -- Admin and operations staff
    (  1, N'System Administrator', N'Isando, Gauteng',                  '0110000001', 'admin@fcl.co.za',       'Admin',    'PBKDF2|100000|SKIb9NfE4lORUp3mJl14FA==|DbPuDxJkn5ZuWGm/CTx5BaiXV/hym6zjDlGKtHs1ElU='),  -- Admin@123
    (  2, N'Thandi Mokoena',       N'Central Depot, JHB',               '0110000003', 'staff@fcl.co.za',       'Staff',    'PBKDF2|100000|xPEt/ddDgLsnRThzbeClTA==|QHTx56Dm6g6RgnNug4vLTPL4H1YVTEYw+7ISuD0yIkI='),  -- Dispatch@123
    -- Drivers (Staff with a dbo.[Driver] row)
    (  3, N'Sipho Dlamini',        N'Soweto, JHB',                      '0110000005', 'driver@fcl.co.za',      'Staff',    'PBKDF2|100000|XlEBj74/uQxgrdrez660Xg==|yz0w3FDwMxxeHiYMTjwH3oqalpy2Y+qoGJhazbxuzAI='),  -- Driver@123
    (  4, N'Ishmael Zakwe',        N'Tembisa, JHB',                     '0110000007', 'driver2@fcl.co.za',     'Staff',    'PBKDF2|100000|pYaLRCGdq77T60LWP89zsA==|XFERUBGMuJxufkvJjld7+Y7OqXoX+Xf+WHQSJsg+pts='),  -- Driver@123
    -- Main demo customer
    (  5, N'Nolwazi Khoza',        N'12 Main Rd, Forte Town',           '0821234567', 'customer@fcl.co.za',    'Customer', 'PBKDF2|100000|3mXoXuaGcuaIbjgom23nCQ==|cDkS/trg5IeJSLBYOGj7QByj0JIJAy6juW8IMXt4ddA='),  -- Customer@123
    -- More drivers
    (  6, N'Bongani Nkosi',        N'Operations Depot, Johannesburg',   '0110000003', 'driver3@fcl.co.za',     'Staff',    'PBKDF2|100000|eamkDHQ0Ga778J1EC/koYg==|xdmHRVuBGKAIB+2JRi1tkh/RkN3eXPupqITnc7/iIUM='),  -- Driver@123
    (  7, N'Pieter van Wyk',       N'Operations Depot, Johannesburg',   '0110000004', 'driver4@fcl.co.za',     'Staff',    'PBKDF2|100000|v/pgIKoJ8dJAlpprYt0g5w==|G7dLi+CsMZrQkRL1HbHeUIhndOhs6JA83ApxmLNZQpM='),  -- Driver@123
    (  8, N'Fatima Patel',         N'Operations Depot, Johannesburg',   '0110000005', 'driver5@fcl.co.za',     'Staff',    'PBKDF2|100000|aKzJNFR70INqfOSTYOkyNQ==|FUzBjhdE5ySzMSg0LYg9V1s1UkAjd2o+Y/ni94J11Sc='),  -- Driver@123
    (  9, N'Lerato Modise',        N'Operations Depot, Johannesburg',   '0110000006', 'driver6@fcl.co.za',     'Staff',    'PBKDF2|100000|t2Hie5eqse5Piu1+Zfvnew==|NRHVZXttsVhAT5PpaTzr6mitrLCWHYspQU012FV9pDY='),  -- Driver@123
    ( 10, N'Johan Botha',          N'Operations Depot, Johannesburg',   '0110000007', 'driver7@fcl.co.za',     'Staff',    'PBKDF2|100000|nQi2hGj9WdRmmoOU8u4SXA==|/cA1YQwfkwbg3l6xWh4X+cVapCzuon/pCxV+/Mxtbaw='),  -- Driver@123
    ( 11, N'Themba Zulu',          N'Operations Depot, Johannesburg',   '0110000008', 'driver8@fcl.co.za',     'Staff',    'PBKDF2|100000|qduwp7nyOz6+Ccn4LqN7Fw==|KKVHIHrkdUgdvpkg9YMcPYvDJLCsPf+i0taGq4JGYg8='),  -- Driver@123
    -- Customers who own the demo shipments
    ( 12, N'Sipho Mahlangu',       N'14 Oak Ave, Johannesburg',         '0820000037', 'customer1@fcl.co.za',   'Customer', 'PBKDF2|100000|zSBzdet18p8xP4YCAU3bTg==|i1XjVFy05Z1LCIc+tsTHKDoIor0kamjgA5IkbTf7UZg='),  -- Customer@123
    ( 13, N'Naledi Ndlovu',        N'27 Main Rd, Johannesburg',         '0820000074', 'customer2@fcl.co.za',   'Customer', 'PBKDF2|100000|xSD7pTMh/lVlBPGKCm+t+w==|iAbLBNjYTiNDU3urM7ER3s2+MKgGO+X4MS+t6qj5qpI='),  -- Customer@123
    ( 14, N'Kabelo Pretorius',     N'40 Church St, Johannesburg',       '0820000111', 'customer3@fcl.co.za',   'Customer', 'PBKDF2|100000|gwFonvrvm7zFRH7EBXCJJQ==|p4Yaz5/OXmLliluWlQTOoyWbn2Q6r7Qp6uXi2IXnSJc='),  -- Customer@123
    ( 15, N'Zanele Khumalo',       N'53 Kerk St, Johannesburg',         '0820000148', 'customer4@fcl.co.za',   'Customer', 'PBKDF2|100000|+9kHr2dhlbSO3ge01znvNw==|r7PmQKgW99MsKQ6i+vNGTW6dRxdRmfV3k+AVRinVGpo='),  -- Customer@123
    ( 16, N'Thabo Naidoo',         N'66 Beach Rd, Johannesburg',        '0820000185', 'customer5@fcl.co.za',   'Customer', 'PBKDF2|100000|438JNiKNJj04rvN7Pj4BaA==|RyampLPYvsK5tiC9BSk7IPt+uz378uB3TzQKEl01/Eg='),  -- Customer@123
    ( 17, N'Aisha Mokoena',        N'79 Oak Ave, Johannesburg',         '0820000222', 'customer6@fcl.co.za',   'Customer', 'PBKDF2|100000|PjIzy39FXfb1CyqOB5VFHw==|3k6xnkXaZRkKHbxXjhzHHI6FCKl9N89CUSm1PCszWWQ='),  -- Customer@123
    ( 18, N'Lwazi Jacobs',         N'92 Main Rd, Johannesburg',         '0820000259', 'customer7@fcl.co.za',   'Customer', 'PBKDF2|100000|lrDZvaEMBAvGdjQ8F5nmtg==|Mb3C2u5sXvtXXgU0wJJrOf2D/nNBc2+QvHSpr9rLL5w='),  -- Customer@123
    ( 19, N'Palesa Sithole',       N'10 Church St, Johannesburg',       '0820000296', 'customer8@fcl.co.za',   'Customer', 'PBKDF2|100000|l+UNYRSr9DE0dVAuO92vFw==|2MUu8zFDbX4WSh08htLy0nvtk8R13OInWizo/A52/HE='),  -- Customer@123
    ( 20, N'Riaan Mahlangu',       N'23 Kerk St, Johannesburg',         '0820000333', 'customer9@fcl.co.za',   'Customer', 'PBKDF2|100000|91O6Teb3+I7B66SvoVHFPw==|iUxzjmIFQSrU0Sfj7aDhytjoqRtUiWOUiPzftJmb1ns='),  -- Customer@123
    ( 21, N'Nandi Ndlovu',         N'36 Beach Rd, Johannesburg',        '0820000370', 'customer10@fcl.co.za',  'Customer', 'PBKDF2|100000|leAezanihLjg5lR0ndJ/1g==|xBsBMs6cANzkXhvV9SS7LxMrZTT+StcmnrOs1zb9pSs='),  -- Customer@123
    ( 22, N'Sipho Pretorius',      N'49 Oak Ave, Johannesburg',         '0820000407', 'customer11@fcl.co.za',  'Customer', 'PBKDF2|100000|aRHISXXJGcl67GbNtAsjiw==|pT12+/q3Yj5zdTCmEnJqy+wojWPSe0CmzexWbGK7r+o='),  -- Customer@123
    ( 23, N'Naledi Khumalo',       N'62 Main Rd, Johannesburg',         '0820000444', 'customer12@fcl.co.za',  'Customer', 'PBKDF2|100000|YIfoZgzIn5Ng2CpbsAWHcw==|nSu1kOknfMLuKiDjIlKdZZy5f8NUU8eUxdCFeyrzQFU='),  -- Customer@123
    ( 24, N'Kabelo Naidoo',        N'75 Church St, Johannesburg',       '0820000481', 'customer13@fcl.co.za',  'Customer', 'PBKDF2|100000|pQtrQsmCfDqIIOdyIGF6PQ==|qwVfvP4PPM+gP6J3YOyFla5WUxTLkA8+Fe8HEqKsTE0='),  -- Customer@123
    ( 25, N'Zanele Mokoena',       N'88 Kerk St, Johannesburg',         '0820000518', 'customer14@fcl.co.za',  'Customer', 'PBKDF2|100000|JOwasABgPLoUwTIvUI2oIQ==|4YYnQ3mciHJGgG+uhulF0WRIzn7X3f+pJ190h2aQ8p4='),  -- Customer@123
    ( 26, N'Thabo Jacobs',         N'6 Beach Rd, Johannesburg',         '0820000555', 'customer15@fcl.co.za',  'Customer', 'PBKDF2|100000|4a4QKQutB4Mhb8fxj/IvuQ==|Oe1JwxoCdx9Gk2ohtJLMEHmIyzxj3ABCLB4j4IqLMEA='),  -- Customer@123
    ( 27, N'Aisha Sithole',        N'19 Oak Ave, Johannesburg',         '0820000592', 'customer16@fcl.co.za',  'Customer', 'PBKDF2|100000|/sdycVJcPxBx6Br8bAKdgg==|H65vC7+8n2Q7AhsTHd9dsqC+whFjIjJXuz8OXaAMqws='),  -- Customer@123
    ( 28, N'Lwazi Mahlangu',       N'32 Main Rd, Johannesburg',         '0820000629', 'customer17@fcl.co.za',  'Customer', 'PBKDF2|100000|P2iHtcxEy4AWZaqGPrqB5g==|756e5Oa9Nh6FVkosDzyUwmeiY3Fz2/1pqf3uDr6qmQA='),  -- Customer@123
    ( 29, N'Palesa Ndlovu',        N'45 Church St, Johannesburg',       '0820000666', 'customer18@fcl.co.za',  'Customer', 'PBKDF2|100000|ZuiYkcafmgEgvrSPWNe5Dg==|G36zIeckbT6Z3VRqRkXaLbCuVNR/w41Vc56zbNj1wFI='),  -- Customer@123
    ( 30, N'Riaan Pretorius',      N'58 Kerk St, Johannesburg',         '0820000703', 'customer19@fcl.co.za',  'Customer', 'PBKDF2|100000|6jpq11mGZkeSLIqWvTQByw==|1zM905WFnqHR6Jo+3r2i4FICg1fH1cyfDrMaF9DkmcU='),  -- Customer@123
    ( 31, N'Nandi Khumalo',        N'71 Beach Rd, Johannesburg',        '0820000740', 'customer20@fcl.co.za',  'Customer', 'PBKDF2|100000|HewYuoDbDYTWIO5FFYQaBw==|1AdSOiUwy6X9YnsVvFieEj3y4ztIZMWKrDY2k+4PvwM='),  -- Customer@123
    ( 32, N'Sipho Naidoo',         N'84 Oak Ave, Johannesburg',         '0820000777', 'customer21@fcl.co.za',  'Customer', 'PBKDF2|100000|t/oePJ+YiSO6cyHkZOQVeg==|FyZPIfjoOs4QYHCBGYs6vu/r++RhDaS9cIsUo9jn+O8='),  -- Customer@123
    ( 33, N'Naledi Mokoena',       N'2 Main Rd, Johannesburg',          '0820000814', 'customer22@fcl.co.za',  'Customer', 'PBKDF2|100000|xJMKaanm7W4dk5IEy6m+pQ==|rH6v8O7UH+g1gwtnh18c9dOH0rhlK2Om2hgu4k4KNMc='),  -- Customer@123
    ( 34, N'Kabelo Jacobs',        N'15 Church St, Johannesburg',       '0820000851', 'customer23@fcl.co.za',  'Customer', 'PBKDF2|100000|mSWce7SrkrOtw5dVs3WxHg==|v8yXEfMclZRJ2mH8SAgK9PAG9d8GdeSJe+xHHAwnT9c='),  -- Customer@123
    ( 35, N'Zanele Sithole',       N'28 Kerk St, Johannesburg',         '0820000888', 'customer24@fcl.co.za',  'Customer', 'PBKDF2|100000|0gfTUf2nUHZlpm6tmgKxnA==|T9F30pUq4YlX+MdbT6Hr1EWtvUHcO4lCbsbAR1sL8Jw='),  -- Customer@123
    ( 36, N'Thabo Mahlangu',       N'41 Beach Rd, Johannesburg',        '0820000925', 'customer25@fcl.co.za',  'Customer', 'PBKDF2|100000|fezfu/Wnwn/ZOknude6NrA==|s9qlVWCrh2Esbs9JIxbKC/EkaMfiu7JqYcKkuOete7k='),  -- Customer@123
    ( 37, N'Aisha Ndlovu',         N'54 Oak Ave, Johannesburg',         '0820000962', 'customer26@fcl.co.za',  'Customer', 'PBKDF2|100000|/wijAK2AUCvtMDj5DFsTIw==|ek0EDc+RGRGm2tdlNBKDB/5PZhbFakhusRkMJlqW6uQ='),  -- Customer@123
    ( 38, N'Lwazi Pretorius',      N'67 Main Rd, Johannesburg',         '0820000999', 'customer27@fcl.co.za',  'Customer', 'PBKDF2|100000|x428kwuE9PKut32O3bsaFA==|fUw9Y4PIrE8Ku4jCMwXJO6+rFLP0iUubOcu++kLU81s='),  -- Customer@123
    ( 39, N'Palesa Khumalo',       N'80 Church St, Johannesburg',       '0820001036', 'customer28@fcl.co.za',  'Customer', 'PBKDF2|100000|Sp3T8rQviqjgRlnro3zklg==|0uhLyRy1zdmi+WVWyZrYSUkOklDL3qMu2oN4wSKnr9Q='),  -- Customer@123
    ( 40, N'Riaan Naidoo',         N'93 Kerk St, Johannesburg',         '0820001073', 'customer29@fcl.co.za',  'Customer', 'PBKDF2|100000|0CI/OLUsFqVjTyNifTZFBw==|SFrOAHzSSlDVl0qVJiBl1/ShQcdA7ulB6yUXeOm52L8='),  -- Customer@123
    ( 41, N'Nandi Mokoena',        N'11 Beach Rd, Johannesburg',        '0820001110', 'customer30@fcl.co.za',  'Customer', 'PBKDF2|100000|ycu1q2C+/lAJTAmlwXw+XA==|DtYlTBMdU9D7QPebLV9m3Dc5N9y91BVN2JwlJJj/mdY=');   -- Customer@123
SET IDENTITY_INSERT dbo.[User] OFF;

/* Every internal user (Admin or Staff, drivers included) has an employee record. */
SET IDENTITY_INSERT dbo.[Admin] ON;
INSERT INTO dbo.[Admin] (EmployeeID, UserID, Department) VALUES
    ( 1,  1, 'IT / Systems'),
    ( 2,  2, 'Operations'),
    ( 3,  3, 'Operations'),
    ( 4,  4, 'Operations'),
    ( 5,  6, 'Operations'),
    ( 6,  7, 'Operations'),
    ( 7,  8, 'Operations'),
    ( 8,  9, 'Operations'),
    ( 9, 10, 'Operations'),
    (10, 11, 'Operations');
SET IDENTITY_INSERT dbo.[Admin] OFF;

/* The driver registry - this table, not the Role column, is what makes someone a driver. */
INSERT INTO dbo.[Driver] (UserID, LicenseNumber, EmployeeNumber, AvailabilityStatus) VALUES
    ( 3, 'DL-8841022', 'EMP-1005', 'Available'),   -- driver@
    ( 4, 'DL-7712556', 'EMP-1007', 'Available'),   -- driver2@
    ( 6, 'DL-0000006', 'EMP-1006', 'Available'),   -- driver3@
    ( 7, 'DL-0000007', 'EMP-1007', 'Available'),   -- driver4@
    ( 8, 'DL-0000008', 'EMP-1008', 'Available'),   -- driver5@
    ( 9, 'DL-0000009', 'EMP-1009', 'Available'),   -- driver6@
    (10, 'DL-0000010', 'EMP-1010', 'Available'),   -- driver7@
    (11, 'DL-0000011', 'EMP-1011', 'Available');   -- driver8@

/* Customer accounts. Business clients are invoiced; Individuals pay before dispatch. */
INSERT INTO dbo.[Client] (UserID, ClientType) VALUES
    ( 5, 'Business'),                                         -- customer@
    (12, 'Business'),   (13, 'Individual'), (14, 'Individual'), (15, 'Individual'), (16, 'Individual'),
    (17, 'Individual'), (18, 'Business'),   (19, 'Individual'), (20, 'Individual'), (21, 'Individual'),
    (22, 'Individual'), (23, 'Individual'), (24, 'Business'),   (25, 'Individual'), (26, 'Individual'),
    (27, 'Individual'), (28, 'Individual'), (29, 'Individual'), (30, 'Business'),   (31, 'Individual'),
    (32, 'Individual'), (33, 'Individual'), (34, 'Individual'), (35, 'Individual'), (36, 'Business'),
    (37, 'Individual'), (38, 'Individual'), (39, 'Individual'), (40, 'Individual'), (41, 'Individual');

/* ---------- 2. Fleet and routes ---------- */
SET IDENTITY_INSERT dbo.[Vehicle] ON;
INSERT INTO dbo.[Vehicle] (VehicleID, DriverID, VehicleType, RegistrationNumber) VALUES
    (1,  3, 'Panel Van',   'FCL 001 GP'),
    (2,  4, '1-Ton Truck', 'FCL 002 GP'),
    (3,  6, 'Panel Van',   'FCL 006 GP'),
    (4,  7, '1-Ton Truck', 'FCL 007 GP'),
    (5,  8, 'Motorcycle',  'FCL 008 GP'),
    (6,  9, 'Panel Van',   'FCL 009 GP'),
    (7, 10, '1-Ton Truck', 'FCL 010 GP'),
    (8, 11, 'Motorcycle',  'FCL 011 GP');
SET IDENTITY_INSERT dbo.[Vehicle] OFF;

/* Two active routes per destination service area, each with its own driver and vehicle. */
DECLARE @today DATE = CAST(GETDATE() AS DATE);
SET IDENTITY_INSERT dbo.[Route] ON;
INSERT INTO dbo.[Route] (RouteID, RouteName, DriverID, VehicleID, RouteDate, Status, ServiceArea) VALUES
    (1, 'Metro-CPT Run 1',          3, 1, @today,                   'Active', 'Metro-CPT'),
    (2, 'Metro-CPT Run 2',          4, 2, DATEADD(day, -1, @today), 'Active', 'Metro-CPT'),
    (3, 'Metro-DBN Run 1',          6, 3, DATEADD(day, -2, @today), 'Active', 'Metro-DBN'),
    (4, 'Metro-DBN Run 2',          7, 4, DATEADD(day, -3, @today), 'Active', 'Metro-DBN'),
    (5, 'Metro-PE Run 1',           8, 5, DATEADD(day, -4, @today), 'Active', 'Metro-PE'),
    (6, 'Metro-PE Run 2',           9, 6, DATEADD(day, -5, @today), 'Active', 'Metro-PE'),
    (7, 'Regional-Outlying Run 1', 10, 7, DATEADD(day, -6, @today), 'Active', 'Regional-Outlying'),
    (8, 'Regional-Outlying Run 2', 11, 8, @today,                   'Active', 'Regional-Outlying');
SET IDENTITY_INSERT dbo.[Route] OFF;

/* ---------- 3. The three original sample shipments (customer@) ---------- */
INSERT INTO dbo.[Shipment]
    (SenderID, DriverID, PickupAddress, DeliveryAddress, ParcelWeight, ParcelDescription, Barcode,
     TrackingNumber, ShipmentDate, DeliveryDate, DeliveryStatus, ReceiverName, ReceiverPhone,
     OriginCity, DestCity, Cost, SortCategory, WarehouseStatus, ExceptionType)
VALUES
    (5, 3,    '12 Main Rd, Forte Town', '88 Long St, Cape Town', 3.5, 'Boxed clothing order', 'FCL-10000001',
     'FCL-10000001', DATEADD(day, -4, GETDATE()), DATEADD(day, -2, GETDATE()), 'Delivered', 'Nomvula Sithole', '0837654321',
     'Johannesburg', 'Cape Town', 172.00, 'Metro-CPT', 'Dispatched', 'None'),
    (5, 4,    '12 Main Rd, Forte Town', '5 Beach Rd, Durban', 8.0, 'Fragile ceramics', 'FCL-10000002',
     'FCL-10000002', DATEADD(day, -1, GETDATE()), NULL, 'InTransit', 'Peter Adams', '0845551212',
     'Johannesburg', 'Durban', 253.00, 'Metro-DBN', 'Dispatched', 'Damaged'),
    (5, NULL, '12 Main Rd, Forte Town', '9 Church St, Bloemfontein', 1.2, 'Documents', 'FCL-10000003',
     'FCL-10000003', GETDATE(), NULL, 'Registered', 'Lerato M', '0829998877',
     'Johannesburg', 'Bloemfontein', 145.00, NULL, 'Pending', 'None');

INSERT INTO dbo.[TrackingHistory] (ShipmentID, Location, Status, Remarks)
SELECT s.ShipmentID, t.Loc, t.St, t.Rm
FROM (VALUES
        (1, 'FCL-10000001', 'Johannesburg Depot', 'Registered', 'Parcel registered at intake'),
        (2, 'FCL-10000001', 'Cape Town',          'Delivered',  'Delivered - signed by recipient'),
        (3, 'FCL-10000002', 'Johannesburg Depot', 'Registered', 'Parcel registered at intake'),
        (4, 'FCL-10000002', 'Dispatch',           'InTransit',  'Allocated to delivery route and dispatched'),
        (5, 'FCL-10000003', 'Johannesburg Depot', 'Registered', 'Parcel registered at intake')
     ) t(Seq, Tracking, Loc, St, Rm)
JOIN dbo.[Shipment] s ON s.TrackingNumber = t.Tracking
ORDER BY t.Seq;

INSERT INTO dbo.[Payment] (ClientID, PaymentStatus, PaymentMethod, ReferenceNumber, InvoiceNumber, ShipmentID, Amount)
SELECT 5, 'Paid', 'EFT', 'PAYREF001', 'INV-20260001', ShipmentID, 172.00
FROM dbo.[Shipment] WHERE TrackingNumber = 'FCL-10000001';

/* ---------- 4. 198 demo shipments across the full lifecycle ----------
     150  Delivered           FCL-20000001 .. FCL-20000150
      20  Upcoming            FCL-20000151 .. FCL-20000170  (Registered / Sorted)
      15  Awaiting collection FCL-20000171 .. FCL-20000185  (Assigned)
      13  Out for delivery    FCL-20000186 .. FCL-20000198  (OutForDelivery)
   Each one is tied to a customer (round-robin over all 31), a route for
   its destination area (and that route's driver), a parcel size, an
   invoice, tracking history and - for a third of the delivered ones - a
   driver rating. */
SELECT UserID, (ROW_NUMBER() OVER (ORDER BY UserID) - 1) AS idx
  INTO #cust FROM dbo.[User] WHERE Role = 'Customer';
DECLARE @C INT = (SELECT COUNT(*) FROM #cust);

SELECT RouteID, ServiceArea, DriverID,
       (ROW_NUMBER() OVER (PARTITION BY ServiceArea ORDER BY RouteID) - 1) AS idx
  INTO #rte FROM dbo.[Route];

DECLARE @cities TABLE (idx INT, City NVARCHAR(60), Area NVARCHAR(60), Lat DECIMAL(9,6), Lng DECIMAL(9,6));
INSERT INTO @cities VALUES
    (0, 'Cape Town',    'Metro-CPT',         -33.924870, 18.424055),
    (1, 'Durban',       'Metro-DBN',         -29.858680, 31.021840),
    (2, 'Gqeberha',     'Metro-PE',          -33.960800, 25.602200),
    (3, 'Bloemfontein', 'Regional-Outlying', -29.085200, 26.159900),
    (4, 'Pretoria',     'Regional-Outlying', -25.747900, 28.229300),
    (5, 'East London',  'Regional-Outlying', -33.015300, 27.911600);

DECLARE @sizes TABLE (idx INT, Nm NVARCHAR(30), Wt DECIMAL(10,2), L DECIMAL(6,1), W DECIMAL(6,1), H DECIMAL(6,1));
INSERT INTO @sizes VALUES
    (0, 'Small', 2, 25, 20, 10), (1, 'Medium', 5, 40, 30, 20),
    (2, 'Large', 10, 60, 40, 30), (3, 'Extra-Large', 25, 80, 60, 50);

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
    CASE WHEN g.Status IN ('Delivered', 'Assigned', 'OutForDelivery') THEN rt.DriverID ELSE NULL END,
    '12 Main Rd, Forte Town, Johannesburg',
    CAST(((g.n * 7) % 90) + 1 AS VARCHAR(4)) + ' '
      + CHOOSE((g.n % 5) + 1, 'Long St', 'Smith St', 'Govan Mbeki Ave', 'Maitland St', 'Oxford Rd') + ', ' + ci.City,
    sz.Wt,
    CHOOSE((g.n % 6) + 1, 'Documents', 'Boxed clothing', 'Electronics', 'Books', 'Spare parts', 'Homeware'),
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
    CHOOSE((g.n % 8) + 1, 'Nomvula Sithole', 'Peter Adams', 'Lerato Mphela', 'Kyle Daniels',
                          'Aisha Khan', 'Sibusiso Ngema', 'Megan Fourie', 'Tshepo Radebe'),
    '083' + RIGHT('0000000' + CAST(g.n * 53 AS VARCHAR(10)), 7),
    'Johannesburg', ci.City,
    145 + CASE WHEN sz.Wt > 2 THEN (sz.Wt - 2) * 18 ELSE 0 END,
    CASE WHEN g.Status IN ('Delivered', 'Assigned', 'OutForDelivery') THEN rt.RouteID ELSE NULL END,
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
                 WHERE ServiceArea = ci.Area AND idx = ((g.n - 1) / 6) % 2) rt
ORDER BY g.n;   -- ShipmentIDs follow the waybill numbers

/* One invoice per demo shipment: paid once it has left the depot, unpaid before. */
;WITH P AS (
    SELECT s.ShipmentID, s.SenderID, s.Cost, s.DeliveryStatus, s.ShipmentDate,
           ROW_NUMBER() OVER (ORDER BY s.ShipmentID) AS rn
    FROM dbo.[Shipment] s
    WHERE s.TrackingNumber LIKE 'FCL-2000%'
)
INSERT INTO dbo.[Payment] (ClientID, PaymentStatus, PaymentMethod, ReferenceNumber,
                           InvoiceNumber, ShipmentID, Amount, IssuedDate, DueDate)
SELECT P.SenderID,
       CASE WHEN P.DeliveryStatus IN ('Delivered', 'OutForDelivery', 'Assigned') THEN 'Paid' ELSE 'Unpaid' END,
       CASE WHEN P.DeliveryStatus IN ('Delivered', 'OutForDelivery', 'Assigned')
            THEN CHOOSE((P.rn % 3) + 1, 'Card', 'EFT', 'Wallet') ELSE NULL END,
       CASE WHEN P.DeliveryStatus IN ('Delivered', 'OutForDelivery', 'Assigned')
            THEN 'PAYSEED-' + RIGHT('0000' + CAST(P.rn AS VARCHAR(6)), 4) ELSE NULL END,
       'INV-2000' + RIGHT('0000' + CAST(P.rn AS VARCHAR(6)), 4),
       P.ShipmentID, P.Cost, P.ShipmentDate, DATEADD(day, 7, P.ShipmentDate)
FROM P
ORDER BY P.rn;

/* Tracking history: every lifecycle stage each parcel has reached so far. */
INSERT INTO dbo.[TrackingHistory] (ShipmentID, DriverID, Location, Status, Remarks, [Timestamp])
SELECT s.ShipmentID, s.DriverID, t.Loc, t.St, t.Rm, DATEADD(hour, t.Ord, s.ShipmentDate)
FROM dbo.[Shipment] s
CROSS APPLY (VALUES
    ('Johannesburg Depot',          'Registered',     'Parcel registered at intake',           0),
    ('Johannesburg Warehouse',      'Sorted',         'Sorted to ' + s.SortCategory + ' lane', 2),
    ('Dispatch Bay, Johannesburg',  'Assigned',       'Allocated to a delivery route',         4),
    ('En route to ' + s.DestCity,   'OutForDelivery', 'Parcel collected - out for delivery',   6),
    (s.DestCity,                    'Delivered',      'Delivered - signed by recipient',      48)
) t(Loc, St, Rm, Ord)
WHERE s.TrackingNumber LIKE 'FCL-2000%'
  AND ( t.St = 'Registered'
     OR (t.St = 'Sorted'         AND s.DeliveryStatus IN ('Sorted', 'Assigned', 'OutForDelivery', 'Delivered'))
     OR (t.St = 'Assigned'       AND s.DeliveryStatus IN ('Assigned', 'OutForDelivery', 'Delivered'))
     OR (t.St = 'OutForDelivery' AND s.DeliveryStatus IN ('OutForDelivery', 'Delivered'))
     OR (t.St = 'Delivered'      AND s.DeliveryStatus = 'Delivered') )
ORDER BY s.ShipmentID, t.Ord;

/* Driver ratings on a third of the delivered parcels. */
INSERT INTO dbo.[Rating] (ClientID, DriverID, Comment, RatingScore, CreatedDate)
SELECT s.SenderID, s.DriverID,
       CHOOSE((s.ShipmentID % 4) + 1, 'Fast and professional.', 'Parcel arrived safely.',
                                      'Great service, thank you.', 'On time - will use again.'),
       3 + (s.ShipmentID % 3),
       s.DeliveryDate
FROM dbo.[Shipment] s
WHERE s.TrackingNumber LIKE 'FCL-2000%' AND s.DeliveryStatus = 'Delivered'
  AND s.DriverID IS NOT NULL AND s.ShipmentID % 3 = 0
ORDER BY s.ShipmentID;

/* A mid-route driver position for every out-for-delivery parcel. The live map only trusts
   positions from the last 15 minutes, so after that it falls back to its simulated drive. */
INSERT INTO dbo.LocationPing (ShipmentID, Role, Lat, Lng, UpdatedDate)
SELECT s.ShipmentID, 'Driver', (-26.204100 + ci.Lat) / 2.0, (28.047300 + ci.Lng) / 2.0, GETDATE()
FROM dbo.[Shipment] s JOIN @cities ci ON ci.City = s.DestCity
WHERE s.TrackingNumber LIKE 'FCL-2000%' AND s.DeliveryStatus = 'OutForDelivery'
ORDER BY s.ShipmentID;

/* Drivers carrying an out-for-delivery parcel are On Route. */
UPDATE dbo.[Driver] SET AvailabilityStatus = 'On Route'
WHERE UserID IN (SELECT DISTINCT DriverID FROM dbo.[Shipment]
                 WHERE DeliveryStatus = 'OutForDelivery' AND DriverID IS NOT NULL);

/* This year's invoice counter starts after the highest seeded invoice for this year,
   so the first new booking never collides with demo data. */
DECLARE @seq NVARCHAR(40) = 'INV-' + CAST(YEAR(GETDATE()) AS NVARCHAR(4));
INSERT INTO dbo.NumberSequence (SeqName, NextValue)
SELECT @seq, ISNULL(MAX(TRY_CAST(RIGHT(InvoiceNumber, 4) AS INT)), 0)
FROM dbo.[Payment] WHERE InvoiceNumber LIKE @seq + '%';

DROP TABLE #cust;
DROP TABLE #rte;

COMMIT TRANSACTION;
GO

/* ============================================================
   CHECK - fails loudly if anything is missing
   ============================================================ */
DECLARE @problems NVARCHAR(MAX) = N'';

IF (SELECT COUNT(*) FROM dbo.[User]) <> 41                                  SET @problems += N' users<>41;';
IF (SELECT COUNT(*) FROM dbo.[User] WHERE Role = 'Admin') <> 1              SET @problems += N' admins<>1;';
IF (SELECT COUNT(*) FROM dbo.[Driver]) <> 8                                 SET @problems += N' drivers<>8;';
IF (SELECT COUNT(*) FROM dbo.[Client]) <> 31                                SET @problems += N' customers<>31;';
IF (SELECT COUNT(*) FROM dbo.[Vehicle]) <> 8                                SET @problems += N' vehicles<>8;';
IF (SELECT COUNT(*) FROM dbo.[Route]) <> 8                                  SET @problems += N' routes<>8;';
IF (SELECT COUNT(*) FROM dbo.[Shipment]) <> 201                             SET @problems += N' shipments<>201;';
IF (SELECT COUNT(*) FROM dbo.[Payment]) <> 199                              SET @problems += N' invoices<>199;';
IF (SELECT COUNT(*) FROM dbo.LocationPing) <> 13                            SET @problems += N' pings<>13;';
IF EXISTS (SELECT 1 FROM dbo.[User] WHERE Password NOT LIKE 'PBKDF2|100000|%') SET @problems += N' unsalted password;';
IF EXISTS (SELECT 1 FROM dbo.[User] u WHERE u.Role IN ('Admin', 'Staff')
             AND NOT EXISTS (SELECT 1 FROM dbo.[Admin] a WHERE a.UserID = u.UserID)) SET @problems += N' staff without employee row;';
IF EXISTS (SELECT 1 FROM dbo.[User] u WHERE u.Role = 'Customer'
             AND NOT EXISTS (SELECT 1 FROM dbo.[Client] c WHERE c.UserID = u.UserID)) SET @problems += N' customer without client row;';
IF EXISTS (SELECT 1 FROM dbo.[Shipment] WHERE TrackingNumber LIKE 'FCL-2000%'
             AND DeliveryStatus IN ('Delivered', 'Assigned', 'OutForDelivery') AND (DriverID IS NULL OR RouteID IS NULL))
                                                                            SET @problems += N' dispatched parcel without driver/route;';

SELECT 'Users' AS [What], COUNT(*) AS [Rows] FROM dbo.[User]
UNION ALL SELECT '  Admin',     COUNT(*) FROM dbo.[User] WHERE Role = 'Admin'
UNION ALL SELECT '  Staff (incl. drivers)', COUNT(*) FROM dbo.[User] WHERE Role = 'Staff'
UNION ALL SELECT '  Customers', COUNT(*) FROM dbo.[User] WHERE Role = 'Customer'
UNION ALL SELECT 'Drivers',          COUNT(*) FROM dbo.[Driver]
UNION ALL SELECT 'Vehicles',         COUNT(*) FROM dbo.[Vehicle]
UNION ALL SELECT 'Routes',           COUNT(*) FROM dbo.[Route]
UNION ALL SELECT 'Shipments',        COUNT(*) FROM dbo.[Shipment]
UNION ALL SELECT 'Invoices',         COUNT(*) FROM dbo.[Payment]
UNION ALL SELECT 'Tracking events',  COUNT(*) FROM dbo.[TrackingHistory]
UNION ALL SELECT 'Ratings',          COUNT(*) FROM dbo.[Rating]
UNION ALL SELECT 'Live-map pings',   COUNT(*) FROM dbo.LocationPing;

IF LEN(@problems) > 0
    RAISERROR(N'Seed check FAILED:%s', 16, 1, @problems);
ELSE
    SELECT N'Seed check passed - Logisticore is ready to back up.' AS [Result];
GO

SET NOEXEC OFF;   -- re-enable execution if the safety guard switched it off
GO

/* ============================================================
   BACKUP BLOCK (you) - select from DECLARE to the last PRINT and press F5.

   Writes Logisticore.bak to your instance's default Backup folder, e.g.
   C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\Backup\
   and then checks the file can be read back. Copy that file to the team.
   (No compression: SQL Server Express cannot make compressed backups.)

DECLARE @dir  NVARCHAR(400) = ISNULL(CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(400)),
                                     CAST(SERVERPROPERTY('InstanceDefaultDataPath')   AS NVARCHAR(400)));
IF RIGHT(@dir, 1) <> '\' SET @dir += '\';
DECLARE @file NVARCHAR(400) = @dir + N'Logisticore.bak';
BACKUP DATABASE Logisticore TO DISK = @file
    WITH COPY_ONLY, INIT, FORMAT, CHECKSUM, STATS = 10,
         NAME = N'Logisticore - demo data for testing';
RESTORE VERIFYONLY FROM DISK = @file WITH CHECKSUM;
PRINT 'Backup written to: ' + @file;

   ============================================================ */

/* ============================================================
   RESTORE BLOCK (team members) - put the .bak in a folder SQL Server
   can read (its own Backup folder is safest), set @bak to that path,
   then select from DECLARE to the last PRINT and press F5.

   This REPLACES any existing Logisticore database on that instance.

DECLARE @bak  NVARCHAR(400) = N'C:\Program Files\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQL\Backup\Logisticore.bak';
DECLARE @data NVARCHAR(400) = CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS NVARCHAR(400));
DECLARE @log  NVARCHAR(400) = ISNULL(CAST(SERVERPROPERTY('InstanceDefaultLogPath') AS NVARCHAR(400)), @data);
IF RIGHT(@data, 1) <> '\' SET @data += '\';
IF RIGHT(@log, 1)  <> '\' SET @log  += '\';
DECLARE @mdf NVARCHAR(400) = @data + N'Logisticore.mdf';
DECLARE @ldf NVARCHAR(400) = @log  + N'Logisticore_log.ldf';
USE master;
IF DB_ID(N'Logisticore') IS NOT NULL
    ALTER DATABASE Logisticore SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE Logisticore FROM DISK = @bak
    WITH MOVE N'Logisticore'     TO @mdf,
         MOVE N'Logisticore_log' TO @ldf,
         REPLACE, RECOVERY, STATS = 10;
DECLARE @restoreError INT = @@ERROR;
IF DB_ID(N'Logisticore') IS NOT NULL
    ALTER DATABASE Logisticore SET MULTI_USER;
IF @restoreError <> 0
    PRINT 'Restore FAILED - the reason is in the red message above. Check that @bak points at the .bak file and that SQL Server can read that folder (its own Backup folder is safest).';
ELSE
    PRINT 'Logisticore restored. Sign in with the logins in CREDENTIALS.md.';

   ============================================================ */
