using DigitalPayPro.Models;
using DigitalPayPro.Data;
using Microsoft.EntityFrameworkCore;

namespace DigitalPayPro.Services
{
    public class PayrollService : IPayrollService
    {
        private readonly ApplicationDbContext _context;

        // Constructor: Initializes the PayrollService with the application's database context
        public PayrollService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Processes payroll for a specific employee and pay period
        public async Task<PayrollRecord> ProcessPayrollAsync(int employeeId, DateTime payPeriod,
            decimal overtimeHours, decimal incentives)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null) throw new Exception("Employee not found");

            // Check if payroll already processed for this period
            var existingRecord = await _context.PayrollRecords
                .FirstOrDefaultAsync(pr => pr.EmployeeId == employeeId && pr.PayPeriod == payPeriod);

            if (existingRecord != null)
            {
                throw new Exception($"Payroll already processed for {payPeriod:MMMM yyyy}");
            }

            // Calculate payroll components
            decimal basicPay = employee.BasicSalary;
            decimal overtimePay = overtimeHours * (employee.BasicSalary / 160); // Overtime calculation
            decimal grossPay = basicPay + overtimePay + incentives;

            decimal paye = await CalculatePAYE(grossPay); // Calculate PAYE tax
            decimal uif = await CalculateUIF(grossPay);   // Calculate UIF deduction
            decimal pension = grossPay * 0.075m;          // Pension contribution (7.5%)
            decimal medicalAid = 1200;                    // Fixed medical aid

            decimal totalDeductions = paye + uif + pension + medicalAid;
            decimal netPay = grossPay - totalDeductions;

            // Create and save the payroll record
            var payrollRecord = new PayrollRecord
            {
                EmployeeId = employeeId,
                PayPeriod = payPeriod,
                BasicPay = basicPay,
                OvertimeHours = overtimeHours,
                OvertimePay = overtimePay,
                Incentives = incentives,
                GrossPay = grossPay,
                PAYE = paye,
                UIF = uif,
                Pension = pension,
                MedicalAid = medicalAid,
                TotalDeductions = totalDeductions,
                NetPay = netPay
            };

            _context.PayrollRecords.Add(payrollRecord);
            await _context.SaveChangesAsync();

            return payrollRecord;
        }

        // Calculates the monthly PAYE (tax) based on South African tax brackets
        public async Task<decimal> CalculatePAYE(decimal taxableIncome)
        {
            // SARS tax calculation for 2024/2025
            decimal annualIncome = taxableIncome * 12;
            decimal tax = 0;

            if (annualIncome <= 237100)
            {
                tax = annualIncome * 0.18m;
            }
            else if (annualIncome <= 370500)
            {
                tax = 42678 + (annualIncome - 237100) * 0.26m;
            }
            else if (annualIncome <= 512800)
            {
                tax = 77362 + (annualIncome - 370500) * 0.31m;
            }
            else if (annualIncome <= 673000)
            {
                tax = 121475 + (annualIncome - 512800) * 0.36m;
            }
            else if (annualIncome <= 857900)
            {
                tax = 179147 + (annualIncome - 673000) * 0.39m;
            }
            else if (annualIncome <= 1817000)
            {
                tax = 251258 + (annualIncome - 857900) * 0.41m;
            }
            else
            {
                tax = 644489 + (annualIncome - 1817000) * 0.45m;
            }

            return tax / 12; // Return monthly tax amount
        }

        // Calculates the UIF deduction (1% of gross pay, capped at R177.12)
        public async Task<decimal> CalculateUIF(decimal grossPay)
        {
            // UIF is 1% of gross pay, capped at R177.12
            decimal uif = grossPay * 0.01m;
            return uif > 177.12m ? 177.12m : uif;
        }

        // Retrieves payroll history for a specific employee, ordered by most recent pay period
        public async Task<List<PayrollRecord>> GetEmployeePayrollHistoryAsync(int employeeId)
        {
            return await _context.PayrollRecords
                .Where(pr => pr.EmployeeId == employeeId)
                .OrderByDescending(pr => pr.PayPeriod)
                .ToListAsync();
        }

        // Retrieves all payroll records for a specific pay period, including employee details
        public async Task<List<PayrollRecord>> GetPayrollRecordsByPeriodAsync(DateTime payPeriod)
        {
            return await _context.PayrollRecords
                .Include(pr => pr.Employee)
                .Where(pr => pr.PayPeriod == payPeriod)
                .OrderBy(pr => pr.Employee.LastName)
                .ThenBy(pr => pr.Employee.FirstName)
                .ToListAsync();
        }
    }
}