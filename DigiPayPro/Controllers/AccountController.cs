using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DigitalPayPro.Data;
using DigitalPayPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace DigitalPayPro.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;
        // Dummy bcrypt hash used to mitigate user enumeration timing attacks
        private const string _dummyBcryptHash = "$2a$10$e0MYzXyjpJS7Pd0RVvHwHeFx4N6t8f0uZ1WvS1Qw7rjvWfWb4y6e.";

        // Constructor: Initializes the controller with database context and logger
        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Displays the login page
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl; // Store return URL for redirect after login
            return View(); // Show login view
        }

        // POST: Handles user login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
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
                    bool isValid = false;

                    // Verify password (use dummy verify when user not found to mitigate timing attacks)
                    if (user != null)
                    {
                        isValid = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);
                    }
                    else
                    {
                        // Run a dummy verify to make the response time similar whether the user exists or not
                        BCrypt.Net.BCrypt.Verify(model.Password, _dummyBcryptHash);
                    }

                    // If user exists and password is correct
                    if (isValid && user != null)
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

                        // Set a success flash message
                        TempData["Success"] = $"Welcome back, {user.FirstName}";

                        // Redirect to return URL if valid, otherwise home page
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }

                        return RedirectToAction("Index", "Home");
                    }

                    // Invalid login attempt
                    TempData["Error"] = "Invalid username or password.";
                    ModelState.AddModelError(string.Empty, "Invalid login attempt");
                    _logger.LogWarning("Invalid login attempt for username: {Username}", model.Username);
                }
                catch (Exception ex)
                {
                    // Log error and show generic error message
                    _logger.LogError(ex, "Error during login for username: {Username}", model.Username);
                    TempData["Error"] = "An error occurred during login. Please try again.";
                    ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
                }
            }

            // Return login view with errors
            return View(model);
        }

        // POST: Logs out the current user
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); // Sign out
            _logger.LogInformation("User logged out at {Time}", DateTime.UtcNow);
            TempData["Success"] = "You have been logged out.";
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