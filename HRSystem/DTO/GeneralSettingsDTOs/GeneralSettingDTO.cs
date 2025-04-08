namespace HRSystem.DTO.GeneralSettingsDTOs
{
    public class GeneralSettingDTO
    {
        public int Id { get; set; }
        public float? OverTimeHour { get; set; }
        public float? LateHour { get; set; }
        public float? ExtraDay { get; set; }
        public float? AbsentDay { get; set; }
        public string? FirstShiftTimeOfAttend { get; set; }
        public string? FirstShiftTimeOfLeave { get; set; } 
        public string? SecondShiftTimeOfAttend { get; set; }
        public string? SecondShiftTimeOfLeave { get; set; }
    }
}
