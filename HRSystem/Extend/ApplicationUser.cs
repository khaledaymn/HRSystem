using HRSystem.Models;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRSystem.Extend
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public Gender Gender { get; set; }
        public DateTime DateOfBarth { get; set; }
        public string Nationalid { get; set; } = string.Empty;
        public double BaseSalary { get; set; }
        public DateTime HiringDate { get; set; }
        public bool IsDeleted { get; set; }

        [ForeignKey(nameof(Branch))]
        public int? BranchId { get; set; }
        public virtual Branch Branch { get; set; }
        public virtual ICollection<EmployeeShift> EmployeeShifts { get;} = new List<EmployeeShift>();
    }
}
