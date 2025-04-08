namespace HRSystem.Models
{
    public class GeneralSetting
    {
        public int Id { get; set; }
        public string CompanyName { get; set; }
        public DateTime StartTimeOfDay { get; set; }
        public int NumberOfVacationsInYear { get; set; }
        public double RateOfExtraHour { get; set; }
    }
}
