using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ItemProcessingApp.Models;

namespace ItemProcessingApp.Controllers
{
    /// <summary>
    /// Simple cookie-based authentication.
    /// Credentials are stored in appsettings.json (Admin section).
    /// </summary>
    public class AccountController : Controller
    {
        private readonly IConfiguration _config;

        public AccountController(IConfiguration config)
        {
            _config = config;
        }

        // ── Login GET ────────────────────────────────────────
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Already logged in? Redirect home.
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Items");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // ── Login POST ───────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            // Validate against appsettings.json  [Admin] section
            var expectedUser = _config["Admin:Username"];
            var expectedPass = _config["Admin:Password"];

            if (model.Username == expectedUser && model.Password == expectedPass)
            {
                // Build a simple claims identity
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, model.Username),
                    new Claim(ClaimTypes.Role, "Admin")
                };

                var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                var authProps = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc   = model.RememberMe
                        ? DateTimeOffset.UtcNow.AddDays(7)
                        : DateTimeOffset.UtcNow.AddHours(2)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    authProps);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Items");
            }

            ModelState.AddModelError("", "Invalid username or password.");
            return View(model);
        }

        // ── Logout ───────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // ── Access Denied ────────────────────────────────────
        public IActionResult AccessDenied() => View();
    }
}
