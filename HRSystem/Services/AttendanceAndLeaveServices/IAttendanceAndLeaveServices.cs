using HRSystem.Models;
using HRSystem.DTO;
using System.Linq.Expressions;

namespace HRSystem.Services.AttendanceServices
{
    public interface IAttendanceAndLeaveServices
    {
        Task<bool> AddLeave(LeaveDTO model);
        Task<bool> AddAttendance(AttendanceDTO model);
        Task<IEnumerable<AttendanceWithLeavesDto>> GetAllAttendancesWithLeavesAsync();
        Task<IEnumerable<AttendanceWithLeavesDto>> GetEmployeeAttendancesAndLeavesAsync(string empId);
        Task<List<string>> GetEmployeesWithoutLeave(DateTime date);
        Task MarkEmployeesWithoutLeave(List<string> employeeIds);
    }

}
