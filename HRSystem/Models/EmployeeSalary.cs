using HRSystem.Extend;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRSystem.Models
{
    [PrimaryKey(nameof(EmployeeId),nameof(Date))]
    public class EmployeeSalary
    {
        [ForeignKey("Employee")]
        public string EmployeeId { get; set; }
        public virtual ApplicationUser Employee { get; set; }
        public decimal Salary { get; set; }
        public DateTime Date { get; set; }
    }
}
