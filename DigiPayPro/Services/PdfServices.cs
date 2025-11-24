using DigitalPayPro.Models;
using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.Extensions.Options;

namespace DigitalPayPro.Services
{
    public class PdfService : IPdfService
    {
        private readonly IConverter _converter;
        private readonly PdfSettings _pdfSettings;

        public PdfService(IConverter converter, IOptions<PdfSettings> pdfSettings)
        {
            _converter = converter;
            _pdfSettings = pdfSettings.Value;
        }

        public async Task<byte[]> GeneratePayslipPdfAsync(PayrollRecord payrollRecord)
        {
            var htmlContent = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Payslip - {payrollRecord.Employee.FirstName} {payrollRecord.Employee.LastName}</title>
                <style>
                    body {{ font-family: Arial, sans-serif; margin: 40px; }}
                    .header {{ text-align: center; margin-bottom: 30px; }}
                    .company-name {{ font-size: 24px; font-weight: bold; }}
                    .company-address {{ font-size: 14px; color: #666; }}
                    .payslip-title {{ text-align: center; font-size: 20px; margin: 20px 0; }}
                    .employee-info {{ margin-bottom: 20px; }}
                    .section {{ margin: 15px 0; }}
                    .section-title {{ font-weight: bold; border-bottom: 1px solid #ddd; padding-bottom: 5px; }}
                    table {{ width: 100%; border-collapse: collapse; }}
                    th, td {{ padding: 8px; text-align: left; border-bottom: 1px solid #ddd; }}
                    .text-right {{ text-align: right; }}
                    .total-row {{ font-weight: bold; }}
                    .footer {{ margin-top: 40px; text-align: center; font-size: 12px; color: #666; }}
                </style>
            </head>
            <body>
                <div class='header'>
                    <div class='company-name'>{_pdfSettings.CompanyName}</div>
                    <div class='company-address'>{_pdfSettings.CompanyAddress}</div>
                </div>
                
                <div class='payslip-title'>PAYSLIP</div>
                
                <div class='employee-info'>
                    <div><strong>Employee:</strong> {payrollRecord.Employee.FirstName} {payrollRecord.Employee.LastName}</div>
                    <div><strong>ID Number:</strong> {payrollRecord.Employee.IDNumber}</div>
                    <div><strong>Pay Period:</strong> {payrollRecord.PayPeriod:MMMM yyyy}</div>
                    <div><strong>Date Processed:</strong> {payrollRecord.DateProcessed:dd MMMM yyyy}</div>
                </div>
                
                <div class='section'>
                    <div class='section-title'>Earnings</div>
                    <table>
                        <tr>
                            <th>Description</th>
                            <th class='text-right'>Amount (R)</th>
                        </tr>
                        <tr>
                            <td>Basic Salary</td>
                            <td class='text-right'>{payrollRecord.BasicPay:N2}</td>
                        </tr>
                        <tr>
                            <td>Overtime ({payrollRecord.OvertimeHours} hours)</td>
                            <td class='text-right'>{payrollRecord.OvertimePay:N2}</td>
                        </tr>
                        <tr>
                            <td>Incentives/Bonus</td>
                            <td class='text-right'>{payrollRecord.Incentives:N2}</td>
                        </tr>
                        <tr class='total-row'>
                            <td>Gross Pay</td>
                            <td class='text-right'>{payrollRecord.GrossPay:N2}</td>
                        </tr>
                    </table>
                </div>
                
                <div class='section'>
                    <div class='section-title'>Deductions</div>
                    <table>
                        <tr>
                            <th>Description</th>
                            <th class='text-right'>Amount (R)</th>
                        </tr>
                        <tr>
                            <td>PAYE (Tax)</td>
                            <td class='text-right'>{payrollRecord.PAYE:N2}</td>
                        </tr>
                        <tr>
                            <td>UIF</td>
                            <td class='text-right'>{payrollRecord.UIF:N2}</td>
                        </tr>
                        <tr>
                            <td>Pension Fund</td>
                            <td class='text-right'>{payrollRecord.Pension:N2}</td>
                        </tr>
                        <tr>
                            <td>Medical Aid</td>
                            <td class='text-right'>{payrollRecord.MedicalAid:N2}</td>
                        </tr>
                        <tr>
                            <td>Other Deductions</td>
                            <td class='text-right'>{payrollRecord.OtherDeductions:N2}</td>
                        </tr>
                        <tr class='total-row'>
                            <td>Total Deductions</td>
                            <td class='text-right'>{payrollRecord.TotalDeductions:N2}</td>
                        </tr>
                    </table>
                </div>
                
                <div class='section'>
                    <div class='section-title'>Summary</div>
                    <table>
                        <tr class='total-row'>
                            <td>Net Pay</td>
                            <td class='text-right'>{payrollRecord.NetPay:N2}</td>
                        </tr>
                    </table>
                </div>
                
                <div class='footer'>
                    This is an computer-generated document and does not require a signature.
                </div>
            </body>
            </html>
            ";

            var globalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Portrait,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings { Top = 20, Bottom = 20, Left = 20, Right = 20 },
                DocumentTitle = $"Payslip_{payrollRecord.Employee.LastName}_{payrollRecord.PayPeriod:yyyyMM}"
            };

            var objectSettings = new ObjectSettings
            {
                PagesCount = true,
                HtmlContent = htmlContent,
                WebSettings = { DefaultEncoding = "utf-8" },
                HeaderSettings = { FontSize = 9, Right = "Page [page] of [toPage]", Line = true },
                FooterSettings = { FontSize = 9, Center = $"{_pdfSettings.CompanyName} Payslip", Line = true }
            };

            var pdf = new HtmlToPdfDocument()
            {
                GlobalSettings = globalSettings,
                Objects = { objectSettings }
            };

            return _converter.Convert(pdf);
        }

        public async Task<byte[]> GeneratePayrollReportPdfAsync(List<PayrollRecord> payrollRecords, DateTime payPeriod)
        {
            decimal totalGross = payrollRecords.Sum(pr => pr.GrossPay);
            decimal totalDeductions = payrollRecords.Sum(pr => pr.TotalDeductions);
            decimal totalNet = payrollRecords.Sum(pr => pr.NetPay);

            var htmlContent = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='utf-8'>
                <title>Payroll Report - {payPeriod:MMMM yyyy}</title>
                <style>
                    body {{ font-family: Arial, sans-serif; margin: 40px; }}
                    .header {{ text-align: center; margin-bottom: 30px; }}
                    .company-name {{ font-size: 24px; font-weight: bold; }}
                    .company-address {{ font-size: 14px; color: #666; }}
                    .report-title {{ text-align: center; font-size: 20px; margin: 20px 0; }}
                    .summary-info {{ margin-bottom: 20px; background-color: #f5f5f5; padding: 15px; border-radius: 5px; }}
                    .section {{ margin: 15px 0; }}
                    .section-title {{ font-weight: bold; border-bottom: 1px solid #ddd; padding-bottom: 5px; }}
                    table {{ width: 100%; border-collapse: collapse; }}
                    th, td {{ padding: 8px; text-align: left; border-bottom: 1px solid #ddd; }}
                    th {{ background-color: #f2f2f2; }}
                    .text-right {{ text-align: right; }}
                    .total-row {{ font-weight: bold; background-color: #e6f7ff; }}
                    .footer {{ margin-top: 40px; text-align: center; font-size: 12px; color: #666; }}
                </style>
            </head>
            <body>
                <div class='header'>
                    <div class='company-name'>{_pdfSettings.CompanyName}</div>
                    <div class='company-address'>{_pdfSettings.CompanyAddress}</div>
                </div>
                
                <div class='report-title'>PAYROLL REPORT - {payPeriod:MMMM yyyy}</div>
                
                <div class='summary-info'>
                    <div><strong>Report Period:</strong> {payPeriod:MMMM yyyy}</div>
                    <div><strong>Employees Processed:</strong> {payrollRecords.Count}</div>
                    <div><strong>Total Gross Pay:</strong> R {totalGross:N2}</div>
                    <div><strong>Total Deductions:</strong> R {totalDeductions:N2}</div>
                    <div><strong>Total Net Pay:</strong> R {totalNet:N2}</div>
                    <div><strong>Report Generated:</strong> {DateTime.Now:dd MMMM yyyy HH:mm}</div>
                </div>
                
                <div class='section'>
                    <div class='section-title'>Employee Details</div>
                    <table>
                        <tr>
                            <th>Employee</th>
                            <th class='text-right'>Basic Pay</th>
                            <th class='text-right'>Overtime</th>
                            <th class='text-right'>Incentives</th>
                            <th class='text-right'>Gross Pay</th>
                            <th class='text-right'>Deductions</th>
                            <th class='text-right'>Net Pay</th>
                        </tr>
            ";

            foreach (var record in payrollRecords)
            {
                htmlContent += $@"
                        <tr>
                            <td>{record.Employee.FirstName} {record.Employee.LastName}</td>
                            <td class='text-right'>{record.BasicPay:N2}</td>
                            <td class='text-right'>{record.OvertimePay:N2}</td>
                            <td class='text-right'>{record.Incentives:N2}</td>
                            <td class='text-right'>{record.GrossPay:N2}</td>
                            <td class='text-right'>{record.TotalDeductions:N2}</td>
                            <td class='text-right'>{record.NetPay:N2}</td>
                        </tr>
                ";
            }

            htmlContent += $@"
                        <tr class='total-row'>
                            <td>TOTALS</td>
                            <td class='text-right'>{payrollRecords.Sum(pr => pr.BasicPay):N2}</td>
                            <td class='text-right'>{payrollRecords.Sum(pr => pr.OvertimePay):N2}</td>
                            <td class='text-right'>{payrollRecords.Sum(pr => pr.Incentives):N2}</td>
                            <td class='text-right'>{totalGross:N2}</td>
                            <td class='text-right'>{totalDeductions:N2}</td>
                            <td class='text-right'>{totalNet:N2}</td>
                        </tr>
                    </table>
                </div>
                
                <div class='footer'>
                    This is an computer-generated payroll report.
                </div>
            </body>
            </html>
            ";

            var globalSettings = new GlobalSettings
            {
                ColorMode = ColorMode.Color,
                Orientation = Orientation.Landscape,
                PaperSize = PaperKind.A4,
                Margins = new MarginSettings { Top = 20, Bottom = 20, Left = 20, Right = 20 },
                DocumentTitle = $"Payroll_Report_{payPeriod:yyyyMM}"
            };

            var objectSettings = new ObjectSettings
            {
                PagesCount = true,
                HtmlContent = htmlContent,
                WebSettings = { DefaultEncoding = "utf-8" },
                HeaderSettings = { FontSize = 9, Right = "Page [page] of [toPage]", Line = true },
                FooterSettings = { FontSize = 9, Center = $"{_pdfSettings.CompanyName} Payroll Report", Line = true }
            };

            var pdf = new HtmlToPdfDocument()
            {
                GlobalSettings = globalSettings,
                Objects = { objectSettings }
            };

            return _converter.Convert(pdf);
        }
    }
}
