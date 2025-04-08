using HRSystem.Extend;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRSystem.Models
{
    public class EmployeeVacation
    {
        public DateTime Date { get; set; }
        [Key,ForeignKey(nameof(User))]
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; }
    }
}
