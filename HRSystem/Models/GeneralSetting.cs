namespace HRSystem.Models
{
    public class GeneralSetting
    {
        public int Id { get; set; }
        public int NumberOfVacationsInYear { get; set; }
        public double RateOfExtraHour { get; set; }
        public int NumberOfDayWorkingHours { get; set; } = 10;
    }
}
