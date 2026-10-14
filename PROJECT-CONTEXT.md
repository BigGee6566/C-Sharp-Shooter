# PROJECT-CONTEXT

Shared baseline for team reviews (e.g. `/ecc:dev-team`) and new contributors.
Keep it short and declarative. For setup/run see `GUIDE.md`; for logins and
tracking numbers see `CREDENTIALS.md`.

## Project name and purpose

**Logisticore Courier Service** (project `FortCarrierLogistics`, namespace
`ForteLogisticsApp`). A courier/logistics management web app: customers book and
track parcels; drivers work a delivery manifest and a live map; operations staff
register, warehouse-sort and route parcels; admin manages users/fleet; finance
handles invoices; reports summarise movement, revenue and exceptions.

## Tech stack

- **ASP.NET Web Forms**, C#, **.NET Framework 4.8**; server-rendered `.aspx`
  pages with code-behind on a single master shell (`Site1.Master`), Tailwind
  (CDN) for styling, Leaflet + OpenStreetMap for the live map.
- **SQL Server** (Express / LocalDB). Data access is **raw parameterised
  ADO.NET** through a thin `Data/Db.cs` helper (`Query`/`NonQuery`/`Scalar`/
  `Insert` + `RunInTransaction(conn, tx)`). **No ORM.**
- Schema + seed live in one idempotent `App_Data/Schema.sql`, replayed by
  `Data/DbInitializer.cs` on startup (it applies guarded upgrade batches; it does
  not create the catalog or run the bulk demo seed).
- A JSON endpoint `TrackApi.aspx` + `Scripts/livetrack.js` drive the live map;
  positions are stored in `LocationPing`.
- Build/test realities: the session git worktree path exceeds Windows MAX_PATH,
  so **build in the main checkout**; test SQL on **LocalDB** (separate from the
  developer's `.\SQLEXPRESS\Logisticore`).

## Roles & domain model

- Exactly three roles (CK-constrained): **Customer / Staff / Admin**. A **driver
  is a Staff user with a `dbo.[Driver]` row** — membership of that table, not the
  Role column, is what grants driver access.
- Core entities: `User`, `Client`/`Business`, `Driver`, `Vehicle`, `Route`,
  `Shipment`, `Payment`/`PaymentAllocation`, `TrackingHistory`, `Rating`,
  `Notification`, `AuditLog`, `LocationPing`, `NumberSequence`.
- Shipment lifecycle status columns: `DeliveryStatus`
  (Registered → Sorted → Assigned → OutForDelivery → Delivered / Failed),
  `WarehouseStatus` (Pending → Received → Sorted → Dispatched),
  `PaymentGateStatus` (NotRequired / PaymentRequired / PaymentPending /
  PaymentSuccessful); plus `Route.Status` and `Driver.AvailabilityStatus`.

## Current phase

Active feature work and hardening on a working app. Recently delivered: a
driver/customer **live tracking map** (Leaflet + `TrackApi.aspx` + `LocationPing`),
**richer sandbox payment entry** (Card / EFT bank details / Wallet, masked
storage), and a **~200-shipment demo seed**. Earlier fixes covered a login
portal crash, driver dead-end navigation, and a typeable booking city.

## Key constraints

- The **payment flow is the reference standard** for correctness discipline
  (idempotency key, server-trusted amounts, transactions, ownership checks,
  `UPDLOCK` + `@@ROWCOUNT` guards). The stated direction is to **generalise that
  discipline to every state change**, not to rewrite the app.
- Must **demo reliably** — effectively a solo / student project; changes should
  be small, surgical and incremental, never big-bang rewrites.
- Server owns **amounts, timestamps, and identity**; never trust client step
  order, hidden IDs, or client clocks. Encode on output (`UiHelper.Enc`/`JsEnc`),
  normalise on input.
- No live third-party services: the payment gateway is a labelled **sandbox**;
  the map needs internet only for OpenStreetMap tiles.

## What "done" looks like

- Every status change goes through **one guarded transactional path**; every
  user-facing fetch is **owner-scoped** (never by bare primary key).
- POSTs are **idempotent**; status transitions are **whitelisted**; inputs are
  **validated server-side**; errors **log loudly, show gently**.
- The app **builds clean** (main checkout) and the **markup precompiles**; an
  **abuse test suite** (IDOR, step-skipping, double-submit, concurrency,
  validation, export) passes alongside the existing payment integration tests.

## Pointers

- `OVERVIEW.md` — architecture narrative.
- `GUIDE.md` — install & run on another PC.
- `CREDENTIALS.md` — demo logins and tracking numbers.
- `App_Data/Schema.sql` — schema + all demo data.
