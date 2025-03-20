using Microsoft.AspNetCore.Identity;

namespace HRSystem.Extend
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public Gender Gender { get; set; }
        public DateTime DateOfBarth { get; set; }
        public string Nationalid { get; set; } = string.Empty;
        public double Salary { get; set; }
        public DateTime DateOfWork { get; set; }
        public DateTime TimeOfAttend { get; set; }
        public DateTime TimeOfLeave { get; set; }
        public bool IsDeleted { get; set; }
    }
    public enum Gender
    {
        male,
        female
    }
}
