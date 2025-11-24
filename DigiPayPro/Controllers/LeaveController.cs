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
    public class LeaveController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LeaveController> _logger;

        public LeaveController(ApplicationDbContext context, ILogger<LeaveController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                List<LeaveRequest> leaveRequests;

                if (User.IsInRole("Admin") || User.IsInRole("HR"))
                {
                    leaveRequests = await _context.LeaveRequests
                        .Include(lr => lr.Employee)
                        .OrderByDescending(lr => lr.DateRequested)
                        .ToListAsync();
                }
                else
                {
                    // Get EmployeeId from claims
                    var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId");
                    if (employeeIdClaim != null && int.TryParse(employeeIdClaim.Value, out int employeeId))
                    {
                        leaveRequests = await _context.LeaveRequests
                            .Include(lr => lr.Employee)
                            .Where(lr => lr.EmployeeId == employeeId)
                            .OrderByDescending(lr => lr.DateRequested)
                            .ToListAsync();
                    }
                    else
                    {
                        return Forbid();
                    }
                }

                return View(leaveRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading leave requests");
                TempData["ErrorMessage"] = "An error occurred while loading leave requests.";
                return View(new List<LeaveRequest>());
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
                    if (employee == null)
                    {
                        return NotFound();
                    }

                    var model = new LeaveRequestViewModel
                    {
                        EmployeeId = employeeId,
                        EmployeeName = $"{employee.FirstName} {employee.LastName}"
                    };

                    return View(model);
                }
                else
                {
                    // For Admin/HR: populate employees dropdown
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

                    return View();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading leave request form");
                TempData["ErrorMessage"] = "An error occurred while loading the leave request form.";
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeaveRequestViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Validate dates
                    if (model.StartDate > model.EndDate)
                    {
                        ModelState.AddModelError("EndDate", "End date must be after start date.");
                        return View(model);
                    }

                    if (model.StartDate < DateTime.Today)
                    {
                        ModelState.AddModelError("StartDate", "Start date cannot be in the past.");
                        return View(model);
                    }

                    var leaveRequest = new LeaveRequest
                    {
                        EmployeeId = model.EmployeeId,
                        StartDate = model.StartDate,
                        EndDate = model.EndDate,
                        LeaveType = model.LeaveType,
                        Reason = model.Reason,
                        Status = "Pending",
                        DateRequested = DateTime.Now
                    };

                    _context.Add(leaveRequest);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Leave request created for employee ID {EmployeeId} by {User}",
                        model.EmployeeId, User.Identity.Name);

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating leave request");
                    ModelState.AddModelError(string.Empty, "An error occurred while creating the leave request. Please try again.");
                }
            }

            // Reload employees dropdown if needed
            if (User.IsInRole("Admin") || User.IsInRole("HR"))
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

            return View(model);
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        public async Task<IActionResult> Approve(int id, string reviewNotes = "")
        {
            try
            {
                var leaveRequest = await _context.LeaveRequests
                    .Include(lr => lr.Employee)
                    .FirstOrDefaultAsync(lr => lr.Id == id);

                if (leaveRequest == null) return NotFound();

                leaveRequest.Status = "Approved";
                leaveRequest.DateReviewed = DateTime.Now;
                leaveRequest.ReviewedBy = User.Identity.Name;
                leaveRequest.ReviewNotes = reviewNotes;

                _context.Update(leaveRequest);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Leave request ID {LeaveRequestId} approved by {User}", id, User.Identity.Name);
                TempData["SuccessMessage"] = $"Leave request for {leaveRequest.Employee.FirstName} {leaveRequest.Employee.LastName} has been approved.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving leave request ID {LeaveRequestId}", id);
                TempData["ErrorMessage"] = "An error occurred while approving the leave request.";
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
                    TempData["ErrorMessage"] = "Review notes are required when rejecting a leave request.";
                    return RedirectToAction(nameof(Index));
                }

                var leaveRequest = await _context.LeaveRequests
                    .Include(lr => lr.Employee)
                    .FirstOrDefaultAsync(lr => lr.Id == id);

                if (leaveRequest == null) return NotFound();

                leaveRequest.Status = "Rejected";
                leaveRequest.DateReviewed = DateTime.Now;
                leaveRequest.ReviewedBy = User.Identity.Name;
                leaveRequest.ReviewNotes = reviewNotes;

                _context.Update(leaveRequest);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Leave request ID {LeaveRequestId} rejected by {User}", id, User.Identity.Name);
                TempData["SuccessMessage"] = $"Leave request for {leaveRequest.Employee.FirstName} {leaveRequest.Employee.LastName} has been rejected.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting leave request ID {LeaveRequestId}", id);
                TempData["ErrorMessage"] = "An error occurred while rejecting the leave request.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var leaveRequest = await _context.LeaveRequests
                    .Include(lr => lr.Employee)
                    .FirstOrDefaultAsync(lr => lr.Id == id);

                if (leaveRequest == null) return NotFound();

                // Check permission
                if (!User.IsInRole("Admin") && !User.IsInRole("HR"))
                {
                    var employeeIdClaim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId");
                    if (employeeIdClaim == null || !int.TryParse(employeeIdClaim.Value, out int employeeId) || leaveRequest.EmployeeId != employeeId)
                    {
                        return Forbid();
                    }
                }

                return View(leaveRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading leave request details for ID {LeaveRequestId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading leave request details.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
