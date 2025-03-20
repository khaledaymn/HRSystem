using HRSystem.Extend;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRSystem.Models
{
    public class Absent
    {
        public int Id { get; set; }
        public DateTime day { get; set; }
        public string EmployeeId { get; set; }
        public ApplicationUser Employee { get; set; } = default!;

    }
}
