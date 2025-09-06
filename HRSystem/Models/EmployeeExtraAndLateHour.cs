using HRSystem.Extend;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRSystem.Models
{
    [PrimaryKey(nameof(EmployeeId),nameof(Date),nameof(Type))]
    public class EmployeeExtraAndLateHour
    {
        [ForeignKey(nameof(Employee))]
        public string EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public double Hours { get; set; }
        public string Type { get; set; }
        public virtual ApplicationUser Employee { get; set; }
    }
}
