using HRSystem.DataBase;
using HRSystem.DTO.AttendanceDTOs;
using HRSystem.DTO.EmployeeDTOs;
using HRSystem.Models;
using HRSystem.Services.VacationServices;
using HRSystem.UnitOfWork;
using Microsoft.EntityFrameworkCore;
namespace HRSystem.Services.VacationServices
{
    public class VacationService : IVacationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<VacationService> _logger;
        private readonly ApplicationDbContext _context;
        public VacationService(IUnitOfWork unitOfWork, ILogger<VacationService> logger, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _context = context;
        }

        #region Add Vacation or Absence

        public async Task AddVacationOrAbsence(string employeeId, DateTime date, double shiftHours)
        {
            try
            {
                var settingsRepository = _unitOfWork.Repository<GeneralSetting>().GetAll().Result.FirstOrDefault();
                var annualVacationsHours = settingsRepository?.NumberOfVacationsInYear * settingsRepository?.NumberOfDayWorkingHours ?? 450;

                var vacationRepository = _unitOfWork.Repository<EmployeeVacation>();
                var totalVacationHoursTaken = vacationRepository.Filter(v => v.UserId == employeeId && v.Date.Year == date.Year)
                    .Sum(v => v.Hours);

                var remainingLeaveHours = annualVacationsHours - totalVacationHoursTaken;

                if (remainingLeaveHours >= shiftHours)
                {
                    var vacation = new EmployeeVacation
                    {
                        UserId = employeeId,
                        Date = date,
                        Hours = shiftHours,
                    };
                    await vacationRepository.ADD(vacation);
                    _logger.LogInformation("Added {Hours} vacation hours for EmployeeId: {EmployeeId} on {Date}", shiftHours, employeeId, date);
                }
                else
                {
                    var absenceRepository = _unitOfWork.Repository<EmployeeAbsent>();
                    var absence = new EmployeeAbsent
                    {
                        EmployeeId = employeeId,
                        AbsentDate = date,
                        Hours = shiftHours
                    };
                    await absenceRepository.ADD(absence);
                    _logger.LogInformation("Added {Hours} absence hours for EmployeeId: {EmployeeId} on {Date}", shiftHours, employeeId, date);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding vacation/absence for EmployeeId: {EmployeeId} on {Date}", employeeId, date);
            }
        }

        #endregion


        #region Get Employee Vacations

        public async Task<EmployeeVacationsDTO> GetEmployeeVacations(string employeeId,ParamDTO dto)
        {
            try
            {
                // Validate input parameters
                if (string.IsNullOrWhiteSpace(employeeId))
                {
                    throw new ArgumentException("Employee ID cannot be null or empty.", nameof(employeeId));
                }

                // Use current date if year/month not provided
                var now = DateTime.UtcNow;
                var year = dto.Year ?? now.Year;
                var month = dto.Month ?? now.Month;
                var startOfMonth = new DateTime(year, month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);

                var generalSettings = _unitOfWork.Repository<GeneralSetting>()
                    .GetAll().Result
                    .FirstOrDefault()
                    ?? throw new InvalidOperationException("General settings not found.");
                // Get the repository for EmployeeVacation
                var vacationRepository = _unitOfWork.Repository<EmployeeVacation>();
                // Retrieve all vacations for the given employee
                var vacations = vacationRepository
                    .Filter(v => v.UserId == employeeId && v.Date.Month == month && v.Date.Year == year)
                    .Select(v => new
                    {
                        v.Hours,
                        v.Date
                    });

                // Check if no vacations are found
                if (vacations == null || !vacations.Any())
                {
                    _logger.LogInformation("No vacations found for EmployeeId: {EmployeeId}", employeeId);
                    return new EmployeeVacationsDTO
                    {
                        EmployeeName = null, // Or fetch the name from the Employee table if available
                        TotalVacationDays = 0,
                        EmployeeVacationDetails = null
                    };
                }

                // Fetch employee details to get the name (assuming EmployeeVacation has a relation with Employee)
                var employee = await _context.Users.Where(u => u.Id == employeeId).Select(u => u.Name)
                    .FirstOrDefaultAsync();

                // If no employee found, log and return empty DTO
                if (employee == null)
                {
                    _logger.LogInformation("No employee found for EmployeeId: {EmployeeId}", employeeId);
                    return new EmployeeVacationsDTO
                    {
                        EmployeeName = "Unknown",
                        TotalVacationDays = 0,
                        EmployeeVacationDetails = null
                    };
                }

                // Calculate total vacation hours
                double totalVacationHours = vacations.Sum(v => v.Hours);
                var dayWorkingHours = _context.GeneralSetting?.FirstOrDefault()?.NumberOfDayWorkingHours;
                // Convert total hours to days (10 hours = 1 day)
                double totalVacationDays = (double)(totalVacationHours / dayWorkingHours);

                // Prepare vacation details for the DTO
                var vacationDetails = vacations.Select(v => new EmployeeDetailsDTO
                {
                    Hours = v.Hours,
                    Date = v.Date.ToString("yyyy-MM-dd")
                }).ToList();

                // Return the DTO with employee name, total days, and vacation details
                return new EmployeeVacationsDTO
                {
                    EmployeeName = employee ?? "Unknown", // Assuming Employee has a Name property
                    TotalVacationDays = totalVacationDays,
                    EmployeeVacationDetails = vacationDetails.Any()?vacationDetails : null // Return the first vacation or null if none
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving vacations for EmployeeId: {EmployeeId}", employeeId);
                throw;
            }
        }

        #endregion


        #region Get All Employees Vacations

        public async Task<List<EmployeeVacationsDTO>> GetAllEmployeesVacations(ParamDTO dto)
        {
            try
            {
                if (dto.PageNumber < 1 || dto.PageSize < 1)
                {
                    throw new ArgumentException("Page number and page size must be greater than zero.");
                }

                var now = DateTime.Now;
                var year = dto.Year ?? now.Year;
                var month = dto.Month ?? now.Month;
                var startOfMonth = new DateTime(year, month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);
                var generalSettings =  _unitOfWork.Repository<GeneralSetting>()
                    .GetAll().Result
                    .FirstOrDefault()
                    ?? throw new InvalidOperationException("General settings not found.");
              
                // Retrieve all vacations from the repository
                var vacationRepository = _unitOfWork.Repository<EmployeeVacation>();
                var vacations = vacationRepository.Filter(v => v.Date.Month == month && v.Date.Year == year)
                    .Select(v => new
                    {
                        v.UserId,
                        v.Hours,
                        v.Date
                    }).ToList();

                // Check if there are any vacations; if not, return an empty list
                if (vacations == null || !vacations.Any())
                {
                    _logger.LogInformation("No vacations found for any employee.");
                    return new List<EmployeeVacationsDTO>();
                }

                // Get distinct employee IDs from the vacations
                var employeeIds = vacations
                        .Select(v => v.UserId)
                        .Distinct()
                        .ToList();

                // Fetch employees with pagination applied at the database level
                var employees = await _context.Users
                    .Where(u => employeeIds.Contains(u.Id))
                     .Skip((dto.PageNumber - 1) * dto.PageSize)
                    .Take(dto.PageSize)
                    .Select(u => new
                    {
                        u.Id,
                        u.Name
                    })
                    .ToListAsync();

                // Check if any employees were found for the given page; if not, return an empty list
                if (!employees.Any())
                {
                    _logger.LogInformation("No employees found for the given page.");
                    return new List<EmployeeVacationsDTO>();
                }

                // Map each employee to EmployeeVacationsDTO with their vacation details
                var employeeVacationsDTOs = employees
                .Select(employee =>
                {
                    var employeeVacations = vacations
                        .Where(v => v.UserId == employee.Id)
                        .Select(v => new EmployeeDetailsDTO
                        {
                            Hours = v.Hours,
                        })
                        .ToList();

                    var totalHours = employeeVacations.Sum(v => v.Hours);
                    var totalVacationDays = totalHours / generalSettings.NumberOfDayWorkingHours;

                    return new EmployeeVacationsDTO
                    {
                        EmployeeName = employee.Name,
                        TotalVacationDays = totalVacationDays
                    };
                }).ToList();

                return employeeVacationsDTOs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all employees' vacations.");
                throw;
            }
        }

        #endregion

    }
}
