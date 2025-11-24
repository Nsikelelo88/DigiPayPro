using System.ComponentModel.DataAnnotations;

namespace DigitalPayPro.ViewModels
{
    public class EmployeeViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [Display(Name = "ID Number")]
        public string IDNumber { get; set; }

        [Required]
        public string Position { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        [Display(Name = "Basic Salary")]
        public decimal BasicSalary { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Bank Name")]
        public string BankName { get; set; }

        [Display(Name = "Account Number")]
        public string AccountNumber { get; set; }

        [Display(Name = "Branch Code")]
        public string BranchCode { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [DataType(DataType.Date)]
        [Display(Name = "Date Joined")]
        public DateTime DateJoined { get; set; } = DateTime.Now;
    }
}