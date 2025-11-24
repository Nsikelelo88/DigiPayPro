using System.ComponentModel.DataAnnotations;

namespace DigitalPayPro.ViewModels
{
    public class PayrollProcessViewModel
    {
        [Required]
        [Display(Name = "Employee")]
        public int EmployeeId { get; set; }

        [Required]
        [Display(Name = "Pay Period")]
        [DataType(DataType.Date)]
        public DateTime PayPeriod { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        [Display(Name = "Overtime Hours")]
        [Range(0, double.MaxValue)]
        public decimal OvertimeHours { get; set; }

        [Display(Name = "Incentives")]
        [DataType(DataType.Currency)]
        public decimal Incentives { get; set; }
    }
}