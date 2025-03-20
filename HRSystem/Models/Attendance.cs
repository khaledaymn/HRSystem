using HRSystem.Extend;
using System.ComponentModel.DataAnnotations;

namespace HRSystem.Models
{
    public class Attendance
    {
        public int Id { get; set; }
        public DateTime TimeOfAttend { get; set; }
        public string Latitude { get; set; } = default!;
        public string Longitude { get; set; } = default!;
        public double Radius { get; set; }
        public string EmployeeId { get; set; }
        public ApplicationUser Employee { get; set; } = default!;
    }
}
