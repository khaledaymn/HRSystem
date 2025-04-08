using System.ComponentModel.DataAnnotations;
namespace HRSystem.DTO
{

    public class AttendanceDto
    {
        [Required(ErrorMessage = "Time of attendance is required.")]
        [RegularExpression(@"^(0[1-9]|1[0-2]):[0-5][0-9] (AM|PM)$", ErrorMessage = "Time must be in hh:mm AM/PM format.")]
        public string TimeOfAttend { get; set; }

        [Required(ErrorMessage = "Latitude is required.")]
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double Latitude { get; set; }

        [Required(ErrorMessage = "Longitude is required.")]
        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double Longitude { get; set; }

        [Required(ErrorMessage = "Employee ID is required.")]
        public string EmployeeId { get; set; } = default!;
        [Required(ErrorMessage = "Branch is required.")]
        public string Branch { get; set; } = default!;
    }

}
