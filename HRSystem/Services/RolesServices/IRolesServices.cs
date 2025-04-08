using HRSystem.DTO.RoleDTOs;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRSystem.Services.RolesServices
{
    public interface IRolesServices
    {
        Task<RoleDTO> CreateAsync(string name);
        Task<IEnumerable<RoleDTO>> GetAllAsync();
        Task<RoleDTO> GetByIdAsync(string id);
        Task<bool> EditAsync(RoleDTO model);
        Task<bool> DeleteAsync(string id);
        Task<(bool Success, string Message, IEnumerable<IdentityError>? Errors)> AddUserToRoleAsync(string userId, string roleName);
        Task<(bool Success, string Message, IEnumerable<IdentityError>? Errors)> DeleteUserFromRoleAsync(string userId, string roleName);

    }

}
