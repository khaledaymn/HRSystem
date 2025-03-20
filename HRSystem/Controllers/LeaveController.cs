using HRSystem.DTO;
using HRSystem.Helper;
using HRSystem.Services.LeaveServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace HRSystem.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class LeaveController : ControllerBase
    {
        private readonly ILeaveServices _leaveService;

        public LeaveController(ILeaveServices leaveService)
        {
            _leaveService = leaveService;
        }

        #region Take Leave

        /// <summary>
        /// Adds a new leave request for an employee in the system.
        /// This endpoint allows recording an employee's leave request by providing details such as the leave dates and employee ID.
        /// </summary>
        /// <param name="leaveDto">
        /// The data required to record a leave request (as defined in LeaveDTO).
        /// Example Request (LeaveDTO):
        /// <code>
        /// {
        ///     "employeeId": "EMP12345",
        ///     "startDate": "2025-04-01T00:00:00Z",
        ///     "endDate": "2025-04-05T00:00:00Z",
        ///     "reason": "Vacation"
        /// }
        /// </code>
        /// </param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> indicating the result of the leave addition operation.
        /// </returns>
        /// <response code="201">
        /// Leave recorded successfully. Returns the employee ID and a success message.
        /// Example Response (Success):
        /// <code>
        /// {
        ///     "employeeId": "EMP12345",
        ///     "message": "Leave added successfully."
        /// }
        /// </code>
        /// </response>
        /// <response code="400">
        /// Bad request. Returned when the provided model is invalid (e.g., missing required fields or invalid dates).
        /// Example Response (Invalid Model):
        /// <code>
        /// {
        ///     "errors": {
        ///         "StartDate": ["The start date is required."],
        ///         "EndDate": ["The end date must be after the start date."]
        ///     }
        /// }
        /// </code>
        /// </response>
        /// <response code="500">
        /// Server error. Returned when an unexpected error occurs or if leave recording fails.
        /// Example Response (Operation Failed):
        /// <code>
        /// "Error adding leave."
        /// </code>
        /// Example Response (Server Error):
        /// <code>
        /// "An internal server error occurred."
        /// </code>
        /// </response>
        [HttpPost]
        [Route("~/Leave/TakeLeave")]
        [Authorize(Roles = StaticClass.User)]
        public async Task<IActionResult> AddLeave([FromBody] LeaveDTO leaveDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var result = await _leaveService.AddLeave(leaveDto);
                if (result)
                    return Created("", new { EmployeeId = leaveDto.EmployeeId, Message = "Leave added successfully." });

                return StatusCode(500, "Error adding leave.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An internal server error occurred.");
            }
        }

        #endregion


        #region Get All Leaves
        /// <summary>
        /// Retrieves all leave records from the system.
        /// This endpoint returns a list of all recorded employee leave requests.
        /// </summary>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> containing the list of leave records or an error message.
        /// </returns>
        /// <response code="200">
        /// Successfully retrieved leave records. Returns a list of leave details.
        /// Example Response (Success):
        /// <code>
        /// [
        ///     {
        ///         "employeeId": "EMP12345",
        ///         "startDate": "2025-04-01T00:00:00Z",
        ///         "endDate": "2025-04-05T00:00:00Z",
        ///         "reason": "Vacation"
        ///     },
        ///     {
        ///         "employeeId": "EMP67890",
        ///         "startDate": "2025-05-10T00:00:00Z",
        ///         "endDate": "2025-05-12T00:00:00Z",
        ///         "reason": "Medical"
        ///     }
        /// ]
        /// </code>
        /// </response>
        /// <response code="404">
        /// No leave records found in the system.
        /// Example Response (Not Found):
        /// <code>
        /// "No leave records found."
        /// </code>
        /// </response>
        /// <response code="500">
        /// Server error. Returned when an unexpected error occurs while retrieving leave records.
        /// Example Response (Server Error):
        /// <code>
        /// "An internal server error occurred."
        /// </code>
        /// </response>
        [HttpGet]
        [Route("~/Leave/GetAll")]
        [Authorize(Roles = StaticClass.Admin)]
        public async Task<IActionResult> GetLeaves()
        {
            var leaves = await _leaveService.GetAllLeaves();

            if (!leaves.Any())
                return NotFound("No leave records found.");

            return Ok(leaves);
        }

        #endregion


        #region Get Employee Leaves

        /// <summary>
        /// Retrieves all leave records for a specific employee based on their employee ID.
        /// This endpoint returns a list of leave records associated with the provided employee ID.
        /// </summary>
        /// <param name="empId">
        /// The unique identifier of the employee whose leave records are to be retrieved.
        /// Example: "EMP12345"
        /// </param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> containing the list of leave records for the specified employee or an error message.
        /// </returns>
        /// <response code="200">
        /// Successfully retrieved leave records for the employee. Returns a list of leave details.
        /// Example Response (Success):
        /// <code>
        /// [
        ///     {
        ///         "employeeId": "EMP12345",
        ///         "startDate": "2025-04-01T00:00:00Z",
        ///         "endDate": "2025-04-05T00:00:00Z",
        ///         "reason": "Vacation"
        ///     },
        ///     {
        ///         "employeeId": "EMP12345",
        ///         "startDate": "2025-06-10T00:00:00Z",
        ///         "endDate": "2025-06-12T00:00:00Z",
        ///         "reason": "Medical"
        ///     }
        /// ]
        /// </code>
        /// </response>
        /// <response code="400">
        /// Bad request. Returned when the employee ID is missing or invalid.
        /// Example Response (Invalid Input):
        /// <code>
        /// "Employee ID is required."
        /// </code>
        /// </response>
        /// <response code="404">
        /// No leave records found for the specified employee.
        /// Example Response (Not Found):
        /// <code>
        /// "No leave records found for the given employee."
        /// </code>
        /// </response>
        /// <response code="500">
        /// Server error. Returned when an unexpected error occurs while retrieving the leave records, including the exception message.
        /// Example Response (Server Error):
        /// <code>
        /// "An internal server error occurred: Database connection failed."
        /// </code>
        /// </response>
        [HttpGet]
        [Route("~/Leave/EmployeeLeaves/{empId}")]
        [Authorize(StaticClass.Admin + "," + StaticClass.User)]
        public async Task<IActionResult> GetEmployeeLeaves(string empId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(empId))
                    return BadRequest("Employee ID is required.");

                var leaves = await _leaveService.GetEmployeeLeaves(empId);

                if (leaves == null || !leaves.Any())
                    return NotFound("No leave records found for the given employee.");

                return Ok(leaves);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An internal server error occurred: {ex.Message}");
            }
        }

        #endregion

    }
}

