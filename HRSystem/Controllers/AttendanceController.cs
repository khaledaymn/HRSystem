using HRSystem.Models;
using HRSystem.Services.AttendanceServices;
using HRSystem.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using HRSystem.Helper;
using System.Linq.Expressions;

namespace HR_System_API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class AttendanceController : ControllerBase
    {

        private readonly IAttendanceServices _attendanceService;

        public AttendanceController(IAttendanceServices attendanceService)
        {
            _attendanceService = attendanceService;
        }

        #region Take Attendance

        /// <summary>
        /// Records attendance for an employee.
        /// This endpoint allows a user to submit attendance details, including time and location, for an employee.
        /// </summary>
        /// <remarks>
        /// This endpoint requires user privileges and expects a valid JSON payload conforming to the <see cref="AttendanceDto"/> structure.
        /// It performs validation on the input data and returns a success message with the employee ID if the attendance is recorded successfully, or an error if the operation fails.
        /// </remarks>
        /// <param name="attendanceDto">
        /// The attendance data to record, provided in the request body as an <see cref="AttendanceDto"/> object.
        /// <para><strong>Example Request:</strong></para>
        /// <code>
        /// POST /Attendance/TakeAttendance
        /// {
        ///     "timeOfAttend": "2025-03-24T08:30:00Z",
        ///     "latitude": 40.7128,
        ///     "longitude": -74.0060,
        ///     "employeeId": "123e4567-e89b-12d3-a456-426614174000",
        ///     "branch": "Main branch"
        /// }
        /// </code>
        /// </param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> containing the result of the attendance recording process.
        /// On success, it returns HTTP 201 (Created) with the employee ID and a success message.
        /// On failure, it returns appropriate error codes with descriptive messages.
        /// </returns>
        /// <response code="201">
        /// Successfully recorded the attendance. Returns the employee ID and a confirmation message.
        /// Example Response:
        /// <code>
        /// {
        ///     "employeeId": "123e4567-e89b-12d3-a456-426614174000",
        ///     "message": "Attendance added successfully."
        /// }
        /// </code>
        /// </response>
        /// <response code="400">
        /// Bad Request. Returned when the input data is invalid or fails validation.
        /// Example Response (Validation Error):
        /// <code>
        /// {
        ///     "timeOfAttend": ["Time of attendance is required."],
        ///     "latitude": ["Latitude must be between -90 and 90."]
        /// }
        /// </code>
        /// Example Response (Business Logic Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "Employee ID does not exist."
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
        /// Forbidden. Returned when the caller lacks user privileges.
        /// Example Response:
        /// <code>
        /// {
        ///     "message": "Access denied. User role required."
        /// }
        /// </code>
        /// </response>
        /// <response code="500">
        /// Server Error. Returned when an unexpected error occurs during attendance recording.
        /// Example Response (Generic Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "An internal server error occurred."
        /// }
        /// </code>
        /// Example Response (Detailed Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "Error adding attendance. Please try again later.",
        ///     "error": "Database timeout occurred",
        ///     "timestamp": "2025-03-24T08:35:00Z"
        /// }
        /// </code>
        /// </response>
        /// <exception cref="Exception">Thrown when an unexpected error occurs (e.g., database failure). Caught and returned as a 500 response with error details.</exception>
        [HttpPost]
        [Route("~/Attendance/TakeAttendance")]
        [Authorize(Roles = Roles.User)]
        public async Task<IActionResult> AddAttendance([FromBody] AttendanceDto attendanceDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _attendanceService.AddAttendance(attendanceDto);
                if (result)
                {
                    return Created("", new { EmployeeId = attendanceDto.EmployeeId, Message = "Attendance added successfully." });
                }

                return StatusCode(500, new { Success = false, Message = "Error adding attendance. Please try again later." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An internal server error occurred." });
            }
        }

        #endregion


        #region Get All Attendances And Leaves

        /// <summary>
        /// Retrieves all attendance and leave records in the system.
        /// This endpoint allows an admin to fetch a list of attendance records, including leave details, for all employees.
        /// </summary>
        /// <remarks>
        /// This endpoint requires admin privileges and does not require any parameters.
        /// It returns a collection of attendance records with leave details if available, or an error message if no records are found.
        /// </remarks>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> containing the result of the attendance retrieval process.
        /// On success, it returns HTTP 200 (OK) with a list of attendance records.
        /// On failure, it returns appropriate error codes with descriptive messages.
        /// </returns>
        /// <response code="200">
        /// Successfully retrieved the attendance and leave records. Returns a list of records.
        /// Example Response:
        /// <code>
        /// [
        ///     {
        ///         "timeOfAttend": "2025-03-24T08:30:00",
        ///         "timeOfLeave": "2025-03-24T17:00:00",
        ///         "latitudeOfAttend": 40.7128,
        ///         "latitudeOfLeave": 40.7130,
        ///         "longitudeOfAttend": -74.0060,
        ///         "longitudeOfLeave": -74.0058,
        ///         "employeeName": "John Doe",
        ///         "employeeId": "123e4567-e89b-12d3-a456-426614174000",
        ///         "branch": "New York"
        ///     },
        ///     {
        ///         "timeOfAttend": "2025-03-24T09:00:00",
        ///         "timeOfLeave": null,
        ///         "latitudeOfAttend": 51.5074,
        ///         "latitudeOfLeave": null,
        ///         "longitudeOfAttend": -0.1278,
        ///         "longitudeOfLeave": null,
        ///         "employeeName": "Jane Smith",
        ///         "employeeId": "987fcdeb-12ab-34cd-e567-890123456789",
        ///         "branch": "London"
        ///     }
        /// ]
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
        /// Not Found. Returned when no attendance records are available.
        /// Example Response:
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "No attendance records found for date."
        /// }
        /// </code>
        /// </response>
        /// <response code="500">
        /// Server Error. Returned when an unexpected error occurs during retrieval.
        /// Example Response (Generic Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "An error occurred while retrieving attendance records",
        ///     "error": "Database connection failed"
        /// }
        /// </code>
        /// Example Response (Detailed Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "An error occurred while retrieving attendance records",
        ///     "error": "Query timeout occurred",
        ///     "timestamp": "2025-03-24T10:00:00Z"
        /// }
        /// </code>
        /// </response>
        [HttpGet]
        [Route("~/AttendanceAndLeave/GetAll")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetAttendances()
        {
            
            var attendance = await _attendanceService.GetAllAttendancesWithLeavesAsync();

            if (attendance == null || !attendance.Any())
            {
                return NotFound(new
                {
                    Success = false,
                    Message = $"No attendance records found for date."
                });
            }

            return Ok(attendance);
        }
        #endregion


        #region Get Employee Attendances And Leaves

        /// <summary>
        /// Retrieves attendance and leave records for a specific employee.
        /// This endpoint allows an admin or user to fetch all attendance and leave records associated with a given employee ID.
        /// </summary>
        /// <remarks>
        /// This endpoint requires either admin or user privileges and expects a valid employee ID as a route parameter.
        /// It returns a list of attendance records if found, or an error message if the employee ID is invalid, no records exist, or an issue occurs.
        /// </remarks>
        /// <param name="empId">
        /// The unique identifier of the employee whose attendance records are to be retrieved (e.g., a GUID or string-based ID).
        /// <para><strong>Example Request:</strong></para>
        /// <code>
        /// GET /AttendanceAndLeave/123e4567-e89b-12d3-a456-426614174000
        /// </code>
        /// </param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> containing the result of the attendance retrieval process.
        /// On success, it returns HTTP 200 (OK) with a list of attendance records.
        /// On failure, it returns appropriate error codes with descriptive messages.
        /// </returns>
        /// <response code="200">
        /// Successfully retrieved the employee’s attendance and leave records. Returns a list of records.
        /// Example Response:
        /// <code>
        /// {
        ///     "success": true,
        ///     "data": [
        ///         {
        ///             "timeOfAttend": "2025-03-24T08:30:00",
        ///             "timeOfLeave": "2025-03-24T17:00:00",
        ///             "latitudeOfAttend": 40.7128,
        ///             "latitudeOfLeave": 40.7130,
        ///             "longitudeOfAttend": -74.0060,
        ///             "longitudeOfLeave": -74.0058,
        ///             "employeeName": "John Doe",
        ///             "employeeId": "123e4567-e89b-12d3-a456-426614174000",
        ///             "branch": "New York"
        ///         },
        ///         {
        ///             "timeOfAttend": "2025-03-25T08:45:00",
        ///             "timeOfLeave": null,
        ///             "latitudeOfAttend": 40.7129,
        ///             "latitudeOfLeave": null,
        ///             "longitudeOfAttend": -74.0061,
        ///             "longitudeOfLeave": null,
        ///             "employeeName": "John Doe",
        ///             "employeeId": "123e4567-e89b-12d3-a456-426614174000",
        ///             "branch": "New York"
        ///         }
        ///     ]
        /// }
        /// </code>
        /// </response>
        /// <response code="400">
        /// Bad Request. Returned when the employee ID is invalid or missing.
        /// Example Response (Validation Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "Employee ID is required."
        /// }
        /// </code>
        /// Example Response (Business Logic Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "Invalid employee ID format."
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
        /// Forbidden. Returned when the caller lacks sufficient privileges (neither admin nor user role).
        /// Example Response:
        /// <code>
        /// {
        ///     "message": "Access denied. Admin or User role required."
        /// }
        /// </code>
        /// </response>
        /// <response code="404">
        /// Not Found. Returned when no attendance records are found for the given employee.
        /// Example Response:
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "No attendance records found for the given employee."
        /// }
        /// </code>
        /// </response>
        /// <response code="500">
        /// Server Error. Returned when an unexpected error occurs during retrieval.
        /// Example Response (Generic Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "An internal server error occurred."
        /// }
        /// </code>
        /// Example Response (Detailed Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "An internal server error occurred.",
        ///     "error": "Database query failed due to timeout",
        ///     "timestamp": "2025-03-24T09:00:00Z"
        /// }
        /// </code>
        /// </response>
        [HttpGet]
        [Route("~/AttendanceAndLeave/{empId}")]
        [Authorize(Roles = Roles.Admin + "," + Roles.User)]
        public async Task<IActionResult> GetEmployeeAttendances(string empId)
        {
            if (string.IsNullOrWhiteSpace(empId))
                return BadRequest(new { Success = false, Message = "Employee ID is required." });

            try
            {
                var attendances = await _attendanceService.GetEmployeeAttendancesAndLeavesAsync(empId);

                if (!attendances.Any())
                    return NotFound(new { Success = false, Message = "No attendance records found for the given employee." });

                return Ok(new { Success = true, Data = attendances });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An internal server error occurred." });
            }
        }

        #endregion
    }
}

