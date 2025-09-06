using HRSystem.DTO.AttendanceDTOs;
using HRSystem.DTO.EmployeeDTOs;
using HRSystem.DTO.UserDTOs;
using HRSystem.Helper;
using HRSystem.Models;
using HRSystem.Services.UsersServices;
using HRSystem.Services.VacationServices;
using HRSystem.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HRSystem.Controllers
{
    [Route("[controller]")]
    [ApiController]
    //[Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UsersController> _logger;

        public UsersController(ILogger<UsersController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        #region Create User

        /// <summary>
        /// Creates a new user in the system.
        /// This endpoint allows an admin to register a new user by providing necessary details such as name, email, and other personal information.
        /// </summary>
        /// <remarks>
        /// This endpoint requires admin privileges and expects a valid JSON payload conforming to the <see cref="CreateUserDTO"/> structure.
        /// It performs validation on the input data and returns an authentication response with user details and a token upon success.
        /// </remarks>
        /// <param name="model">
        /// The user data to create, provided in the request body as a.
        /// /// <para><strong>Example Request:</strong></para>
        /// <code>
        /// POST /Users/Create
        /// {
        ///     "name": "John Doe",
        ///     "email": "john.doe@example.com",
        ///     "address": "123 Main St, Springfield",
        ///     "dateOfBarth": "1990-05-15T00:00:00",
        ///     "phoneNumber": "+1-555-123-4567",
        ///     "userName": "johndoe",
        ///     "nationalid": "987654321",
        ///     "salary": 50000.75,
        ///     "shiftType": "Morning",
        ///     "gender": "Male",
        ///     "dateOfWork": "2025-01-01T00:00:00",
        ///     "password": "SecurePass123!"
        /// }
        /// </code>
        /// </param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> containing the result of the user creation process.
        /// On success, it returns HTTP 200 (OK) with user details and an authentication token.
        /// On failure, it returns appropriate error codes with descriptive messages.
        /// </returns>
        /// <response code="200">
        /// Successfully created the user. Returns user details and an authentication token.
        /// Example Response:
        /// <code>
        /// {
        ///     "message": "User created successfully",
        ///     "user": {
        ///         "id": "123e4567-e89b-12d3-a456-426614174000",
        ///         "name": "John Doe",
        ///         "email": "john.doe@example.com",
        ///         "userName": "johndoe",
        ///         "phoneNumber": "+1-555-123-4567",
        ///         "address": "123 Main St, Springfield",
        ///         "nationalid": "987654321",
        ///         "salary": 50000.75,
        ///         "shiftType": "Morning",
        ///         "gender": "Male",
        ///         "dateOfWork": "2025-01-01",
        ///         "dateOfBarth": "1990-05-15",
        ///         "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        ///         "roles": ["User"]
        ///     }
        /// }
        /// </code>
        /// </response>
        /// <response code="400">
        /// Bad Request. Returned when the input data is invalid or fails validation.
        /// Example Response (Validation Error):
        /// <code>
        /// {
        ///     "name": ["Name is required."],
        ///     "email": ["Invalid email format."]
        /// }
        /// </code>
        /// Example Response (Business Logic Error):
        /// <code>
        /// {
        ///     "message": "Email already exists in the system."
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
        /// <response code="500">
        /// Server Error. Returned when an unexpected error occurs during user creation.
        /// Example Response (Generic Error):
        /// <code>
        /// {
        ///     "message": "An error occurred while creating the user",
        ///     "error": "Database connection failed"
        /// }
        /// </code>
        /// Example Response (Detailed Error):
        /// <code>
        /// {
        ///     "message": "An error occurred while creating the user",
        ///     "error": "User creation failed due to duplicate National ID",
        ///     "timestamp": "2025-03-24T14:30:00Z"
        /// }
        /// </code>
        /// </response>
        /// <exception cref="Exception">Thrown when an unexpected error occurs (e.g., database failure). Caught and returned as a 500 response with error details.</exception>
        [HttpPost]
        [Route("~/Users/Create")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDTO model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for user creation: {Errors}", string.Join("; ", errors));
                return BadRequest(new
                {
                    Success = false,
                    Message = "Invalid user data provided.",
                    Errors = errors
                });
            }

            try
            {
                var createdUser = await _unitOfWork.UsersServices.Create(model);

                if (!createdUser.IsAuthenticated)
                {
                    _logger.LogWarning("Failed to create user with email: {Email}. Reason: {Message}", model.Email, createdUser.Message);
                    return BadRequest(new
                    {
                        Success = false,
                        Message = createdUser.Message
                    });
                }

                _logger.LogInformation("Successfully created user with ID: {UserId} and email: {Email}", createdUser.Id, createdUser.Email);
                return Ok(new
                {
                    Success = true,
                    Message = createdUser.Message,
                    Data = createdUser
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create user with email: {Email}. Error: {Message}", model?.Email, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while creating the user.",
                    Error = ex.Message
                });
            }
        }

        #endregion


        #region Get All Users

        /// <summary>
        /// Retrieves a list of all users in the system.
        /// This endpoint allows an admin to fetch all registered users for administrative purposes.
        /// </summary>
        /// <remarks>
        /// This is a GET endpoint that requires admin privileges. It returns a collection of user data if available, or an appropriate error message if no users exist or an issue occurs.
        /// No parameters are required as it fetches all users indiscriminately.
        /// </remarks>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> containing the result of the user retrieval process.
        /// On success, it returns HTTP 200 (OK) with a list of users.
        /// On failure, it returns appropriate error codes with descriptive messages.
        /// </returns>
        /// <response code="200">
        /// Successfully retrieved the list of users.
        /// Example Response (Multiple Users):
        /// <code>
        /// {
        ///     "success": true,
        ///     "data": [
        ///         {
        ///             "id": "123e4567-e89b-12d3-a456-426614174000",
        ///             "name": "John Doe",
        ///             "email": "john.doe@example.com",
        ///             "userName": "johndoe",
        ///             "phoneNumber": "+1-555-123-4567",
        ///             "address": "123 Main St, Springfield",
        ///             "nationalId": "987654321",
        ///             "salary": 50000.75,
        ///             "shiftType": "Morning",
        ///             "gender": "Male",
        ///             "dateOfWork": "2025-01-01",
        ///             "dateOfBirth": "1990-05-15"
        ///         },
        ///         {
        ///             "id": "987fcdeb-12ab-34cd-e567-890123456789",
        ///             "name": "Jane Smith",
        ///             "email": "jane.smith@example.com",
        ///             "userName": "janesmith",
        ///             "phoneNumber": "+1-555-987-6543",
        ///             "address": "456 Oak Ave, Riverside",
        ///             "nationalId": "123456789",
        ///             "salary": 60000.00,
        ///             "shiftType": "Evening",
        ///             "gender": "Female",
        ///             "dateOfWork": "2024-11-15",
        ///             "dateOfBirth": "1988-09-22"
        ///         }
        ///     ]
        /// }
        /// </code>
        /// Example Response (Single User):
        /// <code>
        /// {
        ///     "success": true,
        ///     "data": [
        ///         {
        ///             "id": "123e4567-e89b-12d3-a456-426614174000",
        ///             "name": "John Doe",
        ///             "email": "john.doe@example.com",
        ///             "userName": "johndoe",
        ///             "phoneNumber": "+1-555-123-4567",
        ///             "address": "123 Main St, Springfield",
        ///             "nationalId": "987654321",
        ///             "salary": 50000.75,
        ///             "shiftType": "Morning",
        ///             "gender": "Male",
        ///             "dateOfWork": "2025-01-01",
        ///             "dateOfBirth": "1990-05-15"
        ///         }
        ///     ]
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
        /// Not Found. Returned when no users exist in the system.
        /// Example Response:
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "No users found."
        /// }
        /// </code>
        /// </response>
        /// <response code="500">
        /// Server Error. Returned when an unexpected error occurs during retrieval.
        /// Example Response (Generic Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "An error occurred while retrieving users.",
        ///     "error": "Database connection failed"
        /// }
        /// </code>
        /// Example Response (Detailed Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "An error occurred while retrieving users.",
        ///     "error": "Timeout occurred while querying the database",
        ///     "timestamp": "2025-03-24T15:45:00Z"
        /// }
        /// </code>
        /// </response>
        /// <exception cref="Exception">Thrown when an unexpected error occurs (e.g., database failure). Caught and returned as a 500 response with error details.</exception>
        [HttpGet]
        [Route("~/Users/GetAll")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetUsers([FromQuery] PaginationDTO pagination)
        {
            try
            {
                var allUsers = await _unitOfWork.UsersServices.GetAllAsync(pagination);

                if (allUsers == null || !allUsers.Any())
                {
                    _logger.LogInformation("No users found in the system.");
                    return Ok(new
                    {
                        Success = true,
                        Message = "No users found.",
                        Data = new List<object>()
                    });
                }

                _logger.LogInformation("Successfully retrieved {UserCount} users.", allUsers.Count());
                return Ok(new
                {
                    Success = true,
                    Message = "Users retrieved successfully.",
                    Data = allUsers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve users. Error: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while retrieving users.",
                    Error = ex.Message
                });
            }
        }
        #endregion


        #region Get User By ID

        /// <summary>
        /// Retrieves a user by their ID.
        /// </summary>
        /// <param name="id">The unique identifier of the user to retrieve.</param>
        /// <returns>
        /// Returns the user details if found, or an error message if the user is not found or the operation fails.
        /// </returns>
        /// <remarks>
        /// This endpoint retrieves a user by ID and requires Admin or User role authorization.
        /// </remarks>
        /// <response code="200">
        /// Returns the user details when the request is successful.
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "User retrieved successfully.",
        ///   "data": {
        ///     "id": "12345",
        ///     "name": "John Doe",
        ///     "email": "john@example.com"
        ///   }
        /// }
        /// ```
        /// </response>
        /// <response code="401">
        /// Returned when the user is not authenticated or lacks required role authorization.
        /// ```json
        /// {
        ///   "message": "Unauthorized"
        /// }
        /// ```
        /// </response>
        /// <response code="404">
        /// Returned when the user with the specified ID is not found.
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "User with ID '12345' not found."
        /// }
        /// ```
        /// </response>
        /// <response code="500">
        /// Returned when an unexpected server error occurs during processing.
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "An error occurred while retrieving the user.",
        ///   "error": "Exception details here"
        /// }
        /// ```
        /// </response>
        [HttpGet]
        [Route("~/Users/GetById/{id}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.User}")]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var user = await _unitOfWork.UsersServices.GetByID(id);

                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found.", id);
                    return NotFound(new
                    {
                        Success = false,
                        Message = $"User with ID '{id}' not found."
                    });
                }

                _logger.LogInformation("User with ID {UserId} retrieved successfully.", id);
                return Ok(new
                {
                    Success = true,
                    Message = user.Message ?? "User retrieved successfully.",
                    Data = user
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve user with ID: {UserId}. Error: {Message}", id, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while retrieving the user.",
                    Error = ex.Message
                });
            }
        }

        #endregion


        #region Get Employees By Branch ID

        [HttpGet]
        [Route("~/Users/GetEmployeesByBranchId/{BranchId}")]
        public async Task<IActionResult> GetEmployeesByBranchId(int BranchId)
        {
            try
            {
                // Call the GetEmployeesByBranchId service method
                var employees = await _unitOfWork.UsersServices.GetEmployeesByBranchId(BranchId);
                // Check if the result is null or empty
                if (employees == null || !employees.Any())
                {
                    return NotFound(new { Message = $"No employees found for BranchId: {BranchId}" });
                }
                // Return the result as a 200 OK response
                return Ok(employees);
            }
            catch (Exception ex)
            {
                // Log the error and return a 500 Internal Server Error response
                _logger.LogError(ex, "Error retrieving employees for BranchId: {BranchId}", BranchId);
                return StatusCode(500, new { Message = "An error occurred while retrieving employees." });
            }
        }

        #endregion


        #region Get Employee Vacations

        [HttpGet]
        [Route("~/Users/EmployeeVacations/{EmployeeId}")]
        [Authorize(Roles = Roles.Admin + "," + Roles.User)]
        public async Task<IActionResult> EmployeeVacations(string EmployeeId,[FromQuery] ParamDTO dto)
        {
            try
            {
                // Call the GetEmployeeVacations service method
                var employeeVacations = await _unitOfWork.VacationService.GetEmployeeVacations(EmployeeId,dto);

                // Check if the result is null or empty
                if (employeeVacations == null || (employeeVacations.TotalVacationDays == 0 && employeeVacations.EmployeeVacationDetails == null))
                {
                    return NotFound(new { Message = $"No vacation records found for EmployeeId: {EmployeeId}" });
                }

                // Return the result as a 200 OK response
                return Ok(employeeVacations);
            }
            catch (Exception ex)
            {
                // Log the error and return a 500 Internal Server Error response
                _logger.LogError(ex, "Error retrieving vacation details for EmployeeId: {EmployeeId}", EmployeeId);
                return StatusCode(500, new { Message = "An error occurred while retrieving vacation details." });
            }
        }

        #endregion


        #region Get All Employees Vacations
        [HttpGet]
        [Route("~/Users/GetAllEmployeesVacations")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> AllEmployeesVacations([FromQuery]ParamDTO dto)
        {
            try
            {
                // Call the GetAllEmployeesVacations service method
                var allEmployeesVacations = await _unitOfWork.VacationService.GetAllEmployeesVacations(dto);
                // Check if the result is null or empty
                if (allEmployeesVacations == null || !allEmployeesVacations.Any())
                {
                    return NotFound(new { Message = "No vacation records found for any employee." });
                }
                var vacationDetails = allEmployeesVacations.Select(v => new
                {
                    v.EmployeeName,
                    v.TotalVacationDays,
                }).ToList();
                // Return the result as a 200 OK response
                return Ok(vacationDetails);
            }
            catch (Exception ex)
            {
                // Log the error and return a 500 Internal Server Error response
                _logger.LogError(ex, "Error retrieving all employees' vacation details.");
                return StatusCode(500, new { Message = "An error occurred while retrieving vacation details." });
            }
        }
        #endregion


        #region Get Employees Salaries
        [HttpGet]
        [Route("~/Users/GetAllEmployeesSalaries")]
        public async Task<IActionResult> EmployeesSalaries([FromQuery] ParamDTO dto)
        {
            try
            {
                // Call the GetEmployeesSalaries service method
                var employeesSalaries = await _unitOfWork.UsersServices.GetEmployeesSalaries(dto);
                // Check if the result is null or empty
                if (employeesSalaries == null || !employeesSalaries.Any())
                {
                    return NotFound(new { Message = "No salary records found for any employee." });
                }
                // Return the result as a 200 OK response
                return Ok(employeesSalaries);
            }
            catch (Exception ex)
            {
                // Log the error and return a 500 Internal Server Error response
                _logger.LogError(ex, "Error retrieving all employees' salary details.");
                return StatusCode(500, new { Message = "An error occurred while retrieving salary details." });
            }
        }
        #endregion


        #region Get Employee Salary Details

        [HttpGet]
        [Route("~/Users/EmployeeSalaryDetails/{EmployeeId}")]
        public async Task<IActionResult> EmployeeSalaryDetails(string EmployeeId, int? month = null, int? year = null)
        {
            try
            {
                // Call the GetEmployeeSalaryDetails service method
                var employeeSalaryDetails = await _unitOfWork.UsersServices.GetEmployeeSalaryDetails(EmployeeId, month, year);
                // Check if the result is null or empty
                if (employeeSalaryDetails == null)
                {
                    return NotFound(new { Message = $"No salary records found for EmployeeId: {EmployeeId}" });
                }
                // Return the result as a 200 OK response
                return Ok(employeeSalaryDetails);
            }
            catch (Exception ex)
            {
                // Log the error and return a 500 Internal Server Error response
                _logger.LogError(ex, "Error retrieving salary details for EmployeeId: {EmployeeId}", EmployeeId);
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        #endregion


        #region Edit User

        /// <summary>
        /// Updates an existing user in the system.
        /// </summary>
        /// <param name="model">The data transfer object containing the updated user details.</param>
        /// <returns>
        /// Returns a success message if the user is updated, or an error message if the user is not found or the operation fails.
        /// </returns>
        /// <remarks>
        /// This endpoint updates a user and requires Admin role authorization. The request body must contain valid user details.
        /// 
        /// Example Request:
        /// ```json
        /// {
        ///   "id": "12345",
        ///   "name": "Updated John Doe",
        ///   "email": "john.updated@example.com"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">
        /// Returns a success message when the user is updated successfully.
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "User updated successfully."
        /// }
        /// ```
        /// </response>
        /// <response code="400">
        /// Returned when the request model is invalid.
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Invalid user data provided.",
        ///   "errors": ["The Email field is required."]
        /// }
        /// ```
        /// </response>
        /// <response code="401">
        /// Returned when the user is not authenticated or lacks Admin role authorization.
        /// ```json
        /// {
        ///   "message": "Unauthorized"
        /// }
        /// ```
        /// </response>
        /// <response code="404">
        /// Returned when the user with the specified ID is not found or cannot be updated.
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "User with ID '12345' not found or could not be updated."
        /// }
        /// ```
        /// </response>
        /// <response code="500">
        /// Returned when an unexpected server error occurs during processing.
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "An error occurred while updating the user.",
        ///   "error": "Exception details here"
        /// }
        /// ```
        /// </response>
        [HttpPut]
        [Route("~/Users/Edit")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> Edit([FromBody] UpdateUserDTO model)
        {
           
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for user update with ID: {UserId}. Errors: {Errors}", model.Id, string.Join("; ", errors));
                return BadRequest(new
                {
                    Success = false,
                    Message = "Invalid user data provided.",
                    Errors = errors
                });
            }

            try
            {
                var result = await _unitOfWork.UsersServices.Edit(model);

                if (!result)
                {
                    _logger.LogWarning("User with ID {UserId} not found or could not be updated.", model.Id);
                    return NotFound(new
                    {
                        Success = false,
                        Message = $"User with ID '{model.Id}' not found or could not be updated."
                    });
                }

                _logger.LogInformation("User with ID {UserId} updated successfully.", model.Id);
                return Ok(new
                {
                    Success = true,
                    Message = "User updated successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user with ID: {UserId}. Error: {Message}", model.Id, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while updating the user.",
                    Error = ex.Message
                });
            }
        }

        #endregion


        #region Update Salary 

        [HttpPut]
        [Route("~/Users/UpdateSalary")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> UpdateSalary([FromBody] UpdateSalaryDTO model, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for salary update with ID: {EmployeeId}. Errors: {Errors}", model?.EmployeeId, string.Join("; ", errors));
                return BadRequest(new
                {
                    Success = false,
                    Message = "Invalid salary data provided.",
                    Errors = errors
                });
            }

            try
            {
                var result = await _unitOfWork.UsersServices.UpdateNetSalary(model);

                if (result == "Salary updated successfully.")
                {
                    _logger.LogInformation("Employee with ID {EmployeeId} salary updated successfully.", model.EmployeeId);
                    return Ok(new
                    {
                        Success = true,
                        Message = result
                    });
                }

                if (result == "Employee not found.")
                {
                    _logger.LogWarning("Employee with ID {EmployeeId} not found.", model.EmployeeId);
                    return NotFound(new
                    {
                        Success = false,
                        Message = result
                    });
                }

                if (result == "Employee ID cannot be null or empty." || result == "Net salary cannot be negative.")
                {
                    _logger.LogWarning("Validation failed for salary update with ID: {EmployeeId}. Reason: {Message}", model?.EmployeeId, result);
                    return BadRequest(new
                    {
                        Success = false,
                        Message = result
                    });
                }

                // Handle exception messages
                _logger.LogError("Failed to update salary for employee with ID: {EmployeeId}. Reason: {Message}", model?.EmployeeId, result);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while updating the employee salary.",
                    Error = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update salary for employee with ID: {EmployeeId}. Error: {Message}", model?.EmployeeId, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while updating the employee salary.",
                    Error = ex.Message
                });
            }
        }

        #endregion


        #region Update User Work Statistics

        [HttpPut]
        [Route("~/Users/UpdateWorkStatistics")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> UpdateWorkStatistics([FromBody] UpdateWorkStatisticsDTO model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for work statistics update with ID: {EmployeeId}. Errors: {Errors}", model?.EmployeeId, string.Join("; ", errors));
                return BadRequest(new
                {
                    Success = false,
                    Message = "Invalid work statistics data provided.",
                    Errors = errors
                });
            }
            try
            {
                var result = await _unitOfWork.UsersServices.UpdateWorkStatisticsAsync(model);
                if (result == "Work statistics updated successfully.")
                {
                    _logger.LogInformation("Employee with ID {EmployeeId} work statistics updated successfully.", model.EmployeeId);
                    return Ok(new
                    {
                        Success = true,
                        Message = result
                    });
                }
                if (result == "Employee not found.")
                {
                    _logger.LogWarning("Employee with ID {EmployeeId} not found.", model.EmployeeId);
                    return NotFound(new
                    {
                        Success = false,
                        Message = result
                    });
                }
                // Handle exception messages
                _logger.LogError("Failed to update work statistics for employee with ID: {EmployeeId}. Reason: {Message}", model?.EmployeeId, result);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while updating the employee work statistics.",
                    Error = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update work statistics for employee with ID: {EmployeeId}. Error: {Message}", model?.EmployeeId, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while updating the employee work statistics.",
                    Error = ex.Message
                });
            }
        }
        #endregion


        #region Delete User

        /// <summary>
        /// Deletes a user from the system by their ID.
        /// </summary>
        /// <param name="id">The unique identifier of the user to delete.</param>
        /// <returns>
        /// Returns a success message if the user is deleted, or an error message if the user is not found or the operation fails.
        /// </returns>
        /// <remarks>
        /// This endpoint deletes a user and requires Admin or User role authorization.
        /// </remarks>
        /// <response code="200">
        /// Returns a success message when the user is deleted successfully.
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "User and associated records deleted successfully."
        /// }
        /// ```
        /// </response>
        /// <response code="401">
        /// Returned when the user is not authenticated or lacks required role authorization.
        /// ```json
        /// {
        ///   "message": "Unauthorized"
        /// }
        /// ```
        /// </response>
        /// <response code="404">
        /// Returned when the user with the specified ID is not found or cannot be deleted.
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "User with ID '12345' not found or could not be deleted."
        /// }
        /// ```
        /// </response>
        /// <response code="500">
        /// Returned when an unexpected server error occurs during processing.
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "An error occurred while deleting the user.",
        ///   "error": "Exception details here"
        /// }
        /// ```
        /// </response>
        [HttpDelete]
        [Route("~/Users/Delete/{id}")]
        [Authorize(Roles = Roles.Admin + "," + Roles.User)]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var result = await _unitOfWork.UsersServices.Delete(id);

                if (!result)
                {
                    _logger.LogWarning("User with ID {UserId} not found or could not be deleted.", id);
                    return NotFound(new
                    {
                        Success = false,
                        Message = $"User with ID '{id}' not found or could not be deleted."
                    });
                }

                _logger.LogInformation("User with ID {UserId} deleted successfully.", id);
                return Ok(new
                {
                    Success = true,
                    Message = "User and associated records deleted successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete user with ID: {UserId}. Error: {Message}", id, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while deleting the user.",
                    Error = ex.Message
                });
            }
        }

        #endregion


        #region Update Sales Percentage

        [HttpPut]
        [Route("~/Users/UpdateSalesPercentage")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> UpdateSalesPercentage([FromBody] UpdateSalesPresentageDTO model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning("Invalid model state for updating sales percentage with EmployeeId: {EmployeeId}. Errors: {Errors}", model?.EmployeeId, string.Join("; ", errors));
                return BadRequest(new
                {
                    Success = false,
                    Message = "Invalid sales percentage data provided.",
                    Errors = errors
                });
            }
            try
            {
                var result = await _unitOfWork.UsersServices.UpdateSalesPresentage(model);
                if (result == "Sales percentage updated successfully.")
                {
                    _logger.LogInformation("Sales percentage for employee with ID {EmployeeId} updated successfully.", model.EmployeeId);
                    return Ok(new
                    {
                        Success = true,
                        Message = result
                    });
                }
                if (result == "Employee not found.")
                {
                    _logger.LogWarning("Employee with ID {EmployeeId} not found.", model.EmployeeId);
                    return NotFound(new
                    {
                        Success = false,
                        Message = result
                    });
                }
                // Handle exception messages
                _logger.LogError("Failed to update sales percentage for employee with ID: {EmployeeId}. Reason: {Message}", model?.EmployeeId, result);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while updating the sales percentage.",
                    Error = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update sales percentage for employee with ID: {EmployeeId}. Error: {Message}", model?.EmployeeId, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while updating the sales percentage.",
                    Error = ex.Message
                });
            }
        }
        #endregion

    }
}
