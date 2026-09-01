using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Web.UI;

namespace LogistiCoreV5
{
    public partial class Login : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                pnlError.Visible = false;

                // Check if user just successfully registered
                if (Request.QueryString["registered"] == "true")
                {
                    ShowSuccess("Account created successfully! Please wait for admin approval if required, or log in with your credentials.");
                }
            }
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text.Trim();
            string selectedRole = hfUserRole.Value; // "Client" or "Staff"

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter both email and password.");
                return;
            }

            if (selectedRole == "Staff" && !email.EndsWith("@logisticore.com", StringComparison.OrdinalIgnoreCase))
            {
                ShowError("Warehouse staff must log in with a valid @logisticore.com work email address.");
                return;
            }

            bool isAuthenticated = AuthenticateUser(email, password, out string dbAccountType, out string userName);

            if (!isAuthenticated)
            {
                // DO NOT overwrite the error message if AuthenticateUser already displayed a status/system error
                if (!pnlError.Visible)
                {
                    ShowError("Invalid credentials or account does not exist.");
                }
                return;
            }

            Session["UserEmail"] = email;
            Session["UserName"] = userName;
            Session["UserType"] = dbAccountType;

            if (selectedRole == "Staff")
            {
                if (dbAccountType == "WarehouseAdmin" || dbAccountType == "Warehouse Worker" || dbAccountType == "Staff")
                {
                    Response.Redirect("WarehouseDashboard.aspx");
                }
                else
                {
                    ShowError("Access Denied: This account does not have warehouse staff privileges.");
                }
            }
            else
            {
                Response.Redirect("ClientDashboard.aspx");
            }
        }

        private bool AuthenticateUser(string email, string password, out string dbAccountType, out string userName)
        {
            dbAccountType = null;
            userName = null;

            // Hash incoming password to match stored hash
            string hashedPassword = HashPassword(password);
            string connString = ConfigurationManager.ConnectionStrings["LogistiCoreDB"].ConnectionString;

            // Direct SQL query selecting AccountStatus alongside Role and User details
            string query = @"
                SELECT u.FirstName, u.LastName, u.AccountStatus, r.RoleName 
                FROM Users u
                INNER JOIN Roles r ON u.RoleId = r.RoleId
                WHERE LOWER(u.Email) = @Email AND u.PasswordHash = @PasswordHash";

            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email.ToLower());
                    cmd.Parameters.AddWithValue("@PasswordHash", hashedPassword);

                    try
                    {
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string accountStatus = reader["AccountStatus"] != DBNull.Value
                                    ? reader["AccountStatus"].ToString()
                                    : "Approved";

                                // Enforce Account Governance Checks
                                if (accountStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                                {
                                    ShowError("Your account registration is currently pending approval by a Warehouse Administrator.");
                                    return false;
                                }

                                if (accountStatus.Equals("Suspended", StringComparison.OrdinalIgnoreCase))
                                {
                                    ShowError("Your account access has been suspended. Please contact your Warehouse Administrator.");
                                    return false;
                                }

                                if (accountStatus.Equals("Disapproved", StringComparison.OrdinalIgnoreCase))
                                {
                                    ShowError("Your account registration request was disapproved. Please contact system support.");
                                    return false;
                                }

                                // Account is Approved
                                string firstName = reader["FirstName"].ToString();
                                string lastName = reader["LastName"].ToString();
                                dbAccountType = reader["RoleName"].ToString();
                                userName = $"{firstName} {lastName}".Trim();

                                return true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowError("System error during login: " + ex.Message);
                    }
                }
            }

            return false;
        }

        private void ShowError(string message)
        {
            litErrorMessage.Text = message;
            pnlError.CssClass = "p-3 bg-red-50 text-error rounded-lg border border-red-200 font-body-md text-xs font-semibold block mb-4";
            pnlError.Visible = true;
        }

        private void ShowSuccess(string message)
        {
            litErrorMessage.Text = message;
            pnlError.CssClass = "p-3 bg-green-50 text-green-700 rounded-lg border border-green-200 font-body-md text-xs font-semibold block mb-4";
            pnlError.Visible = true;
        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}