namespace HRSystem.Models
{
    public class GeneralSetting
    {
        public int Id { get; set; }

        public float OverTimeHour {  get; set; }
        public float LateHour {  get; set; }

        public float ExtraDay {  get; set; }
        public float AbsentDay {  get; set; }

        public string FirstWeekEnd { get; set; } = default!;
        public string SecondWeekEnd { get; set; } = default!;

    }
}
