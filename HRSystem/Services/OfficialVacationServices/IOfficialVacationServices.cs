using HRSystem.DTO;

namespace HRSystem.Services.OfficialVacationServices
{
    public interface IOfficialVacationServices
    {
        Task<CreateOfficialVacationDTO> AddOfficialVacationAsync(CreateOfficialVacationDTO vacation);
        Task<OfficialVacationDTO> UpdateOfficialVacationAsync(int id, OfficialVacationDTO vacation);
        Task<bool> DeleteOfficialVacationAsync(int id);
        Task<OfficialVacationDTO?> GetOfficialVacationByIdAsync(int id);
        Task<IEnumerable<OfficialVacationDTO>> GetAllOfficialVacationsAsync();
        Task<bool> IsOfficialVacationAsync(DateTime date);
    }

}
