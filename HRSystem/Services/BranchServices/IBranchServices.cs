using HRSystem.DTO.BranchDTOs;

namespace HRSystem.Services.BranchServices
{
    public interface IBranchServices
    {

        Task<List<BranchDTO>> GetAllBranchesAsync();
        Task<BranchDTO> GetBranchByIdAsync(int id);
        Task<BranchDTO> CreateAsync(AddBranchDTO branch);
        Task<BranchDTO> UpdateAsync(BranchDTO branch);
        Task<bool> DeleteAsync(int id);
    }
}
