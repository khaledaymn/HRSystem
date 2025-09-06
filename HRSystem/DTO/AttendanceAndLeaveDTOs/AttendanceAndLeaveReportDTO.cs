namespace HRSystem.DTO.AttendanceDTOs
{
    public class AttendanceAndLeaveReportDTO
    {
        public string EmployeeId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public ReportType? ReportType { get; set; }
        public DateTime? DayDate { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? Month { get; set; }
    }
    public enum ReportType
    {
        Daily,
        Monthly
    }
}
