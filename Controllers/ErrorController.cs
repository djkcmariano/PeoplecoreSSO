using Microsoft.AspNetCore.Mvc;

namespace AuthServer.Controllers
{
    public class ErrorController : Controller
    {
        public IActionResult InvalidAudience()
        {
            ViewBag.ErrorMessage = TempData["ErrorMessage"] ?? "Audience not registered.";
            ViewBag.ReturnUrl = TempData["ReturnUrl"] ?? "/"; // Default fallback
            return View();
        }
    }
}
