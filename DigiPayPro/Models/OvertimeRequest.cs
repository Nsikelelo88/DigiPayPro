using DigitalPayPro.Models;
using System.ComponentModel.DataAnnotations;

namespace DigitalPayPro.Models
{
    public class OvertimeRequest
    {
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [Range(0.5, 24, ErrorMessage = "Hours must be between 0.5 and 24")]
        public decimal Hours { get; set; }

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