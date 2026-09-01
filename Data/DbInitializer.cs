using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;
using System.Web.Hosting;

namespace ForteLogisticsApp.Data
{
    /// <summary>
    /// Ensures the Logisticore database has its tables and seed data.
    ///
    /// The database itself (catalog "Logisticore" on .\SQLEXPRESS) is normally
    /// created manually by running App_Data\Schema.sql. This helper is a safety
    /// net: if the app connects to an existing but empty Logisticore database,
    /// it runs the same Schema.sql (minus the CREATE DATABASE / USE batches) to
    /// create the tables and seed data. If the tables already exist, it does
    /// nothing. Any failure is swallowed so startup never crashes.
    /// </summary>
    public static class DbInitializer
    {
        public static void EnsureCreated()
        {
            try
            {
                // On a fresh database run the whole script; on an existing one replay only the
                // guarded upgrade batches so new columns/tables apply themselves on startup.
                bool upgradeOnly = UserTableExists();

                string schemaPath = HostingEnvironment.MapPath("~/App_Data/Schema.sql");
                if (schemaPath == null || !File.Exists(schemaPath)) return;

                string script = File.ReadAllText(schemaPath);
                string[] batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

                using (var conn = Db.GetConnection())
                {
                    conn.Open();
                    foreach (string batch in batches)
                    {
                        string trimmed = batch.Trim();
                        if (trimmed.Length == 0) continue;
                        // Skip database-level statements; we're already connected to Logisticore.
                        if (Regex.IsMatch(trimmed, @"CREATE\s+DATABASE", RegexOptions.IgnoreCase)) continue;
                        if (trimmed.StartsWith("USE", System.StringComparison.OrdinalIgnoreCase)) continue;
                        if (upgradeOnly && !IsUpgradeBatch(trimmed)) continue;

                        using (var cmd = new SqlCommand(batch, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch
            {
                // Connection/DB not available yet (e.g. database not created).
                // Pages surface connection errors individually.
            }
        }

        /// <summary>
        /// True for the self-guarded upgrade batches at the bottom of Schema.sql. Each one
        /// tests for its own object first, so replaying it on every startup is a no-op once
        /// the change has been applied.
        /// </summary>
        private static bool IsUpgradeBatch(string batch)
        {
            return Regex.IsMatch(batch,
                @"^\s*IF\s+(COL_LENGTH|OBJECT_ID|NOT\s+EXISTS\s*\(\s*SELECT\s+1\s+FROM\s+sys\.indexes)",
                RegexOptions.IgnoreCase);
        }

        private static bool UserTableExists()
        {
            try
            {
                using (var conn = Db.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("SELECT OBJECT_ID('dbo.[User]', 'U')", conn))
                    {
                        object result = cmd.ExecuteScalar();
                        return result != null && result != System.DBNull.Value;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
