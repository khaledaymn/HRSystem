namespace HRSystem.DTO.NotificationDTOs
{
    public class AddNotificationDTO
    {
        public string EmployeeId { get; set; }
        public string Title { get; set; }
        public string Name { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Message { get; set; }
        public int ShiftId { get; set; }
    }
}
