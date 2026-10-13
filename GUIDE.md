# Logisticore Courier Service — Setup & Run Guide

How to get this project running on a fresh Windows laptop after unzipping it.
It is an **ASP.NET Web Forms** app (C#, **.NET Framework 4.8**) backed by
**SQL Server**.

---

## 1. Install the prerequisites

Install these (in roughly this order). All are free.

1. **Visual Studio 2022** (Community is fine) — during install, tick the
   **"ASP.NET and web development"** workload. This also installs MSBuild, IIS
   Express (used to run the site), and NuGet.
   - Make sure the **.NET Framework 4.8 targeting pack** is included (it is part
     of that workload; if not, add it under *Individual components*).

2. **SQL Server 2019/2022 Express** — the database engine.
   Download: "SQL Server … Express" from Microsoft. During setup choose a
   **Basic** installation; it creates an instance called `SQLEXPRESS`.

3. **SQL Server Management Studio (SSMS)** — to run the database script.
   (Optional if you prefer Visual Studio's SQL Server Object Explorer.)

> **No internet on the target PC?** The app still runs, but the **map tiles and
> fonts load from the internet** (OpenStreetMap, Google Fonts, Tailwind CDN). On
> an offline machine the pages work but the live map and some fonts won't render.

---

## 2. Create the database

1. Open **SSMS** and connect to `.\SQLEXPRESS`
   (Server name: `.\SQLEXPRESS`, Windows Authentication).
2. Open the file **`App_Data\Schema.sql`** from this project.
3. Press **Execute (F5)**.

That one script:
- creates the **`Logisticore`** database and every table,
- loads the base accounts, **and** the full demo data set
  (≈200 shipments with drivers, vehicles, routes, payments and tracking history).

It is safe to run again — it will not duplicate data.

> Prefer a different SQL instance? You can run the script anywhere; just update
> the connection string in step 3 to match.

---

## 3. Point the app at your database

Open **`Web.config`** and find the connection string named `Logisticore`:

```xml
<add name="Logisticore"
     connectionString="Data Source=.\SQLEXPRESS;Initial Catalog=Logisticore;Integrated Security=True;Encrypt=False"
     providerName="System.Data.SqlClient" />
```

- If your SQL instance is the default (not `SQLEXPRESS`), change
  `Data Source=.\SQLEXPRESS` to `Data Source=.` (or your instance name).
- Everything else can stay as-is for a local machine.

---

## 4. Open and run

1. Double-click **`FortCarrierLogistics.sln`** to open it in Visual Studio.
2. Let Visual Studio **restore NuGet packages** (it does this automatically on
   first build; or right-click the solution → *Restore NuGet Packages*).
   - The only package is the Roslyn C# compiler
     (`Microsoft.CodeDom.Providers.DotNetCompilerPlatform`).
3. Press **F5** (or Ctrl+F5). The site launches in **IIS Express** and opens in
   your browser.

The app also tries to apply any missing tables/columns on startup, so once the
database exists it keeps itself up to date.

---

## 5. Log in and try it

See **`CREDENTIALS.md`** for every login and a list of parcel tracking numbers.

Quick start:
- **Admin:** `admin@fcl.co.za` / `Admin@123`
- **Driver:** `driver@fcl.co.za` / `Driver@123` → *My Manifest* and *Live Map*
- **Customer:** `customer@fcl.co.za` / `Customer@123` → *Book Shipment*, *Track*,
  *My Invoices*
- Track a live parcel: log in as a customer, open **Track**, paste
  **`FCL-20000186`** (out for delivery — the driver marker moves on the map).

---

## 6. Troubleshooting

| Symptom | Fix |
|---------|-----|
| **"A network-related or instance-specific error… SQL Server"** | SQL Express isn't running or the instance name is wrong. Check the `SQL Server (SQLEXPRESS)` service is **Running** (services.msc), and that `Data Source` in `Web.config` matches your instance. |
| **Login page works but no data / empty dashboards** | You haven't run `App_Data\Schema.sql` yet (step 2), or the app is pointing at a different database than the one you seeded. |
| **The build fails about a missing `…DotNetCompilerPlatform.props`** | NuGet packages aren't restored. Right-click the solution → *Restore NuGet Packages*, then rebuild. |
| **The map is blank** | The machine has no internet connection (OpenStreetMap tiles can't load), or location permission was blocked in the browser. |
| **Build fails with "path too long"** | Move the project to a short folder such as `C:\Logisticore\` and open it from there. Deeply nested paths (e.g. under OneDrive) can exceed Windows' 260-character limit. |
| **Barcodes/fonts look plain** | Those load from a CDN; they need internet. The app still works without them. |

---

## 7. What's in the box (quick map of the project)

| Folder | What it holds |
|--------|---------------|
| `App_Data/Schema.sql` | Database schema **and** all demo data (run this once) |
| `Customer/` | Customer portal: dashboard, book shipment, track (+ live map), invoices, payment, profile |
| `Driver/` | Driver portal: manifest, delivery confirmation, **live map** |
| `Ops/`, `Admin/`, `Finance/`, `Reports/` | Operations, administration, billing and reporting |
| `Services/` | Business logic (quotes, payments, sorting, notifications, geo/map coordinates) |
| `Security/` | Authentication, roles, CSRF/ViewState protection |
| `TrackApi.aspx` | JSON endpoint the live map reads/writes positions through |
| `Scripts/livetrack.js` | The shared map code (Leaflet) |

Unused large images (`Images/Plane.png`, `Truck.png`, `Warehouse.png`) are not
required and can be deleted to shrink the zip.
