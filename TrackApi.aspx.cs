using System;
using System.Data;
using System.Globalization;
using System.Web;
using System.Web.Script.Serialization;
using ForteLogisticsApp.Data;
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
    /// Not a security boundary beyond this: it requires a signed-in session, and a ping is
    /// only accepted for the role the caller actually is on that shipment (the customer may
    /// only post a Customer ping for a shipment they sent; the driver only a Driver ping for
    /// one assigned to them). Reads are allowed to any signed-in user, as tracking is already
    /// available to anyone holding the waybill number.
    /// </summary>
    public partial class TrackApi : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.ContentType = "application/json";
            Response.Cache.SetCacheability(HttpCacheability.NoCache);

            int uid = (Session != null && Session["UserID"] != null) ? (int)Session["UserID"] : 0;
            if (uid <= 0) { Write(new { ok = false, error = "auth" }); return; }

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
                HandlePing(shipmentId, senderId, driverId, uid);
                return;
            }

            GeoService.GeoPoint origin = GeoService.ForCity(Db.ToStr(r["OriginCity"]));
            GeoService.GeoPoint dest = GeoService.ForCity(Db.ToStr(r["DestCity"]));

            // Positions shared through the endpoint take priority; otherwise the driver is
            // simulated from the shipment's status and how long it has been out for delivery.
            double? dLat = null, dLng = null, cLat = null, cLng = null;
            DataTable pings = Db.Query(
                "SELECT Role, Lat, Lng FROM dbo.LocationPing WHERE ShipmentID = @s", "@s", shipmentId);
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

        private void HandlePing(int shipmentId, int senderId, int driverId, int uid)
        {
            string role = (Request["role"] ?? string.Empty).Trim();
            double lat, lng;
            if (!double.TryParse(Request["lat"], NumberStyles.Float, CultureInfo.InvariantCulture, out lat) ||
                !double.TryParse(Request["lng"], NumberStyles.Float, CultureInfo.InvariantCulture, out lng) ||
                lat < -90 || lat > 90 || lng < -180 || lng > 180)
            {
                Write(new { ok = false, error = "badcoords" });
                return;
            }

            bool allowed = (role == "Customer" && senderId == uid) || (role == "Driver" && driverId == uid);
            if (!allowed) { Write(new { ok = false, error = "forbidden" }); return; }

            Db.NonQuery(
                @"MERGE dbo.LocationPing AS t
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
