namespace HRSystem.DTO.NotificationDTOs
{
    public class NotificationDTO
    {
        public int? Id { get; set; }
        public string Title { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Name { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Message { get; set; }
        public string EmployeeId { get; set; }
        public int ShiftId { get; set; }
    }
}