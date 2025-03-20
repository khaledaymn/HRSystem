using HRSystem.Models;
using HRSystem.DTO;

namespace HRSystem.Services.AttendanceServices
{
    public interface IAttendanceServices
    {
        Task<bool> AddAttendance(AttendanceDto model);
        Task<IEnumerable<AttendanceDto>> GetAllAttendancesAsync();
        Task<IEnumerable<AttendanceDto>> GetEmployeeAttendancesAsync(string empId);
    }
}
