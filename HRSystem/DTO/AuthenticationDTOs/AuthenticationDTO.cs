using HRSystem.DTO.BranchDTOs;
using HRSystem.DTO.ShiftDTOs;
using System.Text.Json.Serialization;

namespace HRSystem.DTO.AuthenticationDTOs
{
    public class AuthenticationDTO
    {
        public string Message { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? Nationalid { get; set; }
        public double? BaseSalary { get; set; }
        public List<ShiftDTO> Shift { get; set; }
        public string? Gender { get; set; }
        public BranchDTO Branch { get; set; }
        public string? HiringDate { get; set; }
        public string? DateOfBarth { get; set; }
        [JsonIgnore]
        public bool IsAuthenticated { get; set; }
        public string Token { get; set; }
        public IEnumerable<string> Roles { get; set; }
    }
}
