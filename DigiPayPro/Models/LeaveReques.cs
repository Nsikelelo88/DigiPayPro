using DigitalPayPro.Models;
using System.ComponentModel.DataAnnotations;

namespace DigitalPayPro.Models
{
    public class LeaveRequest
    {
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }

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
        public string LeaveType { get; set; } // Annual, Sick, Family Responsibility, Maternity

        [Required]
        public string Reason { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected

        [DataType(DataType.DateTime)]
        [Display(Name = "Date Requested")]
        public DateTime DateRequested { get; set; } = DateTime.Now;

        [DataType(DataType.DateTime)]
        [Display(Name = "Date Reviewed")]
        public DateTime? DateReviewed { get; set; }

        [Display(Name = "Reviewed By")]
        public string ReviewedBy { get; set; }

        [Display(Name = "Review Notes")]
        public string ReviewNotes { get; set; }
    }
}