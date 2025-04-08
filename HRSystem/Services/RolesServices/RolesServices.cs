using HRSystem.Models;
using HRSystem.Extend;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HRSystem.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HRSystem.DTO.RoleDTOs;

namespace HRSystem.Services.RolesServices
{
    public class RolesServices : IRolesServices
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RolesServices> _logger;

        public RolesServices(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork, ILogger<RolesServices> logger)
        {
            this._roleManager = roleManager;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }


        #region Create Role

        public async Task<RoleDTO?> CreateAsync(string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    _logger.LogWarning("Role name is null or empty.");
                    throw new ArgumentException("Role name cannot be null or empty.", nameof(name));
                }

                var role = new IdentityRole
                {
                    Name = name
                };

                var result = await _roleManager.CreateAsync(role);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Successfully created role with ID: {RoleId} and name: {RoleName}", role.Id, role.Name);
                    return new RoleDTO
                    {
                        Id = role.Id,
                        Name = role.Name
                    };
                }

                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to create role with name: {RoleName}. Errors: {Errors}", name, errors);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create role with name: {RoleName}. Error: {Message}", name, ex.Message);
                throw;
            }
        }
        #endregion


        #region Get All Roles

        public async Task<IEnumerable<RoleDTO>> GetAllAsync()
        {
            try
            {
                var roles = await _roleManager.Roles
                    .Select(r => new RoleDTO
                    {
                        Id = r.Id,
                        Name = r.Name
                    })
                    .ToListAsync();

                if (roles == null || !roles.Any())
                {
                    _logger.LogInformation("No roles found in the system.");
                    return Enumerable.Empty<RoleDTO>();
                }

                _logger.LogInformation("Successfully retrieved {RoleCount} roles.", roles.Count);
                return roles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve roles. Error: {Message}", ex.Message);
                throw;
            }
        }


        #endregion


        #region Get By Id

        public async Task<RoleDTO?> GetByIdAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Invalid role ID provided: null or empty.");
                    throw new ArgumentException("Role ID cannot be null or empty.", nameof(id));
                }

                var role = await _roleManager.FindByIdAsync(id);

                if (role == null)
                {
                    _logger.LogWarning("Role with ID {RoleId} not found.", id);
                    return null;
                }

                var roleDto = new RoleDTO
                {
                    Id = role.Id,
                    Name = role.Name
                };

                _logger.LogInformation("Successfully retrieved role with ID: {RoleId}", id);
                return roleDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve role with ID: {RoleId}. Error: {Message}", id, ex.Message);
                throw;
            }
        }


        #endregion


        #region Edit

        public async Task<bool> EditAsync(RoleDTO model)
        {
            try
            {
                if (model == null)
                {
                    _logger.LogWarning("Role data is null.");
                    throw new ArgumentNullException(nameof(model), "Role data cannot be null.");
                }

                if (string.IsNullOrEmpty(model.Id))
                {
                    _logger.LogWarning("Invalid role ID provided: null or empty.");
                    throw new ArgumentException("Role ID cannot be null or empty.", nameof(model.Id));
                }

                if (string.IsNullOrEmpty(model.Name))
                {
                    _logger.LogWarning("Invalid role name provided: null or empty for role ID: {RoleId}", model.Id);
                    throw new ArgumentException("Role name cannot be null or empty.", nameof(model.Name));
                }

                var role = await _roleManager.FindByIdAsync(model.Id);
                if (role == null)
                {
                    _logger.LogWarning("Role with ID {RoleId} not found.", model.Id);
                    return false;
                }

                role.Name = model.Name;
                var result = await _roleManager.UpdateAsync(role);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Successfully updated role with ID: {RoleId} to name: {RoleName}", role.Id, role.Name);
                    return true;
                }

                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to update role with ID: {RoleId}. Errors: {Errors}", model.Id, errors);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update role with ID: {RoleId}. Error: {Message}", model?.Id, ex.Message);
                throw;
            }
        }

        #endregion


        #region Delete
        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    _logger.LogWarning("Invalid role ID provided: null or empty.");
                    throw new ArgumentException("Role ID cannot be null or empty.", nameof(id));
                }

                var role = await _roleManager.FindByIdAsync(id);
                if (role == null)
                {
                    _logger.LogWarning("Role with ID {RoleId} not found.", id);
                    return false;
                }

                var result = await _roleManager.DeleteAsync(role);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Successfully deleted role with ID: {RoleId}", id);
                    return true;
                }

                var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Failed to delete role with ID: {RoleId}. Errors: {Errors}", id, errors);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete role with ID: {RoleId}. Error: {Message}", id, ex.Message);
                throw;
            }
        }

        #endregion


        #region Assign User To Role

        public async Task<(bool Success, string Message, IEnumerable<IdentityError>? Errors)> AddUserToRoleAsync(string userId, string roleName)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Invalid user ID provided: null or empty.");
                    throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
                }

                if (string.IsNullOrEmpty(roleName))
                {
                    _logger.LogWarning("Invalid role name provided: null or empty.");
                    throw new ArgumentException("Role name cannot be null or empty.", nameof(roleName));
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found.", userId);
                    return (false, $"User with ID '{userId}' not found.", null);
                }

                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    _logger.LogWarning("Role '{RoleName}' does not exist.", roleName);
                    return (false, $"Role '{roleName}' does not exist.", null);
                }

                var result = await _userManager.AddToRoleAsync(user, roleName);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Successfully added user with ID: {UserId} to role: {RoleName}", userId, roleName);
                    return (true, $"User with ID '{userId}' added to role '{roleName}' successfully.", null);
                }

                var errors = result.Errors.Select(e => e.Description);
                _logger.LogWarning("Failed to add user with ID: {UserId} to role: {RoleName}. Errors: {Errors}", userId, roleName, string.Join("; ", errors));
                return (false, $"Failed to add user with ID '{userId}' to role '{roleName}'.", result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add user with ID: {UserId} to role: {RoleName}. Error: {Message}", userId, roleName, ex.Message);
                throw;
            }
        }

        #endregion


        #region Delete User from Role

        public async Task<(bool Success, string Message, IEnumerable<IdentityError>? Errors)> DeleteUserFromRoleAsync(string userId, string roleName)
        {
            _logger.LogInformation("Attempting to remove user with ID: {UserId} from role: {RoleName}", userId, roleName);

            try
            {
                // التحقق من أن المعرف والدور صالحان
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Invalid user ID provided: null or empty.");
                    throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
                }

                if (string.IsNullOrEmpty(roleName))
                {
                    _logger.LogWarning("Invalid role name provided: null or empty.");
                    throw new ArgumentException("Role name cannot be null or empty.", nameof(roleName));
                }

                // جلب المستخدم
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found.", userId);
                    return (false, $"User with ID '{userId}' not found.", null);
                }

                // التحقق من وجود الدور
                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    _logger.LogWarning("Role '{RoleName}' does not exist.", roleName);
                    return (false, $"Role '{roleName}' does not exist.", null);
                }

                // التحقق مما إذا كان المستخدم في الدور
                var isInRole = await _userManager.IsInRoleAsync(user, roleName);
                if (!isInRole)
                {
                    _logger.LogWarning("User with ID {UserId} is not in role '{RoleName}'.", userId, roleName);
                    return (false, $"User with ID '{userId}' is not in role '{roleName}'.", null);
                }

                // إزالة المستخدم من الدور
                var result = await _userManager.RemoveFromRoleAsync(user, roleName);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Successfully removed user with ID: {UserId} from role: {RoleName}", userId, roleName);
                    return (true, $"User with ID '{userId}' removed from role '{roleName}' successfully.", null);
                }

                // تسجيل الأخطاء إذا فشلت العملية
                var errors = result.Errors.Select(e => e.Description);
                _logger.LogWarning("Failed to remove user with ID: {UserId} from role: {RoleName}. Errors: {Errors}", userId, roleName, string.Join("; ", errors));
                return (false, $"Failed to remove user with ID '{userId}' from role '{roleName}'.", result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove user with ID: {UserId} from role: {RoleName}. Error: {Message}", userId, roleName, ex.Message);
                throw; // إعادة رمي الاستثناء بدلاً من إرجاع tuple مع رسالة خطأ
            }
        }

        #endregion
    }
}
