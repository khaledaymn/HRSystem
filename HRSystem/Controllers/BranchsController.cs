using HRSystem.DTO.BranchDTOs;
using HRSystem.Helper;
using HRSystem.Services.BranchServices;
using HRSystem.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HRSystem.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class BranchsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BranchsController> _logger;
        public BranchsController(ILogger<BranchsController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        #region Get All Branches
        /// <summary>
        /// Retrieves a list of all branches in the system.
        /// </summary>
        /// <returns>
        /// Returns a list of branch details if successful, or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// This endpoint retrieves all branches available in the system. It requires Admin role authorization.
        /// No request body is needed as it’s a GET request.
        /// </remarks>
        /// <response code="200">
        /// Returns a list of all branches when the request is successful.
        /// ```json
        /// [
        ///   {
        ///     "id": 1,
        ///     "name": "Main Branch",
        ///     "latitude": 40.7128,
        ///     "longitude": -74.0060
        ///   },
        ///   {
        ///     "id": 2,
        ///     "name": "Downtown Branch",
        ///     "latitude": 34.0522,
        ///     "longitude": -118.2437
        ///   }
        /// ]
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
        /// <response code="500">
        /// Returned when an unexpected server error occurs during processing or if the branch service returns null.
        /// ```json
        /// {
        ///   "message": "An error occurred while retrieving branches.",
        ///   "error": "Exception details here"
        /// }
        /// ```
        /// </response>
        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        [Route("~/Branchs/GetAll")]
        public async Task<IActionResult> GetAllBranches()
        {
            _logger.LogInformation("Received request to retrieve all branches.");

            try
            {
                var branches = await _unitOfWork.BranchServices.GetAllBranchesAsync();

                if (branches == null)
                {
                    _logger.LogWarning("Branch service returned null.");
                    return StatusCode(StatusCodes.Status500InternalServerError, "Unexpected error occurred while retrieving branches.");
                }

                _logger.LogInformation("Successfully retrieved {BranchCount} branches.", branches.Count);
                return Ok(branches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve branches. Error: {Message}", ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "An error occurred while retrieving branches.",
                    Error = ex.Message
                });
            }
        }

        #endregion


        #region Get Branch By Id
        /// <summary>
        /// Retrieves the details of a specific branch by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the branch to retrieve.</param>
        /// <returns>
        /// Returns the branch details if found, or an error message if the branch does not exist or the operation fails.
        /// </returns>
        /// <remarks>
        /// This endpoint retrieves a single branch based on the provided ID. The ID must be a valid integer.
        /// </remarks>
        /// <response code="200">
        /// Returns the branch details when the request is successful.
        /// ```json
        /// {
        ///   "id": 1,
        ///   "name": "Main Branch",
        ///   "latitude": 40.7128,
        ///   "longitude": -74.0060
        /// }
        /// ```
        /// </response>
        /// <response code="404">
        /// Returned when the branch with the specified ID is not found.
        /// ```json
        /// {
        ///   "message": "Branch with ID 1 not found."
        /// }
        /// ```
        /// </response>
        /// <response code="500">
        /// Returned when an unexpected server error occurs during processing.
        /// ```json
        /// {
        ///   "message": "An error occurred while retrieving the branch.",
        ///   "error": "Exception details here"
        /// }
        /// ```
        /// </response>
        [HttpGet]
        [Route("~/Branchs/GetById")]
        [Authorize(Roles = Roles.Admin + ", " + Roles.User)]
        public async Task<IActionResult> GetBranchById(int id)
        {

            try
            {
                var branch = await _unitOfWork.BranchServices.GetBranchByIdAsync(id);

                if (branch == null)
                {
                    _logger.LogWarning("Branch with ID {BranchId} not found.", id);
                    return NotFound(new { Message = $"Branch with ID {id} not found." });
                }

                _logger.LogInformation("Successfully retrieved branch with ID: {BranchId}", id);
                return Ok(branch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve branch with ID: {BranchId}. Error: {Message}", id, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "An error occurred while retrieving the branch.",
                    Error = ex.Message
                });
            }
        }

        #endregion


        #region Create Branch
        /// <summary>
        /// Creates a new branch in the system.
        /// </summary>
        /// <param name="branchDto">The data transfer object containing the details of the branch to create.</param>
        /// <returns>
        /// Returns the created branch details with its ID if successful, or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// This endpoint creates a new branch and requires Admin role authorization. The request body must contain valid branch details.
        /// 
        /// Example Request:
        /// ```json
        /// {
        ///   "name": "New Branch",
        ///   "latitude": 37.7749,
        ///   "longitude": -122.4194
        /// }
        /// ```
        /// </remarks>
        /// <response code="201">
        /// Returns the created branch details when the request is successful.
        /// ```json
        /// {
        ///   "id": 3,
        ///   "name": "New Branch",
        ///   "latitude": 37.7749,
        ///   "longitude": -122.4194
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
        /// <response code="500">
        /// Returned when an unexpected server error occurs during processing or if branch creation returns null.
        /// ```json
        /// {
        ///   "message": "An error occurred while creating the branch.",
        ///   "error": "Exception details here"
        /// }
        /// ```
        /// </response>
        [HttpPost]
        [Route("~/Branchs/CreateBranch")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> CreateBranch([FromBody] AddBranchDTO branchDto)
        {
            try
            {
                var createdBranch = await _unitOfWork.BranchServices.CreateAsync(branchDto);

                if (createdBranch == null)
                {
                    _logger.LogError("Branch creation returned null unexpectedly.");
                    return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Failed to create branch due to an unexpected error." });
                }

                _logger.LogInformation("Successfully created branch with ID: {BranchId}", createdBranch.Id);
                return CreatedAtAction(nameof(GetBranchById), new { id = createdBranch.Id }, createdBranch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create branch with name: {BranchName}. Error: {Message}", branchDto.Name, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "An error occurred while creating the branch.",
                    Error = ex.Message 
                });
            }
        }

        #endregion


        #region Update Branch

        /// <summary>
        /// Updates an existing branch in the system.
        /// </summary>
        /// <param name="branchDto">The data transfer object containing the updated branch details.</param>
        /// <returns>
        /// Returns a success message and the updated branch if successful, or an error message if the branch is not found or the operation fails.
        /// </returns>
        /// <remarks>
        /// This endpoint updates a branch based on the provided details. The request body must contain a valid branch ID and updated fields.
        /// 
        /// Example Request:
        /// ```json
        /// {
        ///   "id": 1,
        ///   "name": "Updated Main Branch",
        ///   "latitude": 40.7128,
        ///   "longitude": -74.0060
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">
        /// Returns a success message and the updated branch details when the request is successful.
        /// ```json
        /// {
        ///   "message": "Successfully updated branch",
        ///   "updatedBranch": {
        ///     "id": 1,
        ///     "name": "Updated Main Branch",
        ///     "latitude": 40.7128,
        ///     "longitude": -74.0060
        ///   }
        /// }
        /// ```
        /// </response>
        /// <response code="404">
        /// Returned when the branch with the specified ID is not found.
        /// ```json
        /// {
        ///   "message": "Branch with ID 1 not found."
        /// }
        /// ```
        /// </response>
        /// <response code="500">
        /// Returned when an unexpected server error occurs during processing.
        /// ```json
        /// {
        ///   "message": "An error occurred while updating the branch.",
        ///   "error": "Exception details here"
        /// }
        /// ```
        /// </response>
        [HttpPut]
        [Route("~/Branchs/UpdateBranch")]
        public async Task<IActionResult> UpdateBranch([FromBody] BranchDTO branchDto)
        {
            try
            {
                var updatedBranch = await _unitOfWork.BranchServices.UpdateAsync(branchDto);

                if (updatedBranch == null)
                {
                    _logger.LogWarning("Branch with ID {BranchId} not found.", branchDto.Id);
                    return NotFound(new { Message = $"Branch with ID {branchDto.Id} not found." });
                }

                _logger.LogInformation("Successfully updated branch with ID: {BranchId}", branchDto.Id);
                return Ok(new { Message = "Successfully updated branch", updatedBranch });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update branch with ID: {BranchId}. Error: {Message}", branchDto.Id, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "An error occurred while updating the branch.",
                    Error = ex.Message
                });
            }
        }

        #endregion


        #region Delete Branch

        /// <summary>
        /// Deletes a branch from the system by its ID.
        /// </summary>
        /// <param name="id">The unique identifier of the branch to delete.</param>
        /// <returns>
        /// Returns a success message if the branch is deleted, or an error message if the branch is not found or the operation fails.
        /// </returns>
        /// <remarks>
        /// This endpoint deletes a branch based on the provided ID.
        /// </remarks>
        /// <response code="200">
        /// Returns a success message when the branch is deleted successfully.
        /// ```json
        /// "Successfully deleted branch"
        /// ```
        /// </response>
        /// <response code="404">
        /// Returned when the branch with the specified ID is not found.
        /// ```json
        /// {
        ///   "message": "Branch with ID 1 not found."
        /// }
        /// ```
        /// </response>
        /// <response code="500">
        /// Returned when an unexpected server error occurs during processing.
        /// ```json
        /// {
        ///   "message": "An error occurred while deleting the branch.",
        ///   "error": "Exception details here"
        /// }
        /// ```
        /// </response>
        [HttpDelete]
        [Route("~/Branchs/DeleteBranch/{id}")]
        public async Task<IActionResult> DeleteBranch(int id)
        {
            try
            {
                var result = await _unitOfWork.BranchServices.DeleteAsync(id);

                if (!result)
                {
                    _logger.LogWarning("Branch with ID {BranchId} not found.", id);
                    return NotFound(new { Message = $"Branch with ID {id} not found." });
                }

                _logger.LogInformation("Successfully deleted branch with ID: {BranchId}", id);
                return Ok("Successfully deleted branch"); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete branch with ID: {BranchId}. Error: {Message}", id, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "An error occurred while deleting the branch.",
                    Error = ex.Message
                });
            }
        }

        #endregion
    }
}
