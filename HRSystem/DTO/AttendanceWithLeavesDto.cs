namespace HRSystem.DTO
{
    public class AttendanceWithLeavesDto
    {
        public string TimeOfAttend { get; set; }
        public string? TimeOfLeave { get; set; } 
        public double LatitudeOfAttend { get; set; }
        public double? LatitudeOfLeave { get; set; } 
        public double LongitudeOfAttend { get; set; }
        public double? LongitudeOfLeave { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeId { get; set; }
        public string Branch { get; set; } 
    }
}
