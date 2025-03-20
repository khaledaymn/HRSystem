using HRSystem.DTO;
using HRSystem.Extend;
using HRSystem.Helper;
using HRSystem.Services.RolesServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
namespace HRSystem.Controllers
{
    [Authorize(Roles = StaticClass.Admin)] 
    [ApiController]
    [Route("[controller]")]
    public class RoleController : ControllerBase
    {
        private readonly IRolesServices _rolesService;
        public RoleController(IRolesServices rolesService) => this._rolesService = rolesService;

        #region Create Role

        /// <summary>
        /// Creates a new role in the system.
        /// This endpoint allows an admin or authorized user to create a new role by providing the role name.
        /// </summary>
        /// <param name="model">
        /// The data required to create a new role (as defined in RoleCreateDTO).
        /// Example Request (RoleCreateDTO):
        /// <code>
        /// {
        ///     "roleName": "Editor"
        /// }
        /// </code>
        /// </param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> indicating the result of the role creation operation.
        /// </returns>
        /// <response code="200">
        /// Role created successfully. Returns a success message and the created role details.
        /// Example Response (Success):
        /// <code>
        /// {
        ///     "success": true,
        ///     "message": "Role created successfully.",
        ///     "role": {
        ///         "id": "role-123",
        ///         "name": "Editor"
        ///     }
        /// }
        /// </code>
        /// </response>
        /// <response code="400">
        /// Bad request. Returned when the provided model is invalid or role creation fails (e.g., role name already exists).
        /// Example Response (Invalid Model):
        /// <code>
        /// {
        ///     "errors": {
        ///         "roleName": ["Role name is required."]
        ///     }
        /// }
        /// </code>
        /// Example Response (Creation Failed):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "Error creating role."
        /// }
        /// </code>
        /// </response>
        /// <response code="500">
        /// Server error. Returned when an unexpected error occurs on the server.
        /// Example Response (Server Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "An error occurred while creating the role.",
        ///     "error": "Database connection failed"
        /// }
        /// </code>
        /// </response>
        [HttpPost]
        [Route("~/Roles/Create")]
        public async Task<IActionResult> Create([FromBody] RoleCreateDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _rolesService.CreateAsync(model.RoleName);

                if (result != null)
                    return Ok(new { Success = true, Message = "Role created successfully.", Role = result });

                return BadRequest(new { Success = false, Message = "Error creating role." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while creating the role.", Error = ex.Message });
            }
        }

        #endregion


        #region Get All 

        /// <summary>
        /// Retrieves a list of all roles in the system.
        /// This endpoint allows an admin or authorized user to fetch all role details.
        /// </summary>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> containing the list of roles or an error message.
        /// </returns>
        /// <response code="200">
        /// Roles retrieved successfully. Returns a success message and the list of roles.
        /// Example Response (Success):
        /// <code>
        /// {
        ///     "success": true,
        ///     "roles": [
        ///         {
        ///             "id": "role-123",
        ///             "name": "Admin"
        ///         },
        ///         {
        ///             "id": "role-456",
        ///             "name": "Editor"
        ///         },
        ///         {
        ///             "id": "role-789",
        ///             "name": "User"
        ///         }
        ///     ]
        /// }
        /// </code>
        /// </response>
        /// <response code="404">
        /// Not found. Returned when no roles are found in the system.
        /// Example Response (Not Found):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "No roles found."
        /// }
        /// </code>
        /// </response>
        /// <response code="500">
        /// Server error. Returned when an unexpected error occurs on the server.
        /// Example Response (Server Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "An error occurred while retrieving roles.",
        ///     "error": "Database connection failed"
        /// }
        /// </code>
        /// </response>
        [HttpGet]
        [Route("~/Roles/GetAll")]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var allRoles = await _rolesService.GetAllAsync();

                if (allRoles == null || !allRoles.Any())
                {
                    return NotFound(new { Success = false, Message = "No roles found." });
                }

                return Ok(new { Success = true, Roles = allRoles });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while retrieving roles.", Error = ex.Message });
            }
        }

        #endregion


        #region Get By Id 

        /// <summary>
        /// Retrieves a role by its ID.
        /// This endpoint allows an admin or authorized user to fetch the details of a specific role using its ID.
        /// </summary>
        /// <param name="id">The ID of the role to retrieve.</param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> containing the role details or an error message.
        /// </returns>
        /// <response code="200">
        /// Role retrieved successfully. Returns a success message and the role's details.
        /// Example Response (Success):
        /// <code>
        /// {
        ///     "success": true,
        ///     "role": {
        ///         "id": "role-123",
        ///         "name": "Admin"
        ///     }
        /// }
        /// </code>
        /// </response>
        /// <response code="404">
        /// Not found. Returned when the role with the specified ID does not exist.
        /// Example Response (Not Found):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "Role not found."
        /// }
        /// </code>
        /// </response>
        /// <response code="500">
        /// Server error. Returned when an unexpected error occurs on the server.
        /// Example Response (Server Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "An error occurred while retrieving the role.",
        ///     "error": "Database connection failed"
        /// }
        /// </code>
        /// </response>
        [HttpGet]
        [Route("~/Roles/GetById/{id}")]
        public async Task<IActionResult> GetRoleById(string id)
        {
            try
            {
                var role = await _rolesService.GetByIdAsync(id);

                if (role == null)
                {
                    return NotFound(new { Success = false, Message = "Role not found." });
                }

                return Ok(new { Success = true, Role = role });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while retrieving the role.", Error = ex.Message });
            }
        }

        #endregion


        #region Update

        /// <summary>
        /// Updates an existing role in the system.
        /// This endpoint allows an admin or authorized user to update a role's details by providing the role ID and new name.
        /// </summary>
        /// <param name="model">
        /// The data required to update a role (as defined in RoleUpdateDTO).
        /// Example Request (RoleUpdateDTO):
        /// <code>
        /// {
        ///     "roleId": "role-123",
        ///     "roleName": "SeniorEditor"
        /// }
        /// </code>
        /// </param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> indicating the result of the role update operation.
        /// </returns>
        /// <response code="200">
        /// Role updated successfully. Returns a success message.
        /// Example Response (Success):
        /// <code>
        /// {
        ///     "success": true,
        ///     "message": "Role updated successfully."
        /// }
        /// </code>
        /// </response>
        /// <response code="400">
        /// Bad request. Returned when the provided model is invalid or role update fails (e.g., role not found or name already exists).
        /// Example Response (Invalid Model):
        /// <code>
        /// {
        ///     "errors": {
        ///         "roleId": ["The role ID is required."],
        ///         "roleName": ["The role name is required."]
        ///     }
        /// }
        /// </code>
        /// Example Response (Update Failed):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "Error updating role."
        /// }
        /// </code>
        /// </response>
        /// <response code="500">
        /// Server error. Returned when an unexpected error occurs on the server.
        /// Example Response (Server Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "An error occurred while updating the role.",
        ///     "error": "Database connection failed"
        /// }
        /// </code>
        /// </response>
        [HttpPut]
        [Route("~/Roles/Update")]
        public async Task<IActionResult> Update([FromBody] RoleUpdateDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _rolesService.EditAsync(new RoleDTO
                {
                    Id = model.RoleId,
                    Name = model.RoleName
                });

                if (result)
                    return Ok(new { Success = true, Message = "Role updated successfully." });

                return BadRequest(new { Success = false, Message = "Error updating role." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while updating the role.", Error = ex.Message });
            }
        }

        #endregion


        #region Detete Role

        /// <summary>
        /// Deletes a role by its ID.
        /// This endpoint allows an admin or authorized user to delete a role from the system using its ID.
        /// </summary>
        /// <param name="id">The ID of the role to delete.</param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> indicating the result of the delete operation.
        /// </returns>
        /// <response code="200">
        /// Role deleted successfully. Returns a success message.
        /// Example Response (Success):
        /// <code>
        /// {
        ///     "success": true,
        ///     "message": "Role deleted successfully."
        /// }
        /// </code>
        /// </response>
        /// <response code="400">
        /// Bad request. Returned when the role cannot be deleted (e.g., role is in use or has associated users).
        /// Example Response (Delete Failed):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "Error deleting role. The role may be in use or has associated users."
        /// }
        /// </code>
        /// </response>
        /// <response code="404">
        /// Not found. Returned when the role with the specified ID does not exist.
        /// Example Response (Not Found):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "Role not found."
        /// }
        /// </code>
        /// </response>
        /// <response code="500">
        /// Server error. Returned when an unexpected error occurs on the server.
        /// Example Response (Server Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "An error occurred while deleting the role.",
        ///     "error": "Database connection failed"
        /// }
        /// </code>
        /// </response>
        [HttpDelete]
        [Route("~/Roles/Delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var role = await _rolesService.GetByIdAsync(id);
                if (role == null)
                    return NotFound(new { Success = false, Message = "Role not found." });

                var result = await _rolesService.DeleteAsync(id);
                if (result)
                    return Ok(new { Success = true, Message = "Role deleted successfully." });

                return BadRequest(new { Success = false, Message = "Error deleting role. The role may be in use or has associated users." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while deleting the role.", Error = ex.Message });
            }
        }

        #endregion
    }
}
