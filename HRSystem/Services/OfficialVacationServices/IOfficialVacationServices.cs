using HRSystem.DTO.OfficialVacationDTOs;

namespace HRSystem.Services.OfficialVacationServices
{
    public interface IOfficialVacationServices
    {
        Task<OfficialVacationDTO> AddOfficialVacationAsync(CreateOfficialVacationDTO vacation);
        Task<OfficialVacationDTO> UpdateOfficialVacationAsync(OfficialVacationDTO vacation);
        Task<bool> DeleteOfficialVacationAsync(int id);
        Task<OfficialVacationDTO?> GetOfficialVacationByIdAsync(int id);
        Task<IEnumerable<OfficialVacationDTO>> GetAllOfficialVacationsAsync();
        Task<bool> IsOfficialVacationAsync(DateTime date);
    }
}
