# Logisticore Courier Service — System Overview

> **Project:** FortCarrier & Logistics (assembly/namespace `ForteLogisticsApp`, product name **Logisticore Courier Service**)
> **Stack:** ASP.NET Web Forms, C#, .NET Framework 4.8, SQL Server Express (catalog `Logisticore`), Tailwind-styled UI (LogistiCore V5 design), Material Symbols icons.
> **Data access:** Hand-rolled ADO.NET helper (`Data/Db.cs`), 100% parameterised queries. No EF/ORM.
> **Last reviewed:** 2026-10-01

This document describes the whole system: what works, what is intentionally not wired up, the end-to-end workflow, how each role navigates, and a narrative for every use case.

---

## 1. What the system is

Logisticore is a courier / parcel-logistics web application. It covers the full parcel lifecycle:

**Book → Pay (online individuals) → Register/Intake → Warehouse sort → Route plan & dispatch → Driver delivery → Invoice → Settle → Report.**

It is a single ASP.NET Web Forms project. Every page inherits a shared master (`Site1.Master`) that renders role-aware navigation, and most authenticated pages inherit `Security/SecurePage` which enforces role access on `Page_Load`.

### 1.1 Roles (exactly three)

The `CK_User_Role` constraint on `dbo.[User]` permits only three roles:

| Role | Who | Scope |
|------|-----|-------|
| **Admin** | Administrators | Full administration, operations, finance, and all reports |
| **Staff** | Operations people (office + drivers) | Parcel registration, warehouse sorting, route planning, fleet, operational reports |
| **Customer** | Clients who ship parcels | Book, track, pay, view own invoices and profile |

**"Driver" is NOT a role.** A driver is a `Staff` user who *also* has a row in `dbo.[Driver]`. That table — not the `Role` column — is the driver registry, and is what every assignment query joins on. `SecurePage.IsDriver(userId)` checks membership and is what decides whether driver-only navigation appears. A driver is enrolled via the **"Is a driver"** checkbox in **Admin → Users**.

> Historical note: the system previously had seven roles (Manager, Dispatch, Warehouse, Finance, Driver, …). `Schema.sql` contains an idempotent migration collapsing those to the three above (Finance folded into Admin; Dispatch/Warehouse/Manager folded into Staff). Legacy accounts such as `manager@fcl.co.za` still exist and are now `Staff`.

### 1.2 Seed logins

Created on first run by `DbInitializer` (seeds only when `dbo.[User]` is empty). Passwords in the seed are legacy unsalted SHA-256 and are transparently upgraded to PBKDF2 on first successful login.

| Email | Password | Role | Notes |
|-------|----------|------|-------|
| `admin@fcl.co.za` | `Admin@123` | Admin | |
| `staff@fcl.co.za` | *(see Schema)* | Staff | Office operations |
| `driver@fcl.co.za` | `Driver@123` | Staff | Enrolled in `dbo.[Driver]` |
| `driver2@fcl.co.za` | `Driver@123` | Staff | Enrolled in `dbo.[Driver]` (2 drivers so Route Planning has a choice) |
| `customer@fcl.co.za` | `Customer@123` | Customer | Business client |

---

## 2. Architecture at a glance

```
Browser
  │
Site1.Master  ── role-aware chrome (Sidebar | TopNav | Bare), nav builder, bell, footer
  │
Pages (*.aspx + *.aspx.cs)
  │                         Security/
  ├── SecurePage ──────────  BasePage (CSRF via ViewStateUserKey), RequireRole, RequireOperationsStaff, IsDriver, IsSafeReturnUrl
  │                          PasswordHelper (PBKDF2 + legacy SHA-256 verify/upgrade, temp passwords)
  │
Services/                   (business logic, all static)
  ├── AuthService           login (lockout, session reset), register
  ├── CourierService        tracking numbers, quotes, invoice numbers, weight parsing (en-ZA)
  ├── SortingService        canonical cities & sort lanes, DestCity → lane mapping
  ├── ParcelSize            canonical parcel bands (weight/dims/price single source of truth)
  ├── PaymentGateway        SANDBOX charge simulator (swap-in point for a real PSP)
  ├── PaymentService        server-trusted payment application (transactional, idempotent)
  ├── AutomationService     event-driven automation (invoice, escalate, release drivers…)
  ├── NotificationService   in-app notifications (send, send-to-role, send-once/dedupe)
  ├── ReportExport          DataTable → CSV download
  └── UiHelper              HTML/JS output encoding, status badges
  │
Data/
  ├── Db.cs                 ADO.NET helper, parameterised, RunInTransaction
  └── DbInitializer.cs      replays Schema.sql batches on startup (safety net + idempotent upgrades)
  │
App_Data/Schema.sql        schema + seed + idempotent upgrade/migration batches
```

### 2.1 Security posture (implemented and working)

- **SQL injection:** all access goes through `Db` with name/value parameters; values are never concatenated. `Db.AddParams` throws loudly on misuse.
- **CSRF:** `BasePage` binds encrypted ViewState to a per-session token (`ViewStateUserKey`), protecting even anonymous forms (login, contact, register).
- **Passwords:** PBKDF2-HMAC-SHA1, 16-byte salt, 100k iterations, constant-time compare. Legacy SHA-256 hashes verified and auto-upgraded on login.
- **Login lockout:** 5 failed attempts per session → 5-minute lockout (`AuthService`).
- **Open-redirect guard:** `IsSafeReturnUrl` rejects `//evil.com`, `/\evil`, schemes, backslashes.
- **Stored XSS:** `UiHelper.Enc` / `JsEnc` encode at output; data-bind expressions encode user text.
- **ViewState** signed + encrypted with a fixed `machineKey`; `enableEventValidation` on.
- **Error handling:** `customErrors=RemoteOnly`; `Global.Application_Error` logs the real exception to the Event Log and shows the user only a reference code — SQL text never reaches the browser.
- **Headers:** `X-Content-Type-Options`, `X-Frame-Options=SAMEORIGIN`, `Referrer-Policy`, `X-Powered-By` removed, HttpOnly session cookie.

> For a real deployment, regenerate the `machineKey` in `Web.config` and enable `requireSSL`.

---

## 3. Data model

Core tables (`App_Data/Schema.sql`). ERD attributes first, application extensions after.

| Table | Purpose | Key columns / notes |
|-------|---------|---------------------|
| `User` | All accounts | `Role` ∈ {Customer, Staff, Admin} (CK constraint) |
| `Admin` | Internal-employee rows | every Staff/Admin user gets one |
| `Client` | Customer extension | `ClientType` ∈ {Individual, Business}; optional `BusinessID` |
| `Driver` | **Driver registry** | PK/FK = `UserID`; `AvailabilityStatus` ∈ {Available, On Route, Off Duty} |
| `Vehicle` | Fleet | optional `DriverID` |
| `Route` | Delivery route | `ServiceArea` (one sort lane), `Status` ∈ {Planned, Active, Completed}, audit cols |
| `Shipment` | The parcel | lifecycle statuses below |
| `TrackingHistory` | Per-parcel event log | location, status, remarks, timestamp |
| `Payment` | Invoices & payments | `InvoiceNumber` (unique), `PaymentStatus`, `IdempotencyKey`, `DueDate` |
| `PaymentAllocation` | Bulk-pay allocation | ties one payment across several invoices (`BatchKey`) |
| `Business` | Business billing group | shares/jointly settles invoices |
| `Notification` | In-app messages | `DedupeKey`, `IsRead`, indexed by (User, IsRead, CreatedDate) |
| `Rating` | Customer→driver rating | score + comment |
| `ContactMessage` | Website enquiries | from `Contact.aspx` |
| `AuditLog` | Manual-action audit | bulk payments, route/service-area overrides |
| `NumberSequence` | Race-free invoice numbering | replaced `COUNT(*)+1` |

### 3.1 Shipment status fields (three independent axes)

A shipment carries **three separate status columns** so distinct concerns never get conflated:

1. **`DeliveryStatus`** — where it is in the journey: `Registered → Sorted → Assigned → OutForDelivery → Delivered` (or `Failed`). *(Seed data also uses `InTransit`.)*
2. **`WarehouseStatus`** — physical warehouse state: `Pending → Received → Sorted → Dispatched`.
3. **`PaymentGateStatus`** — payment-before-dispatch gate: `NotRequired | PaymentRequired | PaymentPending | PaymentSuccessful`.

Plus `ExceptionType` ∈ {None, Lost, Damaged}, `AttemptCount`, `LastStatusChange`, `SortCategory`, dimensions, cost, origin/dest cities.

### 3.2 Service areas / sort lanes (single source of truth)

`SortingService` defines the canonical cities and the six sort lanes:
`Metro-JHB, Metro-CPT, Metro-DBN, Metro-PE, Regional-Outlying, Express-Priority`.
`CategoryFor(destCity)` deterministically maps destination city → lane. A route is pinned to one `ServiceArea`, and Route Planning refuses to assign a parcel whose `SortCategory` doesn't match the route's area.

---

## 4. The payment-before-dispatch gate

This is central to how bookings flow, so it is called out explicitly.

- **Online Individual bookings** → `PaymentGateStatus = PaymentRequired`. They **cannot be sorted or dispatched** until paid. The customer pays via `Customer/Payment.aspx` → `PaymentService.PayForShipment` → sandbox gateway → status becomes `PaymentSuccessful`.
- **Counter/walk-in registrations** (Ops) and **Business clients** → `PaymentGateStatus = NotRequired`. Business is invoiced and settles later via Finance/Customer invoices rather than being gated.
- Warehouse and Route Planning only admit shipments whose `PaymentGateStatus` is `NotRequired` or `PaymentSuccessful`.

`PaymentService` is server-trusted: amounts always come from the database (never the browser), everything runs inside one transaction, and an `IdempotencyKey` makes a double-click / resubmit a no-op instead of a second charge.

---

## 5. Automation (event-driven, idempotent)

`AutomationService` advances state off actions staff already perform, so nobody has to remember. Every method is idempotent.

| Trigger | Automation |
|---------|-----------|
| Parcel marked **Delivered** (driver) | `EnsureInvoice` raises an unpaid invoice (due +7 days) if none exists; `RequestRating` invites the customer to rate the driver |
| Any delivery outcome | `CompleteFinishedRoutes` + `ReleaseIdleDrivers` free drivers whose routes are done |
| Warehouse flags **Lost/Damaged** | `EscalateException` notifies Staff (and Admin for Lost), and sends the customer a calm "there's a delay" message (deduped) |
| **Failed delivery** below cap (3) | `EscalateFailedDelivery` returns parcel to the dispatch pool (`Sorted`, clears route/driver) so it reappears on Route Planning; notifies Staff |
| Failed delivery **at cap** | marks `Failed` and escalates to Staff for a human decision |

**Deliberately kept manual** (each is a human/contractual decision): marking Delivered, declaring Lost/Damaged, taking payment / marking Paid, and assigning parcels to a driver.

---

## 6. System workflow (end-to-end)

```
                    ┌─────────────── CUSTOMER ───────────────┐
                    │                                         │
   Register ──► Login ──► Book Shipment (wizard)              │
                            │  (Individual online)            │
                            ▼                                 │
                    PaymentGateStatus = PaymentRequired       │
                            │                                 │
                    Pay (sandbox gateway) ──► PaymentSuccessful│
                    └────────────────────────┬────────────────┘
                                             │
   Ops counter walk-in ──► Register Parcel ──┤ (PaymentGateStatus = NotRequired)
                                             │
                                             ▼
                           ┌──────────── STAFF / OPS ────────────┐
                           │  Warehouse: Receive → Sort (lane)    │  Delivery = Registered→Sorted
                           │  (flag Lost/Damaged → auto-escalate) │  Warehouse = Received→Sorted
                           │                                      │
                           │  Route Planning:                     │
                           │   create Route (ServiceArea, driver, │
                           │   vehicle) → assign sorted parcels    │  Delivery = Assigned
                           │   whose SortCategory matches area     │  Warehouse = Dispatched
                           │   → driver set On Route               │
                           └───────────────┬──────────────────────┘
                                           ▼
                           ┌──────────── DRIVER (Staff) ─────────┐
                           │  My Manifest: pick up assigned       │  Delivery = OutForDelivery
                           │  Confirm Delivery: scan + outcome    │
                           │    ├─ Delivered ─► auto-invoice +     │  Delivery = Delivered
                           │    │               rating request     │
                           │    └─ Failed ─► requeue (<3) or       │  Delivery = Sorted | Failed
                           │                 escalate (=3)         │
                           └───────────────┬──────────────────────┘
                                           ▼
                           ┌──────────── FINANCE (Admin) ────────┐
                           │  Invoices: auto/manual raise, mark   │  Payment = Unpaid→Paid
                           │  Paid, bulk-settle business invoices │
                           └───────────────┬──────────────────────┘
                                           ▼
                           Reports (Parcel movement, Driver perf,
                           Monthly revenue, Lost & Damaged) + CSV export
```

Notifications are generated throughout (registration, dispatch, delivery, delay, payment) and surface in the nav bell + `Notifications.aspx`.

---

## 7. User navigation (per role)

Navigation is built in `Site1.Master.cs` → `BuildNav()`. The master chooses one of three "chromes":

- **Bare** — Login, Register (no nav; focused card).
- **TopNav** — `Default.aspx` (landing) and `Contact.aspx` (public marketing bar). Always shows public links even if signed in; signed-in users reach their portal from the avatar cluster.
- **Sidebar** — every authenticated portal page.

### Public (signed-out, or landing/contact)
Home · Services · Track Parcel · Get a Quote · Contact Us

### Customer
Dashboard · Book Shipment · Track · My Invoices · My Profile
*(plus a "Create Order" shortcut in the top chrome — customers only)*

### Staff — office (NOT a driver)
**Operations:** Dashboard · Register Parcel · Warehouse · Route Planning · Fleet
**Reports:** Reports *(single entry → Daily Parcel Movement; folder-highlighted)*

### Staff — driver (has a `dbo.[Driver]` row)
**My Deliveries:** My Manifest (Driver Dashboard) · Confirm Delivery
**Reports:** Reports

> A driver sees **only** driver links — never Route Planning/Warehouse/Fleet/Registration. This is enforced both in the nav *and* server-side by `RequireOperationsStaff()`, which admits Admin or Staff but redirects a Staff user who is a driver. So a driver cannot assign routes/vehicles/parcels (to themselves or anyone) even by typing the URL.

### Admin
**Administration:** Dashboard · Shipments · Users · Fleet
**Operations:** Ops Dashboard · Register Parcel · Warehouse · Route Planning
**Finance:** Finance · Billing
**Reports:** Parcel Movement · Driver Performance · Monthly Revenue · Lost & Damaged

---

## 8. Page inventory & access control

Role gates come from `RequireRole(...)` / `RequireOperationsStaff()` in each `Page_Load`.

| Page | Access gate | Purpose |
|------|-------------|---------|
| `Default.aspx` | Public (TopNav) | Marketing landing: hero, services, public track, quote calculator |
| `Contact.aspx` | Public | Enquiry form → `ContactMessage` |
| `Login.aspx` | Public (Bare) | Sign in; lockout; safe returnUrl |
| `Register.aspx` | Public (Bare) | Self-service Customer sign-up |
| `AccessDenied.aspx` / `Error.aspx` | — | 403 / generic error pages |
| `Notifications.aspx` | Any authenticated | In-app notifications list, mark read |
| `Invoice.aspx` | Admin, Customer | Printable single invoice (view/print in same tab) |
| **Customer/** | | |
| `Dashboard.aspx` | Customer | Shipment counts, outstanding balance |
| `BookShipment.aspx` | Customer | Multi-step booking wizard → sets payment gate |
| `Track.aspx` | Customer | Track own parcels + tracking history + rate driver |
| `Invoices.aspx` | Customer | Own invoices; pay |
| `Payment.aspx` | Customer | Pay-before-dispatch (sandbox gateway) |
| `Profile.aspx` | Customer | Edit own details |
| **Ops/** | | |
| `Dashboard.aspx` | Admin, Staff | Operational KPIs by status |
| `ParcelRegistration.aspx` | Operations staff¹ | Counter/walk-in intake (NotRequired gate) |
| `Warehouse.aspx` | Operations staff¹ | Receive & sort to lane; flag Lost/Damaged |
| `RoutePlanning.aspx` | Operations staff¹ | Create routes, assign sorted parcels, dispatch |
| **Driver/** | | |
| `Dashboard.aspx` | Admin, Staff | "My Manifest": assigned parcels, pick up |
| `DeliveryConfirmation.aspx` | Admin, Staff | Scan + confirm Delivered/Failed |
| **Admin/** | | |
| `Dashboard.aspx` | Admin | Admin KPIs |
| `Shipments.aspx` | Admin | Search/browse all shipments |
| `Users.aspx` | Admin | Create/manage users, enrol drivers, businesses |
| `Fleet.aspx` | Operations staff¹ | Vehicles & driver availability |
| **Finance/** | | |
| `Dashboard.aspx` | Admin | Paid/outstanding totals, revenue |
| `Invoices.aspx` | Admin | Raise/auto-raise invoices, mark Paid, bulk-settle |
| **Reports/** | | |
| `DailyParcelMovement.aspx` | Admin, Staff | Daily movement + CSV |
| `DriverPerformance.aspx` | Operations staff¹ | Deliveries/failures per driver + CSV |
| `MonthlyRevenue.aspx` | **Admin only** | Revenue by month + CSV |
| `LostDamaged.aspx` | Admin, Staff | Exception report + CSV |

¹ *Operations staff = `RequireOperationsStaff()`: Admin or Staff, but never a driver.*

---

## 9. Use cases & narratives

### UC-1 — Customer registers an account
**Actor:** Visitor → Customer.
**Narrative:** From the landing page a visitor opens **Register**, enters full name, email, phone, address and a password (≥6 chars, confirmed). `AuthService.Register` creates a `User` with role `Customer` and a matching `Client` row, hashing the password with PBKDF2. On success they are signed in (or sent to Login). Email uniqueness is enforced by the DB; a duplicate shows a friendly error. CSRF is covered because even this anonymous form binds ViewState to the session.

### UC-2 — User logs in
**Actor:** Any user.
**Narrative:** On **Login**, credentials go to `AuthService.Login`. After 5 failed attempts in one session the account is made to wait 5 minutes. On success the session is reset (new CSRF token so pre-login ViewState can't be replayed), `UserID/Role/FullName` are stored, and legacy SHA-256 passwords are silently re-hashed to PBKDF2. A `returnUrl` is honoured only if `IsSafeReturnUrl` passes (no open redirect). The user lands on their role's dashboard.

### UC-3 — Customer books a shipment (online, Individual)
**Actor:** Customer.
**Narrative:** The **Book Shipment** wizard collects, step by step: parcel size band (`ParcelSize` — the single source for weight/dimensions/price), origin & destination city, pickup/delivery addresses, and receiver details. A live quote is computed by `CourierService`. On submit, a `Shipment` is created with a unique `FCL-########` tracking number, `DeliveryStatus = Registered`, `WarehouseStatus = Pending`, derived `SortCategory` from destination city, and — because this is an online Individual booking — **`PaymentGateStatus = PaymentRequired`**. The customer is notified and prompted to pay. The parcel will not progress until paid.

### UC-4 — Customer pays before dispatch
**Actor:** Customer.
**Narrative:** On **Payment** (or from **My Invoices**), the customer picks a method. `PaymentService.PayForShipment` re-validates ownership and that the shipment is genuinely `PaymentRequired` with exactly one unpaid `Payment`, mints/uses an idempotency key, and calls the **sandbox** `PaymentGateway.Charge`. On success, inside one transaction it marks the payment Paid, sets `PaymentGateStatus = PaymentSuccessful`, and writes tracking/audit history; a repeated submit returns the original result (no double charge). The parcel is now eligible for warehouse sorting. (Business clients skip this gate and are invoiced instead.)

### UC-5 — Staff registers a walk-in parcel (counter intake)
**Actor:** Operations staff.
**Narrative:** On **Register Parcel**, staff capture sender, receiver, addresses, a free-text weight (parsed with en-ZA comma/point tolerance), and destination. A shipment is created with `PaymentGateStatus = NotRequired` (counter parcels are paid/handled differently), a tracking number, and a derived sort lane. The sender is notified. The parcel enters the warehouse pipeline immediately.

### UC-6 — Warehouse receiving & sorting
**Actor:** Operations staff.
**Narrative:** On **Warehouse**, staff see parcels cleared for handling (gate = NotRequired or PaymentSuccessful). They mark a parcel **Received**, then **Sort** it — the sort lane is derived deterministically from the destination city, so there's nothing to pick by hand. Sorting moves `WarehouseStatus → Sorted` and `DeliveryStatus → Sorted`. If a parcel is found **Lost** or **Damaged**, staff flag it; `AutomationService.EscalateException` notifies Staff (and Admin for Lost) once, and sends the customer a calm delay notice.

### UC-7 — Route planning & dispatch
**Actor:** Operations staff (never a driver).
**Narrative:** On **Route Planning**, staff create a **Route** with a mandatory **Service Area** (one sort lane), an available driver, and a vehicle. The page lists sorted, unassigned, payment-cleared parcels; staff assign those whose `SortCategory` matches the route's `ServiceArea` (mismatches are refused — an East London parcel can't go on a Cape Town route). Assignment sets `RouteID`, `DeliveryStatus = Assigned`, `WarehouseStatus = Dispatched`, flips the driver to **On Route**, and notifies each sender. Route creation/changes are audited (`CreatedBy/UpdatedBy`).

### UC-8 — Driver picks up the manifest
**Actor:** Driver (Staff + `Driver` row).
**Narrative:** The driver opens **My Manifest** (Driver Dashboard), which shows parcels assigned to them (`DeliveryStatus = Assigned`). Marking a parcel picked up sets `DeliveryStatus = OutForDelivery`, stamps `CollectedDate`, and notifies the sender it's on the way. The driver sees their own delivered/failed counts.

### UC-9 — Driver confirms a delivery outcome
**Actor:** Driver.
**Narrative:** On **Confirm Delivery**, the driver scans/enters the tracking number and records the outcome with notes:
- **Delivered** → `DeliveryStatus = Delivered`; `AutomationService.OnDeliveryOutcome` auto-raises the invoice (`EnsureInvoice`, due +7 days) and requests a customer rating. The sender is notified.
- **Failed** → `EscalateFailedDelivery`. Below 3 attempts, the parcel returns to the dispatch pool (`Sorted`, route/driver cleared) so it reappears on Route Planning automatically; at 3 attempts it is marked `Failed` and escalated to Staff for a decision.

In both cases, finished routes are completed and idle drivers are released back to **Available**.

### UC-10 — Customer tracks a parcel & rates the driver
**Actor:** Customer (also an anonymous public track on the landing page).
**Narrative:** On **Track**, the customer finds their parcel and sees its `TrackingHistory` timeline (location, status, remarks). Once delivered, they can submit a `Rating` (score + comment) for the driver. The public landing page offers a tracking box too (the Cards/Map view toggle beside it is **not wired up** — see §10).

### UC-11 — Finance raises & settles invoices
**Actor:** Admin (Finance).
**Narrative:** On **Finance → Invoices**, delivered shipments without an invoice can be invoiced individually (`EnsureInvoice`) or in bulk (`AutoInvoiceDelivered`). Admin marks invoices **Paid**. For business clients, several invoices can be settled in one **bulk payment** that is allocated across them (`PaymentAllocation` + `BatchKey`) inside a transaction, with an audit entry. The Finance Dashboard shows paid vs outstanding totals.

### UC-12 — Customer views & pays own invoices
**Actor:** Customer.
**Narrative:** On **My Invoices**, the customer sees their invoices (unpaid/paid, due dates) and can open a printable **Invoice** page or pay an outstanding one through the same server-trusted payment path.

### UC-13 — Admin manages users & enrols drivers
**Actor:** Admin.
**Narrative:** On **Admin → Users**, the admin creates internal users (Staff/Admin) with an auto-generated temporary password, assigns a department, and can tick **"Is a driver"** to insert a `dbo.[Driver]` row (availability defaults to Available) — which immediately gives that user driver navigation on their next page load (per-request cache, no re-login needed). Businesses can be created for grouped billing. New users are notified.

### UC-14 — Admin oversees shipments & fleet
**Actor:** Admin / Operations staff.
**Narrative:** **Admin → Shipments** searches all shipments by tracking number or city. **Fleet** lists vehicles and live driver availability, and lets staff add vehicles.

### UC-15 — Reporting & export
**Actor:** Admin (all reports) / Staff (operational subset).
**Narrative:** Reports cover **Daily Parcel Movement**, **Driver Performance**, **Monthly Revenue** (Admin only), and **Lost & Damaged**. Each renders a table and can be exported to CSV via `ReportExport.ToCsv`.

### UC-16 — Website enquiry
**Actor:** Visitor.
**Narrative:** The public **Contact** form stores enquiries in `ContactMessage` (previously discarded) for staff follow-up (`HandledBy/HandledDate`).

### UC-17 — Notifications
**Actor:** Any authenticated user.
**Narrative:** A nav bell shows the unread count; **Notifications** lists messages (Registration, Dispatch, Delivery, Delay, System). `SendOnce` de-duplicates by `DedupeKey` so automation can re-run without spamming.

---

## 10. What works vs. what is broken / not wired

### ✅ Working (implemented and functional)
- Authentication: register, login, lockout, PBKDF2 + legacy upgrade, safe redirect, logout.
- Role-aware navigation and server-side access control for all three roles, including the driver/office split.
- Full parcel lifecycle: booking wizard, counter registration, warehouse receive/sort, route planning with service-area validation, dispatch, driver pickup, delivery confirmation (success/failure/requeue).
- Payment-before-dispatch gate for online Individual customers; business invoicing path.
- Server-trusted, transactional, idempotent payments (single + bulk) via a sandbox gateway.
- Event-driven automation: auto-invoicing, exception/failed-delivery escalation, driver release, rating requests, de-duplicated notifications.
- Invoices (auto + manual + bulk settle), printable invoice page, finance dashboard.
- Reports (4) with CSV export.
- Contact enquiries persisted.
- DB auto-creation/seeding and idempotent schema upgrades/migrations on startup.
- A test suite (`Tests/`) covering password hashing, SecurePage role logic, parcel sizing/pricing, sorting, payment service, admin mark-paid, bulk invoice payment, and route service-area validation.

### 🚧 Intentionally not wired / known gaps
- **Landing-page "Cards / Map" view toggle** (`Default.aspx`): a visual placeholder, deliberately disabled (inert to click and keyboard) — not connected to anything. The only explicit "coming soon" element in the app.
- **Payment gateway is a SANDBOX simulator** (`PaymentGateway.cs`): no real card network, bank, or PSP is contacted and no money moves. It's the designed swap-in point for PayFast/Stripe/PayGate — only `Charge` would change. Including `FAIL` in a test note forces the decline path.
- **Notifications are in-app only** — no real SMS/email gateway (`NotificationService` simulates delivery by writing a row).
- **No scheduler/background job** — automation (completing routes, releasing drivers, auto-invoicing) runs opportunistically off user actions, not on a timer.
- **`machineKey` is a committed placeholder** and `requireSSL=false` — must be changed for production.
- **`_recovery/` folder** contains auto-recovered WebForms build artifacts (`App_Web_*.cs`, etc.), not part of the live app; ignore it.

### ⚠️ Minor inconsistencies to be aware of
- Seed/legacy data uses `DeliveryStatus = 'InTransit'`, while the live dispatch path uses `Assigned → OutForDelivery`. Status badges and reports handle both, but the two vocabularies coexist.
- `Schema.sql`'s header comment still lists the old seven seed logins; the actual seed block creates the five three-role accounts in §1.2 (plus the role-collapse migration for pre-existing databases).

---

## 11. Running the system

1. **Database:** either run `App_Data/Schema.sql` in SSMS against `.\SQLEXPRESS`, or just start the app — `Global.asax → DbInitializer.EnsureCreated()` creates and seeds an empty `Logisticore` database and replays idempotent upgrade batches on every startup.
2. **Connection string:** `Web.config` → `Logisticore` (`Data Source=.\SQLEXPRESS`). Change `Data Source` if your instance name differs.
3. **Run:** open `FortCarrierLogistics.sln` in Visual Studio and run (IIS Express), or build and host on IIS. .NET Framework 4.8 required.
4. **Sign in** with any seed account from §1.2.
5. **Tests:** `Tests/ForteLogisticsApp.Tests.csproj` (integration tests expect a reachable `Logisticore` DB).

---

*Generated as a living overview of the Logisticore / FortCarrier & Logistics codebase. Keep it in sync as the sandbox gateway, notification transport, or status vocabulary are finalised.*
