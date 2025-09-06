#region Usings
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HRSystem.Extend;
using HRSystem.Models;
using HRSystem.DTO.AuthenticationDTOs;
using HRSystem.DTO.UserDTOs;
using HRSystem.UnitOfWork;
using HRSystem.DTO.ShiftDTOs;
using HRSystem.DTO.BranchDTOs;
using System.Globalization;
using HRSystem.DTO.EmployeeDTOs;
using HRSystem.DataBase;
using HRSystem.DTO.AttendanceDTOs;
using System.Threading;
#endregion

namespace HRSystem.Services.UsersServices
{
    public class UsersServices : IUsersServices
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UsersServices> _logger;
        private readonly ApplicationDbContext _context;
        public UsersServices(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, RoleManager<IdentityRole> roleManager, ILogger<UsersServices> logger, ApplicationDbContext context)
        {
            _unitOfWork = unitOfWork;
            this._userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _context = context;
        }


        #region Create User

        public async Task<AuthenticationDTO> Create(CreateUserDTO model)
        {
            try
            {
                if (model == null)
                {
                    _logger.LogWarning("User creation data is null.");
                    throw new ArgumentNullException(nameof(model), "User creation data cannot be null.");
                }

                if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                {
                    _logger.LogWarning("Email or password is null or empty.");
                    throw new ArgumentException("Email and password are required.", nameof(model));
                }

                if(await _userManager.FindByEmailAsync(model.Email) != null)
                {
                    _logger.LogWarning("User with email {Email} already exists.", model.Email);
                    return new AuthenticationDTO
                    {
                        Message = "User with this email already exists."
                    };
                }

                if (await _userManager.FindByNameAsync(model.Name) != null)
                {
                    _logger.LogWarning("User with name {Name} already exists.", model.Name);
                    return new AuthenticationDTO
                    {
                        Message = "User with this name already exists."
                    };
                }

                var user = new ApplicationUser
                {
                    UserName = model.Name,
                    Name = model.Name,
                    Email = model.Email,
                    Address = model.Address,
                    DateOfBarth = model.DateOfBarth,
                    PhoneNumber = model.PhoneNumber,
                    Nationalid = model.Nationalid,
                    BaseSalary = model.Salary,
                    Gender = Enum.TryParse<Gender>(model.Gender, true, out var gender) ? gender : Gender.Male,
                    HiringDate = model.DateOfWork,
                    BranchId = model.BranchId
                };

                using var transaction = await _unitOfWork.BeginTransactionAsync();

                try
                {
                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (!result.Succeeded)
                    {
                        var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                        _logger.LogWarning("Failed to create user with email: {Email}. Errors: {Errors}", model.Email, errors);
                        return new AuthenticationDTO
                        {
                            Message = $"Failed to create user: {errors}"
                        };
                    }

                    var Addshift = new AddShiftDTO
                    {
                        StartTime = "9:00",
                        EndTime = "17:00",
                        EmployeeId = user.Id
                    };
                    var shift = await _unitOfWork.ShiftServices.CreateShiftAsync(Addshift);

                    var role = model.Name == "Admin" ? "Admin" : "User";

                    var roleResult = await _userManager.AddToRoleAsync(user, role);
                    if (!roleResult.Succeeded)
                    {
                        var roleErrors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                        _logger.LogWarning("Failed to add user with ID: {UserId} to role: {Role}. Errors: {Errors}", user.Id, role, roleErrors);
                        throw new Exception($"Failed to assign role: {roleErrors}");
                    }

                    await _unitOfWork.CommitAsync();

                    _logger.LogInformation("Successfully created user with ID: {UserId} and email: {Email}", user.Id, user.Email);
                    return new AuthenticationDTO
                    {
                        Id = user.Id,
                        Email = user.Email,
                        Name = user.Name,
                        Address = user.Address,
                        DateOfBarth = user.DateOfBarth.ToLongDateString(),
                        PhoneNumber = user.PhoneNumber,
                        NationalId = user.Nationalid,
                        Salary = user.BaseSalary,
                        Gender = user.Gender.ToString(),
                        HiringDate = user.HiringDate.ToString("yyyy-MM-dd"),
                        IsAuthenticated = true,
                        Message = "User created successfully!",
                        Branch = await _unitOfWork.BranchServices.GetBranchByIdAsync(user.BranchId ?? 0),
                        Shift = await _unitOfWork.ShiftServices.GetByEmployeeId(user.Id),
                        Roles = await _userManager.GetRolesAsync(user)
                    };
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackAsync();
                    _logger.LogError(ex, "Failed to create user with email: {Email}. Error: {Message}", model.Email, ex.Message);
                    throw;
                }



            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating user with email: {Email}. Error: {Message}", model?.Email, ex.Message);
                return new AuthenticationDTO
                {
                    Message = $"An error occurred while creating the user: {ex.Message}"
                };
            }


        }

        #endregion


        #region Delete User

        public async Task<bool> Delete(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Invalid user ID provided: null or empty.");
                    throw new ArgumentException("User ID cannot be null or empty.", nameof(id));
                }

                using (var transaction = _unitOfWork.BeginTransactionAsync())
                {
                    try
                    {
                        var user = await _userManager.FindByIdAsync(id);
                        if (user == null)
                        {
                            _logger.LogWarning("User with ID {UserId} not found.", id);
                            return false;
                        }

                        var employeeShifts = _unitOfWork.Repository<EmployeeShift>()
                            .Filter(es => es.EmployeeId == id)
                            .ToList();

                        if (employeeShifts.Any())
                        {
                            var shiftIds = employeeShifts.Select(es => es.ShiftId).ToList();

                            _logger.LogDebug("Deleting {Count} EmployeeShift records for user ID: {UserId}", employeeShifts.Count, id);
                            foreach (var employeeShift in employeeShifts)
                            {
                                _unitOfWork.Repository<EmployeeShift>().Delete(employeeShift.Id);
                            }

                            var shifts = _unitOfWork.Repository<Shift>()
                                .Filter(s => shiftIds.Contains(s.Id))
                                .ToList();

                            _logger.LogDebug("Deleting {Count} Shift records associated with user ID: {UserId}", shifts.Count, id);
                            foreach (var shift in shifts)
                            {
                                _unitOfWork.Repository<Shift>().Delete(shift.Id);
                            }

                            await _unitOfWork.Save();
                        }
                        else
                        {
                            _logger.LogInformation("No EmployeeShift records found for user ID: {UserId}", id);
                        }

                        var result = await _userManager.DeleteAsync(user);
                        if (!result.Succeeded)
                        {
                            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                            _logger.LogWarning("Failed to delete user with ID: {UserId}. Errors: {Errors}", id, errors);
                            throw new Exception($"Failed to delete user: {errors}");
                        }

                        await _unitOfWork.CommitAsync();

                        _logger.LogInformation("Successfully deleted user with ID: {UserId} and associated records.", id);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        await _unitOfWork.RollbackAsync();
                        _logger.LogError(ex, "Failed to delete user with ID: {UserId} and associated records. Error: {Message}", id, ex.Message);
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting user with ID: {UserId}. Error: {Message}", id, ex.Message);
                throw new Exception($"Error deleting user with ID {id}: {ex.Message}", ex);
            }
        }

        #endregion


        #region Edit User

        public async Task<bool> Edit(UpdateUserDTO model)
        {
            try
            {
                if (model == null || string.IsNullOrEmpty(model.Id))
                {
                    _logger.LogWarning("Invalid update data: Model is null or user ID is empty.");
                    throw new ArgumentException("User ID cannot be null or empty.", nameof(model));
                }

                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found.", model.Id);
                    return false;
                }

                if (!string.IsNullOrEmpty(model.Email)) user.Email = model.Email;
                if (!string.IsNullOrEmpty(model.Name))
                {
                    //var existingUser = await _userManager.FindByNameAsync(model.Name);
                    //if (existingUser != null)
                    //{
                    //    _logger.LogWarning("User with name {Name} already exists.", model.Name);
                    //    throw new ArgumentException($"User with this name already exists: {model.Name}", nameof(model.Name));
                    //}
                    user.Name = model.Name;
                    user.UserName = model.Name;
                }
                if (!string.IsNullOrEmpty(model.Address)) user.Address = model.Address;
                if (!string.IsNullOrEmpty(model.NationalId)) user.Nationalid = model.NationalId;
                if (model.Salary.HasValue) user.BaseSalary = model.Salary.Value;
                if (model.DateOfWork.HasValue) user.HiringDate = model.DateOfWork.Value;
                if (model.DateOfBarth.HasValue) user.DateOfBarth = model.DateOfBarth.Value;
                if(model.BranchId.HasValue) user.BranchId = model.BranchId.Value;
                if (!string.IsNullOrEmpty(model.Gender))
                {
                    if (Enum.TryParse<Gender>(model.Gender, true, out var gender))
                    {
                        user.Gender = gender;
                    }
                    else
                    {
                        _logger.LogWarning("Invalid gender value '{Gender}' provided for user ID: {UserId}", model.Gender, model.Id);
                        throw new ArgumentException($"Invalid gender value: {model.Gender}", nameof(model.Gender));
                    }
                }

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                    _logger.LogWarning("Failed to update user with ID: {UserId}. Errors: {Errors}", model.Id, errors);
                    throw new Exception($"Failed to update user: {errors}");
                }

                _logger.LogInformation("Successfully updated user with ID: {UserId}", model.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID: {UserId}. Error: {Message}", model?.Id, ex.Message);
                throw new Exception($"Error updating user with ID {model?.Id}: {ex.Message}", ex);
            }
        }

        #endregion


        #region Get All Users
        public async Task<List<AuthenticationDTO>> GetAllAsync(PaginationDTO pagination)
        {
            try
            {
                // Build the query with pagination and filtration
                var usersQuery = _userManager.Users.AsQueryable();

                // Apply filters in a single Where clause
                usersQuery = usersQuery.Where(u =>
                    (string.IsNullOrEmpty(pagination.Name) ? true : u.Name.Contains(pagination.Name)) ||
                    (string.IsNullOrEmpty(pagination.Email) ? true : u.Email.Contains(pagination.Email)) ||
                    (string.IsNullOrEmpty(pagination.PhoneNumber) ? true : u.PhoneNumber.Contains(pagination.PhoneNumber)) ||
                    (string.IsNullOrEmpty(pagination.NationalId) ? true : u.Nationalid.Contains(pagination.NationalId)) ||
                    (string.IsNullOrEmpty(pagination.Gender) ? true : u.Gender.ToString() == pagination.Gender) ||
                    (!pagination.HiringDate.HasValue ? true : u.HiringDate.Date == pagination.HiringDate.Value.Date) ||
                    (!pagination.DateOfBarth.HasValue ? true : u.DateOfBarth.Date == pagination.DateOfBarth.Value.Date) ||
                    (!pagination.Salary.HasValue ? true : u.BaseSalary == pagination.Salary.Value)
                );

                // Apply sorting and pagination
                var users = await usersQuery
                    .OrderBy(u => u.Name) // Default sorting by Name
                    .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                // Map users to AuthenticationDTO
                var userList = new List<AuthenticationDTO>();
                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var shift = await _unitOfWork.ShiftServices.GetByEmployeeId(user.Id);
                    var branch = user.BranchId.HasValue && user.BranchId.Value != 0
                        ? await _unitOfWork.BranchServices.GetBranchByIdAsync(user.BranchId.Value)
                        : null;

                    userList.Add(new AuthenticationDTO
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        Address = user.Address,
                        NationalId = user.Nationalid,
                        Salary = user.BaseSalary,
                        Gender = user.Gender.ToString() ?? "Not specified",
                        HiringDate = user.HiringDate.ToString("yyyy-MM-dd"),
                        DateOfBarth = user.DateOfBarth.ToString("yyyy-MM-dd"),
                        Roles = roles,
                        Shift = shift,
                        Branch = branch
                    });
                }

                return userList;
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving users: " + ex.Message, ex);
            }
        }

        #endregion


        #region Get User By ID
        public async Task<AuthenticationDTO?> GetByID(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Invalid user ID provided: null or empty.");
                    return null;
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found.", id);
                    return null;
                }

                var roles = await _userManager.GetRolesAsync(user);

                var shifts = _unitOfWork.Repository<EmployeeShift>()
                    .Filter(es => es.EmployeeId == id);
                var shiftIds = shifts.Select(es => es.ShiftId).ToList();
                var shiftEntities = _unitOfWork.Repository<Shift>()
                    .Filter(s => shiftIds.Contains(s.Id));
                var shiftDTOs = shiftEntities.Select(s => new ShiftDTO
                {
                    Id = s.Id,
                    StartTime = s.StartTime.ToString("H:mm", CultureInfo.InvariantCulture),
                    EndTime = s.EndTime.ToString("H:mm", CultureInfo.InvariantCulture),
                    EmployeeId = id
                }).ToList();

                var branch = _unitOfWork.Repository<Branch>()
                    .Filter(b => b.Id == user.BranchId)
                    .FirstOrDefault();

                var branchDTO = branch != null ? new BranchDTO
                {
                    Id = branch.Id,
                    Name = branch.Name,
                    Latitude = branch.Latitude,
                    Longitude = branch.Longitude,
                    Radius = branch.Radius
                } : null;

                _logger.LogInformation("Successfully retrieved user with ID: {UserId}", id);
                return new AuthenticationDTO
                {
                    Id = user.Id,
                    Name = user.Name ?? "Not specified",
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber ?? "Not specified",
                    Address = user.Address ?? "Not specified",
                    NationalId = user.Nationalid ?? "Not specified",
                    Salary = (double)user.BaseSalary,
                    Shift = shiftDTOs,
                    Gender = user.Gender.ToString() ?? "Not specified",
                    Branch = branchDTO,
                    HiringDate = user.HiringDate.ToString("yyyy-MM-dd"),
                    DateOfBarth = user.DateOfBarth.ToString("yyyy-MM-dd"),
                    IsAuthenticated = true,
                    Message = "User found",
                    Roles = roles
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user with ID: {UserId}. Error: {Message}", id, ex.Message);
                throw new Exception($"Error retrieving user with ID {id}: {ex.Message}", ex);
            }
        }


        #endregion


        #region Get Employees Salaries

        public async Task<List<EmployeesWithSalaryDTO>> GetEmployeesSalaries(ParamDTO dto)
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

                // Retrieve employee salaries with pagination and date filter
                var employeeSalaries = await _context.EmployeeSalaries
                    .Where(es => es.Date >= startOfMonth && es.Date <= endOfMonth)
                    .Select(es => new
                    {
                        es.EmployeeId,
                        EmployeeName = es.Employee != null ? es.Employee.Name : "Not specified",
                        es.Salary
                    })
                    .OrderBy(es => es.EmployeeName)
                    .Skip((dto.PageNumber - 1) * dto.PageSize)
                    .Take(dto.PageSize)
                    .ToListAsync();

                // If no salaries found, log and return empty list
                if (!employeeSalaries.Any())
                {
                    _logger.LogInformation("No employee salaries found for year {Year} and month {Month}", year, month);
                    return new List<EmployeesWithSalaryDTO>();
                }
                // Map to DTO
                var employeeSalariesDTO = employeeSalaries
                    .Select(es => new EmployeesWithSalaryDTO
                    {
                        EmployeeId = es.EmployeeId,
                        EmployeeName = es.EmployeeName,
                        NetSalary = (double)es.Salary
                    })
                    .ToList();

                return employeeSalariesDTO;
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving employee salaries: " + ex.Message, ex);
            }
        }

        #endregion


        #region Get Employee Salary Details

        //public async Task<SalaryDetailsDTO> GetEmployeeSalaryDetails(string employeeId, DateTime? startDate = null, DateTime? endDate = null)
        //{
        //    try
        //    {
        //        var now = DateTime.Now;
        //        var StartDate = startDate ?? new DateTime(now.Year, now.Month, 1);

        //        var EndDate = endDate ?? StartDate.AddMonths(1).AddTicks(-1);

        //        if(StartDate > EndDate)
        //        {
        //            throw new ArgumentException("Start date cannot be later than end date.");
        //        }

        //        // Fetch the employee details including base salary
        //        var employee = await _context.Users
        //            .Where(u => u.Id == employeeId)
        //            .Select(u => new { u.Name, u.BaseSalary })
        //            .FirstOrDefaultAsync();

        //        if (employee == null)
        //        {
        //            _logger.LogInformation("No employee found for EmployeeId: {EmployeeId}", employeeId);
        //            throw new Exception($"Employee with ID {employeeId} not found.");
        //        }


        //        // Get the base salary from the ApplicationUser
        //        double baseSalary = employee.BaseSalary;

        //        // Fetch attendance and leave records within the date range (not used for calculations but kept as per your code)
        //        var attendanceRecords = await _context.AttendancesAndLeaves
        //            .Where(a => a.EmployeeId == employeeId && a.Time >= StartDate && a.Time <= endDate)
        //            .ToListAsync();

        //        // Calculate absence details
        //        double absenceHours = await _context.EmployeeAbsents
        //            .Where(a => a.EmployeeId == employeeId && a.AbsentDate >= StartDate && a.AbsentDate <= endDate)
        //            .SumAsync(a => a.Hours);

        //        double absentDays = absenceHours / 10.0; // 1 day = 10 hours
        //        double penalizedDays = absentDays * 1.5; // 1 absent day = 1.5 days deduction
        //        double dailySalary = baseSalary / 30.0; // Daily salary for deduction (using 30 days as per your example)
        //        double absentDaysSalary = penalizedDays * dailySalary; // Total deduction for absences

        //        // Calculate overtime and late hours
        //        double overtimeHours = await _context.EmployeeExtraAndLateHours
        //            .Where(a => a.EmployeeId == employeeId && a.Type == "OverTime" && a.Date >= StartDate && a.Date <= endDate)
        //            .SumAsync(a => a.Hours);

        //        double lateHours = await _context.EmployeeExtraAndLateHours
        //            .Where(a => a.EmployeeId == employeeId && a.Type == "Late" && a.Date >= startDate && a.Date <= endDate)
        //            .SumAsync(a => a.Hours);
        //        // Calculate hourly rate and adjustments
        //        double hourlyRate = baseSalary / (30.0 * 10.0); // Hourly rate = Base Salary / (30 days * 10 hours) = Base Salary / 300
        //        double overtimeSalary = overtimeHours * 1.5 * hourlyRate; // 1 overtime hour = 1.5x hourly rate
        //        double lateSalary = lateHours * 1.5 * hourlyRate; // 1 late hour = 1.5x hourly rate deduction

        //        // Calculate total salary
        //        double totalSalary = baseSalary - absentDaysSalary + overtimeSalary - lateSalary;

        //        // Map to DTO
        //        return new SalaryDetailsDTO
        //        {
        //            EmployeeName = employee.Name ?? "Not specified",
        //            BaseSalary = baseSalary,
        //            OverTime = overtimeHours,
        //            OverTimeSalary = overtimeSalary,
        //            LateTime = lateHours,
        //            LateTimeSalary = lateSalary,
        //            NumberOfAbsentDays = penalizedDays,
        //            AbsentDaysSalary = absentDaysSalary,
        //            TotalSalary = totalSalary
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"Error retrieving employee salary details: {ex.Message}", ex);
        //    }
        //}

        public async Task<SalaryDetailsDTO> GetEmployeeSalaryDetails(string employeeId, int? month = null, int? year = null)
        {
            try
            {
                // Default to current month/year if not provided
                var now = DateTime.Today;
                var effectiveMonth = month ?? now.Month;
                var effectiveYear = year ?? now.Year;

                // Calculate start and end of month
                var startDate = new DateTime(effectiveYear, effectiveMonth, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                // Fetch general settings
                var generalSettings = await _unitOfWork.Repository<GeneralSetting>().GetAll();
                var settings = generalSettings.FirstOrDefault();
                if (settings == null)
                {
                    _logger.LogWarning("General settings not found. Cannot calculate salary details for EmployeeId: {EmployeeId}", employeeId);
                    throw new Exception("General settings not found.");
                }

                int hoursPerDay = settings.NumberOfDayWorkingHours;
                decimal rateOfExtraHour = (decimal)settings.RateOfExtraHour;
                decimal rateOfLateHour = (decimal)settings.RateOfExtraHour;

                // Calculate working days in the month (excluding Fridays)
                int totalDaysInMonth = DateTime.DaysInMonth(effectiveYear, effectiveMonth);
                var officialVacations = _unitOfWork.Repository<OfficialVacation>()
                    .Filter(v => v.VacationDay >= startDate && v.VacationDay <= endDate)
                    .Select(v => v.VacationDay.Date)
                    .Distinct()
                    .ToList();

                int vacationDays = officialVacations.Count;

                // Calculate Fridays in the period
                int fridayCount = 0;
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    if (date.DayOfWeek == DayOfWeek.Friday)
                    {
                        fridayCount++;
                    }
                }
                //&& !officialVacations.Contains(date)
                int daysPerMonth = totalDaysInMonth - fridayCount;
                if (daysPerMonth <= 0)
                {
                    _logger.LogWarning("No working days available after excluding Fridays for EmployeeId: {EmployeeId} in {Month}/{Year}",
                        employeeId, effectiveMonth, effectiveYear);
                    throw new Exception("No working days available for salary calculation.");
                }

                // Fetch employee details
                var employee = await _context.Users
                    .Where(u => u.Id == employeeId)
                    .Select(u => new { u.Id, u.Name, u.BaseSalary })
                    .FirstOrDefaultAsync();

                if (employee == null)
                {
                    _logger.LogInformation("No employee found for EmployeeId: {EmployeeId}", employeeId);
                    throw new Exception($"Employee with ID {employeeId} not found.");
                }

                // Fetch absence hours
                double absenceHours = await _context.EmployeeAbsents
                    .Where(a => a.EmployeeId == employeeId && a.AbsentDate >= startDate && a.AbsentDate <= endDate)
                    .SumAsync(a => a.Hours);

                // Fetch overtime and late hours
                var extraAndLateHours = await _context.EmployeeExtraAndLateHours
                    .Where(a => a.EmployeeId == employeeId && a.Date >= startDate && a.Date <= endDate)
                    .GroupBy(a => a.Type)
                    .Select(g => new
                    {
                        Type = g.Key,
                        Hours = g.Sum(a => a.Hours)
                    })
                    .ToListAsync();

                double overtimeHours = extraAndLateHours.FirstOrDefault(a => a.Type == "OverTime")?.Hours ?? 0;
                double lateHours = extraAndLateHours.FirstOrDefault(a => a.Type == "Late")?.Hours ?? 0;

                // Calculate salary components
                decimal baseSalary = (decimal)employee.BaseSalary;
                decimal dailySalary = baseSalary / daysPerMonth;
                decimal hourlyRate = dailySalary / hoursPerDay;

                // Calculate deductions and rewards
                double absentDays = absenceHours / hoursPerDay;
                decimal absentDeduction = (decimal)absentDays * dailySalary;
                decimal lateDeduction = (decimal)lateHours * rateOfLateHour * hourlyRate;
                decimal totalDiscount = absentDeduction + lateDeduction;
                decimal overtimeReward = (decimal)overtimeHours * rateOfExtraHour * hourlyRate;
                decimal totalSalary = baseSalary - totalDiscount + overtimeReward;

                var AttendanceDays =
                            _unitOfWork.Repository<AttendanceAndLeave>()
                            .Filter(a => a.EmployeeId == employee.Id &&
                            a.Time >= startDate && a.Time <= endDate &&
                            a.Type == "Attendance"
                            );

                var numberOfAttendanceFridayDays = 0;
                foreach (var attendance in AttendanceDays)
                {
                    if (attendance.Time.DayOfWeek == DayOfWeek.Friday)
                        numberOfAttendanceFridayDays++;
                }
                var additonalSalary = _unitOfWork.Repository<AdditionalSalary>()
                            .Filter(sp => sp.UserId == employee.Id
                            && sp.Date >= startDate && sp.Date <= endDate)
                            .Select(sp => new { sp.SalesPercentage, sp.FridaySalary })
                            .FirstOrDefault();
                if (additonalSalary != null)
                {
                    var FridaySalary = additonalSalary.FridaySalary;
                    if (numberOfAttendanceFridayDays > 0 && FridaySalary > 0)
                        totalSalary += (decimal)(numberOfAttendanceFridayDays * FridaySalary);


                    if (additonalSalary.SalesPercentage > 0)
                        totalSalary += (decimal)additonalSalary.SalesPercentage;
                }

                // Map to DTO
                return new SalaryDetailsDTO
                {
                    EmployeeName = employee.Name ?? "Not specified",
                    BaseSalary = (double)baseSalary,
                    OverTime = overtimeHours,
                    OverTimeSalary = (double)overtimeReward,
                    LateTime = lateHours,
                    LateTimeSalary = (double)lateDeduction,
                    NumberOfAbsentDays = absentDays,
                    AbsentDaysSalary = (double)absentDeduction,
                    TotalSalary = (double)totalSalary,
                    Month = effectiveMonth,
                    Year = effectiveYear,
                    SalesPercentage = additonalSalary?.SalesPercentage ?? 0,
                    FridaySalary = additonalSalary?.FridaySalary ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving salary details for EmployeeId: {EmployeeId}", employeeId);
                throw new Exception($"Error retrieving employee salary details: {ex.Message}", ex);
            }
        }

        #endregion


        #region Calculate Employee Salary

        public async Task CalculateSalary(DateTime? calculationDate = null)
        {
            try
            {
                // Default to current month if no date provided
                var now = DateTime.Today;
                var effectiveDate = calculationDate ?? new DateTime(now.Year, now.Month, 1);
                var startDate = effectiveDate;
                var endDate = startDate.AddMonths(1).AddDays(-1); // End of the month
                if(_context.EmployeeSalaries.Any(s => s.Date.Month == startDate.Month && startDate.Year == s.Date.Year))
                {
                    _logger.LogWarning("Salary calculation for the period {StartDate} to {EndDate} has already been performed.", startDate, endDate);
                    return;
                }
                _logger.LogInformation("Starting salary calculation for period {StartDate} to {EndDate}", startDate, endDate);

                
                // Fetch general settings
                var generalSettings = await _unitOfWork.Repository<GeneralSetting>().GetAll();
                var settings = generalSettings.FirstOrDefault();
                if (settings == null)
                {
                    _logger.LogWarning("General settings not found. Cannot calculate salaries.");
                    return;
                }

                int hoursPerDay = settings.NumberOfDayWorkingHours; // e.g., 8 hours per day
                decimal rateOfExtraHour = (decimal)settings.RateOfExtraHour; // e.g., 1.5x for overtime
                decimal rateOfLateHour = (decimal)settings.RateOfExtraHour; // Same rate for late deduction

                // Calculate daysPerMonth (excluding Official Vacations and Fridays)
                int totalDaysInMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);
                var officialVacations = _unitOfWork.Repository<OfficialVacation>()
                    .Filter(v => v.VacationDay >= startDate && v.VacationDay <= endDate)
                    .Select(v => v.VacationDay.Date)
                    .Distinct()
                    .ToList();
                int vacationDays = officialVacations.Count;

                // Calculate Fridays in the month
                int fridayCount = 0;
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    if (date.DayOfWeek == DayOfWeek.Friday /* && !officialVacations.Contains(date)*/)
                    {
                        fridayCount++;
                    }
                }

                int daysPerMonth = totalDaysInMonth - fridayCount;
                if (daysPerMonth <= 0)
                {
                    _logger.LogWarning("No working days available after excluding vacations and Fridays for period {StartDate} to {EndDate}", startDate, endDate);
                    return;
                }

                // Fetch all employees with base salary
                var employees = await _context.Users
                    .Where(u => u.BaseSalary > 0)
                    .Select(u => new { u.Id, u.Name, u.BaseSalary })
                    .ToListAsync();

                if (!employees.Any())
                {
                    _logger.LogWarning("No employees with valid base salary found for salary calculation.");
                    return;
                }

                // Fetch absence hours for all employees
                var absenceHours = _unitOfWork.Repository<EmployeeAbsent>()
                    .Filter(a => a.AbsentDate >= startDate && a.AbsentDate <= endDate)
                    .GroupBy(a => a.EmployeeId)
                    .Select(g => new
                    {
                        EmployeeId = g.Key,
                        Hours = g.Sum(a => a.Hours)
                    })
                    .ToList();

                // Fetch overtime and late hours for all employees
                var extraAndLateHours = _unitOfWork.Repository<EmployeeExtraAndLateHour>()
                    .Filter(a => a.Date >= startDate && a.Date <= endDate)
                    .GroupBy(a => new { a.EmployeeId, a.Type })
                    .Select(g => new
                    {
                        g.Key.EmployeeId,
                        g.Key.Type,
                        Hours = g.Sum(a => a.Hours)
                    })
                    .ToList();

                // Calculate salaries for each employee
                var salariesToSave = new List<EmployeeSalary>();
                foreach (var employee in employees)
                {
                    try
                    {
                        // Get absence hours
                        var employeeAbsenceHours = absenceHours
                            .FirstOrDefault(a => a.EmployeeId == employee.Id)?.Hours ?? 0;

                        // Get overtime and late hours
                        var employeeExtraHours = extraAndLateHours
                            .Where(a => a.EmployeeId == employee.Id)
                            .ToList();

                        var overtimeHours = employeeExtraHours
                            .FirstOrDefault(a => a.Type == "OverTime")?.Hours ?? 0;
                        var lateHours = employeeExtraHours
                            .FirstOrDefault(a => a.Type == "Late")?.Hours ?? 0;

                        // Calculate salary components
                        decimal baseSalary = (decimal)employee.BaseSalary; // Base salary
                        decimal dailySalary = baseSalary / daysPerMonth; // Daily rate
                        decimal hourlyRate = dailySalary / hoursPerDay; // Hourly rate

                        // Calculate Absent deduction
                        double absentDays = employeeAbsenceHours / hoursPerDay; // Convert hours to days
                        decimal absentDeduction = (decimal)absentDays * dailySalary; // Deduction for absent days

                        // Calculate Late deduction
                        decimal lateDeduction = (decimal)lateHours * rateOfLateHour * hourlyRate; // Late deduction

                        // Total Discount
                        decimal totalDiscount = absentDeduction + lateDeduction;

                        // Calculate Reward (OverTime)
                        decimal overtimeReward = (decimal)overtimeHours * rateOfExtraHour * hourlyRate; // Overtime reward

                        // Final Salary
                        decimal totalSalary = baseSalary - totalDiscount + overtimeReward;

                        var AttendanceDays =
                            _unitOfWork.Repository<AttendanceAndLeave>()
                            .Filter(a => a.EmployeeId == employee.Id &&
                            a.Time >= startDate && a.Time <= endDate &&
                            a.Type == "Attendance"
                            );
                        var numberOfAttendanceFridayDays = 0;
                        foreach (var attendance in AttendanceDays)
                        {
                            if (attendance.Time.DayOfWeek == DayOfWeek.Friday)
                                numberOfAttendanceFridayDays++;
                        }

                        var additonalSalary = _unitOfWork.Repository<AdditionalSalary>()
                            .Filter(sp => sp.UserId == employee.Id
                            && sp.Date >= startDate && sp.Date <= endDate)
                            .Select(sp => new { sp.SalesPercentage, sp.FridaySalary})
                            .FirstOrDefault();

                        if (additonalSalary != null)
                        {
                            var FridaySalary = additonalSalary.FridaySalary;
                            if (numberOfAttendanceFridayDays > 0 && FridaySalary > 0)
                                totalSalary += (decimal)(numberOfAttendanceFridayDays * FridaySalary );


                            if (additonalSalary.SalesPercentage >= 0)
                                totalSalary += (decimal)additonalSalary.SalesPercentage;
                        }

                        var salaryRecord = new EmployeeSalary
                        {
                            EmployeeId = employee.Id,
                            Date = startDate,
                            Salary = Math.Round(totalSalary, 2)
                        };

                        salariesToSave.Add(salaryRecord);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error calculating salary for EmployeeId: {EmployeeId} for period {StartDate} to {EndDate}",
                            employee.Id, startDate, endDate);
                        continue; // Skip to next employee to avoid failing the entire job
                    }
                }

                // Save salary records
                if (salariesToSave.Any())
                {
                    await _unitOfWork.Repository<EmployeeSalary>().AddRange(salariesToSave);
                    await _unitOfWork.Save();
                    _logger.LogInformation("Successfully saved {Count} salary records for period {StartDate} to {EndDate}",
                        salariesToSave.Count, startDate, endDate);
                }
                else
                {
                    _logger.LogWarning("No salary records generated for period {StartDate} to {EndDate}", startDate, endDate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during salary calculation for period {StartDate} to {EndDate}",
                    calculationDate, calculationDate?.AddMonths(1).AddDays(-1));
                throw; // Rethrow to allow Hangfire to retry the job
            }
        }

        #endregion


        #region Get Employees By Branch ID

        public async Task<List<AuthenticationDTO>> GetEmployeesByBranchId(int id)
        {
            _logger.LogInformation("Starting to retrieve employees for branch ID: {BranchId}", id);

            try
            {
                // Validate input
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid branch ID provided: {BranchId}", id);
                    throw new ArgumentException("Branch ID must be greater than zero", nameof(id));
                }

                // Fetch users filtered by branch ID in a single query
                var users = await _userManager.Users
                    .Where(u => u.BranchId == id)
                    .ToListAsync();

                if (!users.Any())
                {
                    _logger.LogInformation("No employees found for branch ID: {BranchId}", id);
                    return new List<AuthenticationDTO>();
                }

                
                // Build DTOs efficiently
                var userList = new List<AuthenticationDTO>();
                foreach (var user in users)
                {
                    var shifts = _unitOfWork.Repository<EmployeeShift>()
                   .Filter(es => es.EmployeeId == user.Id);
                    var shiftIds = shifts.Select(es => es.ShiftId).ToList();
                    var shiftEntities = _unitOfWork.Repository<Shift>()
                        .Filter(s => shiftIds.Contains(s.Id));
                    var shiftDTOs = shiftEntities.Select(s => new ShiftDTO
                    {
                        Id = s.Id,
                        StartTime = s.StartTime.ToString("H:mm", CultureInfo.InvariantCulture),
                        EndTime = s.EndTime.ToString("H:mm", CultureInfo.InvariantCulture),
                        EmployeeId = user.Id
                    }).ToList();

                    // Parallel async operations for roles, shift, and branch
                    var rolesTask = await _userManager.GetRolesAsync(user);
                    var branchTask = await _unitOfWork.BranchServices.GetBranchByIdAsync(user.BranchId ?? 0);


                    userList.Add(new AuthenticationDTO
                    {
                        Id = user.Id,
                        Name = user.Name ?? string.Empty,
                        Email = user.Email ?? string.Empty,
                        PhoneNumber = user.PhoneNumber ?? string.Empty,
                        Address = user.Address ?? string.Empty,
                        NationalId = user.Nationalid ?? string.Empty,
                        Salary = user.BaseSalary,
                        Gender = user.Gender.ToString() ?? "Not specified",
                        HiringDate = user.HiringDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        DateOfBarth = user.DateOfBarth.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        Roles = rolesTask,
                        Shift = shiftDTOs,
                        Branch = branchTask
                    });
                }

                _logger.LogInformation("Successfully retrieved {EmployeeCount} employees for branch ID: {BranchId}", userList.Count, id);
                return userList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employees for branch ID: {BranchId}", id);
                throw new Exception($"Failed to retrieve employees for branch ID {id}: {ex.Message}", ex);
            }
        }


        #endregion


        #region Update Net Salary

        public async Task<string> UpdateNetSalary(UpdateSalaryDTO dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.EmployeeId))
            {
                _logger.LogWarning("Invalid update data: DTO is null or employee ID is empty.");
                return "Employee ID cannot be null or empty.";
            }

            try
            {
                var employeeSalaryRepository = _unitOfWork.Repository<EmployeeSalary>();
                var employee = employeeSalaryRepository
                    .Filter(e => e.EmployeeId == dto.EmployeeId && e.Date.Month == dto.Month && e.Date.Year == dto.Year)
                    .FirstOrDefault();

                if (employee == null)
                {
                    _logger.LogWarning("Employee with ID {EmployeeId} not found.", dto.EmployeeId);
                    return "Employee not found."    ;
                }

                if (dto.NetSalary < 0)
                {
                    _logger.LogWarning("Invalid net salary value {NetSalary} for employee ID {EmployeeId}.", dto.NetSalary, dto.EmployeeId);
                    return "Net salary cannot be negative.";
                }

                employee.Salary = (decimal)dto.NetSalary;
                employeeSalaryRepository.Update(employee);
                await _unitOfWork.Save();

                _logger.LogInformation("Successfully updated salary for employee with ID: {EmployeeId}", dto.EmployeeId);
                return "Salary updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating salary for employee with ID: {EmployeeId}. Error: {Message}", dto.EmployeeId, ex.Message);
                return $"Error updating salary for employee with ID {dto.EmployeeId}: {ex.Message}";
            }
        }

        #endregion


        #region Update Absent Or Vacation Or OverTime Or Late Hours

        public async Task<string> UpdateWorkStatisticsAsync(UpdateWorkStatisticsDTO dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.EmployeeId) || string.IsNullOrEmpty(dto.Type))
            {
                _logger.LogWarning("Invalid input: DTO is null, or EmployeeId/Type is null or empty. EmployeeId: {EmployeeId}, Type: {Type}", dto?.EmployeeId, dto?.Type);
                return "DTO, Employee ID, and Type cannot be null or empty.";
            }

            if (dto.Date == null)
            {
                _logger.LogWarning("Invalid input: Date is null for EmployeeId: {EmployeeId}, Type: {Type}", dto.EmployeeId, dto.Type);
                return "Date cannot be null.";
            }

            if (dto.Hours < 0)
            {
                _logger.LogWarning("Invalid hours value: {Hours} for EmployeeId: {EmployeeId}, Type: {Type}", dto.Hours, dto.EmployeeId, dto.Type);
                return "Hours cannot be negative.";
            }

            var Day = dto.Date.Day;
            var Month = dto.Date.Month;
            var Year = dto.Date.Year;

            // Validate Type
            string type = dto.Type.ToLower();
            if (!new[] { "absent", "vacation", "overtime", "late" }.Contains(type))
            {
                _logger.LogWarning("Invalid type: {Type} for EmployeeId: {EmployeeId}", dto.Type, dto.EmployeeId);
                return "Invalid Type. Must be 'Absent', 'Vacation', 'Overtime', or 'LateHours'.";
            }

            try
            {
                switch (type)
                {
                    case "absent":
                        var AbsentRepository = _unitOfWork.Repository<EmployeeAbsent>();

                        var Absent = AbsentRepository
                            .Filter(x => x.EmployeeId == dto.EmployeeId
                            && x.AbsentDate.Month == Month
                            && x.AbsentDate.Year == Year
                            && x.AbsentDate.Day == Day).FirstOrDefault();

                        if (Absent == null)
                        {
                            _logger.LogWarning("No record found for EmployeeId: {EmployeeId}, Type: {Type}", dto.EmployeeId, type);
                            return "Record not found.";
                        }
                        Absent.Hours = dto.Hours; // For overtime, you might want additional logic (e.g., max hours)
                        _unitOfWork.Repository<EmployeeAbsent>().Update(Absent);
                        break;
                    case "vacation":

                        var VacationRepository = _unitOfWork.Repository<EmployeeVacation>();
                        var Vacation = VacationRepository
                            .Filter(x => x.UserId == dto.EmployeeId
                            && x.Date.Month == Month
                            && x.Date.Year == Year
                            && x.Date.Day == Day).FirstOrDefault();
                        if (Vacation == null)
                        {
                            _logger.LogWarning("No record found for EmployeeId: {EmployeeId}, Type: {Type}", dto.EmployeeId, type);
                            return "Record not found.";
                        }
                        Vacation.Hours = dto.Hours; // For overtime, you might want additional logic (e.g., max hours)
                        _unitOfWork.Repository<EmployeeVacation>().Update(Vacation);
                        break;

                    case "overtime":
                    case "late":
                        var LateOrOverTimeRepository = _unitOfWork.Repository<EmployeeExtraAndLateHour>();
                        var LateOrOverTime = LateOrOverTimeRepository
                            .Filter(x => x.EmployeeId == dto.EmployeeId
                        && x.Date.Month == Month
                        && x.Date.Year == Year
                        && x.Date.Day == Day
                        && x.Type.ToLower() == type).FirstOrDefault();
                        if (LateOrOverTime == null)
                        {
                            _logger.LogWarning("No record found for EmployeeId: {EmployeeId}, Type: {Type}", dto.EmployeeId, type);
                            return "Record not found.";
                        }

                        LateOrOverTime.Hours = dto.Hours; // For overtime, you might want additional logic (e.g., max hours)
                        _unitOfWork.Repository<EmployeeExtraAndLateHour>().Update(LateOrOverTime);
                        break;

                    default:
                        _logger.LogWarning("Unexpected type: {Type} for EmployeeId: {EmployeeId}", type, dto.EmployeeId);
                        return "Unexpected Type.";
                }

                await _unitOfWork.Save();
                _logger.LogInformation("Successfully updated work statistics for EmployeeId: {EmployeeId}, Type: {Type}, Hours: {Hours}",
                        dto.EmployeeId, type, dto.Hours);
                return "Record updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating work statistics for EmployeeId: {EmployeeId}, Type: {Type}", dto.EmployeeId, type);
                return $"Error updating work statistics: {ex.Message}";
            }
        }

        #endregion


        #region Update Sales Percentage
        public async Task<string> UpdateSalesPresentage(UpdateSalesPresentageDTO dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.EmployeeId) || dto.SalesPercentage < 0)
            {
                _logger.LogWarning("Invalid input: DTO is null, EmployeeId is empty, or SalesPercentage is negative.");
                return "Invalid input data.";
            }
            try
            {
                var sales = _unitOfWork.Repository<AdditionalSalary>()
                    .Filter(s => s.UserId == dto.EmployeeId &&
                    s.Date.Month == dto.Month && s.Date.Year == dto.Year)
                    .FirstOrDefault();

                if (sales == null)
                {
                    AdditionalSalary newSales = new AdditionalSalary
                    {
                        UserId = dto.EmployeeId,
                        SalesPercentage = dto.SalesPercentage,
                        FridaySalary = dto.FridaySalary ?? 0,
                        Date = new DateTime(dto.Year, dto.Month, 1)
                    };
                    await _unitOfWork.Repository<AdditionalSalary>().ADD(newSales);
                }
                else
                {
                    sales.SalesPercentage = dto.SalesPercentage;
                    sales.FridaySalary = dto.FridaySalary ?? 0;
                    _unitOfWork.Repository<AdditionalSalary>().Update(sales);
                }
                await _unitOfWork.Save();
                _logger.LogInformation("Successfully updated sales percentage for employee with ID: {EmployeeId}", dto.EmployeeId);
                return "Sales percentage updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sales percentage for employee with ID: {EmployeeId}. Error: {Message}", dto.EmployeeId, ex.Message);
                return $"Error sales percentage for employee with ID {dto.EmployeeId}: {ex.Message}";
            }
        }

        #endregion


    }
}

