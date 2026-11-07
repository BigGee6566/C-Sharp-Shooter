# Logisticore demo: a day in the life

The examiner asked for a day-in-the-life walkthrough, not a tour of what each screen does.
So this script follows **one parcel through one working day**, and each person only opens
a screen because their job needs it next.

**Length:** about 12 minutes. **Logins and passwords:** see `CREDENTIALS.md`.

> **The rule for the whole video:** say why the person is doing something, not what the
> screen is. Say "Thandi needs this parcel on a van to Durban", not "this is the Route
> Planning page".

---

## Before you record

**1. Give the laptop room to work.** In rehearsal the laptop hit 100% CPU with 379 MB of
7.8 GB memory free, and pages failed with "The wait operation timed out". That was load,
not a fault in the system. Restart the laptop, then close Brave and anything else you don't
need, and pause OneDrive. Your recording software needs memory too.

**2. Run the app and open each login once.** The first load after starting is slow. Signing
in also takes a few seconds, so wait for the page rather than clicking again.

**3. Open two browser windows.** Use a normal window and a private (InPrivate) window, so
the driver and the customer can be signed in at the same time for Scene 6.

**4. Choose your people.** Sign in as admin, open **Users**, and read the **Account** column.

| You need | Look for | Note |
|---|---|---|
| Individual customer | Account = `Individual` | `customer3@fcl.co.za` is Individual in a freshly seeded database, but **Business in your current one**. Check, don't assume. |
| Business customer | Account = `Business` | `customer@fcl.co.za` |
| Driver | Account = `Driver` | `driver@fcl.co.za` is Sipho Dlamini |
| Operations staff | Role = `Staff` | `staff@fcl.co.za` is Thandi Mokoena |

**5. Open Route Planning and see who is free.** This also tidies up old routes and driver
statuses, which is intended.

| What you see | What to do |
|---|---|
| A driver **and** a vehicle are listed | Good. Skip Scene 3 and use them in Scene 4. |
| No driver listed | Pick the driver with the shortest manifest and use them for Scene 3. |
| No vehicle listed | Vehicles come back when their route completes. Scene 3 frees one, or deactivate a route that has no open parcels. |

Write the driver's name and vehicle down before you press record.

---

## Scene 1. Morning: a customer sends a parcel (2 min)

**Who:** the Individual customer, in the private window.

| Do | Say |
|---|---|
| Sign in on the **Client Portal** tab. Show the dashboard. | "Meet our customer. They're not a business, so they get a simple personal dashboard: what's on the way, what's arrived, and how to send something." |
| **Book a Delivery**. Keep **Small**. **Next: Addresses**. | "They're sending a birthday gift to Durban." |
| Collection: `12 Rivonia Road`, `Johannesburg`, `2196`. Delivery: `50 Florida Road`, `Durban`, `4001`. **Next: Recipient**. | "The price depends on the route, so the system works it out now." |
| Recipient `Thabo Mokoena`, phone `0712345678`, description `Birthday gift`. **Confirm Booking**. | "An individual pays before we move anything." |
| On the payment page choose **Card**: any name, `4242 4242 4242 4242`, `12/29`, `123`. **Pay now (sandbox)**. | "This is a sandbox, so no real money moves, and only the last four digits are kept." |
| **Write down the waybill number** on the receipt. | "That waybill now follows the parcel everywhere." |

> On the Step 3 page, don't read out the blue note ("no card details are needed now"). That
> wording is for Business customers. See **Rough edges** below.

---

## Scene 2. Mid-morning: the warehouse (2 min)

**Who:** operations staff, in the normal window, on the **Staff Portal** tab.

| Do | Say |
|---|---|
| **Register Parcel**. Sender `Lindiwe Dube`, recipient `Sam Naidoo`, delivery address `1 Beach Road, Durban`, weight `2`. **Register Parcel**. | "Someone also walks in at the counter. Thandi captures it, and the system creates the waybill and a barcode label ready to print." |
| Show the barcode inside the label. | Fixes the examiner's note about the barcode running off the screen. |
| **Warehouse**. Show the queue. | "Everything paid and waiting is here, with clear columns: where it's going, which lane, and its status." |
| Type the customer's waybill and press **Scan**. | "She scans the parcel. It's going to Durban, so the system has already chosen the Metro-DBN lane." |
| Leave the status on **Sorted**. **Save Parcel**. | "Sorted, and now it needs a driver." You'll see *Parcel updated.* |

---

## Scene 3. Midday: everyone is busy (2 min)

*Skip this scene if a driver was already free in step 5.*

| Do | Say |
|---|---|
| As staff, open **Route Planning** and open the driver dropdown. | "Thandi can only be offered drivers who are genuinely free. A driver with parcels never appears here, so nobody gets double-booked. The same goes for vehicles." |
| In the other window, sign in as the driver. Show **My Manifest**. | "This driver is finishing their last run." |
| Press **Collect All Assigned** if any parcel still needs pickup. | "They collect anything still waiting." |
| For each parcel: **Confirm Delivery**, add a note, **Submit Confirmation**. | "Proof of delivery goes in as each drop happens." |
| Back as staff, reload **Route Planning**. | "The last parcel is delivered, so the route completed by itself, and the driver and van came straight back. Nobody had to update anything." |

---

## Scene 4. Early afternoon: dispatch (2 min)

**Who:** operations staff.

| Do | Say |
|---|---|
| Replace the suggested route name with `Metro-DBN - Afternoon Run`. | "She names the run." |
| Choose the free driver and vehicle. Set **Service Area** to `Metro-DBN`. **Create Route**. | "A route covers one area, so a Durban parcel can't end up on a Cape Town van." |
| Open the Driver and Vehicle dropdowns again. | "They've gone from both lists, so that van can't be given to anyone else." |
| Under **Allocate Parcels**, select the new route. Tick your parcel. **Assign Selected to Route**. | "The driver gets it straight away." You'll see *1 parcel(s) assigned to the driver for collection.* |

> The suggested route name follows the busiest lane, so it may say Regional-Outlying. That's
> why you retype it. Check **Service Area** too, because it starts on Metro-JHB.

---

## Scene 5. Afternoon: on the road (2 min)

This scene needs both windows side by side.

| Do | Say |
|---|---|
| Driver window: **My Manifest**. **Confirm Pickup** on the parcel. | "The driver collects it. The customer is told automatically." |
| Driver window: **Live Map**. Pick the parcel. | "The driver sees the delivery point." |
| Customer window: **Track Parcel**, open the waybill. | "And the customer can see the same parcel on their map." |
| Driver window: press **Simulate drive**. **Stay on this page** until it finishes, about 40 seconds. | "Here we simulate the drive to Durban. In real use this comes from the driver's phone location." |
| Watch the customer map. It updates about every 5 seconds. | "The customer watches it approach without phoning anyone." |

---

## Scene 6. Late afternoon: delivered (1 min)

| Do | Say |
|---|---|
| Driver window: **Confirm Delivery**. Check the name, add `Signed for by recipient`. **Submit Confirmation**. | "It's delivered, with a name and a note as proof." |
| Back on **My Manifest**. | "Their manifest is empty, so they're available for the next route." |

> The screen says *The invoice has been raised automatically.* This customer already paid, so
> say "the billing is already settled". No second invoice appeared in rehearsal.

---

## Scene 7. Evening: the customer closes the loop (1 min)

| Do | Say |
|---|---|
| Customer window: **Notifications**. | "Every step reached them without anyone sending a message: booked, paid, assigned, collected, delivered." |
| **Delivery History**. Open the parcel. | "It has moved out of active deliveries and into history." |
| Under **Rate your delivery**, choose a score, add a comment, **Submit Rating**. | "Their rating goes into the driver's performance record." |

---

## Scene 8. Same day, different customer: a business (30 sec)

| Do | Say |
|---|---|
| Sign in as the Business customer. Show the dashboard. | "A business needs something different: spending, trends and outstanding invoices across many parcels. Same system, a different view of it." |

---

## Scene 9. End of day: the admin (1 min)

| Do | Say |
|---|---|
| Sign in as admin. **Users**. | "The admin manages everyone, and all of it is readable at a glance." |
| **Fleet**. | "Driver statuses here come from their actual work, not from someone remembering to update them." |
| **Business Performance** report. | "Today's work is already in the numbers. Management didn't have to compile anything." |

**Close with:** "One parcel, five people, and each of them only did their own job. The system
handled the handovers."

---

## If something goes wrong while recording

| Problem | Fix |
|---|---|
| "The wait operation timed out" | The laptop is overloaded. Close something and reload. |
| The map background is missing | The map needs internet. Use **Retry Map**, or carry on, because the markers still work. |
| Route Planning shows nobody free | Expected if everyone is busy. Finish a driver's parcels (Scene 3), or deactivate a route with no open parcels. |
| The Create Route button is greyed out | No driver or vehicle is free. The message on the page says so. |
| Access Denied | You're on a page for a different role. Sign out and use the right login. |
| "That is a staff account" | Use the **Staff Portal** tab on the login page. |

## Rough edges to avoid showing

These are small and don't stop the journey:

- Step 3 of booking says no card details are needed now, but Individuals go straight to payment.
- The Track page shows raw statuses such as `OUTFORDELIVERY`. The dashboard shows friendlier ones.
- A walk-in parcel starts on the Johannesburg to Cape Town route unless you change the
  dropdowns. That's fine for Scene 2, because only its barcode is shown.

## What was rehearsed

Rehearsed in the real app on 7 November 2026, on a freshly seeded test database and not on
your real one. That's why the people and parcels on your screen will differ.

| Part | Result |
|---|---|
| Scenes 1 to 7, up to and including Notifications and Delivery History | Worked end to end |
| Scene 2 barcode | Checked at 375, 1024 and 1366px wide |
| Scene 5 Simulate drive | Driver position moved and showed as live |
| Scene 7 Submit Rating | The rating was saved, but the page reload timed out because the laptop was overloaded. **Check this screen once before recording.** |
| Scene 8 Business dashboard | Checked earlier the same day |
| Customer **Payments** page | Timed out for the same reason. It isn't in the script, but check it before showing it. |
| Scene 9 Admin **Users** | Checked earlier the same day |
| Scene 9 **Fleet** and **Business Performance** | **Not rehearsed.** Open them once beforehand. |
| Your real database | **Not rehearsed.** Do steps 4 and 5 of **Before you record** first. |
