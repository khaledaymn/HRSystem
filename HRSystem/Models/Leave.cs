using HRSystem.Extend;
using System.ComponentModel.DataAnnotations;

namespace HRSystem.Models
{
    public class Leave
    {
        
        public int Id { get; set; }
        public DateTime TimeOfLeave { get; set; }
        public string Latitude { get; set; } = default!;
        public string Longitude { get; set; } = default!;
        public double Radius { get; set; }
        public string EmployeeId { get; set; }
        public ApplicationUser Employee { get; set; } = default!;

    }
}
