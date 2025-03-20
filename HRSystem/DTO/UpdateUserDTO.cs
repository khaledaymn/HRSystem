using System.ComponentModel.DataAnnotations;

namespace HRSystem.DTO
{
    public class UpdateUserDTO
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? PhoneNumber { get; set; }
        public string? UserName { get; set; }
        public string? NationalId { get; set; }
        public double? Salary { get; set; }
        public string? TimeOfAttend { get; set; }
        public string? TimeOfLeave { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfWork { get; set; }
    }
}
