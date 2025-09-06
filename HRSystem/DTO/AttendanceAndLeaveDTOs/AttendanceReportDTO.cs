namespace HRSystem.DTO.AttendanceDTOs
{
    public class AttendanceReportDTO
    {
        public string EmployeeId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public double NumberOfMonthlyWorkingHours { get; set; }
        public double NumberOfLateHours { get; set; }
        public double NumberOfAbsentDays { get; set; }
        public double NumberOfVacationDays { get; set; }
        public double NumberOfOverTime { get; set; }
    }
}
