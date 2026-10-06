using System;
using System.Linq;
using System.Threading.Tasks;
using DigitalPayPro.Data;
using Microsoft.EntityFrameworkCore;

namespace DigitalPayPro.Services
{
    public class LeaveService : ILeaveService
    {
        private readonly ApplicationDbContext _context;

        public LeaveService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasOverlappingLeaveAsync(int employeeId, DateTime startDate, DateTime endDate)
        {
            // Normalize dates to date component only to avoid time issues
            var start = startDate.Date;
            var end = endDate.Date;

            // Consider statuses that should block new requests
            var blockingStatuses = new[] { "Pending", "Approved" };

            return await _context.LeaveRequests
                .AsNoTracking()
                .Where(lr => lr.EmployeeId == employeeId && blockingStatuses.Contains(lr.Status))
                .AnyAsync(lr => lr.StartDate.Date <= end && lr.EndDate.Date >= start);
        }
    }
}
