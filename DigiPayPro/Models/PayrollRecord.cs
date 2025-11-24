using DigitalPayPro.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigitalPayPro.Models
{
    public class PayrollRecord
    {
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Pay Period")]
        public DateTime PayPeriod { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Basic Pay")]
        public decimal BasicPay { get; set; }

        [Display(Name = "Overtime Hours")]
        public decimal OvertimeHours { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Overtime Pay")]
        public decimal OvertimePay { get; set; }

        [DataType(DataType.Currency)]
        public decimal Incentives { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Gross Pay")]
        public decimal GrossPay { get; set; }

        [DataType(DataType.Currency)]
        public decimal PAYE { get; set; }

        [DataType(DataType.Currency)]
        public decimal UIF { get; set; }

        [DataType(DataType.Currency)]
        public decimal Pension { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Medical Aid")]
        public decimal MedicalAid { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Other Deductions")]
        public decimal OtherDeductions { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Total Deductions")]
        public decimal TotalDeductions { get; set; }

        [DataType(DataType.Currency)]
        [Display(Name = "Net Pay")]
        public decimal NetPay { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Date Processed")]
        public DateTime DateProcessed { get; set; } = DateTime.Now;
    }
}