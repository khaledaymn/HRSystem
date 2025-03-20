using HRSystem.Models;
using HRSystem.DTO;
using HRSystem.Extend;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HRSystem.Services.RolesServices
{
    public class RolesServices : IRolesServices
    {
        private readonly RoleManager<IdentityRole> roleManager;

        public RolesServices(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            this.roleManager = roleManager;
        }

        #region Create Role

        public async Task<RoleDTO?> CreateAsync(string name)
        {
            var role = new IdentityRole 
            { 
                Name = name 
            };
            var result = await roleManager.CreateAsync(role);

            if (result.Succeeded)
                return new RoleDTO
                {
                    Id = role.Id,
                    Name = role.Name
                };

            return null;
        }

        #endregion


        #region Get All Roles

        public async Task<IEnumerable<RoleDTO>> GetAllAsync()
        {
            var roles = await Task.Run(() => roleManager.Roles
                .Select(r => new RoleDTO
                {
                    Id = r.Id,
                    Name = r.Name
                })
                .ToList());

            return roles;
        }


        #endregion


        #region Get By Id

        public async Task<RoleDTO?> GetByIdAsync(string id)
        {
            var role = await roleManager.FindByIdAsync(id);

            if (role == null)
                return null;

            return new RoleDTO
            {
                Id = role.Id,
                Name = role.Name
            };
        }


        #endregion


        #region Edit

        public async Task<bool> EditAsync(RoleDTO model)
        {
            try
            {
                var role = await roleManager.FindByIdAsync(model.Id);
                if (role == null)
                    return false;

                role.Name = model.Name;
                var result = await roleManager.UpdateAsync(role);

                return result.Succeeded;
            }
            catch (Exception ex)
            {
                return false;
            }
        }


        #endregion


        #region Delete
        public async Task<bool> DeleteAsync(string id)
        {
            try
            {
                var role = await roleManager.FindByIdAsync(id);
                if (role == null)
                    return false; 
                var result = await roleManager.DeleteAsync(role);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        #endregion


    }
}
