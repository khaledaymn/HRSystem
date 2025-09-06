using HRSystem.DTO.AttendanceDTOs;
using HRSystem.DTO.ReportDTOs;

namespace HRSystem.Services.ReportServices
{
    public interface IReportServices
    {
        Task<List<ReportDTO>> GetMonthlyOvertimeReport();
        Task<List<ReportDTO>> GetMonthlyLateReport();
        Task<List<AttendanceLeaveReportDTO>> GetAttendanceAndLeaveReport(
            AttendanceAndLeaveReportDTO dto);
        Task<List<AttendanceReportDTO>> AttendanceReport(ParamDTO dto);
        Task<List<AbsenceReportDTO>> GetEmployeeAbsent(AttendanceAndLeaveReportDTO dto);
        Task<List<AbsenceSummaryReportDTO>> AbsenceReport(ParamDTO dto);
    }
}
