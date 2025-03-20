using System.Text.Json.Serialization;

namespace HRSystem.DTO
{
    public class AuthenticationDTO
    {
        public string Message { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? UserName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? Nationalid { get; set; }
        public double? Salary { get; set; }
        public string? TimeOfAttend { get; set; }
        public string? TimeOfLeave { get; set; }
        public string? Gender { get; set; }
        public string? DateOfWork { get; set; }
        public string? DateOfBarth { get; set; }
        [JsonIgnore]
        public bool IsAuthenticated { get; set; }
        public string Token { get; set; }
        public IEnumerable<string> Roles { get; set; }
    }
}
