using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace ForteLogisticsApp.Data
{
    /// <summary>
    /// Thin ADO.NET helper around the ForteDB connection.
    /// All access uses parameterised queries.
    /// </summary>
    public static class Db
    {
        public static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["Logisticore"].ConnectionString; }
        }

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        /// <summary>
        /// Binds name/value pairs as real SQL parameters. Values are NEVER concatenated into
        /// the command text, so a value such as <c>' OR 1=1--</c> is sent to SQL Server as a
        /// literal string and can never alter the shape of the statement.
        ///
        /// The guards below turn a developer mistake (an odd argument, a name missing its @,
        /// a value accidentally passed where a name belongs) into an immediate, loud failure
        /// rather than something that silently degrades into string building.
        /// </summary>
        private static void AddParams(SqlCommand cmd, object[] pairs)
        {
            if (pairs == null || pairs.Length == 0) return;

            if (pairs.Length % 2 != 0)
            {
                throw new ArgumentException(
                    "Db parameters must be supplied as name/value pairs; received " +
                    pairs.Length + " argument(s). Never concatenate a value into the SQL text.");
            }

            for (int i = 0; i < pairs.Length; i += 2)
            {
                string name = pairs[i] as string;
                if (string.IsNullOrEmpty(name) || name[0] != '@')
                {
                    throw new ArgumentException(
                        "Db parameter name at position " + i + " must be a string starting with '@'.");
                }

                object value = pairs[i + 1] ?? DBNull.Value;
                cmd.Parameters.AddWithValue(name, value);
            }
        }

        private static SqlCommand CreateCommand(SqlConnection conn, string sql, object[] pairs)
        {
            return CreateCommand(conn, null, sql, pairs);
        }

        private static SqlCommand CreateCommand(SqlConnection conn, SqlTransaction tx, string sql, object[] pairs)
        {
            if (string.IsNullOrEmpty(sql))
                throw new ArgumentException("SQL command text may not be empty.");

            var cmd = new SqlCommand(sql, conn, tx);
            cmd.CommandType = CommandType.Text;
            cmd.CommandTimeout = 30;
            AddParams(cmd, pairs);
            return cmd;
        }

        /// <summary>
        /// Runs <paramref name="work"/> against one open connection inside one transaction,
        /// committing on success and rolling back on any exception. Use the (conn, tx)
        /// overloads of Query/NonQuery/Scalar/Insert inside <paramref name="work"/> so every
        /// statement shares the same transaction - a multi-step write (e.g. a payment that
        /// updates Payment, Shipment and TrackingHistory together) must never leave the
        /// database half-updated if a later step fails.
        /// </summary>
        public static void RunInTransaction(Action<SqlConnection, SqlTransaction> work)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        work(conn, tx);
                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>Runs a SELECT and returns a DataTable. Params passed as name,value,name,value...</summary>
        public static DataTable Query(string sql, params object[] pairs)
        {
            using (var conn = GetConnection())
            using (var cmd = CreateCommand(conn, sql, pairs))
            {
                var dt = new DataTable();
                using (var da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
                return dt;
            }
        }

        /// <summary>Runs INSERT/UPDATE/DELETE, returns rows affected.</summary>
        public static int NonQuery(string sql, params object[] pairs)
        {
            using (var conn = GetConnection())
            using (var cmd = CreateCommand(conn, sql, pairs))
            {
                conn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Runs a scalar query (e.g. SELECT SCOPE_IDENTITY or COUNT).</summary>
        public static object Scalar(string sql, params object[] pairs)
        {
            using (var conn = GetConnection())
            using (var cmd = CreateCommand(conn, sql, pairs))
            {
                conn.Open();
                return cmd.ExecuteScalar();
            }
        }

        /// <summary>Runs an INSERT and returns the new identity value.</summary>
        public static int Insert(string sql, params object[] pairs)
        {
            var result = Scalar(sql + "; SELECT CAST(SCOPE_IDENTITY() AS INT);", pairs);
            return (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);
        }

        /// <summary>Transaction-bound SELECT - see <see cref="RunInTransaction"/>.</summary>
        public static DataTable Query(SqlConnection conn, SqlTransaction tx, string sql, params object[] pairs)
        {
            using (var cmd = CreateCommand(conn, tx, sql, pairs))
            {
                var dt = new DataTable();
                using (var da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
                return dt;
            }
        }

        /// <summary>Transaction-bound INSERT/UPDATE/DELETE - see <see cref="RunInTransaction"/>.</summary>
        public static int NonQuery(SqlConnection conn, SqlTransaction tx, string sql, params object[] pairs)
        {
            using (var cmd = CreateCommand(conn, tx, sql, pairs))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Transaction-bound scalar query - see <see cref="RunInTransaction"/>.</summary>
        public static object Scalar(SqlConnection conn, SqlTransaction tx, string sql, params object[] pairs)
        {
            using (var cmd = CreateCommand(conn, tx, sql, pairs))
            {
                return cmd.ExecuteScalar();
            }
        }

        /// <summary>Transaction-bound INSERT returning the new identity value - see <see cref="RunInTransaction"/>.</summary>
        public static int Insert(SqlConnection conn, SqlTransaction tx, string sql, params object[] pairs)
        {
            var result = Scalar(conn, tx, sql + "; SELECT CAST(SCOPE_IDENTITY() AS INT);", pairs);
            return (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);
        }

        public static int ToInt(object o, int fallback = 0)
        {
            return (o == null || o == DBNull.Value) ? fallback : Convert.ToInt32(o);
        }

        public static decimal ToDecimal(object o, decimal fallback = 0)
        {
            return (o == null || o == DBNull.Value) ? fallback : Convert.ToDecimal(o);
        }

        public static string ToStr(object o)
        {
            return (o == null || o == DBNull.Value) ? string.Empty : o.ToString();
        }
    }
}
