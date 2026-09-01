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
            RequireRole("Admin", "Staff");
            if (!IsPostBack) LoadAll();
        }

        private void LoadAll()
        {
            DataTable drivers = Db.Query(
                @"SELECT d.UserID AS DriverID, u.FullName, d.LicenseNumber, d.EmployeeNumber, d.AvailabilityStatus
                  FROM dbo.[Driver] d JOIN dbo.[User] u ON u.UserID = d.UserID ORDER BY u.FullName");

            ddlDriver.Items.Clear();
            ddlDriver.Items.Add(new ListItem("— Unassigned —", ""));
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
            if (txtReg.Text.Trim().Length == 0)
            {
                lblMsg.CssClass = UiHelper.AlertClass("error") + " mb-4";
                lblMsg.Text = "Please enter a registration number.";
                return;
            }

            object driverId = string.IsNullOrEmpty(ddlDriver.SelectedValue)
                ? (object)DBNull.Value : int.Parse(ddlDriver.SelectedValue);

            Db.NonQuery(
                @"INSERT INTO dbo.[Vehicle] (DriverID, VehicleType, RegistrationNumber)
                  VALUES (@d, @t, @r)",
                "@d", driverId, "@t", ddlType.SelectedValue, "@r", txtReg.Text.Trim());

            lblMsg.CssClass = UiHelper.AlertClass("success") + " mb-4";
            lblMsg.Text = "Vehicle added.";
            txtReg.Text = string.Empty;
            LoadAll();
        }
    }
}
