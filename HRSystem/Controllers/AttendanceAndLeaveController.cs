using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HRSystem.Helper;
using HRSystem.UnitOfWork;
using HRSystem.DTO.AttendanceDTOs;
using HRSystem.DTO.AttendanceAndLeaveDTOs;

namespace HR_System_API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class AttendanceAndLeaveController : ControllerBase
    {

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AttendanceAndLeaveController> logger;
        public AttendanceAndLeaveController(ILogger<AttendanceAndLeaveController> logger, IUnitOfWork unitOfWork)
        {
            this.logger = logger;
            _unitOfWork = unitOfWork;
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
        public async Task<IActionResult> AddAttendance([FromBody] AttendanceDTO attendanceDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                logger.LogWarning("Invalid model state for attendance request. EmployeeId: {EmployeeId}, Errors: {Errors}", attendanceDto?.EmployeeId, string.Join("; ", errors));
                return BadRequest(new { Success = false, Message = "Invalid input data.", Errors = errors });
            }

            try
            {
                var result = await _unitOfWork.AttendanceAndLeaveServices.AddAttendance(attendanceDto);
                logger.LogInformation("Successfully recorded attendance for EmployeeId: {EmployeeId}, UserId", attendanceDto.EmployeeId);
                return Created("","Attendance recorded successfully." );
            }
            catch (ArgumentNullException ex)
            {
                logger.LogWarning("Null attendance data provided. EmployeeId: {EmployeeId}, Error: {Error}", attendanceDto?.EmployeeId, ex.Message);
                return BadRequest(new { Success = false, Message = "Attendance data is required." });
            }
            catch (FormatException ex)
            {
                logger.LogWarning("Invalid time format for attendance. EmployeeId: {EmployeeId}, Error: {Error}", attendanceDto?.EmployeeId, ex.Message);
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning("Failed to record attendance. EmployeeId: {EmployeeId}, Error: {Error}", attendanceDto?.EmployeeId, ex.Message);
                return UnprocessableEntity(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while processing attendance for EmployeeId: {EmployeeId}", attendanceDto?.EmployeeId);
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        #endregion


        #region Take Leave

        [HttpPost]
        [Route("~/Attendance/TakeLeave")]
        [Authorize(Roles = Roles.User)]
        public async Task<IActionResult> AddLeave([FromBody] LeaveDTO leaveDto)
        {
            //_unitOfWork.ShiftAnalysisService.AnalyzePreviousShiftForEmployees(new DateTime(2025, 3, 24, 10, 00, 0));
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                logger.LogWarning("Invalid model state for leave request. EmployeeId: {EmployeeId}, Errors: {Errors}", leaveDto?.EmployeeId, string.Join("; ", errors));
                return BadRequest(new { Success = false, Message = "Invalid input data.", Errors = errors });
            }

            try
            {
                var result = await _unitOfWork.AttendanceAndLeaveServices.AddLeave(leaveDto);
                logger.LogInformation("Successfully recorded leave for EmployeeId: {EmployeeId}", leaveDto.EmployeeId);
                return Created("", new { Success = true, EmployeeId = leaveDto.EmployeeId, Message = "Leave recorded successfully." });
            }
            catch (ArgumentNullException ex)
            {
                logger.LogWarning("Null leave data provided. EmployeeId: {EmployeeId}, Error: {Error}", leaveDto?.EmployeeId, ex.Message);
                return BadRequest(new { Success = false, Message = "Leave data is required." });
            }
            catch (FormatException ex)
            {
                logger.LogWarning("Invalid time format for leave. EmployeeId: {EmployeeId}, Error: {Error}", leaveDto?.EmployeeId, ex.Message);
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning("Failed to record leave. EmployeeId: {EmployeeId}, Error: {Error}", leaveDto?.EmployeeId, ex.Message);
                return UnprocessableEntity(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while processing leave for EmployeeId: {EmployeeId}", leaveDto?.EmployeeId);
                return StatusCode(500, new { Success = false, Message = ex.Message });
            }
        }

        #endregion


        #region Take Leave By Admin
        [HttpPost]
        [Route("~/Attendance/TakeLeaveByAdmin")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> AddLeaveByAdmin([FromBody] LeaveByAdminDTO leaveDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                logger.LogWarning("Invalid model state for leave request. EmployeeId: {EmployeeId}, Errors: {Errors}", leaveDto?.EmployeeId, string.Join("; ", errors));
                return BadRequest(new { Success = false, Message = "Invalid input data.", Errors = errors });
            }
            try
            {
                var result = await _unitOfWork.AttendanceAndLeaveServices.AddLeaveByAdmin(leaveDto);
                logger.LogInformation("Successfully recorded leave for EmployeeId: {EmployeeId}", leaveDto.EmployeeId);
                return Ok("Leave recorded successfully.");
            }
            catch (ArgumentNullException ex)
            {
                logger.LogWarning("Null leave data provided. EmployeeId: {EmployeeId}, Error: {Error}", leaveDto?.EmployeeId, ex.Message);
                return BadRequest(new { Success = false, Message = "Leave data is required." });
            }
            catch (FormatException ex)
            {
                logger.LogWarning("Invalid time format for leave. EmployeeId: {EmployeeId}, Error: {Error}", leaveDto?.EmployeeId, ex.Message);
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                logger.LogWarning("Failed to record leave. EmployeeId: {EmployeeId}, Error: {Error}", leaveDto?.EmployeeId, ex.Message);
                return UnprocessableEntity(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while processing leave for EmployeeId: {EmployeeId}", leaveDto?.EmployeeId);
                return StatusCode(500, new { Success = false, Message = "An unexpected error occurred. Please try again later." });
            }
        }

        #endregion

    }
}

