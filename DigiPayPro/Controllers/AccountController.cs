using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DigitalPayPro.Data;
using DigitalPayPro.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace DigitalPayPro.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        // Constructor: Initializes the controller with database context and logger
        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Displays the login page
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl; // Store return URL for redirect after login
            return View(); // Show login view
        }

        // POST: Handles user login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl; // Store return URL

            if (ModelState.IsValid) // Check if form data is valid
            {
                try
                {
                    // Find user by username
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Username == model.Username);

                    // If user exists and password is correct
                    if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                    {
                        // Create authentication claims
                        var claims = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, user.Username),
                                new Claim(ClaimTypes.Role, user.Role),
                                new Claim("UserId", user.Id.ToString()),
                                new Claim(ClaimTypes.GivenName, $"{user.FirstName} {user.LastName}")
                            };

                        // Add EmployeeId claim if available
                        if (user.EmployeeId.HasValue)
                        {
                            claims.Add(new Claim("EmployeeId", user.EmployeeId.Value.ToString()));
                        }

                        // Create identity and authentication properties
                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        var authProperties = new AuthenticationProperties
                        {
                            IsPersistent = model.RememberMe, // Remember me option
                            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) // Session expiration
                        };

                        // Sign in the user
                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        _logger.LogInformation("User {Username} logged in at {Time}", user.Username, DateTime.UtcNow);

                        // Redirect to return URL if valid, otherwise home page
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }

                        return RedirectToAction("Index", "Home");
                    }

                    // Invalid login attempt
                    ModelState.AddModelError(string.Empty, "Invalid login attempt");
                    _logger.LogWarning("Invalid login attempt for username: {Username}", model.Username);
                }
                catch (Exception ex)
                {
                    // Log error and show generic error message
                    _logger.LogError(ex, "Error during login for username: {Username}", model.Username);
                    ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
                }
            }

            // Return login view with errors
            return View(model);
        }

        // POST: Logs out the current user
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // Sign out
            _logger.LogInformation("User logged out at {Time}", DateTime.UtcNow);
            return RedirectToAction("Index", "Home"); // Redirect to home page
        }

        // GET: Displays the access denied page
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View(); // Show access denied view
        }
    }
}