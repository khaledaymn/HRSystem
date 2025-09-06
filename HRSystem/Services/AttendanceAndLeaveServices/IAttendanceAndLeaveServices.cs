using HRSystem.Models;
using System.Linq.Expressions;
using HRSystem.DTO.AttendanceDTOs;
using HRSystem.DTO.AttendanceAndLeaveDTOs;

namespace HRSystem.Services.AttendanceServices
{
    public interface IAttendanceAndLeaveServices
    {
        Task<bool> AddLeave(LeaveDTO model);
        Task<bool> AddAttendance(AttendanceDTO model);
        //Task<bool> UpdateAttendanceOrLeave(AttendanceDTO model);
        Task<bool> AddLeaveByAdmin(LeaveByAdminDTO model);
    }
}