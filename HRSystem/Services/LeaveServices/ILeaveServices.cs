using HRSystem.Models;
using HRSystem.DTO;

namespace HRSystem.Services.LeaveServices
{
    public interface ILeaveServices
    {
        Task<bool> AddLeave(LeaveDTO model);
    }
}
