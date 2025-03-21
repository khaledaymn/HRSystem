using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HRSystem.DTO;

namespace HRSystem.Services.UsersServices
{
    public interface IUsersServices
    {
        Task<List<AuthenticationDTO>> GetAllAsync();
        Task<AuthenticationDTO> GetByID(string id);
        Task<bool> Edit(string id, UpdateUserDTO model);
        Task<bool> Delete(string id);
        Task<AuthenticationDTO> Create(CreateUserDTO model);
        Task<(bool Success, string Message, IEnumerable<IdentityError>? Errors)> AddUserToRoleAsync(string userId, string roleName);
        Task<(bool Success, string Message, IEnumerable<IdentityError>? Errors)> DeleteUserFromRoleAsync(string userId, string roleName);
    }
}
