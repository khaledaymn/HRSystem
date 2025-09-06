using HRSystem.DTO.AttendanceDTOs;
using HRSystem.DTO.EmployeeDTOs;

namespace HRSystem.DTO.ReportDTOs
{
    public class AbsenceSummaryReportDTO
    {
        public ReportDTO BasicInformation { get; set; }
        public EmployeeDetailsDTO OtherData { get; set; }
    }
}
