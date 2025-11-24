using System.ComponentModel.DataAnnotations;

namespace DigitalPayPro.ViewModels
{
    public class OvertimeRequestViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Employee")]
        public int EmployeeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [Range(0.5, 24, ErrorMessage = "Hours must be between 0.5 and 24")]
        public decimal Hours { get; set; }

        [Required]
        public string Reason { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending";

        // For display purposes
        public string EmployeeName { get; set; }
    }
}