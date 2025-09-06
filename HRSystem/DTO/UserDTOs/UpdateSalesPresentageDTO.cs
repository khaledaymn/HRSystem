namespace HRSystem.DTO.UserDTOs
{
    public class UpdateSalesPresentageDTO
    {
        public string? EmployeeId { get; set; }
        public decimal? SalesPercentage { get; set; }
        public decimal? FridaySalary { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }
}
