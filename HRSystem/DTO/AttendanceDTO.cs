using System.ComponentModel.DataAnnotations;
namespace HRSystem.DTO
{

    public class AttendanceDto
    {
        [Required(ErrorMessage = "Time of attendance is required.")]
        public DateTime TimeOfAttend { get; set; }

        [Required(ErrorMessage = "Latitude is required.")]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required.")]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double Longitude { get; set; }

        [Range(1, 1000, ErrorMessage = "Radius must be between 1 and 1000 meters.")]
        public double Radius { get; set; }

        [Required(ErrorMessage = "Employee ID is required.")]
        public string EmployeeId { get; set; } = default!;
    }

}
