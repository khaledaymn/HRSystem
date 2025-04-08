using System.ComponentModel.DataAnnotations;

namespace HRSystem.DTO.UserDTOs
{
    public class UpdateUserDTO
    {
        [Required]
        public string Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public DateTime? DateOfBarth { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Nationalid { get; set; }
        public double? Salary { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfWork { get; set; }
        public int? BranchId { get; set; }
    }
}
