using HRSystem.Extend;

namespace HRSystem.Models
{
    public class Leave
    {
        public int Id { get; set; }
        public DateTime TimeOfLeave { get; set; }
        public double Latitude { get; set; } = default!;
        public double Longitude { get; set; } = default!;
        public double Radius { get; set; }
        public string EmployeeId { get; set; }
        public ApplicationUser Employee { get; set; } = default!;

    }
}
