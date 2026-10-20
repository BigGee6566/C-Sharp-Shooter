using System;
using System.Data;
using System.Web.UI.WebControls;
using ForteLogisticsApp.Data;
using ForteLogisticsApp.Security;
using ForteLogisticsApp.Services;

namespace ForteLogisticsApp.AdminPortal
{
    public partial class Fleet : SecurePage
    {
        protected TextBox txtReg;
        protected DropDownList ddlType, ddlDriver;
        protected Button btnAddVehicle;
        protected Label lblMsg;
        protected Repeater rptVehicles, rptDrivers;

        protected void Page_Load(object sender, EventArgs e)
        {
            RequireOperationsStaff();
            if (!IsPostBack) LoadAll();
        }

        private void LoadAll()
        {
            DataTable drivers = Db.Query(
                @"SELECT d.UserID AS DriverID, u.FullName, d.LicenseNumber, d.EmployeeNumber, d.AvailabilityStatus
                  FROM dbo.[Driver] d JOIN dbo.[User] u ON u.UserID = d.UserID ORDER BY u.FullName");

            ddlDriver.Items.Clear();
            ddlDriver.Items.Add(new ListItem("-- Unassigned --", ""));
            foreach (DataRow r in drivers.Rows)
                ddlDriver.Items.Add(new ListItem(Db.ToStr(r["FullName"]), Db.ToStr(r["DriverID"])));

            rptDrivers.DataSource = drivers;
            rptDrivers.DataBind();

            DataTable vehicles = Db.Query(
                @"SELECT v.RegistrationNumber, v.VehicleType, ISNULL(u.FullName, '-') AS DriverName
                  FROM dbo.[Vehicle] v
                  LEFT JOIN dbo.[User] u ON u.UserID = v.DriverID
                  ORDER BY v.RegistrationNumber");
            rptVehicles.DataSource = vehicles;
            rptVehicles.DataBind();
        }

        protected void btnAddVehicle_Click(object sender, EventArgs e)
        {
            // Validation, the duplicate-plate check and the audit entry live in FleetService.
            int d;
            int? driverId = int.TryParse(ddlDriver.SelectedValue, out d) ? (int?)d : null;
            AccountResult result = FleetService.AddVehicle(txtReg.Text, ddlType.SelectedValue, driverId, UserId);

            lblMsg.CssClass = UiHelper.AlertClass(result.Success ? "success" : "error") + " mb-4";
            lblMsg.Text = result.Success ? "Vehicle added." : UiHelper.Enc(result.Message);
            if (!result.Success) return;

            txtReg.Text = string.Empty;
            LoadAll();
        }
    }
}
