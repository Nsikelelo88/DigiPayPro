using DigitalPayPro.Models;
using DigitalPayPro.Models;

namespace DigitalPayPro.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public decimal TotalPayrollThisMonth { get; set; }
        public int PendingLeaveRequests { get; set; }
        public int PendingOvertimeRequests { get; set; }
        public List<PayrollRecord> RecentPayrolls { get; set; }
        public List<LeaveRequest> RecentLeaveRequests { get; set; }
    }
}