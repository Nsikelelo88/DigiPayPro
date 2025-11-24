using DigitalPayPro.Data;
using DigitalPayPro.Models;
using DigitalPayPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DigitalPayPro.Controllers
{
    [Authorize]
    public class OvertimeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OvertimeController> _logger;

        public OvertimeController(ApplicationDbContext context, ILogger<OvertimeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                List<OvertimeRequest> overtimeRequests;

                if (User.IsInRole("Admin") || User.IsInRole("HR"))
                {
                    overtimeRequests = await _context.OvertimeRequests
                        .Include(orr => orr.Employee)
                        .OrderByDescending(orr => orr.DateRequested)
                        .ToListAsync();
                }
                else
                {
                    var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId");
                    if (employeeIdClaim != null && int.TryParse(employeeIdClaim.Value, out int employeeId))
                    {
                        overtimeRequests = await _context.OvertimeRequests
                            .Include(orr => orr.Employee)
                            .Where(orr => orr.EmployeeId == employeeId)
                            .OrderByDescending(orr => orr.DateRequested)
                            .ToListAsync();
                    }
                    else
                    {
                        return Forbid();
                    }
                }

                return View(overtimeRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading overtime requests");
                TempData["ErrorMessage"] = "An error occurred while loading overtime requests.";
                return View(new List<OvertimeRequest>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId");

                if (employeeIdClaim != null && int.TryParse(employeeIdClaim.Value, out int employeeId))
                {
                    var employee = await _context.Employees.FindAsync(employeeId);
                    if (employee == null) return NotFound();

                    var model = new OvertimeRequestViewModel
                    {
                        EmployeeId = employeeId,
                        EmployeeName = $"{employee.FirstName} {employee.LastName}",
                        Date = DateTime.Today
                    };

                    return View(model);
                }
                else
                {
                    // For HR/Admin creating overtime requests for employees
                    await PopulateEmployeesDropdown();
                    return View();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading overtime request form");
                TempData["ErrorMessage"] = "An error occurred while loading the overtime request form.";
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OvertimeRequestViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Validate date
                    if (model.Date > DateTime.Today)
                    {
                        ModelState.AddModelError("Date", "Overtime date cannot be in the future.");
                        return View(model);
                    }

                    var overtimeRequest = new OvertimeRequest
                    {
                        EmployeeId = model.EmployeeId,
                        Date = model.Date,
                        Hours = model.Hours,
                        Reason = model.Reason,
                        Status = "Pending",
                        DateRequested = DateTime.Now
                    };

                    _context.Add(overtimeRequest);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Overtime request created for employee ID {EmployeeId} by {User}",
                        model.EmployeeId, User.Identity.Name);

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating overtime request");
                    ModelState.AddModelError(string.Empty, "An error occurred while creating the overtime request. Please try again.");
                }
            }

            // Reload employees dropdown if needed
            if (User.IsInRole("Admin") || User.IsInRole("HR"))
            {
                await PopulateEmployeesDropdown();
            }

            return View(model);
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        public async Task<IActionResult> Approve(int id, string reviewNotes = "")
        {
            try
            {
                var overtimeRequest = await _context.OvertimeRequests
                    .Include(orr => orr.Employee)
                    .FirstOrDefaultAsync(orr => orr.Id == id);

                if (overtimeRequest == null) return NotFound();

                overtimeRequest.Status = "Approved";
                overtimeRequest.DateReviewed = DateTime.Now;
                overtimeRequest.ReviewedBy = User.Identity.Name;
                overtimeRequest.ReviewNotes = reviewNotes;

                _context.Update(overtimeRequest);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Overtime request ID {OvertimeRequestId} approved by {User}", id, User.Identity.Name);
                TempData["SuccessMessage"] = $"Overtime request for {overtimeRequest.Employee.FirstName} {overtimeRequest.Employee.LastName} has been approved.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving overtime request ID {OvertimeRequestId}", id);
                TempData["ErrorMessage"] = "An error occurred while approving the overtime request.";
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        public async Task<IActionResult> Reject(int id, string reviewNotes)
        {
            try
            {
                if (string.IsNullOrEmpty(reviewNotes))
                {
                    TempData["ErrorMessage"] = "Review notes are required when rejecting an overtime request.";
                    return RedirectToAction(nameof(Index));
                }

                var overtimeRequest = await _context.OvertimeRequests
                    .Include(orr => orr.Employee)
                    .FirstOrDefaultAsync(orr => orr.Id == id);

                if (overtimeRequest == null) return NotFound();

                overtimeRequest.Status = "Rejected";
                overtimeRequest.DateReviewed = DateTime.Now;
                overtimeRequest.ReviewedBy = User.Identity.Name;
                overtimeRequest.ReviewNotes = reviewNotes;

                _context.Update(overtimeRequest);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Overtime request ID {OvertimeRequestId} rejected by {User}", id, User.Identity.Name);
                TempData["SuccessMessage"] = $"Overtime request for {overtimeRequest.Employee.FirstName} {overtimeRequest.Employee.LastName} has been rejected.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting overtime request ID {OvertimeRequestId}", id);
                TempData["ErrorMessage"] = "An error occurred while rejecting the overtime request.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var overtimeRequest = await _context.OvertimeRequests
                    .Include(orr => orr.Employee)
                    .FirstOrDefaultAsync(orr => orr.Id == id);

                if (overtimeRequest == null) return NotFound();

                // Check permission
                if (!User.IsInRole("Admin") && !User.IsInRole("HR"))
                {
                    var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId");
                    if (employeeIdClaim == null || !int.TryParse(employeeIdClaim.Value, out int employeeId)
                        || overtimeRequest.EmployeeId != employeeId)
                    {
                        return Forbid();
                    }
                }

                return View(overtimeRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading overtime request details for ID {OvertimeRequestId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading overtime request details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // Helper method to populate the employees dropdown
        private async Task PopulateEmployeesDropdown()
        {
            var employees = await _context.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync();

            ViewBag.Employees = new SelectList(
                employees.Select(e => new { e.Id, FullName = e.FirstName + " " + e.LastName }),
                "Id",
                "FullName"
            );
        }
    }
}
