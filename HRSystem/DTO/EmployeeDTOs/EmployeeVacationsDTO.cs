namespace HRSystem.DTO.EmployeeDTOs
{
    public class EmployeeVacationsDTO
    {
        public string EmployeeName { get; set; }
        public double TotalVacationDays { get; set; }
        public List<EmployeeDetailsDTO> EmployeeVacationDetails { get; set; }
    }
}
