using HRSystem.DTO.ShiftDTOs;
using HRSystem.Helper;
using HRSystem.Services.ShiftServices;
using HRSystem.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HRSystem.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ShiftsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public ShiftsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        #region Create Shift

        /// <summary>
        /// Creates a new shift for an employee.
        /// </summary>
        /// <param name="shiftDto">The data transfer object containing the shift details to create.</param>
        /// <returns>
        /// Returns the created shift details if successful, or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// This endpoint creates a new shift and requires Admin role authorization. The request body must contain valid shift details.
        /// 
        /// Example Request:
        /// ```json
        /// {
        ///   "startTime": "09:00",
        ///   "endTime": "17:00",
        ///   "employeeId": "12345"
        /// }
        /// ```
        /// </remarks>
        /// <response code="201">
        /// Returns the created shift details when the request is successful.
        /// ```json
        /// {
        ///   "startTime": "09:00",
        ///   "endTime": "17:00",
        ///   "employeeId": "12345"
        /// }
        /// ```
        /// </response>
        /// <response code="400">
        /// Returned when the request model is invalid or the shift creation fails.
        /// ```json
        /// "Failed to create shift. Please check the provided data."
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
        [HttpPost]
        [Route("~/Shifts/Create")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> CreateShift([FromBody] AddShiftDTO shiftDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _unitOfWork.ShiftServices.CreateShiftAsync(shiftDto);
            if (!result)
            {
                return BadRequest("Failed to create shift. Please check the provided data.");
            }

            return CreatedAtAction(nameof(GetShiftsByEmployeeId), new { employeeId = shiftDto.EmployeeId }, shiftDto);
        }

        #endregion


        #region Update Shift

        /// <summary>
        /// Updates an existing shift in the system.
        /// </summary>
        /// <param name="shiftDto">The data transfer object containing the updated shift details.</param>
        /// <returns>
        /// Returns a success message if the shift is updated, or an error message if the shift is not found or the operation fails.
        /// </returns>
        /// <remarks>
        /// This endpoint updates a shift and requires Admin role authorization. The request body must contain valid shift details.
        /// 
        /// Example Request:
        /// ```json
        /// {
        ///   "id": 1,
        ///   "startTime": "10:00",
        ///   "endTime": "18:00",
        ///   "employeeId": "12345"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">
        /// Returns a success message when the shift is updated successfully.
        /// ```json
        /// "Shift updated successfully."
        /// ```
        /// </response>
        /// <response code="400">
        /// Returned when the request model is invalid.
        /// ```json
        /// {
        ///   "startTime": [
        ///     "The StartTime field is required."
        ///   ]
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
        /// Returned when the shift is not found or the update fails.
        /// ```json
        /// "Shift not found or update failed."
        /// ```
        /// </response>
        [HttpPut]
        [Route("~/Shifts/Update")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> UpdateShift([FromBody] ShiftDTO shiftDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _unitOfWork.ShiftServices.UpdateShiftAsync(shiftDto);
            if (!result)
            {
                return NotFound($"Shift not found or update failed.");
            }

            return Ok("Shift updated successfully.");
        }

        #endregion


        #region Delete Shift

        /// <summary>
        /// Deletes a shift for an employee.
        /// </summary>
        /// <param name="dto">The data transfer object containing the shift ID and employee ID to delete.</param>
        /// <returns>
        /// Returns a success message if the shift is deleted, or an error message if the shift is not found or the operation fails.
        /// </returns>
        /// <remarks>
        /// This endpoint deletes a shift and requires Admin role authorization. The request body must contain valid shift and employee IDs.
        /// 
        /// Example Request:
        /// ```json
        /// {
        ///   "shiftId": 1,
        ///   "employeeId": "12345"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">
        /// Returns a success message when the shift is deleted successfully.
        /// ```json
        /// "Shift deleted successfully."
        /// ```
        /// </response>
        /// <response code="400">
        /// Returned when the request model is invalid.
        /// ```json
        /// {
        ///   "shiftId": [
        ///     "The ShiftId field is required."
        ///   ]
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
        /// Returned when the shift is not found or deletion fails.
        /// ```json
        /// "Shift with ID 1 for EmployeeId 12345 not found or deletion failed."
        /// ```
        /// </response>
        [HttpDelete]
        [Route("~/Shifts/Delete")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> DeleteShift([FromBody] DeleteShiftDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _unitOfWork.ShiftServices.DeleteShiftAsync(dto);
            if (!result)
            {
                return NotFound($"Shift with ID {dto.ShiftId} for EmployeeId {dto.EmployeeId} not found or deletion failed.");
            }

            return Ok("Shift deleted successfully.");
        }

        #endregion


        #region Get Shifts by EmployeeId

        /// <summary>
        /// Retrieves all shifts for a specific employee by their ID.
        /// </summary>
        /// <param name="employeeId">The unique identifier of the employee whose shifts are to be retrieved.</param>
        /// <returns>
        /// Returns a list of shifts if found, or an error message if no shifts exist or the operation fails.
        /// </returns>
        /// <remarks>
        /// This endpoint retrieves shifts for an employee and requires Admin role authorization.
        /// </remarks>
        /// <response code="200">
        /// Returns a list of shifts when the request is successful.
        /// ```json
        /// [
        ///   {
        ///     "id": 1,
        ///     "startTime": "09:00",
        ///     "endTime": "17:00",
        ///     "employeeId": "12345"
        ///   }
        /// ]
        /// ```
        /// </response>
        /// <response code="400">
        /// Returned when the employee ID is empty or invalid.
        /// ```json
        /// "EmployeeId cannot be empty."
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
        /// Returned when no shifts are found for the specified employee ID.
        /// ```json
        /// "No shifts found for EmployeeId 12345."
        /// ```
        /// </response>
        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        [Route("~/Shifts/GetByEmployeeId/{employeeId}")]
        public async Task<IActionResult> GetShiftsByEmployeeId(string employeeId)
        {
            if (string.IsNullOrEmpty(employeeId))
            {
                return BadRequest("EmployeeId cannot be empty.");
            }

            var shifts = await _unitOfWork.ShiftServices.GetByEmployeeId(employeeId);
            if (shifts == null || !shifts.Any())
            {
                return NotFound($"No shifts found for EmployeeId {employeeId}.");
            }

            return Ok(shifts);
        }
        #endregion


        #region Get Shift by Id

        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        [Route("~/Shifts/GetById/{id}")]
        public async Task<IActionResult> GetShiftById(int id)
        {
            
            var shift = await _unitOfWork.ShiftServices.GetShiftByIdAsync(id);
            if (shift == null)
            {
                return NotFound($"Shift with ID {id} not found.");
            }
            return Ok(shift);
        }

        #endregion
    }
}