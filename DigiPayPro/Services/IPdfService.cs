using DigitalPayPro.Models;

namespace DigitalPayPro.Services
{
    public interface IPdfService
    {
        Task<byte[]> GeneratePayslipPdfAsync(PayrollRecord payrollRecord);
        Task<byte[]> GeneratePayrollReportPdfAsync(List<PayrollRecord> payrollRecords, DateTime payPeriod);
    }

    public class PdfSettings
    {
        public string CompanyName { get; set; }
        public string CompanyAddress { get; set; }
        public string CompanyLogo { get; set; }
    }
}