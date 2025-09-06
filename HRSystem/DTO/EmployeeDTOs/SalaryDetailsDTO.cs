namespace HRSystem.DTO.EmployeeDTOs
{
    public class SalaryDetailsDTO
    {
        public string EmployeeName { get; set; }
        public double BaseSalary { get; set; }
        public double OverTime { get; set; }
        public double OverTimeSalary { get; set; }
        public double LateTime { get; set; }
        public double LateTimeSalary { get; set; }
        public double NumberOfAbsentDays{ get; set; }
        public double AbsentDaysSalary { get; set; }
        public decimal SalesPercentage { get; set; }
        public decimal FridaySalary { get; set; }
        public double TotalSalary { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }
}
