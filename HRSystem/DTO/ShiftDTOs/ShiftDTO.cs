namespace HRSystem.DTO.ShiftDTOs
{
    public class ShiftDTO
    {
        public int Id { get; set; }
        public string? StartTime { get; set; } = string.Empty;
        public string? EndTime { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
    }
}
