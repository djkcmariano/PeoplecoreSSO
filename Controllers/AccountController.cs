using AuthServer.Helpers;
using AuthServer.Models;
using AuthServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;

        public AccountController(AuthService authService)
        {
            _authService = authService;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            string callerUrl;

    // 1️⃣ Try to detect the caller system
    if (!string.IsNullOrWhiteSpace(returnUrl))
    {
        callerUrl = returnUrl;
    }
    else if (Request.Headers.TryGetValue("Origin", out var origin))
    {
        callerUrl = origin.ToString();
    }
    else
    {
        // fallback if nothing provided
        callerUrl = $"{Request.Scheme}://{Request.Host}";
    }

    // 2️⃣ Normalize to base URL (remove any paths, query strings, etc.)
    string cleanUrl;
    try
    {
        var uri = new Uri(callerUrl);
        cleanUrl = $"{uri.Scheme}://{uri.Host}:{uri.Port}";
    }
    catch
    {
        cleanUrl = callerUrl; // fallback if not a valid URI
    }

    ViewData["ReturnUrl"] = returnUrl;

    try
    {
        // 3️⃣ Validate audience from AuthService
        var audience = _authService.GetAudienceFromSystemID(cleanUrl);

        // ✅ OK — return login view
        return View(new UserModel());
    }
    catch (InvalidOperationException ex)
    {
        // 4️⃣ Handle missing audience gracefully
        TempData["ErrorMessage"] = ex.Message;
        TempData["CleanUrl"] = cleanUrl;
        TempData["ReturnUrl"] = returnUrl;

                return RedirectToAction("InvalidAudience", "Error");
    }
            }


        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(UserModel model, string returnUrl = null)
        {
            // Check for missing username/password
            if (string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError("", "Username and password are required.");
                return View(model);
            }
            var callerUrl = Request.Query["ReturnUrl"].ToString();

            // Authenticate via your AuthService
            var result = _authService.Authenticate(model, callerUrl);

            if (result == null || !result.Success)
            {
                ModelState.AddModelError("", result?.XMessage ?? "Invalid username or password.");
                return View(model);
            }

            // Redirect after successful login
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return Redirect(returnUrl + "?token=" + result.Token);
            }
            
            return Redirect("/?token=" + result.Token);
        }

        [HttpGet]
        public IActionResult TestDb()
        {
            try
            {
                var testResult = SQLHelp.ExecuteScalar("SELECT 'Connection works!' as Result");
                return Content($"✅ SUCCESS: {testResult}", "text/plain");
            }
            catch (Exception ex)
            {
                return Content($"❌ FAILED: {ex.Message}", "text/plain");
            }
        }
    }
}
