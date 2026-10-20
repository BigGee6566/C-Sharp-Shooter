using System;
using System.Data;
using System.Globalization;
using System.Web;
using System.Web.Script.Serialization;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp
{
    /// <summary>
    /// JSON endpoint for the live-tracking map (customer Track page and driver Map page).
    ///
    /// GET  ?wb=FCL-...            -> current positions for that waybill.
    /// POST  wb, role, lat, lng    -> stores a position ping (the sender shares their own
    ///                                location, or the assigned driver shares theirs).
    ///
    /// It requires a signed-in session. Anyone holding the waybill sees the public view (route,
    /// status, a simulated driver position); the real GPS positions - the sender's shared
    /// location and the driver's live position - go only to the parties to the shipment and
    /// office staff (TrackAccess.CanSeeLivePositions). A ping must come from a page on this
    /// site (TrackAccess.IsSameOrigin), is only accepted for the role the caller actually holds
    /// on that shipment, and only while the parcel is still on its way (TrackAccess.CanPing).
    /// </summary>
    public partial class TrackApi : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.ContentType = "application/json";
            Response.Cache.SetCacheability(HttpCacheability.NoCache);

            int uid = (Session != null && Session["UserID"] != null) ? (int)Session["UserID"] : 0;
            if (uid <= 0 || !SessionGuard.IsCurrent(Context)) { Write(new { ok = false, error = "auth" }); return; }

            string wb = (Request["wb"] ?? string.Empty).Trim();
            if (wb.Length == 0) { Write(new { ok = false, error = "nowaybill" }); return; }

            DataTable dt = Db.Query(
                @"SELECT ShipmentID, SenderID, DriverID, OriginCity, DestCity, DeliveryStatus,
                         CollectedDate, LastStatusChange, ShipmentDate
                  FROM dbo.[Shipment] WHERE TrackingNumber = @t", "@t", wb);
            if (dt.Rows.Count == 0) { Write(new { ok = false, error = "notfound" }); return; }

            DataRow r = dt.Rows[0];
            int shipmentId = Db.ToInt(r["ShipmentID"]);
            int senderId = Db.ToInt(r["SenderID"]);
            int driverId = Db.ToInt(r["DriverID"]);
            string status = Db.ToStr(r["DeliveryStatus"]);

            if (string.Equals(Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                HandlePing(shipmentId, senderId, driverId, uid, status);
                return;
            }

            string callerRole = (Session["Role"] as string) ?? string.Empty;
            bool canSeeLive = TrackAccess.CanSeeLivePositions(uid, callerRole, SecurePage.IsDriver(uid), senderId, driverId);

            GeoService.GeoPoint origin = GeoService.ForCity(Db.ToStr(r["OriginCity"]));
            GeoService.GeoPoint dest = GeoService.ForCity(Db.ToStr(r["DestCity"]));

            // Shared positions take priority for the parties to the shipment; everyone else (and
            // anyone when nothing is shared) sees the driver simulated from the shipment's status
            // and how long it has been out for delivery.
            double? dLat = null, dLng = null, cLat = null, cLng = null;
            // Only fresh positions on a parcel still on its way: an old one is no longer "live".
            bool finished = status == "Delivered" || status == "Failed";
            DataTable pings = canSeeLive && !finished
                ? Db.Query("SELECT Role, Lat, Lng FROM dbo.LocationPing WHERE ShipmentID = @s AND UpdatedDate >= DATEADD(minute, -@m, GETDATE())",
                    "@s", shipmentId, "@m", TrackAccess.FreshPingMinutes)
                : new DataTable();
            foreach (DataRow p in pings.Rows)
            {
                string role = Db.ToStr(p["Role"]);
                if (role == "Driver") { dLat = (double)Db.ToDecimal(p["Lat"]); dLng = (double)Db.ToDecimal(p["Lng"]); }
                else if (role == "Customer") { cLat = (double)Db.ToDecimal(p["Lat"]); cLng = (double)Db.ToDecimal(p["Lng"]); }
            }

            bool driverLive = dLat.HasValue;
            if (!driverLive)
            {
                GeoService.GeoPoint sim = SimulateDriver(status, origin, dest, r);
                dLat = sim.Lat; dLng = sim.Lng;
            }

            Write(new
            {
                ok = true,
                status = status,
                mine = (senderId == uid),
                origin = new { lat = origin.Lat, lng = origin.Lng, label = Db.ToStr(r["OriginCity"]) },
                dest = new { lat = dest.Lat, lng = dest.Lng, label = Db.ToStr(r["DestCity"]) },
                driver = new { lat = dLat, lng = dLng, live = driverLive, active = (status == "OutForDelivery" || status == "Assigned") },
                customer = cLat.HasValue ? (object)new { lat = cLat, lng = cLng } : null
            });
        }

        private static GeoService.GeoPoint SimulateDriver(
            string status, GeoService.GeoPoint origin, GeoService.GeoPoint dest, DataRow r)
        {
            if (status == "Delivered") return dest;
            if (status == "OutForDelivery")
            {
                DateTime start = GetDate(r, "CollectedDate") ?? GetDate(r, "LastStatusChange")
                                 ?? GetDate(r, "ShipmentDate") ?? DateTime.Now;
                double frac = (DateTime.Now - start).TotalHours / 8.0;   // ~8h simulated journey
                if (frac < 0) frac = 0;
                if (frac > 0.97) frac = 0.97;                            // never "arrive" before Delivered
                return GeoService.Lerp(origin, dest, frac);
            }
            return origin;   // Registered / Sorted / Assigned sit at the depot
        }

        private static DateTime? GetDate(DataRow r, string col)
        {
            return (r.Table.Columns.Contains(col) && r[col] != DBNull.Value)
                ? (DateTime?)Convert.ToDateTime(r[col]) : null;
        }

        private void HandlePing(int shipmentId, int senderId, int driverId, int uid, string status)
        {
            // Not an ASP.NET postback, so there is no ViewState to forge-proof it: insist the
            // request comes from a page on this site.
            if (!TrackAccess.IsSameOrigin(Request.Headers["Origin"], Request.Headers["Referer"], Request.Headers["Sec-Fetch-Site"], Request.Url))
            {
                Write(new { ok = false, error = "forbidden" });
                return;
            }

            // Posted fields only - Request[...] would also read the query string and cookies.
            string role = (Request.Form["role"] ?? string.Empty).Trim();
            double lat, lng;
            if (!double.TryParse(Request.Form["lat"], NumberStyles.Float, CultureInfo.InvariantCulture, out lat) ||
                !double.TryParse(Request.Form["lng"], NumberStyles.Float, CultureInfo.InvariantCulture, out lng) ||
                double.IsNaN(lat) || double.IsNaN(lng) ||
                lat < -90 || lat > 90 || lng < -180 || lng > 180)
            {
                Write(new { ok = false, error = "badcoords" });
                return;
            }

            if (!TrackAccess.CanPing(role, uid, senderId, driverId, status)) { Write(new { ok = false, error = "forbidden" }); return; }

            Db.NonQuery(
                // HOLDLOCK: two first pings at once must not both take the INSERT branch.
                @"MERGE dbo.LocationPing WITH (HOLDLOCK) AS t
                  USING (SELECT @s AS ShipmentID, @r AS Role) AS s
                    ON t.ShipmentID = s.ShipmentID AND t.Role = s.Role
                  WHEN MATCHED THEN UPDATE SET Lat = @lat, Lng = @lng, UpdatedDate = GETDATE()
                  WHEN NOT MATCHED THEN INSERT (ShipmentID, Role, Lat, Lng) VALUES (@s, @r, @lat, @lng);",
                "@s", shipmentId, "@r", role, "@lat", (decimal)lat, "@lng", (decimal)lng);

            Write(new { ok = true });
        }

        private void Write(object o)
        {
            Response.Write(new JavaScriptSerializer().Serialize(o));
            Response.End();
        }
    }
}
