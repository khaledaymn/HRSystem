namespace HRSystem.DTO.EmployeeDTOs
{
    public class EmployeesWithSalaryDTO
    {
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public double NetSalary { get; set; }
    }
}
