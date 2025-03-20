using HRSystem.Models;
using HRSystem.Services.AttendanceServices;
using HRSystem.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using HRSystem.Helper;

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
        /// Adds a new attendance record for an employee in the system.
        /// This endpoint allows recording an employee's attendance by providing the necessary details such as time, location coordinates, and employee ID.
        /// </summary>
        /// <param name="attendanceDto">
        /// The data required to record attendance (as defined in AttendanceDto).
        /// Example Request (AttendanceDto):
        /// <code>
        /// {
        ///     "timeOfAttend": "2025-03-20T08:30:00Z",
        ///     "latitude": 40.7128,
        ///     "longitude": -74.0060,
        ///     "radius": 100,
        ///     "employeeId": "EMP12345"
        /// }
        /// </code>
        /// </param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> indicating the result of the attendance addition operation.
        /// </returns>
        /// <response code="201">
        /// Attendance recorded successfully. Returns the employee ID and a success message.
        /// Example Response (Success):
        /// <code>
        /// {
        ///     "employeeId": "EMP12345",
        ///     "message": "Attendance added successfully."
        /// }
        /// </code>
        /// </response>
        /// <response code="400">
        /// Bad request. Returned when the provided model is invalid (e.g., missing required fields or invalid coordinates).
        /// Example Response (Invalid Model):
        /// <code>
        /// {
        ///     "errors": {
        ///         "TimeOfAttend": ["Time of attendance is required."],
        ///         "Latitude": ["Latitude must be between -90 and 90."]
        ///     }
        /// }
        /// </code>
        /// </response>
        /// <response code="500">
        /// Server error. Returned when an unexpected error occurs during the operation or if attendance recording fails.
        /// Example Response (Operation Failed):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "Error adding attendance. Please try again later."
        /// }
        /// </code>
        /// Example Response (Server Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "An internal server error occurred."
        /// }
        /// </code>
        /// </response>
        [HttpPost]
        [Route("~/Attendance/TakeAttendance")]
        [Authorize(Roles = StaticClass.User)]
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


        #region Get All Attendances

        /// <summary>
        /// Retrieves all attendance records from the system.
        /// This endpoint returns a list of all recorded employee attendances.
        /// </summary>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> containing the list of attendance records or an error message.
        /// </returns>
        /// <response code="200">
        /// Successfully retrieved attendance records. Returns a list of attendance details.
        /// Example Response (Success):
        /// <code>
        /// [
        ///     {
        ///         "timeOfAttend": "2025-03-20T08:30:00Z",
        ///         "latitude": 40.7128,
        ///         "longitude": -74.0060,
        ///         "radius": 100,
        ///         "employeeId": "EMP12345"
        ///     },
        ///     {
        ///         "timeOfAttend": "2025-03-20T09:00:00Z",
        ///         "latitude": 34.0522,
        ///         "longitude": -118.2437,
        ///         "radius": 50,
        ///         "employeeId": "EMP67890"
        ///     }
        /// ]
        /// </code>
        /// </response>
        /// <response code="404">
        /// No attendance records found in the system.
        /// Example Response (Not Found):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "No attendance records found."
        /// }
        /// </code>
        /// </response>
        /// <response code="500">
        /// Server error. Returned when an unexpected error occurs while retrieving attendance records.
        /// Example Response (Server Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "An internal server error occurred."
        /// }
        /// </code>
        /// </response>
        [HttpGet]
        [Route("~/Attendance/GetAll")]
        [Authorize(Roles = StaticClass.Admin)]
        public async Task<IActionResult> GetAttendances()
        {
            var attendance = await _attendanceService.GetAllAttendancesAsync();

            if (!attendance.Any())
                return NotFound(new { Success = false, Message = "No attendance records found." });

            return Ok(attendance);
        }

        #endregion


        #region Get Employee Attendances

        /// <summary>
        /// Retrieves all attendance records for a specific employee based on their employee ID.
        /// This endpoint returns a list of attendance records associated with the provided employee ID.
        /// </summary>
        /// <param name="empId">
        /// The unique identifier of the employee whose attendance records are to be retrieved.
        /// Example: "EMP12345"
        /// </param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> containing the list of attendance records for the specified employee or an error message.
        /// </returns>
        /// <response code="200">
        /// Successfully retrieved attendance records for the employee. Returns a success flag and the attendance data.
        /// Example Response (Success):
        /// <code>
        /// {
        ///     "success": true,
        ///     "data": [
        ///         {
        ///             "timeOfAttend": "2025-03-20T08:30:00Z",
        ///             "latitude": 40.7128,
        ///             "longitude": -74.0060,
        ///             "radius": 100,
        ///             "employeeId": "EMP12345"
        ///         },
        ///         {
        ///             "timeOfAttend": "2025-03-21T09:00:00Z",
        ///             "latitude": 40.7128,
        ///             "longitude": -74.0060,
        ///             "radius": 100,
        ///             "employeeId": "EMP12345"
        ///         }
        ///     ]
        /// }
        /// </code>
        /// </response>
        /// <response code="400">
        /// Bad request. Returned when the employee ID is missing or invalid.
        /// Example Response (Invalid Input):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "Employee ID is required."
        /// }
        /// </code>
        /// </response>
        /// <response code="404">
        /// No attendance records found for the specified employee.
        /// Example Response (Not Found):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "No attendance records found for the given employee."
        /// }
        /// </code>
        /// </response>
        /// <response code="500">
        /// Server error. Returned when an unexpected error occurs while retrieving the attendance records.
        /// Example Response (Server Error):
        /// <code>
        /// {
        ///     "success": false,
        ///     "message": "An internal server error occurred."
        /// }
        /// </code>
        /// </response>
        [HttpGet]
        [Route("~/Attendance/{empId}")]
        [Authorize(StaticClass.Admin + "," + StaticClass.User)]
        public async Task<IActionResult> GetEmployeeAttendances(string empId)
        {
            if (string.IsNullOrWhiteSpace(empId))
                return BadRequest(new { Success = false, Message = "Employee ID is required." });

            try
            {
                var attendances = await _attendanceService.GetEmployeeAttendancesAsync(empId);

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

