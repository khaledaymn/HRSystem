using HRSystem.DTO.EmployeeDTOs;

namespace HRSystem.DTO.ReportDTOs
{
    public class AbsenceReportDTO
    {
        public string EmployeeName { get; set; }
        public string Email { get; set; }
        public List<EmployeeDetailsDTO> Data { get; set; }
    }
}
