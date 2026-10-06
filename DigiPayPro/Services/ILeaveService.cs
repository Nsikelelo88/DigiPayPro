using System;
using System.Threading.Tasks;
using DigitalPayPro.Models;

namespace DigitalPayPro.Services
{
    public interface ILeaveService
    {
        /// <summary>
        /// Returns true if there is any existing leave (Pending or Approved) for the employee that overlaps the given range.
        /// </summary>
        Task<bool> HasOverlappingLeaveAsync(int employeeId, DateTime startDate, DateTime endDate);
    }
}
