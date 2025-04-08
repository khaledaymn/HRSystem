using HRSystem.Extend;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRSystem.Models
{
    public class EmployeeShift
    {
        public int Id { get; set; }
        [ForeignKey("Employee")]
        public string EmployeeId { get; set; }
        public virtual ApplicationUser Employee { get; set; }
        [ForeignKey("Shift")]
        public int ShiftId { get; set; }
        public virtual Shift Shift { get; set; }
    }
}
