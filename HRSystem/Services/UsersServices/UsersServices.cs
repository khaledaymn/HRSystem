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
#endregion

namespace HRSystem.Services.UsersServices
{
    public class UsersServices : IUsersServices
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UsersServices> _logger;
        public UsersServices(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, RoleManager<IdentityRole> roleManager, ILogger<UsersServices> logger)
        {
            _unitOfWork = unitOfWork;
            this._userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
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

                var user = new ApplicationUser
                {
                    UserName = model.Email,
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
                        Nationalid = user.Nationalid,
                        BaseSalary = user.BaseSalary,
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
                if (!string.IsNullOrEmpty(model.Name)) user.Name = model.Name;
                if (!string.IsNullOrEmpty(model.Address)) user.Address = model.Address;
                if (!string.IsNullOrEmpty(model.Nationalid)) user.Nationalid = model.Nationalid;
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
        public async Task<List<AuthenticationDTO>> GetAllAsync()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();

                var userList = users.Select(user => new AuthenticationDTO
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                    Nationalid = user.Nationalid,
                    BaseSalary = user.BaseSalary,
                    Gender = user.Gender.ToString() ?? "Not specified",
                    HiringDate = user.HiringDate.ToString("yyyy-MM-dd"),
                    DateOfBarth = user.DateOfBarth.ToString("yyyy-MM-dd"),
                    Roles = Task.Run(() => _userManager.GetRolesAsync(user)).Result,
                    Shift = Task.Run(() => _unitOfWork.ShiftServices.GetByEmployeeId(user.Id)).Result,
                    Branch = Task.Run(() => _unitOfWork.BranchServices.GetBranchByIdAsync(user.BranchId ?? 0)).Result,
                }).ToList();

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
                    Nationalid = user.Nationalid ?? "Not specified",
                    BaseSalary = (double)user.BaseSalary,
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


        #region TODO: Implement the following methods as per your requirements

        //TODO: Task<List<AuthenticationDTO>> GetEmployeesByBranchId(int id);
        //TODo: Task<List<EmployeesWithSalaryDTO>> GetEmployeesSalaries();
        //TODO: Task<SalaryDetailsDTO> GetEmployeeSalaryDetails(int EmployeeId);
        //ToDo: Task<EmployeeVacationsDTO> GetEmployeeVacations(int EmployeeId);

        #region Get Employees By Branch ID

        #endregion


        #region Get Employee Vacations


        #endregion


        #region Get Employees NetSalaries


        #endregion


        #region Get Net Salary Details

        #endregion

        #endregion


        //#region Assign User To Role

        //public async Task<(bool Success, string Message, IEnumerable<IdentityError>? Errors)> AddUserToRoleAsync(string userId, string roleName)
        //{
        //    try
        //    {
        //        var user = await _userManager.FindByIdAsync(userId);
        //        if (user == null)
        //            return (false, $"User with ID '{userId}' not found.", null);

        //        var roleExists = await _roleManager.RoleExistsAsync(roleName);
        //        if (!roleExists)
        //            return (false, $"Role '{roleName}' does not exist.", null);

        //        var result = await _userManager.AddToRoleAsync(user, roleName);
        //        if (result.Succeeded)
        //            return (true, $"User with ID '{userId}' added to role '{roleName}' successfully.", null);

        //        return (false, $"Failed to add user with ID '{userId}' to role '{roleName}'.", result.Errors);
        //    }
        //    catch (Exception ex)
        //    {
        //        return (false, $"An error occurred while adding user with ID '{userId}' to role '{roleName}': {ex.Message}", null);
        //    }
        //}

        //#endregion


        //#region Delete User from Role

        //public async Task<(bool Success, string Message, IEnumerable<IdentityError>? Errors)> DeleteUserFromRoleAsync(string userId, string roleName)
        //{
        //    try
        //    {
        //        var user = await _userManager.FindByIdAsync(userId);
        //        if (user == null)
        //            return (false, $"User with ID '{userId}' not found.", null);

        //        var roleExists = await _roleManager.RoleExistsAsync(roleName);
        //        if (!roleExists)
        //            return (false, $"Role '{roleName}' does not exist.", null);

        //        var isInRole = await _userManager.IsInRoleAsync(user, roleName);
        //        if (!isInRole)
        //            return (false, $"User with ID '{userId}' is not in role '{roleName}'.", null);

        //        var result = await _userManager.RemoveFromRoleAsync(user, roleName);
        //        if (result.Succeeded)
        //            return (true, $"User with ID '{userId}' removed from role '{roleName}' successfully.", null);

        //        return (false, $"Failed to remove user with ID '{userId}' from role '{roleName}'.", result.Errors);
        //    }
        //    catch (Exception ex)
        //    {
        //        return (false, $"An error occurred while removing user with ID '{userId}' from role '{roleName}': {ex.Message}", null);
        //    }
        //}


        //#endregion


        #region Generate Unique User Name

        //public async Task<string> GenerateUniqueUserName(string Name)
        //{
        //    string userName = string.Join("", Name.Split(" ").Select(name => name.ToLower()));
        //    // Ensure the username is unique
        //    if (await _userManager.FindByNameAsync(userName) is not null)
        //    {
        //        Random random = new Random();
        //        // Append a random number between 00 and 99
        //        userName += random.Next(00, 99).ToString("D2");
        //    }
        //    return userName;
        //}

        #endregion
    }
}
