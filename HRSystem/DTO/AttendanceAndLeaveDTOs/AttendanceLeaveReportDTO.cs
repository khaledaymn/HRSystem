using HRSystem.DTO.ShiftDTOs;

namespace HRSystem.DTO.AttendanceDTOs
{
    public class AttendanceLeaveReportDTO
    {
        public string? EmployeeName { get; set; }
        public string? Email { get; set; }
        public DateTime? Date { get; set; }
        public string? TimeOfAttend { get; set; }
        public string? TimeOfLeave { get; set; }
        public string? BranchName { get; set; }
        public double? NumberOfOverTime { get; set; }
        public double? NumberOfLateHour { get; set; }
    }
}
