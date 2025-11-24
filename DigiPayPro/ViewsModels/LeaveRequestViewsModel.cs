using System.ComponentModel.DataAnnotations;

namespace DigitalPayPro.ViewModels
{
    public class LeaveRequestViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Employee")]
        public int EmployeeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Required]
        [Display(Name = "Leave Type")]
        public string LeaveType { get; set; }

        [Required]
        public string Reason { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending";

        // For display purposes
        public string EmployeeName { get; set; }
        public int TotalDays => (EndDate - StartDate).Days + 1;
    }
}