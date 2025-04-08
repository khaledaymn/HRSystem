using HRSystem.DTO.RoleDTOs;
using HRSystem.Extend;
using HRSystem.Helper;
using HRSystem.Services.RolesServices;
using HRSystem.UnitOfWork;
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
    //[Authorize(Roles = Roles.Admin)] 
    [ApiController]
    [Route("[controller]")]
    public class RoleController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RoleController> _logger;
        public RoleController(ILogger<RoleController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

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
        /// <response code="401">
        /// Unauthorized. Returned when the caller is not authenticated.
        /// </response>
        /// <response code="403">
        /// Forbidden. Returned when the caller is not an admin.
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
            {
                _logger.LogWarning("Invalid model state for creating role: {Errors}",
                    string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(new
                {
                    Success = false,
                    Message = "Invalid data provided.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }
            try
            {
                var result = await _unitOfWork.RolesServices.CreateAsync(model.RoleName);

                if (result != null)
                {
                    _logger.LogInformation("Role created successfully with ID: {RoleId}", result.Id);
                    return Created(
                        "",
                        new
                        {
                            Success = true,
                            Message = "Role created successfully.",
                            Data = result
                        });
                }

                _logger.LogWarning("Failed to create role with name: {RoleName}", model.RoleName);
                return BadRequest(new { Success = false, Message = "Error creating role. Role may already exist or invalid data provided." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create role with name: {RoleName}. Error: {Message}", model?.RoleName, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while creating the role.",
                    Error = ex.Message
                });
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
        /// <response code="401">
        /// Unauthorized. Returned when the caller is not authenticated.
        /// </response>
        /// <response code="403">
        /// Forbidden. Returned when the caller is not an admin.
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
                var allRoles = await _unitOfWork.RolesServices.GetAllAsync();

                if (allRoles == null || !allRoles.Any())
                {
                    _logger.LogInformation("No roles available.");
                    return Ok(new
                    {
                        Success = true,
                        Message = "No roles found in the system.",
                        Data = Enumerable.Empty<RoleDTO>()
                    });
                }

                _logger.LogInformation("Successfully retrieved {RoleCount} roles.", allRoles.Count());
                return Ok(new
                {
                    Success = true,
                    Message = "Roles retrieved successfully.",
                    Data = allRoles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve roles. Error: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while retrieving roles.",
                    Error = ex.Message
                });
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
        /// <response code="401">
        /// Unauthorized. Returned when the caller is not authenticated.
        /// </response>
        /// <response code="403">
        /// Forbidden. Returned when the caller is not an admin.
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
                var role = await _unitOfWork.RolesServices.GetByIdAsync(id);

                if (role == null)
                {
                    _logger.LogWarning("Role with ID {RoleId} not found.", id);
                    return NotFound(new { Success = false, Message = $"No role found with ID {id}." });
                }

                _logger.LogInformation("Successfully retrieved role with ID: {RoleId}", id);
                return Ok(new
                {
                    Success = true,
                    Message = "Role retrieved successfully.",
                    Data = role
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve role with ID: {RoleId}. Error: {Message}", id, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while retrieving the role.",
                    Error = ex.Message 
                });
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
        /// <response code="401">
        /// Unauthorized. Returned when the caller is not authenticated.
        /// </response>
        /// <response code="403">
        /// Forbidden. Returned when the caller is not an admin.
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
            {
                _logger.LogWarning("Invalid model state for updating role: {Errors}",
                    string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(new
                {
                    Success = false,
                    Message = "Invalid data provided.",
                    Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            try
            {
                var result = await _unitOfWork.RolesServices.EditAsync(new RoleDTO
                {
                    Id = model.RoleId,
                    Name = model.RoleName
                });

                if (result)
                {
                    _logger.LogInformation("Role with ID {RoleId} updated successfully.", model.RoleId);
                    return Ok(new
                    {
                        Success = true,
                        Message = "Role updated successfully.",
                        Data = new RoleDTO { Id = model.RoleId, Name = model.RoleName }
                    });
                }

                _logger.LogWarning("Failed to update role with ID: {RoleId}", model.RoleId);
                return NotFound(new { Success = false, Message = $"No role found with ID {model.RoleId} or error updating role." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update role with ID: {RoleId}. Error: {Message}", model?.RoleId, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while updating the role.",
                    Error = ex.Message
                });
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
        /// <response code="401">
        /// Unauthorized. Returned when the caller is not authenticated.
        /// </response>
        /// <response code="403">
        /// Forbidden. Returned when the caller is not an admin.
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
                var result = await _unitOfWork.RolesServices.DeleteAsync(id);

                if (result)
                {
                    _logger.LogInformation("Role with ID {RoleId} deleted successfully.", id);
                    return Ok(new { Success = true, Message = "Role deleted successfully." });
                }

                _logger.LogWarning("Failed to delete role with ID: {RoleId}", id);
                return NotFound(new { Success = false, Message = $"No role found with ID {id} or error deleting role." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete role with ID: {RoleId}. Error: {Message}", id, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while deleting the role.",
                    Error = ex.Message
                });
            }
        }

        #endregion


        #region Assign User To Role

        /// <summary>
        /// Assigns a role to a specific user in the system.
        /// This endpoint allows an admin to add a role to a user based on their unique identifier and the role name.
        /// </summary>
        /// <remarks>
        /// This endpoint requires admin privileges and expects a user ID and role name as route parameters.
        /// It returns a success message if the role is assigned successfully, or an error if the operation fails or an issue occurs.
        /// </remarks>
        /// <param name="userId">
        /// The unique identifier of the user to assign the role to (e.g., a GUID or string-based ID).
        /// </param>
        /// <param name="roleName">
        /// The name of the role to assign to the user (e.g., "Admin", "User").
        /// <para><strong>Example Request:</strong></para>
        /// <code>
        /// POST /Users/AddUserToRole/123e4567-e89b-12d3-a456-426614174000/User
        /// </code>
        /// </param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> containing the result of the role assignment process.
        /// On success, it returns HTTP 200 (OK) with a success message.
        /// On failure, it returns appropriate error codes with descriptive messages.
        /// </returns>
        /// <response code="200">
        /// Successfully added the user to the role. Returns a confirmation message.
        /// Example Response:
        /// <code>
        /// {
        ///     "success": true,
        ///     "message": "User successfully added to the 'User' role."
        /// }
        /// </code>
        /// </response>
        /// <response code="400">
        /// Bad Request. Returned when the input data is invalid or the operation fails.
        /// Example Response (Validation Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "Invalid role assignment request.",
        ///     "errors": [
        ///         "Role 'User' does not exist.",
        ///         "User ID is invalid."
        ///     ]
        /// }
        /// </code>
        /// Example Response (Business Logic Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "User is already assigned to the 'User' role.",
        ///     "errors": []
        /// }
        /// </code>
        /// </response>
        /// <response code="401">
        /// Unauthorized. Returned when the caller is not authenticated.
        /// Example Response:
        /// <code>
        /// {
        ///     "message": "Authentication required. Please provide a valid token."
        /// }
        /// </code>
        /// </response>
        /// <response code="403">
        /// Forbidden. Returned when the caller lacks admin privileges.
        /// Example Response:
        /// <code>
        /// {
        ///     "message": "Access denied. Admin role required."
        /// }
        /// </code>
        /// </response>
        /// <response code="404">
        /// Not Found. Returned when the user or role does not exist.
        /// Example Response:
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "User or role not found.",
        ///     "errors": []
        /// }
        /// </code>
        /// </response>
        /// <response code="500">
        /// Server Error. Returned when an unexpected error occurs during role assignment.
        /// Example Response (Generic Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "An error occurred while adding the user to the role",
        ///     "error": "Database connection failed"
        /// }
        /// </code>
        /// Example Response (Detailed Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "An error occurred while adding the user to the role",
        ///     "error": "Role assignment failed due to permission conflict",
        ///     "timestamp": "2025-03-24T17:00:00Z"
        /// }
        /// </code>
        /// </response>
        /// <exception cref="Exception">Thrown when an unexpected error occurs (e.g., database failure). Caught and returned as a 500 response with error details.</exception>
        [HttpPost]
        [Route("~/Users/AddUserToRole/{userId}/{roleName}")]
        public async Task<IActionResult> AddUserToRole(string userId, string roleName)
        {
            try
            {
                var (success, message, errors) = await _unitOfWork.RolesServices.AddUserToRoleAsync(userId, roleName);

                if (!success)
                {
                    _logger.LogWarning("Failed to add user with ID: {UserId} to role: {RoleName}. Message: {Message}", userId, roleName, message);
                    return BadRequest(new { Success = false, Message = message, Errors = errors?.Select(e => e.Description) });
                }

                _logger.LogInformation("User with ID: {UserId} added to role: {RoleName} successfully.", userId, roleName);
                return Ok(new { Success = true, Message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add user with ID: {UserId} to role: {RoleName}. Error: {Message}", userId, roleName, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while adding the user to the role.",
                    Error = ex.Message
                });
            }
        }

        #endregion


        #region Delete User From Role

        /// <summary>
        /// Removes a role from a specific user in the system.
        /// This endpoint allows an admin to delete a role assignment for a user based on their unique identifier and the role name.
        /// </summary>
        /// <remarks>
        /// This endpoint requires admin privileges and expects a user ID and role name as route parameters.
        /// It returns a success message if the role is removed successfully, or an error if the operation fails or an issue occurs.
        /// </remarks>
        /// <param name="userId">
        /// The unique identifier of the user from whom the role will be removed (e.g., a GUID or string-based ID).
        /// </param>
        /// <param name="roleName">
        /// The name of the role to remove from the user (e.g., "Admin", "User").
        /// <para><strong>Example Request:</strong></para>
        /// <code>
        /// DELETE /users/123e4567-e89b-12d3-a456-426614174000/roles/User
        /// </code>
        /// </param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> containing the result of the role removal process.
        /// On success, it returns HTTP 200 (OK) with a success message.
        /// On failure, it returns appropriate error codes with descriptive messages.
        /// </returns>
        /// <response code="200">
        /// Successfully removed the user from the role. Returns a confirmation message.
        /// Example Response:
        /// <code>
        /// {
        ///     "success": true,
        ///     "message": "User successfully removed from the 'User' role."
        /// }
        /// </code>
        /// </response>
        /// <response code="400">
        /// Bad Request. Returned when the input data is invalid or the operation fails.
        /// Example Response (Validation Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "Invalid role removal request.",
        ///     "errors": [
        ///         "Role 'User' does not exist.",
        ///         "User ID is invalid."
        ///     ]
        /// }
        /// </code>
        /// Example Response (Business Logic Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "User is not assigned to the 'User' role.",
        ///     "errors": []
        /// }
        /// </code>
        /// </response>
        /// <response code="401">
        /// Unauthorized. Returned when the caller is not authenticated.
        /// Example Response:
        /// <code>
        /// {
        ///     "message": "Authentication required. Please provide a valid token."
        /// }
        /// </code>
        /// </response>
        /// <response code="403">
        /// Forbidden. Returned when the caller lacks admin privileges.
        /// Example Response:
        /// <code>
        /// {
        ///     "message": "Access denied. Admin role required."
        /// }
        /// </code>
        /// </response>
        /// <response code="404">
        /// Not Found. Returned when the user or role does not exist.
        /// Example Response:
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "User or role not found.",
        ///     "errors": []
        /// }
        /// </code>
        /// </response>
        /// <response code="500">
        /// Server Error. Returned when an unexpected error occurs during role removal.
        /// Example Response (Generic Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "An error occurred while removing the user from the role",
        ///     "error": "Database connection failed"
        /// }
        /// </code>
        /// Example Response (Detailed Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "An error occurred while removing the user from the role",
        ///     "error": "Role removal failed due to permission conflict",
        ///     "timestamp": "2025-03-24T17:15:00Z"
        /// }
        /// </code>
        /// </response>
        /// <exception cref="Exception">Thrown when an unexpected error occurs (e.g., database failure). Caught and returned as a 500 response with error details.</exception>
        [HttpDelete]
        [Route("users/{userId}/roles/{roleName}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> DeleteUserFromRole(string userId, string roleName)
        {
            try
            {
                var (success, message, errors) = await _unitOfWork.RolesServices.DeleteUserFromRoleAsync(userId, roleName);

                if (!success)
                {
                    _logger.LogWarning("Failed to remove user with ID: {UserId} from role: {RoleName}. Message: {Message}", userId, roleName, message);
                    return BadRequest(new { Success = false, Message = message, Errors = errors?.Select(e => e.Description) });
                }

                _logger.LogInformation("User with ID: {UserId} removed from role: {RoleName} successfully.", userId, roleName);
                return Ok(new { Success = true, Message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove user with ID: {UserId} from role: {RoleName}. Error: {Message}", userId, roleName, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while removing the user from the role.",
                    Error = ex.Message
                });
            }
        }
        #endregion

    }
}
