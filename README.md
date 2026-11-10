# Logisticore Courier Service

A courier and parcel-logistics web application built by **C Sharp Shooters**.
Customers book, pay for and track parcels. Operations staff register, sort and
route them. Drivers work a delivery manifest with a live map. Admins run the
users, fleet, finance and reports.

It covers the whole parcel lifecycle:

**Book → Pay → Register → Warehouse sort → Route plan & dispatch → Deliver → Invoice → Settle → Report**

| | |
|---|---|
| **Project / assembly** | `FortCarrier&Logistics.csproj`, namespace `ForteLogisticsApp` |
| **Stack** | ASP.NET Web Forms, C#, .NET Framework 4.8 |
| **Database** | SQL Server (Express or LocalDB), catalog `Logisticore` |
| **Data access** | Parameterised ADO.NET through `Data/Db.cs` (no ORM) |
| **UI** | One master page (`Site1.Master`), Tailwind (CDN), Material Symbols, Chart.js |
| **Live map** | Leaflet + OpenStreetMap, fed by `TrackApi.aspx` |
| **Tests** | MSTest (unit + database integration) in `Tests/` |

---

## Features

### Customers
- Book a shipment with a quote based on parcel size, weight and destination.
- Pay invoices through a **sandbox** payment gateway (card, EFT or wallet).
  Individual customers pay before the parcel enters the warehouse.
- Track any parcel by waybill number, with a live map of the driver's position.
- View invoices, delivery history and profile, and rate the driver after delivery.
- Individual and Business customers get different dashboards. Business clients
  see every invoice for their business and can pay several at once.

### Operations staff
- Register parcels at the counter and print a waybill label.
- Receive and sort parcels into warehouse lanes by destination city.
- Plan routes per service area, assign a driver and vehicle, and dispatch.
- Driver and vehicle availability rules stop double-booking the fleet.

### Drivers
A driver is a **Staff** user who also has a row in `dbo.[Driver]`.
- **My Manifest**: parcels to collect and deliver.
- Confirm delivery or record a failed attempt.
- **Live Map**: share a live location, or simulate a drive for demos.

### Admins
- Manage users (including enrolling drivers) and the fleet.
- See every shipment and mark invoices paid.
- Finance dashboard, invoice list and printable invoices.
- Decision-making reports, all exportable to CSV:
  Business Performance, Comparison (period vs period), Daily Parcel Movement,
  Driver Performance, Lost & Damaged, and Monthly Revenue.

### Everyone
- In-app notifications with a bell in the top bar.
- Self-service **Forgot password**, which issues a short-lived temporary
  password and forces a change on next login.

---

## Roles

The database allows exactly three roles:

| Role | Scope |
|------|-------|
| **Admin** | Administration, operations, finance and all reports |
| **Staff** | Parcel registration, warehouse, route planning, fleet, operational reports. Drivers are Staff. |
| **Customer** | Book, track, pay, own invoices and profile |

Every protected page inherits `Security/SecurePage`, which checks the role on
each request. The master page builds the navigation for the signed-in role.

---

## Getting started

Full step-by-step instructions, with troubleshooting, are in **[GUIDE.md](GUIDE.md)**.
In short:

1. **Install** Visual Studio 2022 with the *ASP.NET and web development*
   workload, SQL Server Express (instance `SQLEXPRESS`), and SQL Server
   Management Studio.
2. **Create the database.** Open `App_Data/Schema.sql` in SSMS, connected to
   `.\SQLEXPRESS`, and run it. It creates the `Logisticore` database, every
   table, the demo accounts and about 200 demo shipments. It is safe to run again.
3. **Check the connection string** in `Web.config` (name `Logisticore`). The
   default is:
   ```
   Data Source=.\SQLEXPRESS;Initial Catalog=Logisticore;Integrated Security=True;Encrypt=False
   ```
4. **Open** `FortCarrierLogistics.sln`, let NuGet restore packages, and press
   **F5**. The site runs in IIS Express.

On startup, `Data/DbInitializer.cs` replays the guarded upgrade batches in
`Schema.sql`, so an existing database picks up new tables and columns. It does
not create the database or load the demo data; step 2 does that.

> Keep the project in a short folder (for example `C:\Logisticore\`). Deep paths,
> such as ones under OneDrive, can exceed Windows' 260-character limit and break
> the NuGet restore.

### Demo logins

| Role | Email | Password |
|------|-------|----------|
| Admin | `admin@fcl.co.za` | `Admin@123` |
| Operations staff | `staff@fcl.co.za` | `Dispatch@123` |
| Driver | `driver@fcl.co.za` | `Driver@123` |
| Customer | `customer@fcl.co.za` | `Customer@123` |

Staff, admins and drivers sign in on the **Staff Portal** tab of the login page.
Try tracking `FCL-20000186` to see a driver moving on the live map.
**[CREDENTIALS.md](CREDENTIALS.md)** lists every account and waybill range.

---

## Running the tests

The `Tests/` project (`ForteLogisticsApp.Tests`, MSTest, net48) has two parts:

- `Tests/Unit/`: pure logic, such as validation, pricing, sorting, state
  machine, report rules and password hashing. No database needed.
- `Tests/Integration/`: run against SQL Server. They cover payments,
  dispatch concurrency, ownership (IDOR) checks, shipment transitions, fleet
  availability and temporary passwords.

> **The integration tests write data.** `Tests/App.config` points at
> `.\SQLEXPRESS` / `Logisticore` by default. Point it at a throwaway database,
> such as `(localdb)\MSSQLLocalDB`, before running them if you want to keep your
> demo data clean.

Run them from Visual Studio's **Test Explorer**, or from a Developer Command Prompt:

```bash
msbuild Tests\ForteLogisticsApp.Tests.csproj /restore /t:Build
```

```bash
vstest.console Tests\bin\Debug\net48\ForteLogisticsApp.Tests.dll
```

If the database can't be reached, the integration tests report **Skipped**
rather than failing. Check the skipped count, not just the overall result.

---

## Project structure

```
Admin/          Admin dashboard, users, fleet, all shipments
Customer/       Customer portal: dashboard, book, track, invoices, payment, profile, history
Driver/         Driver manifest, delivery confirmation, live map
Finance/        Finance dashboard and invoices
Ops/            Ops dashboard, parcel registration, warehouse sorting, route planning
Reports/        Decision-making reports (charts + CSV export)
Controls/       Shared user controls (delivery cards, report header/footer)
Services/       Business logic: auth, quotes, payments, sorting, routing, notifications, reports
Security/       SecurePage, CSRF, password hashing, input validation, throttling, state machine
Data/           Db.cs (ADO.NET helper), DbInitializer.cs, ownership-scoped queries
App_Data/       Schema.sql (schema + seed + upgrades), SEEDDATASCHEMA.sql (clean shareable copy)
Scripts/        livetrack.js (Leaflet map), lc-charts.js, vendored Chart.js
Styles/         Report stylesheet
Images/         Logos and site imagery
Tests/          MSTest unit and integration tests
Site1.Master    Shared layout and role-aware navigation
TrackApi.aspx   JSON endpoint for live map positions
```

---

## Security

- **SQL injection:** every query is parameterised through `Data/Db.cs`.
- **CSRF:** ViewState is tied to the session (`ViewStateUserKey`), and is signed
  and encrypted.
- **Passwords:** PBKDF2 with a per-user salt. Legacy SHA-256 seed hashes are
  upgraded on first login.
- **Brute force:** login lockout after repeated failures, and throttled
  password-reset requests.
- **Access control:** role checks on every page, and owner-scoped data access
  so customers only see their own parcels and invoices.
- **State changes:** shipment status changes go through a whitelisted,
  transactional path. Payments are idempotent and use server-side amounts.
  One-time submit tokens stop double bookings.
- **Output encoding** against XSS, CSV formula-injection guards on exports,
  safe return URLs, security headers, and errors logged server-side with only a
  reference code shown to the user.

---

## Known limitations

- **Payments are a sandbox.** `Services/PaymentGateway.cs` simulates charges; no
  real money moves. It is the swap-in point for a real payment provider.
- **No mail server.** Forgot password shows the temporary password on screen,
  so anyone who knows a customer's email can use it. A real deployment should
  email it instead.
- **Internet needed for some assets.** Map tiles, fonts and Tailwind load from
  the internet. Without it the app works, but the map and some styling won't show.
- **Before deploying publicly,** generate a new `machineKey` in `Web.config` and
  turn on `requireSSL`.

---

## Further documentation

| File | What's in it |
|------|--------------|
| [GUIDE.md](GUIDE.md) | Install and run on a new PC, with troubleshooting |
| [CREDENTIALS.md](CREDENTIALS.md) | All demo logins and tracking numbers |
| [OVERVIEW.md](OVERVIEW.md) | Architecture, data model, workflows and use cases in depth |
| [PROJECT-CONTEXT.md](PROJECT-CONTEXT.md) | Short project brief for contributors and reviewers |
| [DEMO-WALKTHROUGH.md](DEMO-WALKTHROUGH.md) | Day-in-the-life demo script |
