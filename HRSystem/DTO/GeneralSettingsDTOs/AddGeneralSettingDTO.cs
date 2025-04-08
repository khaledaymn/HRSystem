using System.ComponentModel.DataAnnotations;
namespace HRSystem.DTO.GeneralSettingsDTOs
{
    public class AddGeneralSettingDTO
    {
        [Range(0, float.MaxValue, ErrorMessage = "Overtime hours must be a positive value.")]
        public float OverTimeHourPrice { get; set; }

        [Range(0, float.MaxValue, ErrorMessage = "Late hours must be a positive value.")]
        public float LateHourPrice { get; set; }

        [Required(ErrorMessage = "Time of attendance is required.")]
        [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Time of attendance must be in HH:mm format.")]
        public string FirstShiftTimeOfAttend { get; set; }

        [Required(ErrorMessage = "Time of leave is required.")]
        [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Time of leave must be in HH:mm format.")]
        public string FirstShiftTimeOfLeave { get; set; }
        
        [Required(ErrorMessage = "Time of attendance is required.")]
        [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Time of attendance must be in HH:mm format.")]
        public string SecondShiftTimeOfAttend { get; set; }

        [Required(ErrorMessage = "Time of leave is required.")]
        [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Time of leave must be in HH:mm format.")]
        public string SecondShiftTimeOfLeave { get; set; }
    }
}
