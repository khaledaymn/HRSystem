using HRSystem.DTO.AuthenticationDTOs;
using HRSystem.Services.AuthenticationServices;
using HRSystem.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HRSystem.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly ILogger<AuthenticationServices> _logger;
        private readonly IUnitOfWork _unitOfWork;
        public AuthenticationController(ILogger<AuthenticationServices> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        #region Login
        /// <summary>
        /// Authenticates a user and returns authentication details including a token if successful.
        /// </summary>
        /// <param name="model">The login data transfer object containing the user's email and password.</param>
        /// <returns>
        /// Returns an authentication result with a token if successful, or an error message if authentication fails.
        /// </returns>
        /// <remarks>
        /// This endpoint validates the user's credentials and returns an authentication token if the login is successful.
        /// The request body must contain a valid email and password.
        /// 
        /// Example Request:
        /// ```json
        /// {
        ///   "email": "user@example.com",
        ///   "password": "P@ssw0rd123"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">
        /// Returns the authentication details including token when login is successful.
        /// Successful Response (200 OK):
        /// ```json
        /// {
        ///   "message": "Login successful",
        ///   "id": "12345",
        ///   "name": "John Doe",
        ///   "email": "user@example.com",
        ///   "phoneNumber": "123-456-7890",
        ///   "address": "123 Main St",
        ///   "nationalId": "ABC123456",
        ///   "baseSalary": 50000.0,
        ///   "shift": [
        ///     {
        ///       "id": 1,
        ///       "startTime": "08:00",
        ///       "endTime": "16:00",
        ///       "employeeId": "12345"
        ///     }
        ///   ],
        ///   "gender": "Male",
        ///   "branch": {
        ///     "id": 1,
        ///     "name": "Main Branch",
        ///     "latitude": 40.7128,
        ///     "longitude": -74.0060
        ///   },
        ///   "hiringDate": "2023-01-15",
        ///   "dateOfBarth": "1990-05-20",
        ///   "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        ///   "roles": ["User"]
        /// }
        /// </response>
        /// ```
        /// <response code="400">
        /// Returned when the login credentials are invalid or the request is malformed.
        /// Bad Request Response (400):
        /// ```json
        /// {
        ///   "message": "Invalid email or password"
        /// }
        /// </response>
        /// <response code="500">
        /// Returned when an unexpected server error occurs during processing.
        /// Server Error Response (500):
        /// ```json
        /// {
        ///   "message": "An error occurred while processing your request",
        ///   "error": "Exception details here"
        /// }
        /// ```
        /// </response>
        [HttpPost]
        [Route("~/Account/Login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid login model state for email: {Email}", model.Email);
                    return BadRequest(ModelState);
                }

                var result = await _unitOfWork.AuthenticationService.Login(model);

                if (!result.IsAuthenticated)
                {
                    _logger.LogWarning("Authentication failed for email: {Email}. Message: {Message}", model.Email, result.Message);
                    return BadRequest(result.Message);
                }

                _logger.LogInformation("Login successful for email: {Email}", model.Email);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing login request for email: {Email}", model.Email);
                return StatusCode(500, new { message = "An error occurred while processing your request", error = ex.Message });
            }
        }

        #endregion


        #region Forget Password
        /// <summary>
        /// Initiates a password reset process for a user based on their email.
        /// </summary>
        /// <param name="model">The forget password data transfer object containing the user's email.</param>
        /// <returns>
        /// Returns a success message if the request is processed successfully, or an error message if it fails.
        /// </returns>
        /// <remarks>
        /// This endpoint triggers a password reset process for the specified email. If the email exists, a reset link or code will be sent.
        /// The request body must contain a valid email.
        /// 
        /// Example Request:
        /// ```json
        /// {
        ///   "email": "user@example.com"
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">
        /// Returns a success message when the forget password request is processed successfully.
        /// ```json
        /// "success"
        /// ```
        /// </response>
        /// <response code="400">
        /// Returned when the request model is invalid or malformed.
        /// ```json
        /// {
        ///   "email": [
        ///     "The Email field is required."
        ///   ]
        /// }
        /// ```
        /// </response>
        /// <response code="404">
        /// Returned when the email is not found or the forget password process fails.
        /// ```json
        /// "Email not found"
        /// ```
        /// </response>
        /// <response code="500">
        /// Returned when an unexpected server error occurs during processing.
        /// ```json
        /// {
        ///   "message": "An error occurred while processing your request",
        ///   "error": "Exception details here"
        /// }
        /// ```
        /// </response>
        [HttpPost]
        [Route("~/Account/ForgetPassword")]
        public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid forget password model state for email: {Email}", model.Email);
                    return BadRequest(ModelState);
                }

                var result = await _unitOfWork.AuthenticationService.ForgetPassword(model);

                if (result != "success")
                {
                    _logger.LogWarning("Forget password failed for email: {Email}. Message: {Message}", model.Email, result);
                    return NotFound(result);
                }

                _logger.LogInformation("Forget password request completed successfully for email: {Email}", model.Email);
                return Ok($"Forget password request completed successfully for email: {model.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing forget password request for email: {Email}", model.Email);
                return StatusCode(500, new { message = "An error occurred while processing your request", error = ex.Message });
            }
        }

        #endregion


        #region Reset Password
        /// <summary>
        /// Resets a user's password using provided credentials and returns authentication details if successful.
        /// </summary>
        /// <param name="model">The reset password data transfer object containing the user's email, reset token, and new password.</param>
        /// <returns>
        /// Returns an authentication result with a token if the reset is successful, or an error message if it fails.
        /// </returns>
        /// <remarks>
        /// This endpoint validates the reset token and updates the user's password. The request body must contain a valid email, reset token, and new password.
        /// 
        /// Example Request:
        /// ```json
        /// {
        ///   "password": "P@ssw0rd123"
        ///   "email": "user@example.com",
        ///   "token": "reset-token-123",
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">
        /// Returns the authentication details including token when the password reset is successful.
        /// ```json
        /// {
        ///   "message": "Password reset successful",
        ///   "id": "12345",
        ///   "name": "John Doe",
        ///   "email": "user@example.com",
        ///   "phoneNumber": "123-456-7890",
        ///   "address": "123 Main St",
        ///   "nationalId": "ABC123456",
        ///   "baseSalary": 50000.0,
        ///   "shift": [
        ///     {
        ///       "id": 1,
        ///       "startTime": "08:00",
        ///       "endTime": "16:00",
        ///       "employeeId": "12345"
        ///     }
        ///   ],
        ///   "gender": "Male",
        ///   "branch": {
        ///     "id": 1,
        ///     "name": "Main Branch",
        ///     "latitude": 40.7128,
        ///     "longitude": -74.0060
        ///   },
        ///   "hiringDate": "2023-01-15",
        ///   "dateOfBarth": "1990-05-20",
        ///   "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        ///   "roles": ["User"]
        /// }
        /// ```
        /// </response>
        /// <response code="400">
        /// Returned when the request model is invalid or malformed.
        /// ```json
        /// {
        ///   "email": [
        ///     "The Email field is required."
        ///   ],
        ///   "newPassword": [
        ///     "The NewPassword field is required."
        ///   ]
        /// }
        /// ```
        /// </response>
        /// <response code="404">
        /// Returned when the reset token is invalid or the email is not found.
        /// ```json
        /// "Invalid token or email"
        /// ```
        /// </response>
        /// <response code="500">
        /// Returned when an unexpected server error occurs during processing.
        /// ```json
        /// {
        ///   "message": "An error occurred while resetting your password",
        ///   "error": "Exception details here"
        /// }
        /// ```
        /// </response>        
        [HttpPost]
        [Route("~/Account/ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid reset password model state for email: {Email}", model.Email);
                    return BadRequest(ModelState);
                }

                var result = await _unitOfWork.AuthenticationService.ResetPassword(model);

                if (!result.IsAuthenticated)
                {
                    _logger.LogWarning("Reset password failed for email: {Email}. Message: {Message}", model.Email, result.Message);
                    return NotFound(result.Message);
                }

                _logger.LogInformation("Reset password completed successfully for email: {Email}", model.Email);
                return Ok(result);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing reset password request for email: {Email}", model.Email);
                return StatusCode(500, new { message = "An error occurred while resetting your password", error = ex.Message });
            }
        }

        #endregion

    }
}
