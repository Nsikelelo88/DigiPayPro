using DigitalPayPro.Data;
using DigitalPayPro.Models;
using DigitalPayPro.Services;
using DigitalPayPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DigitalPayPro.Controllers
{
    [Authorize(Roles = "Admin,HR")]
    public class PayrollController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPayrollService _payrollService;
        private readonly IPdfService _pdfService;
        private readonly ILogger<PayrollController> _logger;

        public PayrollController(ApplicationDbContext context, IPayrollService payrollService,
            IPdfService pdfService, ILogger<PayrollController> logger)
        {
            _context = context;
            _payrollService = payrollService;
            _pdfService = pdfService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var payrollRecords = await _context.PayrollRecords
                    .Include(pr => pr.Employee)
                    .OrderByDescending(pr => pr.PayPeriod)
                    .ThenByDescending(pr => pr.DateProcessed)
                    .ToListAsync();

                return View(payrollRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payroll records");
                ViewBag.ErrorMessage = "An error occurred while loading payroll records.";
                return View(new List<PayrollRecord>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> PreviewPayslip(int id)
        {
            try
            {
                var payrollRecord = await _context.PayrollRecords
                    .Include(pr => pr.Employee)
                    .FirstOrDefaultAsync(pr => pr.Id == id);

                if (payrollRecord == null)
                {
                    return NotFound();
                }

                var pdfBytes = await _pdfService.GeneratePayslipPdfAsync(payrollRecord);

                var fileName = $"Payslip_{payrollRecord.Employee.LastName}_{payrollRecord.PayPeriod:yyyyMM}.pdf";

                // Force inline display in browser
                Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";

                _logger.LogInformation("Payslip preview generated for employee {EmployeeName} for period {Period} by {User}",
                    $"{payrollRecord.Employee.FirstName} {payrollRecord.Employee.LastName}",
                    payrollRecord.PayPeriod.ToString("yyyy-MM"), User.Identity.Name);

                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating payslip preview for payroll ID {PayrollId}", id);
                ViewBag.ErrorMessage = "An error occurred while generating the payslip preview.";
                return RedirectToAction("Details", new { id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Process()
        {
            try
            {
                var employees = await _context.Employees
                    .Where(e => e.IsActive)
                    .OrderBy(e => e.LastName)
                    .ThenBy(e => e.FirstName)
                    .ToListAsync();

                ViewBag.Employees = new SelectList(employees, "Id", "FullName");

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payroll process form");
                ViewBag.ErrorMessage = "An error occurred while loading the payroll form.";
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Process(PayrollProcessViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var payrollRecord = await _payrollService.ProcessPayrollAsync(
                        model.EmployeeId, model.PayPeriod, model.OvertimeHours, model.Incentives);

                    _logger.LogInformation("Payroll processed for employee ID {EmployeeId} for period {Period} by {User}",
                        model.EmployeeId, model.PayPeriod.ToString("yyyy-MM"), User.Identity.Name);

                    return RedirectToAction("Details", new { id = payrollRecord.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing payroll for employee ID {EmployeeId}", model.EmployeeId);
                    ModelState.AddModelError(string.Empty, $"Error processing payroll: {ex.Message}");
                }
            }

            try
            {
                var employees = await _context.Employees
                    .Where(e => e.IsActive)
                    .OrderBy(e => e.LastName)
                    .ThenBy(e => e.FirstName)
                    .ToListAsync();

                ViewBag.Employees = new SelectList(employees, "Id", "FullName");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading employees for dropdown");
                ModelState.AddModelError(string.Empty, "An error occurred while loading employee data.");
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            
            
                try
                {
                    // Include Employee data to avoid null reference errors
                    var payrollRecord = await _context.PayrollRecords
                        .Include(pr => pr.Employee)  // This is crucial!
                        .FirstOrDefaultAsync(pr => pr.Id == id);

                    if (payrollRecord == null)
                    {
                        return NotFound();
                    }

                    return View(payrollRecord);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading payroll details for ID {PayrollId}", id);
                    TempData["ErrorMessage"] = "An error occurred while loading payroll details.";
                    return RedirectToAction(nameof(Index));
                }
            
        }

        [HttpGet]
        public async Task<IActionResult> GeneratePayslip(int id)
        {
            try
            {
                var payrollRecord = await _context.PayrollRecords
                    .Include(pr => pr.Employee)
                    .FirstOrDefaultAsync(pr => pr.Id == id);

                if (payrollRecord == null)
                {
                    return NotFound();
                }

                var pdfBytes = await _pdfService.GeneratePayslipPdfAsync(payrollRecord);

                _logger.LogInformation("Payslip generated for employee {EmployeeName} for period {Period} by {User}",
                    $"{payrollRecord.Employee.FirstName} {payrollRecord.Employee.LastName}",
                    payrollRecord.PayPeriod.ToString("yyyy-MM"), User.Identity.Name);

                return File(pdfBytes, "application/pdf",
                    $"Payslip_{payrollRecord.Employee.LastName}_{payrollRecord.PayPeriod:yyyyMM}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating payslip for payroll ID {PayrollId}", id);
                ViewBag.ErrorMessage = "An error occurred while generating the payslip.";
                return RedirectToAction("Details", new { id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PeriodReport(DateTime? period)
        {
            try
            {
                var reportPeriod = period ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var payrollRecords = await _payrollService.GetPayrollRecordsByPeriodAsync(reportPeriod);

                if (!payrollRecords.Any())
                {
                    ViewBag.InfoMessage = $"No payroll records found for {reportPeriod:MMMM yyyy}";
                }

                ViewBag.SelectedPeriod = reportPeriod;
                return View(payrollRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading payroll period report for period {Period}", period);
                ViewBag.ErrorMessage = "An error occurred while loading the payroll report.";
                return View(new List<PayrollRecord>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GeneratePeriodReport(DateTime period)
        {
            try
            {
                var payrollRecords = await _payrollService.GetPayrollRecordsByPeriodAsync(period);

                if (!payrollRecords.Any())
                {
                    TempData["ErrorMessage"] = $"No payroll records found for {period:MMMM yyyy}";
                    return RedirectToAction("PeriodReport", new { period });
                }

                var pdfBytes = await _pdfService.GeneratePayrollReportPdfAsync(payrollRecords, period);

                _logger.LogInformation("Payroll report generated for period {Period} by {User}",
                    period.ToString("yyyy-MM"), User.Identity.Name);

                return File(pdfBytes, "application/pdf", $"Payroll_Report_{period:yyyyMM}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating payroll report for period {Period}", period);
                TempData["ErrorMessage"] = "An error occurred while generating the payroll report.";
                return RedirectToAction("PeriodReport", new { period });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessBulkPayroll(DateTime payPeriod)
        {
            try
            {
                var activeEmployees = await _context.Employees
                    .Where(e => e.IsActive)
                    .ToListAsync();

                int processedCount = 0;
                List<string> errors = new List<string>();

                foreach (var employee in activeEmployees)
                {
                    try
                    {
                        // Check if payroll already processed for this employee and period
                        var existingRecord = await _context.PayrollRecords
                            .FirstOrDefaultAsync(pr => pr.EmployeeId == employee.Id && pr.PayPeriod == payPeriod);

                        if (existingRecord != null)
                        {
                            errors.Add($"Payroll already processed for {employee.FirstName} {employee.LastName}");
                            continue;
                        }

                        // Process payroll with zero overtime and incentives
                        await _payrollService.ProcessPayrollAsync(employee.Id, payPeriod, 0, 0);
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error processing payroll for {employee.FirstName} {employee.LastName}: {ex.Message}");
                    }
                }

                if (errors.Any())
                {
                    TempData["WarningMessage"] = $"Processed {processedCount} employees, but encountered {errors.Count} errors.";
                    TempData["ErrorList"] = errors;
                }
                else
                {
                    TempData["SuccessMessage"] = $"Successfully processed payroll for {processedCount} employees.";
                }

                _logger.LogInformation("Bulk payroll processing completed for period {Period}: {Processed} processed, {Errors} errors",
                    payPeriod.ToString("yyyy-MM"), processedCount, errors.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk payroll processing for period {Period}", payPeriod);
                TempData["ErrorMessage"] = "An error occurred during bulk payroll processing.";
            }

            return RedirectToAction("PeriodReport", new { period = payPeriod });
        }
    }
}