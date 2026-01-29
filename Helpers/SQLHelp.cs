using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AuthServer.Helpers
{
    public static class SQLHelp
    {
        private static readonly string connStr;

        public static string ConnectionString => connStr;

        static SQLHelp()
        {
            try
            {
                string iniPath = Path.Combine(AppContext.BaseDirectory, "connection.ini");
                if (!File.Exists(iniPath))
                    throw new FileNotFoundException($"Missing configuration file: {iniPath}");

                var lines = File.ReadAllLines(iniPath);
                if (lines.Length < 4)
                    throw new Exception("Invalid connection.ini format. Expected 4 lines (Server, Database, User, Password).");

                string server = PeopleCoreCrypt.Decrypt(lines[0]);
                string database = PeopleCoreCrypt.Decrypt(lines[1]);
                string user = PeopleCoreCrypt.Decrypt(lines[2]);
                string password = PeopleCoreCrypt.Decrypt(lines[3]);

                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = server,
                    InitialCatalog = database,
                    UserID = user,
                    Password = password,
                    MultipleActiveResultSets = true,
                    TrustServerCertificate = true
                };

                connStr = builder.ConnectionString;
            }
            catch (Exception ex)
            {
                throw new Exception($"SQLHelp initialization failed: {ex.Message}", ex);
            }
        }

        #region 🔸 Database Access Helpers

        public static DataTable ExecuteDataTable(string storedProc, params object[] parameters)
        {
            using var ds = ExecuteDataSet(storedProc, parameters);
            return ds != null && ds.Tables.Count > 0 ? ds.Tables[0] : new DataTable();
        }

        public static DataSet ExecuteDataSet(string storedProc, params object[] parameters)
        {
            try
            {
                using var conn = new SqlConnection(connStr);
                using var cmd = new SqlCommand(storedProc, conn) { CommandType = CommandType.StoredProcedure };
                AddParameters(cmd, parameters);

                using var adapter = new SqlDataAdapter(cmd);
                var ds = new DataSet();
                adapter.Fill(ds);
                return ds;
            }
            catch
            {
                return null;
            }
        }

        public static int ExecuteNonQuery(string storedProc, params object[] parameters)
        {
            try
            {
                using var conn = new SqlConnection(connStr);
                using var cmd = new SqlCommand(storedProc, conn) { CommandType = CommandType.StoredProcedure };
                AddParameters(cmd, parameters);

                conn.Open();
                return cmd.ExecuteNonQuery();
            }
            catch
            {
                return 0;
            }
        }

        public static object ExecuteScalar(string storedProc, params object[] parameters)
        {
            try
            {
                using var conn = new SqlConnection(connStr);
                using var cmd = new SqlCommand(storedProc, conn) { CommandType = CommandType.StoredProcedure };
                AddParameters(cmd, parameters);

                conn.Open();
                return cmd.ExecuteScalar();
            }
            catch
            {
                return null;
            }
        }

        private static void AddParameters(SqlCommand cmd, object[] parameters)
        {
            if (parameters == null) return;

            int i = 1;
            foreach (var p in parameters)
            {
                if (p is SqlParameter sqlParam)
                {
                    cmd.Parameters.Add(sqlParam);
                }
                else
                {
                    cmd.Parameters.AddWithValue($"@p{i}", p ?? DBNull.Value);
                    i++;
                }
            }
        }

        #endregion

        #region 🔸 Async Versions

        public static async Task<DataTable> ExecuteDataTableAsync(string storedProc, params object[] parameters)
        {
            var ds = await ExecuteDataSetAsync(storedProc, parameters);
            return ds != null && ds.Tables.Count > 0 ? ds.Tables[0] : null;
        }

        public static async Task<DataSet> ExecuteDataSetAsync(string storedProc, params object[] parameters)
        {
            using var conn = new SqlConnection(connStr);
            using var cmd = new SqlCommand(storedProc, conn) { CommandType = CommandType.StoredProcedure };
            AddParameters(cmd, parameters);

            await conn.OpenAsync();
            var ds = new DataSet();
            using var adapter = new SqlDataAdapter(cmd);
            await Task.Run(() => adapter.Fill(ds));
            return ds;
        }

        public static async Task<int> ExecuteNonQueryAsync(string storedProc, params object[] parameters)
        {
            using var conn = new SqlConnection(connStr);
            using var cmd = new SqlCommand(storedProc, conn) { CommandType = CommandType.StoredProcedure };
            AddParameters(cmd, parameters);

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<object> ExecuteScalarAsync(string storedProc, params object[] parameters)
        {
            using var conn = new SqlConnection(connStr);
            using var cmd = new SqlCommand(storedProc, conn) { CommandType = CommandType.StoredProcedure };
            AddParameters(cmd, parameters);

            await conn.OpenAsync();
            return await cmd.ExecuteScalarAsync();
        }

        #endregion

        #region 🔸 Utility Converters

        public static class Generic
        {
            public static int ToInt(object obj) => obj != null && int.TryParse(obj.ToString(), out var val) ? val : 0;
            public static string ToStr(object obj) => obj?.ToString() ?? string.Empty;
            public static decimal ToDec(object obj) => obj != null && decimal.TryParse(obj.ToString(), out var val) ? val : 0;
            public static bool ToBol(object obj) => obj != null && bool.TryParse(obj.ToString(), out var val) && val;
            public static double ToDbl(object obj) => obj != null && double.TryParse(obj.ToString(), out var val) ? val : 0;
        }

        #endregion

        #region 🔸 Report Helpers (Optional, copy your original methods)

        // Copy GetBasePath, BuildParameters, GetReportData, GetSubReports, etc.
        // These can now call ExecuteDataTable/ExecuteDataSet or their async versions.

        #endregion
    }
}
