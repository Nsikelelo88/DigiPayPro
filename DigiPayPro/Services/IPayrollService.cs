using DigitalPayPro.Models;

namespace DigitalPayPro.Services
{
    public interface IPayrollService
    {
        Task<PayrollRecord> ProcessPayrollAsync(int employeeId, DateTime payPeriod,
            decimal overtimeHours, decimal incentives);
        Task<List<PayrollRecord>> GetEmployeePayrollHistoryAsync(int employeeId);
        Task<decimal> CalculatePAYE(decimal taxableIncome);
        Task<decimal> CalculateUIF(decimal grossPay);
        Task<List<PayrollRecord>> GetPayrollRecordsByPeriodAsync(DateTime payPeriod);
    }
}