using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Web.UI;

namespace LogistiCoreV5
{
    public partial class Register : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            lblError.Visible = false;
        }

        protected void btnRegister_Click(object sender, EventArgs e)
        {
            // 1. Validate Terms
            if (!chkTerms.Checked)
            {
                ShowError("You must agree to the Terms of Service and Privacy Policy to register.");
                return;
            }

            // 2. Map Form Inputs
            string selectedRoleKey = hfRole.Value?.ToLower() ?? "individual";

            // Map UI roles to database Roles table values
            string dbRoleName = (selectedRoleKey == "staff") ? "Warehouse Worker" : "Customer";

            // Determine email based on selected tab
            string email = (selectedRoleKey == "staff" && !string.IsNullOrWhiteSpace(txtWorkEmail.Text))
                ? txtWorkEmail.Text.Trim()
                : txtEmail.Text.Trim();

            string rawPassword = txtPassword.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string rawFullName = txtFullName.Text.Trim();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(rawPassword) || string.IsNullOrWhiteSpace(rawFullName))
            {
                ShowError("Please fill in all required fields (Full Name, Email, and Password).");
                return;
            }

            // 3. Parse First and Last Name
            string firstName = rawFullName;
            string lastName = "";
            int spaceIndex = rawFullName.IndexOf(' ');

            if (spaceIndex > 0)
            {
                firstName = rawFullName.Substring(0, spaceIndex).Trim();
                lastName = rawFullName.Substring(spaceIndex + 1).Trim();
            }

            // 4. Hash Password
            string passwordHash = HashPassword(rawPassword);

            // 5. Execute Stored Procedure via ADO.NET
            string connString = ConfigurationManager.ConnectionStrings["LogistiCoreDB"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.sp_RegisterUser", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Email", email.ToLower());
                    cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                    cmd.Parameters.AddWithValue("@FirstName", firstName);
                    cmd.Parameters.AddWithValue("@LastName", string.IsNullOrEmpty(lastName) ? firstName : lastName);
                    cmd.Parameters.AddWithValue("@PhoneNumber", string.IsNullOrEmpty(phone) ? (object)DBNull.Value : phone);
                    cmd.Parameters.AddWithValue("@RoleName", dbRoleName);

                    try
                    {
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string approvalStatus = reader["ApprovalStatus"].ToString();

                                // Success! Redirect to login page with a query flag
                                Response.Redirect($"Login.aspx?registered=true&status={approvalStatus}", false);
                                Context.ApplicationInstance.CompleteRequest();
                            }
                        }
                    }
                    catch (SqlException ex)
                    {
                        // Duplicate Email violation (Unique constraint)
                        if (ex.Number == 2627 || ex.Number == 2601)
                        {
                            ShowError("An account with this email address already exists.");
                        }
                        else
                        {
                            ShowError("Database error: " + ex.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowError("An error occurred during registration: " + ex.Message);
                    }
                }
            }
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
        }

        // SHA-256 Hash Helper (or replace with BCrypt.Net.BCrypt.HashPassword if installed)
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