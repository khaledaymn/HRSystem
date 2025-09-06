using HRSystem.Extend;
using HRSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HRSystem.DataBase
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<EmployeeAbsent> EmployeeAbsents { get; set; }
        public DbSet<EmployeeExtraAndLateHour> EmployeeExtraAndLateHours { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<EmployeeVacation> EmployeeVacations { get; set; }
        public DbSet<AttendanceAndLeave> AttendancesAndLeaves { get; set; }
        public DbSet<GeneralSetting> GeneralSetting { get; set; }
        public DbSet<OfficialVacation> OfficialVacation { get; set; }
        //public DbSet<Absent> Absent { get; set; }
        public DbSet<EmployeeSalary> EmployeeSalaries { get; set; }
        public DbSet<EmployeeShift> EmployeeShifts { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<Branch> Branch { get; set; }
        public DbSet<AdditionalSalary> AdditionalSalaries { get; set; }

    }
}
