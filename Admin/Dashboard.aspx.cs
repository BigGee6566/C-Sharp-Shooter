using System;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;

namespace ForteLogisticsApp.AdminPortal
{
    public partial class Dashboard : SecurePage
    {
        protected Literal litUsers, litDrivers, litVehicles, litShipments;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireRole("Admin");
            if (!IsPostBack)
            {
                litUsers.Text = Db.ToStr(Db.Scalar("SELECT COUNT(*) FROM dbo.[User]"));
                litDrivers.Text = Db.ToStr(Db.Scalar("SELECT COUNT(*) FROM dbo.[Driver]"));
                litVehicles.Text = Db.ToStr(Db.Scalar("SELECT COUNT(*) FROM dbo.[Vehicle]"));
                litShipments.Text = Db.ToStr(Db.Scalar("SELECT COUNT(*) FROM dbo.[Shipment]"));
            }
        }
    }
}
