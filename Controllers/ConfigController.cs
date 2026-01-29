using AuthServer.Helpers;
using clsBase;
using Microsoft.AspNetCore.Mvc;
using AuthServer.Helpers;
using AuthServer.Models;
using System;
using System.IO;

namespace AuthServer.Controllers
{
    [ApiController]
    [Route("api/config")]
    public class ConfigController : ControllerBase
    {
        private readonly string iniFilePath;

        public ConfigController()
        {
            iniFilePath = Path.Combine(AppContext.BaseDirectory, "connection.ini");
        }

        [HttpPost("save-dbconfig")]
        public IActionResult SaveDbConfig([FromBody] DbConfig request)
        {
            if (request == null)
                return BadRequest("Request body is empty.");

            if (string.IsNullOrWhiteSpace(request.Server) ||
                string.IsNullOrWhiteSpace(request.Database) ||
                string.IsNullOrWhiteSpace(request.User) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("All fields (Server, Database, User, Password) are required.");
            }

            try
            {
                string encServer = PeopleCoreCrypt.Encrypt(request.Server.Trim());
                string encDatabase = PeopleCoreCrypt.Encrypt(request.Database.Trim());
                string encUser = PeopleCoreCrypt.Encrypt(request.User.Trim());
                string encPassword = PeopleCoreCrypt.Encrypt(request.Password.Trim());

                string[] lines =
                {
                    encServer,
                    encDatabase,
                    encUser,
                    encPassword
                };

                System.IO.File.WriteAllLines(iniFilePath, lines);

                return Ok(new
                {
                    Message = "Database configuration saved successfully.",
                    FilePath = iniFilePath
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Error = $"Failed to save connection.ini: {ex.Message}"
                });
            }
        }
    }
}
