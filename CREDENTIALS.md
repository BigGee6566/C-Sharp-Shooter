# Logisticore — Logins & Tracking Numbers

Everything here works **after you have run `App_Data/Schema.sql`** against your
`Logisticore` database (it creates the tables and loads all the demo data).

> All passwords are case-sensitive.

---

## 1. Staff & admin logins

| Role | What they see | Email | Password |
|------|---------------|-------|----------|
| **Admin** | Everything: admin, operations, finance, reports | `admin@fcl.co.za` | `Admin@123` |
| **Operations staff** | Ops dashboard, parcel registration, warehouse, route planning | `staff@fcl.co.za` | `Dispatch@123` |

## 2. Driver logins (Staff + a driver record)

Drivers land on **My Manifest** and have the **Live Map** page. All 8 share the
same password: **`Driver@123`**

| Email | Password |
|-------|----------|
| `driver@fcl.co.za`  | `Driver@123` |
| `driver2@fcl.co.za` | `Driver@123` |
| `driver3@fcl.co.za` | `Driver@123` |
| `driver4@fcl.co.za` | `Driver@123` |
| `driver5@fcl.co.za` | `Driver@123` |
| `driver6@fcl.co.za` | `Driver@123` |
| `driver7@fcl.co.za` | `Driver@123` |
| `driver8@fcl.co.za` | `Driver@123` |

> `driver@fcl.co.za` is the best one to demo the driver side — it has parcels
> both *awaiting collection* and *out for delivery* on its manifest.

## 3. Customer logins

Customers can **book shipments, pay invoices, and track parcels**. All share the
password **`Customer@123`**

| Email | Password | Notes |
|-------|----------|-------|
| `customer@fcl.co.za` | `Customer@123` | Main demo account (Business client) |
| `customer1@fcl.co.za` … `customer30@fcl.co.za` | `Customer@123` | 30 more customers who own the seeded shipments |

---

## 4. Tracking numbers to test

Log in as **any customer** (e.g. `customer@fcl.co.za` / `Customer@123`), open
**Track**, and paste a waybill below. Any customer can track any waybill.

| Status | Waybill range | Count | On the map |
|--------|---------------|-------|------------|
| **Out for delivery** (driver moving) | `FCL-20000186` – `FCL-20000198` | 13 | **Driver marker moves toward the destination — best for the live map** |
| **Awaiting collection** (assigned) | `FCL-20000171` – `FCL-20000185` | 15 | Driver at the depot, destination pinned |
| **Upcoming** (just booked) | `FCL-20000151` – `FCL-20000170` | 20 | At the depot |
| **Delivered** (history) | `FCL-20000001` – `FCL-20000150` | 150 | Driver at the destination |

**Quick picks**

- Live-tracking demo: `FCL-20000186`, `FCL-20000190`, `FCL-20000198`
- Awaiting collection: `FCL-20000171`, `FCL-20000180`
- Delivered (full history + rate the driver): `FCL-20000001`, `FCL-20000100`

There are also three original sample waybills: `FCL-10000001` (delivered),
`FCL-10000002` (in transit), `FCL-10000003` (registered).

---

## 5. How the live map works

- On **Track**, a customer sees the delivery route and the driver's position.
  For *out-for-delivery* parcels the driver marker is simulated moving along the
  route; it also shows the driver's real position if the driver is sharing it.
- A customer can tap **Share my location** (needs browser location permission) so
  their driver can see where they are.
- A driver, on **Live Map**, can **Share my live location** or **Simulate drive**
  — either makes the customer's Track map show the parcel moving toward them.

> The map uses OpenStreetMap tiles, so the machine needs an internet connection
> for the map to display.
