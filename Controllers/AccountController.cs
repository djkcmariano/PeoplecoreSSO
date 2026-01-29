using AuthServer.Helpers;
using AuthServer.Models;
using AuthServer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace AuthServer.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;

        public AccountController(AuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Validates a return URL to prevent open redirects.
        /// Allows relative URLs that are local to this application and absolute URLs
        /// that match the current scheme, host and port.
        /// Returns null if the URL is not considered safe.
        /// </summary>
        /// <param name="returnUrl">The URL provided by the client.</param>
        /// <returns>A safe URL string or null.</returns>
        private string ValidateReturnUrl(string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return null;
            }

            // Prefer the built-in check for local/relative URLs.
            if (Url != null && Url.IsLocalUrl(returnUrl))
            {
                return returnUrl;
            }

            // Fallback for absolute URLs: require same scheme, host and port.
            if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
            {
                return null;
            }

            var currentScheme = Request.Scheme;
            var currentHostString = Request.Host;
            var currentHost = currentHostString.Host;
            var currentPort = currentHostString.Port;

            if (!string.Equals(uri.Scheme, currentScheme, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!string.Equals(uri.Host, currentHost, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (currentPort.HasValue && uri.Port != currentPort.Value)
            {
                return null;
            }

            return returnUrl;
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
            var safeReturnUrl = ValidateReturnUrl(returnUrl);
            if (!string.IsNullOrEmpty(safeReturnUrl))
            {
                string redirectUrl;

                if (Uri.TryCreate(safeReturnUrl, UriKind.Absolute, out var absoluteUri))
                {
                    // Absolute URL: safely append the token as a query parameter.
                    var builder = new UriBuilder(absoluteUri);
                    var query = string.IsNullOrEmpty(builder.Query)
                        ? string.Empty
                        : builder.Query.TrimStart('?') + "&";
                    query += "token=" + Uri.EscapeDataString(result.Token);
                    builder.Query = query;
                    redirectUrl = builder.Uri.ToString();
                }
                else
                {
                    // Relative/local URL: safely append the token as a query parameter.
                    redirectUrl = QueryHelpers.AddQueryString(safeReturnUrl, "token", result.Token);
                }

                return Redirect(redirectUrl);
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
