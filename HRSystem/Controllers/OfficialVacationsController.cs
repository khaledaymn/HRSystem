using HRSystem.DTO;
using HRSystem.Helper;
using HRSystem.Services.OfficialVacationServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HRSystem.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class OfficialVacationsController : ControllerBase
    {
        private readonly IOfficialVacationServices _officialVacationServices;

        public OfficialVacationsController(IOfficialVacationServices officialVacationServices) => _officialVacationServices = officialVacationServices;

        #region Add Official Vacation
        /// <summary>
        /// Adds a new official vacation to the system.
        /// This endpoint allows an admin to create an official vacation by providing the necessary details.
        /// </summary>
        /// <param name="vacation">
        /// The official vacation details, including the name and date.
        /// Example Request:
        /// <code>
        /// {
        ///     "vacationName": "Christmas Day",
        ///     "vacationDay": "2025-12-25T00:00:00"
        /// }
        /// </code>
        /// </param>
        /// <returns>
        /// Returns an <see cref="ActionResult"/> indicating the result of the vacation creation operation.
        /// </returns>
        /// <response code="201">
        /// Official vacation added successfully. Returns a confirmation message.
        /// Example Response (Success):
        /// <code>
        /// {
        ///     "success": true,
        ///     "message": "Official vacation added successfully."
        /// }
        /// </code>
        /// </response>
        /// <response code="400">
        /// Bad request. Returned when the provided model is invalid (e.g., missing required fields like vacation name or date).
        /// Example Response (Failure):
        /// <code>
        /// {
        ///     "vacationName": [
        ///         "Vacation name is required"
        ///     ],
        ///     "vacationDay": [
        ///         "Vacation day is required"
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
        ///     "message": "An error occurred while adding the official vacation."
        /// }
        /// </code>
        /// </response>
        [HttpPost]
        [Route("~/OfficialVacations/Create")]
        [Authorize(Roles = StaticClass.Admin)]
        public async Task<ActionResult> AddOfficialVacationAsync([FromBody] CreateOfficialVacationDTO vacation)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _officialVacationServices.AddOfficialVacationAsync(vacation);
                return Created("~/OfficialVacations/Create", new { Success = true, Message = "Official vacation added successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while adding the official vacation." });
            }
        }

        #endregion


        #region Get All Official Vacations

        /// <summary>
        /// Retrieves all official vacations from the system.
        /// This endpoint allows an admin to retrieve a list of all official vacations stored in the system.
        /// Example Request:
        /// <code>
        /// GET ~/OfficialVacations/GetAll
        /// </code>
        /// </summary>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> containing the list of official vacations or an appropriate status code.
        /// </returns>
        /// <response code="200">
        /// Successfully retrieved the list of official vacations.
        /// Example Response (Success):
        /// <code>
        /// [
        ///     {
        ///         "id": 1,
        ///         "vacationName": "Christmas Day",
        ///         "vacationDay": "2025-12-25T00:00:00"
        ///     },
        ///     {
        ///         "id": 2,
        ///         "vacationName": "New Year's Day",
        ///         "vacationDay": "2026-01-01T00:00:00"
        ///     }
        /// ]
        /// </code>
        /// </response>
        /// <response code="204">
        /// No official vacations found in the system.
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
        ///     "message": "An error occurred while retrieving official vacations."
        /// }
        /// </code>
        /// </response>
        [HttpGet]
        [Route("~/OfficialVacations/GetAll")]
        [Authorize(Roles = StaticClass.Admin)]
        public async Task<IActionResult> GetAllOfficialVacation()
        {
            try
            {
                var vacations = await _officialVacationServices.GetAllOfficialVacationsAsync();

                if (vacations == null || !vacations.Any())
                    return NoContent();

                return Ok(vacations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while retrieving official vacations." });
            }
        }

        #endregion


        #region Get Official Vacation By Id

        /// <summary>
        /// Retrieves an official vacation by its ID.
        /// This endpoint allows an admin to retrieve the details of a specific official vacation by providing its ID.
        /// </summary>
        /// <param name="id">The ID of the official vacation to retrieve.</param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> containing the official vacation details or an appropriate status code.
        /// </returns>
        /// <response code="200">
        /// Successfully retrieved the official vacation.
        /// Example Response (Success):
        /// <code>
        /// {
        ///     "id": 1,
        ///     "vacationName": "Christmas Day",
        ///     "vacationDay": "2025-12-25T00:00:00"
        /// }
        /// </code>
        /// </response>
        /// <response code="404">
        /// Official vacation not found.
        /// Example Response (Not Found):
        /// <code>
        /// "No vacation found with ID 1."
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
        ///     "message": "An error occurred while retrieving the official vacation."
        /// }
        /// </code>
        /// </response>
        [HttpGet]
        [Route("~/OfficialVacations/GetById/{id}")]
        [Authorize(Roles = StaticClass.Admin)]
        public async Task<IActionResult> GetOfficialVacationByIdAsync(int id)
        {
            try
            {
                var vacation = await _officialVacationServices.GetOfficialVacationByIdAsync(id);

                if (vacation == null)
                    return NotFound($"No vacation found with ID {id}.");

                return Ok(vacation);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while retrieving the official vacation." });
            }
        }


        #endregion


        #region Update Official Vacation

        /// <summary>
        /// Updates an existing official vacation by its ID.
        /// This endpoint allows an admin to update the details of a specific official vacation by providing its ID and updated details.
        /// </summary>
        /// <param name="id">The ID of the official vacation to update.</param>
        /// <param name="vacation">
        /// The updated official vacation details, including the ID, name, and date (as defined in <see cref="OfficialVacationDTO"/>).
        /// Example Request:
        /// <code>
        /// PUT ~/OfficialVacations/Edit/1
        /// {
        ///     "id": 1,
        ///     "vacationName": "Updated Christmas Day",
        ///     "vacationDay": "2025-12-25T00:00:00"
        /// }
        /// </code>
        /// </param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> containing the updated official vacation details or an appropriate status code.
        /// </returns>
        /// <response code="200">
        /// Successfully updated the official vacation.
        /// Example Response (Success):
        /// <code>
        /// {
        ///     "id": 1,
        ///     "vacationName": "Updated Christmas Day",
        ///     "vacationDay": "2025-12-25T00:00:00"
        /// }
        /// </code>
        /// </response>
        /// <response code="400">
        /// Bad request. Returned when the ID in the route does not match the ID in the body, or the model is invalid.
        /// Example Response (ID Mismatch):
        /// <code>
        /// "ID mismatch between route and body."
        /// </code>
        /// Example Response (Invalid Model):
        /// <code>
        /// {
        ///     "vacationName": [
        ///         "Vacation name is required"
        ///     ],
        ///     "vacationDay": [
        ///         "Vacation day is required"
        ///     ]
        /// }
        /// </code>
        /// </response>
        /// <response code="404">
        /// Official vacation not found.
        /// Example Response (Not Found):
        /// <code>
        /// "Official vacation with ID 1 not found."
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
        ///     "message": "An error occurred while updating the official vacation."
        /// }
        /// </code>
        /// </response>
        [HttpPut]
        [Route("~/OfficialVacations/Edit/{id}")]
        [Authorize(Roles = StaticClass.Admin)]
        public async Task<IActionResult> UpdateOfficialVacationAsync(int id, [FromBody] OfficialVacationDTO vacation)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                if (id != vacation.Id)
                    return BadRequest("ID mismatch between route and body.");

                var updatedVacation = await _officialVacationServices.UpdateOfficialVacationAsync(id, vacation);

                return Ok(updatedVacation);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while updating the official vacation." });
            }
        }

        #endregion


        #region Delete Official Vacation

        /// <summary>
        /// Deletes an official vacation by its ID.
        /// This endpoint allows an admin to delete a specific official vacation by providing its ID.
        /// </summary>
        /// <param name="id">
        /// The ID of the official vacation to delete.
        /// </param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> indicating the result of the deletion operation.
        /// </returns>
        /// <response code="204">
        /// Official vacation deleted successfully.
        /// </response>
        /// <response code="404">
        /// Official vacation not found.
        /// Example Response (Not Found):
        /// <code>
        /// "No vacation found with ID 1."
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
        ///     "message": "An error occurred while deleting the official vacation."
        /// }
        /// </code>
        /// </response>
        [HttpDelete]
        [Route("~/OfficialVacations/Delete/{id}")]
        [Authorize(Roles = StaticClass.Admin)]
        public async Task<IActionResult> DeleteOfficialVacationAsync(int id)
        {
            try
            {
                var result = await _officialVacationServices.DeleteOfficialVacationAsync(id);

                if (!result)
                    return NotFound($"No vacation found with ID {id}.");

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while deleting the official vacation." });
            }
        }


        #endregion


        #region Check Official Vacation

        /// <summary>
        /// Checks if a specific date is an official vacation.
        /// This endpoint allows an admin to check if a given date is marked as an official vacation in the system.
        /// </summary>
        /// <param name="date">
        /// The date to check (in ISO 8601 format, e.g., "2025-12-25T00:00:00").
        /// <code>
        /// Example Request:
        /// GET /api/official-vacations/check?date=2025-12-25T00:00:00
        /// </code>
        /// </param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> indicating whether the specified date is an official vacation.
        /// </returns>
        /// <response code="200">
        /// Successfully checked the date.
        /// Example Response (Is Vacation):
        /// <code>
        /// {
        ///     "date": "2025-12-25T00:00:00",
        ///     "isVacation": true
        /// }
        /// </code>
        /// Example Response (Not a Vacation):
        /// <code>
        /// {
        ///     "date": "2025-12-26T00:00:00",
        ///     "isVacation": false
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
        ///     "message": "An error occurred while checking the official vacation."
        /// }
        /// </code>
        /// </response>
        [HttpGet]
        [Route("official-vacations/check")]
        [Authorize(Roles = StaticClass.Admin)]
        public async Task<IActionResult> IsOfficialVacationAsync([FromQuery] DateTime date)
        {
            try
            {
                var isVacation = await _officialVacationServices.IsOfficialVacationAsync(date);
                return Ok(new { date, isVacation });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Success = false, Message = "An error occurred while checking the official vacation." });
            }
        }


        #endregion


    }
}
