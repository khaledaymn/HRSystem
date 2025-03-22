using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HRSystem.Extend;
using HRSystem.DTO;

namespace HRSystem.Services.UsersServices
{
    public class UsersServices : IUsersServices
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public UsersServices(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            this._userManager = userManager;
            _roleManager = roleManager;
        }

        #region Create User

        public async Task<AuthenticationDTO> Create(CreateUserDTO model)
        {
            try
            {
                var user = new ApplicationUser
                {
                    Name = model.Name,
                    Email = model.Email,
                    Address = model.Address,
                    DateOfBarth = model.DateOfBarth,
                    PhoneNumber = model.PhoneNumber,
                    UserName = model.UserName,
                    Nationalid = model.Nationalid,
                    Salary = model.Salary,
                    TimeOfAttend = DateTime.Parse(model.TimeOfAttend),
                    TimeOfLeave = DateTime.Parse(model.TimeOfLeave),
                    Gender = Enum.Parse<Gender>(model.Gender),
                    DateOfWork = model.DateOfWork
                };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return new()
                    {
                        Message = errors
                    };
                }

                if(model.Name == "Admin")
                    await _userManager.AddToRoleAsync(user, "Admin");
                else
                    await _userManager.AddToRoleAsync(user, "User");

                return new()
                {
                    Email = user.Email,
                    Name = user.Name,
                    Address = user.Address,
                    DateOfBarth = user.DateOfBarth.ToLongDateString(),
                    PhoneNumber = user.PhoneNumber,
                    UserName = user.UserName,
                    Nationalid = user.Nationalid,
                    Salary = user.Salary,
                    TimeOfAttend = user.TimeOfAttend.ToShortTimeString(),
                    TimeOfLeave = user.TimeOfLeave.ToShortTimeString(),
                    Gender = user.Gender.ToString(),
                    DateOfWork = user.DateOfWork.ToString(),
                    IsAuthenticated = true,
                    Message = "User created successfully!",
                    Id = user.Id,
                    Roles = await _userManager.GetRolesAsync(user)
                };
            }
            catch (Exception ex)
            {
                return new()
                {
                    Message = ex.Message
                };
            }
        }

        #endregion


        #region Delete User

        public async Task<bool> Delete(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return false;
                var result = await _userManager.DeleteAsync(user);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting user: ", ex);
            }
        }

        #endregion


        #region Edit User

        public async Task<bool> Edit(string id, UpdateUserDTO model)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                    return false;

                if (!string.IsNullOrEmpty(model.UserName)) user.UserName = model.UserName;
                if (!string.IsNullOrEmpty(model.Email)) user.Email = model.Email;
                if (!string.IsNullOrEmpty(model.Name)) user.Name = model.Name;
                if (!string.IsNullOrEmpty(model.Address)) user.Address = model.Address;
                if (!string.IsNullOrEmpty(model.Gender)) user.Gender = Enum.Parse<Gender>(model.Gender);
                if (!string.IsNullOrEmpty(model.NationalId)) user.Nationalid = model.NationalId;
                if (model.Salary.HasValue) user.Salary = model.Salary.Value;
                if (model.DateOfWork.HasValue) user.DateOfWork = model.DateOfWork.Value;
                if (model.DateOfBirth.HasValue) user.DateOfBarth = model.DateOfBirth.Value; 

                if (!string.IsNullOrEmpty(model.TimeOfAttend))
                {
                    if (DateTime.TryParse(model.TimeOfAttend, out DateTime timeOfAttend))
                    {
                        user.TimeOfAttend = timeOfAttend;
                    }
                    else
                    {
                        throw new Exception($"Invalid TimeOfAttend format: {model.TimeOfAttend}");
                    }
                }

                if (!string.IsNullOrEmpty(model.TimeOfLeave))
                {
                    if (DateTime.TryParse(model.TimeOfLeave, out DateTime timeOfLeave))
                    {
                        user.TimeOfLeave = timeOfLeave;
                    }
                    else
                    {
                        throw new Exception($"Invalid TimeOfLeave format: {model.TimeOfLeave}");
                    }
                }

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    throw new Exception($"Failed to update user: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
                }

                return updateResult.Succeeded;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating user with ID {id}: {ex.Message}", ex); // تحسين الـ Exception
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
                    UserName = user.UserName,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                    Nationalid = user.Nationalid,
                    Salary = user.Salary,
                    TimeOfAttend = user.TimeOfAttend.ToString("HH:mm"),
                    TimeOfLeave = user.TimeOfLeave.ToString("HH:mm"),
                    Gender = user.Gender.ToString() ?? "Not specified",
                    DateOfWork = user.DateOfWork.ToString("dd/MM/yyyy"),
                    DateOfBarth = user.DateOfBarth.ToString("dd/MM/yyyy"),
                    Roles = Task.Run(() => _userManager.GetRolesAsync(user)).Result
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
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return null;
                }

                var roles = await _userManager.GetRolesAsync(user);

                return new AuthenticationDTO
                {
                    Message = "User found",
                    IsAuthenticated = true,
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    UserName = user.UserName,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                    Nationalid = user.Nationalid,
                    Salary = user.Salary,
                    TimeOfAttend = user.TimeOfAttend.ToString("HH:mm"), 
                    TimeOfLeave = user.TimeOfLeave.ToString("HH:mm"),   
                    Gender = user.Gender.ToString() ?? "Not specified", 
                    DateOfWork = user.DateOfWork.ToString("dd/MM/yyyy"),
                    DateOfBarth = user.DateOfBarth.ToString("dd/MM/yyyy"),
                    Roles = roles.ToList()
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving user with ID {id}: {ex.Message}", ex);
            }
        }


        #endregion


        #region Assign User To Role

        public async Task<(bool Success, string Message, IEnumerable<IdentityError>? Errors)> AddUserToRoleAsync(string userId, string roleName)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return (false, $"User with ID '{userId}' not found.", null);

                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                    return (false, $"Role '{roleName}' does not exist.", null);

                var result = await _userManager.AddToRoleAsync(user, roleName);
                if (result.Succeeded)
                    return (true, $"User with ID '{userId}' added to role '{roleName}' successfully.", null);

                return (false, $"Failed to add user with ID '{userId}' to role '{roleName}'.", result.Errors);
            }
            catch (Exception ex)
            {
                return (false, $"An error occurred while adding user with ID '{userId}' to role '{roleName}': {ex.Message}", null);
            }
        }

        #endregion


        #region Delete User from Role

        public async Task<(bool Success, string Message, IEnumerable<IdentityError>? Errors)> DeleteUserFromRoleAsync(string userId, string roleName)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return (false, $"User with ID '{userId}' not found.", null);

                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                    return (false, $"Role '{roleName}' does not exist.", null);

                var isInRole = await _userManager.IsInRoleAsync(user, roleName);
                if (!isInRole)
                    return (false, $"User with ID '{userId}' is not in role '{roleName}'.", null);

                var result = await _userManager.RemoveFromRoleAsync(user, roleName);
                if (result.Succeeded)
                    return (true, $"User with ID '{userId}' removed from role '{roleName}' successfully.", null);

                return (false, $"Failed to remove user with ID '{userId}' from role '{roleName}'.", result.Errors);
            }
            catch (Exception ex)
            {
                return (false, $"An error occurred while removing user with ID '{userId}' from role '{roleName}': {ex.Message}", null);
            }
        }


        #endregion


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
