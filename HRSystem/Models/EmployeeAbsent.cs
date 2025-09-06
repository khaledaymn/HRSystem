using HRSystem.Extend;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRSystem.Models
{
    [PrimaryKey(nameof(EmployeeId), nameof(AbsentDate), nameof(Hours))]
    public class EmployeeAbsent
    {
        [ForeignKey(nameof(Employee))]
        public string EmployeeId { get; set; }
        public DateTime AbsentDate { get; set; }
        public double Hours { get; set; }
        public virtual ApplicationUser Employee { get; set; }
    }
}
