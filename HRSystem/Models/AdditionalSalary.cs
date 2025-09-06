using HRSystem.Extend;
using Microsoft.EntityFrameworkCore;

namespace HRSystem.Models
{
    [PrimaryKey(nameof(UserId), nameof(Date))]
    public class AdditionalSalary
    {
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }
        public decimal? SalesPercentage { get; set; }
        public decimal? FridaySalary { get; set; }
        public DateTime Date { get; set; }
    }
}
