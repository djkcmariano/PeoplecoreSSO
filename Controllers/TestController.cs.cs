using AuthServer.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Data;

namespace AuthServer.Controllers
{
    public class TestController : Controller
    {
        // GET: /Test/DatabaseConnection
        [HttpGet]
        public IActionResult DatabaseConnection()
        {
            var result = new
            {
                Success = false,
                Message = "",
                ConnectionString = "",
                ServerVersion = "",
                DatabaseName = "",
                UserName = "",
                TestQueryResult = "",
                ErrorDetails = "",
                IniFileExists = false,
                IniFilePath = ""
            };

            try
            {
                // Check ini file first
                var iniPath = Path.Combine(AppContext.BaseDirectory, "connection.ini");
                var iniExists = System.IO.File.Exists(iniPath);

                // Test 1: Check if connection string is loaded
                var connStr = SQLHelp.ConnectionString;
                if (string.IsNullOrEmpty(connStr))
                {
                    result = new
                    {
                        Success = false,
                        Message = "❌ Connection string is empty",
                        ConnectionString = "Not loaded",
                        ServerVersion = "",
                        DatabaseName = "",
                        UserName = "",
                        TestQueryResult = "",
                        ErrorDetails = "Connection string failed to load. Check if connection.ini exists and PeopleCoreCrypt can decrypt it.",
                        IniFileExists = iniExists,
                        IniFilePath = iniPath
                    };
                    return View(result);
                }

                // Mask sensitive parts of connection string for display
                var maskedConnStr = MaskConnectionString(connStr);

                // Test 2: Try to open connection
                using (var conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    var serverVersion = conn.ServerVersion;
                    var database = conn.Database;
                    var dataSource = conn.DataSource;

                    // Test 3: Execute a simple query
                    using (var cmd = new SqlCommand("SELECT @@VERSION as Version, DB_NAME() as DatabaseName, SYSTEM_USER as UserName", conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                result = new
                                {
                                    Success = true,
                                    Message = "✅ Database connection successful!",
                                    ConnectionString = maskedConnStr,
                                    ServerVersion = reader["Version"].ToString(),
                                    DatabaseName = reader["DatabaseName"].ToString(),
                                    UserName = reader["UserName"].ToString(),
                                    TestQueryResult = "Query executed successfully",
                                    ErrorDetails = "",
                                    IniFileExists = iniExists,
                                    IniFilePath = iniPath
                                };
                            }
                        }
                    }
                }

                // Test 4: Test SQLHelp methods
                try
                {
                    var testTable = SQLHelp.ExecuteDataTable("SELECT 'SQLHelp.ExecuteDataTable works!' as TestResult");
                    var testScalar = SQLHelp.ExecuteScalar("SELECT 'SQLHelp.ExecuteScalar works!' as TestResult");

                    var sqlHelpTest = "";
                    if (testTable != null && testTable.Rows.Count > 0)
                    {
                        sqlHelpTest += testTable.Rows[0]["TestResult"].ToString() + " | ";
                    }
                    if (testScalar != null)
                    {
                        sqlHelpTest += testScalar.ToString();
                    }

                    result = new
                    {
                        result.Success,
                        result.Message,
                        result.ConnectionString,
                        result.ServerVersion,
                        result.DatabaseName,
                        result.UserName,
                        TestQueryResult = sqlHelpTest,
                        ErrorDetails = "",
                        result.IniFileExists,
                        result.IniFilePath
                    };
                }
                catch (Exception ex)
                {
                    result = new
                    {
                        Success = true, // Connection worked, but SQLHelp had issues
                        Message = "⚠️ Connection OK, but SQLHelp test failed",
                        result.ConnectionString,
                        result.ServerVersion,
                        result.DatabaseName,
                        result.UserName,
                        TestQueryResult = "SQLHelp methods failed",
                        ErrorDetails = $"SQLHelp Error: {ex.Message}",
                        result.IniFileExists,
                        result.IniFilePath
                    };
                }
            }
            catch (SqlException sqlEx)
            {
                result = new
                {
                    Success = false,
                    Message = "❌ SQL Connection Failed",
                    ConnectionString = "Unable to connect",
                    ServerVersion = "",
                    DatabaseName = "",
                    UserName = "",
                    TestQueryResult = "",
                    ErrorDetails = $"SQL Error Code: {sqlEx.Number}\nMessage: {sqlEx.Message}\n\nPossible causes:\n- SQL Server not running\n- Incorrect credentials\n- Firewall blocking connection\n- Database doesn't exist",
                    IniFileExists = System.IO.File.Exists(Path.Combine(AppContext.BaseDirectory, "connection.ini")),
                    IniFilePath = Path.Combine(AppContext.BaseDirectory, "connection.ini")
                };
            }
            catch (Exception ex)
            {
                result = new
                {
                    Success = false,
                    Message = "❌ Connection Test Failed",
                    ConnectionString = "Error occurred",
                    ServerVersion = "",
                    DatabaseName = "",
                    UserName = "",
                    TestQueryResult = "",
                    ErrorDetails = $"Error Type: {ex.GetType().Name}\nMessage: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    IniFileExists = System.IO.File.Exists(Path.Combine(AppContext.BaseDirectory, "connection.ini")),
                    IniFilePath = Path.Combine(AppContext.BaseDirectory, "connection.ini")
                };
            }

            return View(result);
        }

        private string MaskConnectionString(string connStr)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connStr);
                builder.Password = "********";
                return builder.ConnectionString;
            }
            catch
            {
                return "Connection string format error";
            }
        }

        // API endpoint for AJAX calls
        [HttpGet]
        public IActionResult CheckConnection()
        {
            try
            {
                using (var conn = new SqlConnection(SQLHelp.ConnectionString))
                {
                    conn.Open();
                    return Json(new
                    {
                        success = true,
                        message = "Database connected successfully",
                        database = conn.Database,
                        server = conn.DataSource,
                        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    type = ex.GetType().Name
                });
            }
        }
    }
}