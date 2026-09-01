using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace LogistiCoreV5
{
    public partial class WarehouseDashboard : System.Web.UI.Page
    {
        private readonly string connString = ConfigurationManager.ConnectionStrings["LogistiCoreDB"].ConnectionString;

        protected void Page_Load(object sender, EventArgs args)
        {
            // 1. Session Authorization Check
            if (Session["UserEmail"] == null)
            {
                Response.Redirect("Login.aspx");
                return;
            }

            if (!IsPostBack)
            {
                // 2. Load existing parcel queue
                BindQueueGrid();

                // 3. Enable Governance Panel ONLY for WarehouseAdmin
                string userType = Session["UserType"] != null ? Session["UserType"].ToString() : string.Empty;
                if (string.Equals(userType, "WarehouseAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    pnlAdminGovernance.Visible = true;
                    BindUserManagementGrid();
                }
            }
        }

        #region User Governance Methods (WarehouseAdmin Only)

        private void BindUserManagementGrid()
        {
            string query = @"
                SELECT u.UserId, u.FirstName, u.LastName, u.Email, u.AccountStatus, r.RoleName 
                FROM Users u
                INNER JOIN Roles r ON u.RoleId = r.RoleId
                ORDER BY CASE WHEN u.AccountStatus = 'Pending' THEN 1 ELSE 2 END, u.UserId DESC";

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            da.Fill(dt);
                            gvUserGovernance.DataSource = dt;
                            gvUserGovernance.DataBind();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowGovernanceAlert("Error loading user governance data: " + ex.Message, "danger");
            }
        }

        protected void gvUserGovernance_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandArgument == null) return;

            if (!int.TryParse(e.CommandArgument.ToString(), out int userId)) return;

            string newStatus = string.Empty;

            switch (e.CommandName)
            {
                case "ApproveUser":
                    newStatus = "Approved";
                    ShowGovernanceAlert($"Employee (ID: {userId}) approved successfully.", "success");
                    break;

                case "ReactivateUser":
                    newStatus = "Approved";
                    ShowGovernanceAlert($"Employee account (ID: {userId}) reactivated.", "success");
                    break;

                case "DisapproveUser":
                    newStatus = "Disapproved";
                    ShowGovernanceAlert($"Employee registration (ID: {userId}) declined.", "warning");
                    break;

                case "SuspendUser":
                    newStatus = "Suspended";
                    ShowGovernanceAlert($"Employee access (ID: {userId}) suspended.", "danger");
                    break;

                case "RemoveUser":
                    DeleteUserAccount(userId);
                    return;
            }

            if (!string.IsNullOrEmpty(newStatus))
            {
                UpdateUserStatus(userId, newStatus);
                BindUserManagementGrid();
            }
        }

        private void UpdateUserStatus(int userId, string status)
        {
            string query = "UPDATE Users SET AccountStatus = @Status WHERE UserId = @UserId";

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Status", status);
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowGovernanceAlert("Database error updating user status: " + ex.Message, "danger");
            }
        }

        private void DeleteUserAccount(int userId)
        {
            string query = "DELETE FROM Users WHERE UserId = @UserId";

            try
            {
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", userId);
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                ShowGovernanceAlert($"Employee account (ID: {userId}) permanently removed.", "info");
                BindUserManagementGrid();
            }
            catch (SqlException ex) when (ex.Number == 547) // Foreign key constraint conflict
            {
                ShowGovernanceAlert($"Cannot remove employee (ID: {userId}) because they have linked activity records in the database. Consider suspending access instead.", "warning");
            }
            catch (Exception ex)
            {
                ShowGovernanceAlert("Error removing user account: " + ex.Message, "danger");
            }
        }

        private void ShowGovernanceAlert(string message, string alertType)
        {
            pnlGovernanceAlert.Visible = true;
            litGovernanceAlert.Text = message;

            switch (alertType.ToLower())
            {
                case "success":
                    pnlGovernanceAlert.CssClass = "p-3.5 mb-4 rounded-lg text-xs font-medium bg-emerald-50 text-emerald-800 border border-emerald-200 flex justify-between items-center";
                    break;
                case "warning":
                    pnlGovernanceAlert.CssClass = "p-3.5 mb-4 rounded-lg text-xs font-medium bg-amber-50 text-amber-800 border border-amber-200 flex justify-between items-center";
                    break;
                case "danger":
                    pnlGovernanceAlert.CssClass = "p-3.5 mb-4 rounded-lg text-xs font-medium bg-rose-50 text-rose-800 border border-rose-200 flex justify-between items-center";
                    break;
                default:
                    pnlGovernanceAlert.CssClass = "p-3.5 mb-4 rounded-lg text-xs font-medium bg-indigo-50 text-indigo-800 border border-indigo-200 flex justify-between items-center";
                    break;
            }
        }

        public string GetStatusBadgeCss(string status)
        {
            switch (status)
            {
                case "Approved":
                    return "inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-emerald-100 text-emerald-800";
                case "Pending":
                    return "inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-800";
                case "Suspended":
                case "Disapproved":
                    return "inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-rose-100 text-rose-800";
                default:
                    return "inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800";
            }
        }

        #endregion

        #region Parcel Sorting Queue Methods

        private void BindQueueGrid()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("TrackingID");
            dt.Columns.Add("Destination");
            dt.Columns.Add("Priority");
            dt.Columns.Add("Status");

            dt.Rows.Add("LC-8472-A", "Sector 4, North District", "High", "Pending Scan");
            dt.Rows.Add("LC-9931-B", "Sector 2, East District", "Standard", "Sorted");
            dt.Rows.Add("LC-1045-C", "Sector 7, West Hub", "Standard", "Sorted");

            gvSortingQueue.DataSource = dt;
            gvSortingQueue.DataBind();
        }

        protected void txtSearch_TextChanged(object sender, EventArgs e)
        {
            // Search filtering logic
        }

        protected void ddlPriority_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Priority filtering logic
        }

        protected void gvSortingQueue_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandArgument == null) return;
            string trackingId = e.CommandArgument.ToString();

            if (e.CommandName == "ScanIn")
            {
                // Scan logic
            }
            else if (e.CommandName == "AssignDriver")
            {
                // Driver assignment logic
            }
        }

        protected void btnGenerateReport_Click(object sender, EventArgs e)
        {
            // Report generation logic
        }

        protected void btnPrevPage_Click(object sender, EventArgs e)
        {
            // Pagination previous
        }

        protected void btnNextPage_Click(object sender, EventArgs e)
        {
            // Pagination next
        }

        #endregion
    }
}