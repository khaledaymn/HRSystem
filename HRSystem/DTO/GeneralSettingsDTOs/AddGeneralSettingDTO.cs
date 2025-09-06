using System.ComponentModel.DataAnnotations;
namespace HRSystem.DTO.GeneralSettingsDTOs
{
    public class AddGeneralSettingDTO
    {
        public int NumberOfVacationsInYear { get; set; }
        public double RateOfExtraAndLateHour { get; set; }
        public int NumberOfDayWorkingHours { get; set; }
    }
}
