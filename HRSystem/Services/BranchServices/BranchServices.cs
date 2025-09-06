using HRSystem.DataBase;
using HRSystem.DTO.BranchDTOs;
using HRSystem.Models;
using HRSystem.UnitOfWork;

namespace HRSystem.Services.BranchServices
{
    public class BranchServices : IBranchServices
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BranchServices> _logger;
        public BranchServices(IUnitOfWork unitOfWork, ILogger<BranchServices> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #region Get All Branches

        public async Task<List<BranchDTO>> GetAllBranchesAsync()
        {
            try
            {
                var branches = await _unitOfWork.Repository<Branch>().GetAll();

                if (branches == null || !branches.Any())
                {
                    _logger.LogWarning("No branches found in the database.");
                    return new List<BranchDTO>(); 
                }

                var branchDtos = branches.Select(b => new BranchDTO
                {
                    Id = b.Id,
                    Name = b.Name,
                    Latitude = b.Latitude,
                    Longitude = b.Longitude,
                    Radius = b.Radius
                }).ToList();

                _logger.LogInformation("Successfully retrieved {BranchCount} branches.", branchDtos.Count);
                return branchDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve branches. Error: {Message}", ex.Message);
                throw;
            }
        }

        #endregion


        #region Get Branch By Id

        public async Task<BranchDTO> GetBranchByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid branch ID provided: {BranchId}", id);
                    return null;
                }

                var branch = await _unitOfWork.Repository<Branch>().GetById(id);

                if (branch == null)
                {
                    _logger.LogWarning("Branch with ID {BranchId} not found.", id);
                    return null;
                }

                var branchDto = new BranchDTO
                {
                    Id = branch.Id,
                    Name = branch.Name,
                    Latitude = branch.Latitude,
                    Longitude = branch.Longitude,
                    Radius = branch.Radius
                };

                _logger.LogInformation("Successfully retrieved branch with ID: {BranchId}", id);
                return branchDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve branch with ID: {BranchId}. Error: {Message}", id, ex.Message);
                throw;
            }
        }

        #endregion


        #region Create Branch

        public async Task<BranchDTO> CreateAsync(AddBranchDTO branch)
        {
            try
            {
                if (branch == null)
                {
                    _logger.LogWarning("Branch data is null.");
                    throw new ArgumentNullException(nameof(branch), "Branch data cannot be null.");
                }

                if (string.IsNullOrEmpty(branch.Name))
                {
                    _logger.LogWarning("Branch name is required but was null or empty.");
                    throw new ArgumentException("Branch name is required.", nameof(branch.Name));
                }

                var newBranch = new Branch
                {
                    Name = branch.Name,
                    Latitude = branch.Latitude,
                    Longitude = branch.Longitude,
                    Radius = branch.Radius
                };

                await _unitOfWork.Repository<Branch>().ADD(newBranch);
                await _unitOfWork.Save();

                var branchDto = new BranchDTO
                {
                    Id = newBranch.Id,
                    Name = newBranch.Name,
                    Latitude = newBranch.Latitude,
                    Longitude = newBranch.Longitude,
                    Radius = newBranch.Radius
                };

                _logger.LogInformation("Successfully created branch with ID: {BranchId}", newBranch.Id);
                return branchDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create branch with name: {BranchName}. Error: {Message}", branch?.Name, ex.Message);
                throw;
            }
        }

        #endregion


        #region Update Branch

        public async Task<BranchDTO> UpdateAsync(BranchDTO branch)
        {
            try
            {
                if (branch == null)
                {
                    _logger.LogWarning("Branch data is null.");
                    throw new ArgumentNullException(nameof(branch), "Branch data cannot be null.");
                }

                if (branch.Id <= 0)
                {
                    _logger.LogWarning("Invalid branch ID provided: {BranchId}", branch.Id);
                    throw new ArgumentException("Branch ID must be a positive integer.", nameof(branch.Id));
                }

                var existingBranch = await _unitOfWork.Repository<Branch>().GetById(branch.Id);
                if (existingBranch == null)
                {
                    _logger.LogWarning("Branch with ID {BranchId} not found.", branch.Id);
                    return null;
                }

                if (!string.IsNullOrEmpty(branch.Name))
                {
                    existingBranch.Name = branch.Name;
                }

                if (branch.Latitude.HasValue)
                {
                    existingBranch.Latitude = branch.Latitude.Value;
                }

                if (branch.Longitude.HasValue)
                {
                    existingBranch.Longitude = branch.Longitude.Value;
                }

                if (branch.Radius.HasValue)
                {
                    existingBranch.Radius = branch.Radius.Value;
                }

                _unitOfWork.Repository<Branch>().Update(existingBranch);
                await _unitOfWork.Save();

                var updatedBranchDto = new BranchDTO
                {
                    Id = existingBranch.Id,
                    Name = existingBranch.Name,
                    Latitude = existingBranch.Latitude,
                    Longitude = existingBranch.Longitude,
                    Radius = existingBranch.Radius
                };

                _logger.LogInformation("Successfully updated branch with ID: {BranchId}", existingBranch.Id);
                return updatedBranchDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update branch with ID: {BranchId}. Error: {Message}", branch?.Id, ex.Message);
                throw; 
            }
        }

        #endregion


        #region Delete Branch

        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogInformation("Attempting to delete branch with ID: {BranchId}", id);

            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid branch ID provided: {BranchId}", id);
                    throw new ArgumentException("Branch ID must be a positive integer.", nameof(id));
                }

                var branch = await _unitOfWork.Repository<Branch>().GetById(id);
                if (branch == null)
                {
                    _logger.LogWarning("Branch with ID {BranchId} not found.", id);
                    return false;
                }

                _unitOfWork.Repository<Branch>().Delete(branch.Id);
                await _unitOfWork.Save();

                _logger.LogInformation("Successfully deleted branch with ID: {BranchId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete branch with ID: {BranchId}. Error: {Message}", id, ex.Message);
                throw;
            }
        }

        #endregion

    }
}
