using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DigitalPayPro.Data;
using DigitalPayPro.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace DigitalPayPro.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var dashboardData = new DashboardViewModel
                {
                    TotalEmployees = await _context.Employees.CountAsync(),
                    ActiveEmployees = await _context.Employees.CountAsync(e => e.IsActive),
                    PendingLeaveRequests = await _context.LeaveRequests.CountAsync(lr => lr.Status == "Pending"),
                    PendingOvertimeRequests = await _context.OvertimeRequests.CountAsync(orr => orr.Status == "Pending")
                };

                // Calculate total payroll for current month
                var currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var payrollThisMonth = await _context.PayrollRecords
                    .Where(pr => pr.PayPeriod == currentMonth)
                    .ToListAsync();

                dashboardData.TotalPayrollThisMonth = payrollThisMonth.Sum(pr => pr.NetPay);

                // Get recent payroll records
                dashboardData.RecentPayrolls = await _context.PayrollRecords
                    .Include(pr => pr.Employee)
                    .OrderByDescending(pr => pr.DateProcessed)
                    .Take(5)
                    .ToListAsync();

                // Get recent leave requests
                dashboardData.RecentLeaveRequests = await _context.LeaveRequests
                    .Include(lr => lr.Employee)
                    .OrderByDescending(lr => lr.DateRequested)
                    .Take(5)
                    .ToListAsync();

                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data");
                ViewBag.ErrorMessage = "An error occurred while loading dashboard data.";
                return View(new DashboardViewModel());
            }
        }

        [Authorize(Roles = "Admin,HR")]
        public IActionResult AdminDashboard()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}