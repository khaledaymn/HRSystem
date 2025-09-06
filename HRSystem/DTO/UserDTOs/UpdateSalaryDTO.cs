namespace HRSystem.DTO.UserDTOs
{
    public class UpdateSalaryDTO
    {
        public string EmployeeId { get; set; }
        public double NetSalary { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }
}
