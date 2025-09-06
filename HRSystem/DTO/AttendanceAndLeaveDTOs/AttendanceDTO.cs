using System.ComponentModel.DataAnnotations;
namespace HRSystem.DTO.AttendanceDTOs
{

    public class AttendanceDTO
    {
        [Required(ErrorMessage = "Time of attendance is required.")]
        public DateTime TimeOfAttend { get; set; }

        [Required(ErrorMessage = "Latitude is required.")]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required.")]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double Longitude { get; set; }

        [Required(ErrorMessage = "Employee ID is required.")]
        public string EmployeeId { get; set; } = default!;
    }
}
