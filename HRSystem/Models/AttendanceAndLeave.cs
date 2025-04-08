using HRSystem.Extend;
using System.ComponentModel.DataAnnotations;

namespace HRSystem.Models
{
    public class AttendanceAndLeave
    {
        public int Id { get; set; }
        public DateTime Time { get; set; }
        public double Latitude { get; set; } = default!;
        public double Longitude { get; set; } = default!;
        public string Type { get; set; }
        public string EmployeeId { get; set; }
        public virtual ApplicationUser Employee { get; set; } = default!;
    }
}
