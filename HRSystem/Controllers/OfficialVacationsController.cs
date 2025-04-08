using HRSystem.DTO.OfficialVacationDTOs;
using HRSystem.Helper;
using HRSystem.Services.OfficialVacationServices;
using HRSystem.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HRSystem.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class OfficialVacationsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OfficialVacationsController> _logger;
        public OfficialVacationsController(ILogger<OfficialVacationsController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

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
        [Authorize(Roles = Roles.Admin)]
        public async Task<ActionResult> AddOfficialVacationAsync([FromBody] CreateOfficialVacationDTO vacation)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for creating official vacation: {Errors}",
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
                var createdVacation = await _unitOfWork.OfficialVacationServices.AddOfficialVacationAsync(vacation);

                _logger.LogInformation("Successfully created official vacation with ID: {VacationId}", createdVacation.Id);
                return Created(
                    "",
                    new
                    {
                        Success = true,
                        Message = "Official vacation added successfully.",
                        Data = createdVacation
                    });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid data for creating official vacation: {Message}", ex.Message);
                return BadRequest(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create official vacation with name: {VacationName}. Error: {Message}", vacation?.VacationName, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while adding the official vacation.",
                    Error = ex.Message 
                });
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
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetAllOfficialVacation()
        {
            _logger.LogInformation("Received request to retrieve all official vacations.");

            try
            {
                var vacations = await _unitOfWork.OfficialVacationServices.GetAllOfficialVacationsAsync();

                if (vacations == null || !vacations.Any())
                {
                    _logger.LogInformation("No official vacations available.");
                    return NotFound("No official vacations found in the database."); 
                }

                _logger.LogInformation("Successfully retrieved {VacationCount} official vacations.", vacations.Count());
                return Ok(new
                {
                    Success = true,
                    Message = "Official vacations retrieved successfully.",
                    Data = vacations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve official vacations. Error: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while retrieving official vacations.",
                    Error = ex.Message 
                });
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
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetOfficialVacationByIdAsync(int id)
        {
            try
            {
                var vacation = await _unitOfWork.OfficialVacationServices.GetOfficialVacationByIdAsync(id);

                if (vacation == null)
                {
                    _logger.LogWarning("Official vacation with ID {VacationId} not found.", id);
                    return NotFound(new { Success = false, Message = $"No official vacation found with ID {id}." });
                }

                _logger.LogInformation("Successfully retrieved official vacation with ID: {VacationId}", id);
                return Ok(new
                {
                    Success = true,
                    Message = "Official vacation retrieved successfully.",
                    Data = vacation
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve official vacation with ID: {VacationId}. Error: {Message}", id, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while retrieving the official vacation.",
                    Error = ex.Message 
                });
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
        [Route("~/OfficialVacations/Edit")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> UpdateOfficialVacationAsync(OfficialVacationDTO vacation)
        {
            try
            {
                var updatedVacation = await _unitOfWork.OfficialVacationServices.UpdateOfficialVacationAsync(vacation);

                _logger.LogInformation("Successfully updated official vacation with ID: {VacationId}", vacation.Id);
                return Ok(new
                {
                    Success = true,
                    Message = "Official vacation updated successfully.",
                    Data = updatedVacation
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Official vacation with ID {VacationId} not found.", vacation.Id);
                return NotFound(new { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update official vacation with ID: {VacationId}. Error: {Message}", vacation.Id, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while updating the official vacation.",
                    Error = ex.Message
                });
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
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> DeleteOfficialVacationAsync(int id)
        {
            try
            {
                var result = await _unitOfWork.OfficialVacationServices.DeleteOfficialVacationAsync(id);

                if (!result)
                {
                    _logger.LogWarning("Official vacation with ID {VacationId} not found.", id);
                    return NotFound(new { Success = false, Message = $"No official vacation found with ID {id}." });
                }

                _logger.LogInformation("Successfully deleted official vacation with ID: {VacationId}", id);
                return NoContent(); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete official vacation with ID: {VacationId}. Error: {Message}", id, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while deleting the official vacation.",
                    Error = ex.Message
                });
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
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> IsOfficialVacationAsync([FromQuery] DateTime date)
        {
            try
            {
                var isVacation = await _unitOfWork.OfficialVacationServices.IsOfficialVacationAsync(date);

                _logger.LogInformation("Successfully checked {Date}: {Status} an official vacation.",
                    date.ToString("yyyy-MM-dd"), isVacation ? "confirmed as" : "not");
                return Ok(new
                {
                    Success = true,
                    Message = $"Date {date:yyyy-MM-dd} is {(isVacation ? "" : "not ")}an official vacation.",
                    Data = new { Date = date.ToString("yyyy-MM-dd"), IsVacation = isVacation }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if {Date} is an official vacation. Error: {Message}",
                    date.ToString("yyyy-MM-dd"), ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Success = false,
                    Message = "An error occurred while checking the official vacation.",
                    Error = ex.Message
                });
            }
        }


        #endregion

    }
}
