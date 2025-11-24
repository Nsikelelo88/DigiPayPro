using DigitalPayPro.Data;
using DigitalPayPro.Models;
using DigitalPayPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Admin,HR")]
[Route("Employees")]
public class EmployeeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(ApplicationDbContext context, ILogger<EmployeeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        try
        {
            var employees = await _context.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.LastName)
                .ThenBy(e => e.FirstName)
                .ToListAsync();

            return View(employees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading employees list");
            ViewBag.ErrorMessage = "An error occurred while loading employees.";
            return View(new List<Employee>());
        }
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeViewModel model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var employee = new Employee
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    IDNumber = model.IDNumber,
                    Position = model.Position,
                    BasicSalary = model.BasicSalary,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    BankName = model.BankName,
                    AccountNumber = model.AccountNumber,
                    BranchCode = model.BranchCode,
                    IsActive = model.IsActive,
                    DateJoined = model.DateJoined
                };

                _context.Add(employee);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Employee {EmployeeName} created by {User}",
                    $"{employee.FirstName} {employee.LastName}", User.Identity.Name);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee");
                ModelState.AddModelError(string.Empty, "An error occurred while creating the employee. Please try again.");
            }
        }

        return View(model);
    }

    [HttpGet("Edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            var model = new EmployeeViewModel
            {
                Id = employee.Id,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                IDNumber = employee.IDNumber,
                Position = employee.Position,
                BasicSalary = employee.BasicSalary,
                Email = employee.Email,
                PhoneNumber = employee.PhoneNumber,
                BankName = employee.BankName,
                AccountNumber = employee.AccountNumber,
                BranchCode = employee.BranchCode,
                IsActive = employee.IsActive,
                DateJoined = employee.DateJoined
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading employee for edit");
            ViewBag.ErrorMessage = "An error occurred while loading the employee details.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost("Edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EmployeeViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var employee = await _context.Employees.FindAsync(id);
                if (employee == null)
                {
                    return NotFound();
                }

                employee.FirstName = model.FirstName;
                employee.LastName = model.LastName;
                employee.IDNumber = model.IDNumber;
                employee.Position = model.Position;
                employee.BasicSalary = model.BasicSalary;
                employee.Email = model.Email;
                employee.PhoneNumber = model.PhoneNumber;
                employee.BankName = model.BankName;
                employee.AccountNumber = model.AccountNumber;
                employee.BranchCode = model.BranchCode;
                employee.IsActive = model.IsActive;
                employee.DateJoined = model.DateJoined;

                _context.Update(employee);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Employee {EmployeeName} updated by {User}",
                    $"{employee.FirstName} {employee.LastName}", User.Identity.Name);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeExists(model.Id))
                {
                    return NotFound();
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee");
                ModelState.AddModelError(string.Empty, "An error occurred while updating the employee. Please try again.");
            }
        }

        return View(model);
    }

    [HttpPost("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            employee.IsActive = false;
            _context.Update(employee);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Employee {EmployeeName} deleted by {User}",
                $"{employee.FirstName} {employee.LastName}", User.Identity.Name);

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting employee");
            ViewBag.ErrorMessage = "An error occurred while deleting the employee. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet("Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var employee = await _context.Employees
                .Include(e => e.PayrollRecords)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading employee details");
            ViewBag.ErrorMessage = "An error occurred while loading employee details.";
            return RedirectToAction(nameof(Index));
        }
    }

    private bool EmployeeExists(int id)
    {
        return _context.Employees.Any(e => e.Id == id);
    }
}