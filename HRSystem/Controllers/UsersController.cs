using HRSystem.DTO;
using HRSystem.Helper;
using HRSystem.Services.UsersServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HRSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUsersServices _usersService;

        public UsersController(IUsersServices usersService) => _usersService = usersService;

        #region Create User

        /// <summary>
        /// Creates a new user in the system and returns authentication details.
        /// This endpoint allows an admin or authorized user to create a new user by providing the necessary details.
        /// </summary>
        /// <param name="model">
        /// The data required to create a new user (as defined in CreateUserDTO).
        /// Example Request (CreateUserDTO):
        /// <code>
        /// {
        ///     "name": "John Doe",
        ///     "email": "john.doe@example.com",
        ///     "userName": "johndoe",
        ///     "password": "P@ssw0rd123",
        ///     "phoneNumber": "123-456-7890",
        ///     "address": "123 Main St",
        ///     "nationalId": "987654321",
        ///     "salary": 50000,
        ///     "timeOfAttend": "09:00:00",
        ///     "timeOfLeave": "17:00:00",
        ///     "gender": "male",
        ///     "dateOfWork": "2023-01-15",
        ///     "dateOfBirth": "1990-05-20",
        /// }
        /// </code>
        /// </param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> indicating the result of the user creation operation.
        /// </returns>
        /// <response code="200">
        /// User created successfully. Returns a success message and the created user's details.
        /// Example Response (Success):
        /// <code>
        /// {
        ///     "message": "User created successfully",
        ///     "user": {
        ///         "message": "User created successfully",
        ///         "id": "12345",
        ///         "name": "John Doe",
        ///         "email": "john.doe@example.com",
        ///         "userName": "johndoe",
        ///         "phoneNumber": "123-456-7890",
        ///         "address": "123 Main St",
        ///         "nationalId": "987654321",
        ///         "salary": 50000,
        ///         "timeOfAttend": "09:00",
        ///         "timeOfLeave": "17:00",
        ///         "gender": "Male",
        ///         "dateOfWork": "2023-01-15",
        ///         "dateOfBirth": "1990-05-20",
        ///         "roles": ["User"]
        ///     }
        /// }
        /// </code>
        /// </response>
        /// <response code="400">
        /// Bad request. Returned when the provided model is invalid or user creation fails (e.g., duplicate email or username).
        /// Example Response (Invalid Model):
        /// <code>
        /// {
        ///     "errors": {
        ///         "email": ["The email field is required."],
        ///         "userName": ["The username field is required."],
        ///         "password": ["The password field is required."]
        ///     }
        /// }
        /// </code>
        /// Example Response (Creation Failed):
        /// <code>
        /// {
        ///     "message": "Email or username already exists."
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
        ///     "message": "An error occurred while creating the user",
        ///     "error": "Database connection failed"
        /// }
        /// </code>
        /// </response>
        [HttpPost]
        [Route("~/Users/Create")]
        [Authorize(Roles = StaticClass.Admin)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdUser = await _usersService.Create(model);
                if (!createdUser.IsAuthenticated)
                    return BadRequest(new { Message = createdUser.Message });

                return Ok(new { Message = createdUser.Message, User = createdUser });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the user", error = ex.Message });
            }
        }

        #endregion


        #region Get All Users

        /// <summary>
        /// Retrieves a list of all users in the system.
        /// This endpoint allows an admin or authorized user to fetch all user details.
        /// </summary>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> containing the list of users or an error message.
        /// </returns>
        /// <response code="200">
        /// Users retrieved successfully. Returns a success message and the list of users.
        /// Example Response (Success):
        /// <code>
        /// {
        ///     "success": true,
        ///     "data": [
        ///         {
        ///             "id": "12345",
        ///             "name": "John Doe",
        ///             "email": "john.doe@example.com",
        ///             "userName": "johndoe",
        ///             "phoneNumber": "123-456-7890",
        ///             "address": "123 Main St",
        ///             "nationalId": "987654321",
        ///             "salary": 50000,
        ///             "timeOfAttend": "09:00",
        ///             "timeOfLeave": "17:00",
        ///             "gender": "Male",
        ///             "dateOfWork": "2023-01-15",
        ///             "dateOfBirth": "1990-05-20",
        ///             "roles": ["User"]
        ///         },
        ///         {
        ///             "id": "67890",
        ///             "name": "Jane Smith",
        ///             "email": "jane.smith@example.com",
        ///             "userName": "janesmith",
        ///             "phoneNumber": "987-654-3210",
        ///             "address": "456 Oak St",
        ///             "nationalId": "123456789",
        ///             "salary": 60000,
        ///             "timeOfAttend": "08:30",
        ///             "timeOfLeave": "16:30",
        ///             "gender": "Female",
        ///             "dateOfWork": "2022-06-10",
        ///             "dateOfBirth": "1985-03-15",
        ///             "roles": ["Admin"]
        ///         }
        ///     ]
        /// }
        /// </code>
        /// </response>
        /// <response code="401">
        /// Unauthorized. Returned when the caller is not authenticated.
        /// </response>
        /// <response code="403">
        /// Forbidden. Returned when the caller is not an admin.
        /// </response>
        /// <response code="404">
        /// Not found. Returned when no users are found in the system.
        /// Example Response (Not Found):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "No users found."
        /// }
        /// </code>
        /// </response>
        /// <response code="500">
        /// Server error. Returned when an unexpected error occurs on the server.
        /// Example Response (Server Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "An error occurred while retrieving users.",
        ///     "error": "Database connection failed"
        /// }
        /// </code>
        /// </response>
        [HttpGet]
        [Route("~/Users/GetAll")]
        [Authorize(Roles = StaticClass.Admin)]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var allUsers = await _usersService.GetAllAsync();

                if (allUsers == null)
                {
                    return NotFound(new { Success = false, Message = "No users found." });
                }

                return Ok(new { Success = true, Data = allUsers });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while retrieving users.", Error = ex.Message });
            }
        }

        #endregion


        #region Get User By ID

        /// <summary>
        /// Retrieves a user by their ID.
        /// This endpoint allows an admin or authorized user to fetch the details of a specific user using their ID.
        /// </summary>
        /// <param name="id">The ID of the user to retrieve.</param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> containing the user details or an error message.
        /// </returns>
        /// <response code="200">
        /// User retrieved successfully. Returns a success message and the user's details.
        /// Example Response (Success):
        /// <code>
        /// {
        ///     "success": true,
        ///     "data": {
        ///         "id": "12345",
        ///         "name": "John Doe",
        ///         "email": "john.doe@example.com",
        ///         "userName": "johndoe",
        ///         "phoneNumber": "123-456-7890",
        ///         "address": "123 Main St",
        ///         "nationalId": "987654321",
        ///         "salary": 50000,
        ///         "timeOfAttend": "09:00",
        ///         "timeOfLeave": "17:00",
        ///         "gender": "Male",
        ///         "dateOfWork": "2023-01-15",
        ///         "dateOfBirth": "1990-05-20",
        ///         "roles": ["User"]
        ///     }
        /// }
        /// </code>
        /// </response>
        /// <response code="404">
        /// Not found. Returned when the user with the specified ID does not exist.
        /// Example Response (Not Found):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "User not found."
        /// }
        /// </code>
        /// </response>
        /// <response code="401">
        /// Unauthorized. Returned when the caller is not authenticated.
        /// </response>
        /// <response code="500">
        /// Server error. Returned when an unexpected error occurs on the server.
        /// Example Response (Server Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "An error occurred while retrieving the user.",
        ///     "error": "Database connection failed"
        /// }
        /// </code>
        /// </response>
        [HttpGet]
        [Route("~/Users/GetById/{id}")]
        [Authorize(Roles = StaticClass.Admin+","+StaticClass.User)]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var user = await _usersService.GetByID(id);

                if (user == null)
                {
                    return NotFound(new { Success = false, Message = "User not found." });
                }

                return Ok(new { Success = true, Data = user });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while retrieving the user.", Error = ex.Message });
            }
        }

        #endregion


        #region Edit User

        /// <summary>
        /// Updates an existing user's details by their ID.
        /// This endpoint allows an admin or authorized user to update a user's information using their ID and updated details.
        /// </summary>
        /// <param name="id">The ID of the user to update.</param>
        /// <param name="model">
        /// The updated user details (as defined in UpdateUserDTO).
        /// Example Request (UpdateUserDTO):
        /// <code>
        /// {
        ///     "name": "John Smith",
        ///     "email": "john.smith@example.com",
        ///     "userName": "johnsmith",
        ///     "phoneNumber": "123-456-7890",
        ///     "address": "456 Main St",
        ///     "nationalId": "987654321",
        ///     "salary": 55000,
        ///     "timeOfAttend": "08:30",
        ///     "timeOfLeave": "16:30",
        ///     "gender": "Male",
        ///     "dateOfWork": "2023-01-15",
        ///     "dateOfBirth": "1990-05-20",
        ///     "roles": ["User"]
        /// }
        /// </code>
        /// </param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> indicating the result of the update operation.
        /// </returns>
        /// <response code="200">
        /// User updated successfully. Returns a success message.
        /// Example Response (Success):
        /// <code>
        /// {
        ///     "success": true,
        ///     "message": "User updated successfully."
        /// }
        /// </code>
        /// </response>
        /// <response code="400">
        /// Bad request. Returned when the provided model is invalid.
        /// Example Response (Invalid Model):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "Invalid data.",
        ///     "errors": [
        ///         "The email field is required.",
        ///         "The userName field is required."
        ///     ]
        /// }
        /// </code>
        /// </response>
        /// <response code="401">
        /// Unauthorized. Returned when the caller is not authenticated.
        /// </response>
        /// <response code="404">
        /// Not found. Returned when the user with the specified ID does not exist or the update process fails.
        /// Example Response (Not Found):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "User not found or update failed."
        /// }
        /// </code>
        /// </response>
        /// <response code="500">
        /// Server error. Returned when an unexpected error occurs on the server.
        /// Example Response (Server Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "An error occurred while updating the user.",
        ///     "error": "Database connection failed"
        /// }
        /// </code>
        /// </response>
        [HttpPut]
        [Route("~/Users/Edit/{id}")]
        [Authorize(Roles = StaticClass.Admin + "," + StaticClass.User)]
        public async Task<IActionResult> Edit(string id, [FromBody] UpdateUserDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { Success = false, Message = "Invalid data.", Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

            try
            {
                var result = await _usersService.Edit(id, model);

                if (result)
                    return Ok(new { Success = true, Message = "User updated successfully." });

                return NotFound(new { Success = false, Message = "User not found or update failed." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while updating the user.", Error = ex.Message });
            }
        }

        #endregion


        #region Delete User

        /// <summary>
        /// Deletes a user by their ID.
        /// This endpoint allows an admin or authorized user to delete a user from the system using their ID.
        /// </summary>
        /// <param name="id">The ID of the user to delete.</param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> indicating the result of the delete operation.
        /// </returns>
        /// <response code="200">
        /// User deleted successfully. Returns a success message.
        /// Example Response (Success):
        /// <code>
        /// {
        ///     "success": true,
        ///     "message": "User deleted successfully."
        /// }
        /// </code>
        /// </response>
        /// <response code="404">
        /// Not found. Returned when the user with the specified ID does not exist or could not be deleted.
        /// Example Response (Not Found):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "User not found or could not be deleted."
        /// }
        /// </code>
        /// </response>
        /// <response code="401">
        /// Unauthorized. Returned when the caller is not authenticated.
        /// </response>
        /// <response code="500">
        /// Server error. Returned when an unexpected error occurs on the server.
        /// Example Response (Server Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "An error occurred while deleting the user.",
        ///     "error": "Database connection failed"
        /// }
        /// </code>
        /// </response>
        [HttpDelete]
        [Route("~/Users/Delete/{id}")]
        [Authorize(Roles = StaticClass.Admin + "," + StaticClass.User)]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var result = await _usersService.Delete(id);
                if (result)
                    return Ok(new { Success = true, Message = "User deleted successfully." });

                return NotFound(new { Success = false, Message = "User not found or could not be deleted." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while deleting the user.", Error = ex.Message });
            }
        }

        #endregion


        #region Assign User To Role

        /// <summary>
        /// Adds a user to a specific role by their ID and role name.
        /// This endpoint allows an admin or authorized user to assign a role to a user in the system.
        /// </summary>
        /// <param name="userId">The ID of the user to add to the role.</param>
        /// <param name="roleName">The name of the role to assign to the user.</param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> indicating the result of the role assignment operation.
        /// </returns>
        /// <response code="200">
        /// Role assigned successfully. Returns a success message.
        /// Example Response (Success):
        /// <code>
        /// {
        ///     "success": true,
        ///     "message": "User added to role successfully."
        /// }
        /// </code>
        /// </response>
        /// <response code="400">
        /// Bad request. Returned when the role assignment fails (e.g., user or role not found, or user already in role).
        /// Example Response (Failure):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "Failed to add user to role.",
        ///     "errors": [
        ///         "User with ID 12345 not found.",
        ///         "Role 'Admin' does not exist."
        ///     ]
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
        ///     "message": "An error occurred while adding the user to the role.",
        ///     "error": "Database connection failed"
        /// }
        /// </code>
        /// </response>
        [HttpPost]
        [Route("~/Users/AddUserToRole/{userId}/{roleName}")]
        [Authorize(Roles = StaticClass.Admin)]
        public async Task<IActionResult> AddUserToRole(string userId, string roleName)
        {
            try
            {
                var result = await _usersService.AddUserToRoleAsync(userId, roleName);

                if (!result.Success)
                {
                    return BadRequest(new { Success = false, Message = result.Message, Errors = result.Errors });
                }

                return Ok(new { Success = true, Message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while adding the user to the role.", Error = ex.Message });
            }
        }

        #endregion


        #region Delete User From Role

        /// <summary>
        /// Removes a user from a specific role by their ID and role name.
        /// This endpoint allows an admin to remove a user from a role in the system.
        /// </summary>
        /// <param name="userId">The ID of the user to remove from the role.</param>
        /// <param name="roleName">The name of the role to remove the user from.</param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> indicating the result of the role removal operation.
        /// </returns>
        /// <response code="200">
        /// Role removed successfully. Returns a success message.
        /// Example Response (Success):
        /// <code>
        /// {
        ///     "success": true,
        ///     "message": "User with ID '12345' removed from role 'Editor' successfully."
        /// }
        /// </code>
        /// </response>
        /// <response code="400">
        /// Bad request. Returned when the role removal fails (e.g., user or role not found, or user not in role).
        /// Example Response (Failure):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "Failed to remove user from role.",
        ///     "errors": [
        ///         "User with ID '12345' not found.",
        ///         "Role 'Editor' does not exist.",
        ///         "User with ID '12345' is not in role 'Editor'."
        ///     ]
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
        ///     "message": "An error occurred while removing the user from the role.",
        ///     "error": "Database connection failed"
        /// }
        /// </code>
        /// </response>
        [HttpDelete]
        [Route("users/{userId}/roles/{roleName}")]
        [Authorize(Roles = StaticClass.Admin)]
        public async Task<IActionResult> DeleteUserFromRole(string userId, string roleName)
        {
            try
            {
                var result = await _usersService.DeleteUserFromRoleAsync(userId, roleName);

                if (!result.Success)
                {
                    return BadRequest(new { Success = false, Message = result.Message, Errors = result.Errors });
                }

                return Ok(new { Success = true, Message = result.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while removing the user from the role.", Error = ex.Message });
            }
        }

        #endregion
    }
}
