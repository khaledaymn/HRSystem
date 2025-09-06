using HRSystem.Extend;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRSystem.Models
{
    [PrimaryKey(nameof(Date), nameof(UserId))]
    public class EmployeeVacation
    {
        public DateTime Date { get; set; }
        public double Hours { get; set; }
        [ForeignKey(nameof(User))]
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; }
    }
}
