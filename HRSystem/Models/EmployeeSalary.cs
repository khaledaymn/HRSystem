using HRSystem.Extend;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRSystem.Models
{
    public class EmployeeSalary
    {
        [Key]
        [ForeignKey("Employee")]
        public string EmployeeId { get; set; }
        public virtual ApplicationUser Employee { get; set; }
        public decimal Salary { get; set; }
        public DateTime Date { get; set; }
    }
}
