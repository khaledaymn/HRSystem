using HRSystem.Models;
using HRSystem.DTO;
using System.Linq.Expressions;

namespace HRSystem.Services.AttendanceServices
{
    public interface IAttendanceServices
    {
        Task<bool> AddAttendance(AttendanceDto model);
        Task<IEnumerable<AttendanceWithLeavesDto>> GetAllAttendancesWithLeavesAsync();
        Task<IEnumerable<AttendanceWithLeavesDto>> GetEmployeeAttendancesAndLeavesAsync(string empId);
        Task<List<string>> GetEmployeesWithoutLeave(DateTime date);
        Task MarkEmployeesWithoutLeave(List<string> employeeIds);
    }

}
