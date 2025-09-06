using HRSystem.DataBase;
using HRSystem.DTO.OfficialVacationDTOs;
using HRSystem.Models;
using HRSystem.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace HRSystem.Services.OfficialVacationServices
{
    public class OfficialVacationServices : IOfficialVacationServices
    {
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OfficialVacationServices> _logger;
        public OfficialVacationServices(ApplicationDbContext context, IUnitOfWork unitOfWork, ILogger<OfficialVacationServices> logger)
        {
            _context = context;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #region Create Official Vacation

        public async Task<OfficialVacationDTO> AddOfficialVacationAsync(CreateOfficialVacationDTO vacation)
        {
            try
            {
                if (vacation == null)
                {
                    _logger.LogWarning("Vacation data is null.");
                    throw new ArgumentNullException(nameof(vacation), "Vacation data cannot be null.");
                }

                if (string.IsNullOrEmpty(vacation.VacationName))
                {
                    _logger.LogWarning("Vacation name is required but was null or empty.");
                    throw new ArgumentException("Vacation name is required.", nameof(vacation.VacationName));
                }

                if (string.IsNullOrEmpty(vacation.VacationDay))
                {
                    _logger.LogWarning("Vacation day is required but was null or empty.");
                    throw new ArgumentException("Vacation day is required.", nameof(vacation.VacationDay));
                }

                if (!DateTime.TryParse(vacation.VacationDay, out var vacationDay))
                {
                    _logger.LogWarning("Invalid vacation day format: {VacationDay}", vacation.VacationDay);
                    throw new ArgumentException("Invalid vacation day format. Please provide a valid date.", nameof(vacation.VacationDay));
                }

                var entity = new OfficialVacation
                {
                    VacationName = vacation.VacationName,
                    VacationDay = vacationDay
                };

                await _unitOfWork.Repository<OfficialVacation>().ADD(entity);
                await _unitOfWork.Save();

                var resultDto = new OfficialVacationDTO
                {
                    Id = entity.Id,
                    VacationName = entity.VacationName,
                    VacationDay = entity.VacationDay.ToString("yyyy/MM/dd")
                };

                _logger.LogInformation("Successfully created official vacation with ID: {VacationId}", resultDto.Id);
                return resultDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create official vacation with name: {VacationName}. Error: {Message}", vacation?.VacationName, ex.Message);
                throw;
            }
        }

        #endregion


        #region Get All Official Vacations

        public async Task<IEnumerable<OfficialVacationDTO>> GetAllOfficialVacationsAsync()
        {
            try
            {
                var vacations = await _unitOfWork.Repository<OfficialVacation>().GetAll();

                if (vacations == null || !vacations.Any())
                {
                    _logger.LogInformation("No official vacations found in the database.");
                    return Enumerable.Empty<OfficialVacationDTO>();
                }

                var vacationDtos = vacations.Select(v => new OfficialVacationDTO
                {
                    Id = v.Id,
                    VacationName = v.VacationName,
                    VacationDay = v.VacationDay.ToString("yyyy/MM/dd")
                }).ToList();

                _logger.LogInformation("Successfully retrieved {VacationCount} official vacations.", vacationDtos.Count);
                return vacationDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve official vacations. Error: {Message}", ex.Message);
                throw;
            }
        }

        #endregion


        #region Get Official Vacation By Id

        public async Task<OfficialVacationDTO?> GetOfficialVacationByIdAsync(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid vacation ID provided: {VacationId}", id);
                    throw new ArgumentException("Vacation ID must be a positive integer.", nameof(id));
                }

                var vacation = await _unitOfWork.Repository<OfficialVacation>().GetById(id);

                if (vacation == null)
                {
                    _logger.LogWarning("Official vacation with ID {VacationId} not found.", id);
                    return null;
                }

                var vacationDto = new OfficialVacationDTO
                {
                    Id = vacation.Id,
                    VacationName = vacation.VacationName,
                    VacationDay = vacation.VacationDay.ToString("yyyy/MM/dd")
                };

                _logger.LogInformation("Successfully retrieved official vacation with ID: {VacationId}", id);
                return vacationDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve official vacation with ID: {VacationId}. Error: {Message}", id, ex.Message);
                throw; 
            }
        }

        #endregion


        #region Update Official Vacation

        public async Task<OfficialVacationDTO> UpdateOfficialVacationAsync(OfficialVacationDTO vacation)
        {
            try
            {
                if (vacation == null)
                {
                    _logger.LogWarning("Vacation data is null.");
                    throw new ArgumentNullException(nameof(vacation), "Vacation data cannot be null.");
                }

                if (vacation.Id <= 0)
                {
                    _logger.LogWarning("Invalid vacation ID provided: {VacationId}", vacation.Id);
                    throw new ArgumentException("Vacation ID must be a positive integer.", nameof(vacation.Id));
                }

                var existingVacation = await _unitOfWork.Repository<OfficialVacation>().GetById(vacation.Id);
                if (existingVacation == null)
                {
                    _logger.LogWarning("Official vacation with ID {VacationId} not found.", vacation.Id);
                    throw new KeyNotFoundException($"No official vacation found with ID {vacation.Id}.");
                }

                if (!string.IsNullOrEmpty(vacation.VacationName))
                {
                    existingVacation.VacationName = vacation.VacationName;
                }

                if (!string.IsNullOrEmpty(vacation.VacationDay))
                {
                    if (!DateTime.TryParse(vacation.VacationDay, out var parsedVacationDay))
                    {
                        _logger.LogWarning("Invalid vacation day format provided: {VacationDay}", vacation.VacationDay);
                        throw new ArgumentException("Invalid vacation day format. Please provide a valid date (e.g., 'yyyy-MM-dd').", nameof(vacation.VacationDay));
                    }
                    existingVacation.VacationDay = parsedVacationDay;
                }

                _unitOfWork.Repository<OfficialVacation>().Update(existingVacation);
                await _unitOfWork.Save();

                var updatedVacationDto = new OfficialVacationDTO
                {
                    Id = existingVacation.Id,
                    VacationName = existingVacation.VacationName,
                    VacationDay = existingVacation.VacationDay.ToString("yyyy-MM-dd")
                };

                _logger.LogInformation("Successfully updated official vacation with ID: {VacationId}", vacation.Id);
                return updatedVacationDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update official vacation with ID: {VacationId}. Error: {Message}", vacation?.Id, ex.Message);
                throw;
            }
        }

        #endregion


        #region Delete Official Vacation

        public async Task<bool> DeleteOfficialVacationAsync(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid vacation ID provided: {VacationId}", id);
                    throw new ArgumentException("Vacation ID must be a positive integer.", nameof(id));
                }

                var existingVacation = await _unitOfWork.Repository<OfficialVacation>().GetById(id);
                if (existingVacation == null)
                {
                    _logger.LogWarning("Official vacation with ID {VacationId} not found.", id);
                    return false;
                }

                _unitOfWork.Repository<OfficialVacation>().Delete(existingVacation.Id);
                await _unitOfWork.Save();

                _logger.LogInformation("Successfully deleted official vacation with ID: {VacationId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete official vacation with ID: {VacationId}. Error: {Message}", id, ex.Message);
                throw;
            }
        }


        #endregion


        #region Is Official Vacation

        public async Task<bool> IsOfficialVacationAsync(DateTime date)
        {
            try
            {
                if (date == default(DateTime))
                {
                    _logger.LogWarning("Invalid date provided: default DateTime value.");
                    throw new ArgumentException("A valid date must be provided.", nameof(date));
                }

                var isVacation = await _unitOfWork.Repository<OfficialVacation>()
                    .AnyAsync(v => v.VacationDay.Date == date.Date);

                isVacation = isVacation || date.DayOfWeek == DayOfWeek.Friday;

                _logger.LogInformation("Date {Date} is {Status} an official vacation.",
                    date.ToString("yyyy-MM-dd"), isVacation ? "confirmed as" : "not");
                return isVacation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if {Date} is an official vacation. Error: {Message}",
                    date.ToString("yyyy-MM-dd"), ex.Message);
                throw;
            }
        }

        #endregion

    }
}
