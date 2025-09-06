namespace HRSystem.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; }
        public int ShiftId { get; set; }
        public string Name { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Title { get; set; }
    }
}


