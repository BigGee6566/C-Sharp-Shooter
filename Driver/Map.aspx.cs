using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;

namespace ForteLogisticsApp.DriverPortal
{
    /// <summary>
    /// Driver live map: pick one of the parcels currently on the manifest (Assigned or
    /// OutForDelivery) and see its delivery point, the customer's shared location when they
    /// have shared one, and the driver's own position. The driver can share their live
    /// location (real geolocation) or run a movement simulation so the customer sees the
    /// parcel move toward them. All positions flow through TrackApi.aspx.
    /// </summary>
    public partial class Map : SecurePage
    {
        protected DropDownList ddlParcel;
        protected Panel pnlEmpty;
        protected Panel pnlMap;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Drivers are Staff; the manifest query below is scoped to DriverID = UserId, so a
            // non-driver staff member simply sees nothing to show.
            RequireRole("Admin", "Staff");
            if (!IsPostBack) LoadParcels();
        }

        private void LoadParcels()
        {
            DataTable dt = Db.Query(
                @"SELECT TrackingNumber, ReceiverName, DestCity, DeliveryStatus
                  FROM dbo.[Shipment]
                  WHERE DriverID = @d AND DeliveryStatus IN ('Assigned','OutForDelivery')
                  ORDER BY CASE DeliveryStatus WHEN 'OutForDelivery' THEN 0 ELSE 1 END, ShipmentDate",
                "@d", UserId);

            ddlParcel.Items.Clear();
            foreach (DataRow r in dt.Rows)
            {
                string wb = Db.ToStr(r["TrackingNumber"]);
                ddlParcel.Items.Add(new ListItem(
                    wb + "  -  " + Db.ToStr(r["ReceiverName"]) + "  (" + Db.ToStr(r["DestCity"]) +
                    ", " + Db.ToStr(r["DeliveryStatus"]) + ")", wb));
            }

            bool any = dt.Rows.Count > 0;
            pnlMap.Visible = any;
            pnlEmpty.Visible = !any;
        }
    }
}
