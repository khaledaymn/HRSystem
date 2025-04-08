using HRSystem.DTO.ShiftDTOs;
using HRSystem.Models;

namespace HRSystem.Services.ShiftServices
{
    public interface IShiftServices
    {
        Task<bool> CreateShiftAsync(AddShiftDTO shift);
        Task<bool> UpdateShiftAsync(ShiftDTO shift);
        Task<bool> DeleteShiftAsync(DeleteShiftDTO dto);
        Task<List<ShiftDTO>> GetByEmployeeId(string EmployeeId);
        Task<ShiftDTO> GetShiftByIdAsync(int id);
    }
}
