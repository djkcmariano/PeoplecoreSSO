using AuthServer.Models;
using AuthServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest login)
        {
            if (login == null ||
                string.IsNullOrWhiteSpace(login.Username) ||
                string.IsNullOrWhiteSpace(login.Password) ||
                login.PayLocNo <= 0)
            {
                return BadRequest(new { success = false, message = "Missing credentials." });
            }
            string callerUrl = Request.Headers.TryGetValue("Origin", out var origin)
             ? origin.ToString()
             : $"{Request.Scheme}://{Request.Host}";
            string audience;
            try
            {
                audience = _authService.GetAudienceFromSystemID(callerUrl);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }


            //var result = _authService.Authenticate(login, callerUrl);
            var result = _authService.Authenticate(
                                                    login.Username!,
                                                    login.Password!,
                                                    login.PayLocNo,
                                                    callerUrl
                                                );

            if (result == null)
                return Unauthorized(new { success = false, message = "Invalid username or password." });

            return Ok(new
            {
                success = true,
                employeenumber = result.EmployeeNo,
                token = result.Token
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
                return BadRequest(new { success = false, message = "Missing token header." });

            string token = authHeader.FirstOrDefault()?.Split(' ').Last() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new { success = false, message = "Invalid token." });

            _authService.RevokeToken(token);

            return Ok(new { success = true, message = "Token revoked successfully." });
        }
    }
}
