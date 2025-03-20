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
        public DbSet<Attendance> Attendance { get; set; }
        public DbSet<GeneralSetting> GeneralSetting { get; set; }
        public DbSet<OfficialVacation> OfficialVacation { get; set; }
        public DbSet<Leave> Leave { get; set; }
        public DbSet<Absent> Absent { get; set; }
    }
}
