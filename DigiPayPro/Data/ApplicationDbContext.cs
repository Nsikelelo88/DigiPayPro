using DigitalPayPro.Models;
using DigitalPayPro.Models;
using Microsoft.EntityFrameworkCore;

namespace DigitalPayPro.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<PayrollRecord> PayrollRecords { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }
        public DbSet<OvertimeRequest> OvertimeRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Seed initial admin user
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    Role = "Admin",
                    Email = "admin@company.com",
                    FirstName = "System",
                    LastName = "Administrator"
                }
            );
        }
    }
}