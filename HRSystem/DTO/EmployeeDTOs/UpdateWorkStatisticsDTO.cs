namespace HRSystem.DTO.EmployeeDTOs
{
    public class UpdateWorkStatisticsDTO
    {
        public string EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public double Hours { get; set; }
    }
}
