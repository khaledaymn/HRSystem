using HRSystem.DTO;
using HRSystem.Services.AuthenticationServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HRSystem.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationServices _authenticationServices;
        public AuthenticationController(IAuthenticationServices authenticationService) => _authenticationServices = authenticationService;

        #region Login
        /// <summary>
        /// Authenticates a user by verifying their credentials and returns authentication details.
        /// This endpoint allows users to log in to the system using their username and password.
        /// </summary>
        /// <param name="model">
        /// The login details, which must include a valid username and password (as defined in LoginDTO).
        /// Example Request (LoginDTO):
        /// <code>
        /// {
        ///     "email": "user@example.com",
        ///     "password": "P@ssw0rd123"
        /// }
        /// </code>
        /// </param>
        /// <returns>
        /// Returns an <see cref="AuthenticationDTO"/> object containing:
        /// - IsAuthenticated: A boolean indicating if the login was successful.
        /// - Message: A string describing the result of the login attempt.
        /// - User details (e.g., Id, Name, Email, etc.) if the login is successful.
        /// </returns>
        /// <response code="200">
        /// Login successful. Returns the <see cref="AuthenticationDTO"/> with user details.
        /// Example Response (Success):
        /// <code>
        /// {
        ///     "message": "Login successful",
        ///     "id": "12345",
        ///     "name": "John Doe",
        ///     "email": "user@example.com",
        ///     "userName": "johndoe",
        ///     "phoneNumber": "123-456-7890",
        ///     "address": "123 Main St",
        ///     "nationalId": "987654321",
        ///     "salary": 50000,
        ///     "timeOfAttend": "09:00",
        ///     "timeOfLeave": "17:00",
        ///     "gender": "Male",
        ///     "dateOfWork": "2023/01/15",
        ///     "dateOfBirth": "1990/05/20",
        ///     "roles": ["User"]
        /// }
        /// </code>
        /// </response>
        /// <response code="400">
        /// Bad request. Returned when the model is invalid (e.g., missing email or password) or authentication fails (e.g., incorrect credentials).
        /// Example Response (Invalid Model):
        /// <code>
        /// {
        ///     "errors": {
        ///         "username": ["The username field is required."],
        ///         "password": ["The password field is required."]
        ///     }
        /// }
        /// </code>
        /// Example Response (Authentication Failed):
        /// <code>
        /// "Invalid username or password."
        /// </code>
        /// </response>
        /// <response code="500">
        /// Server error. Returned when an unexpected error occurs on the server, with a JSON object containing 'message' and 'error' properties.
        /// Example Response (Server Error):
        /// <code>
        /// {
        ///     "message": "An error occurred",
        ///     "error": "Database connection failed"
        /// }
        /// </code>
        /// </response>
        [HttpPost]
        [Route("~/Account/Login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState); // Returns validation errors

                var result = await _authenticationServices.Login(model);

                if (!result.IsAuthenticated)
                    return BadRequest(result.Message); // Returns authentication failure message

                return Ok(result); // Returns successful authentication details
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred", error = ex.Message }); // Returns server error
            }
        }

        #endregion


        #region Forget Password
        /// <summary>
        /// Initiates the password reset process by sending a reset link to the user's email.
        /// This endpoint allows users to request a password reset by providing their email address.
        /// </summary>
        /// <param name="model">
        /// The data containing the user's email address (as defined in ForgetPasswordDTO).
        /// Example Request (ForgetPasswordDTO):
        /// <code>
        /// {
        ///     "email": "user@example.com"
        /// }
        /// </code>
        /// </param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> indicating the result of the password reset request.
        /// </returns>
        /// <response code="200">
        /// Password reset initiated successfully. Returns a success message indicating the reset link was sent.
        /// Example Response (Success):
        /// <code>
        /// "success"
        /// </code>
        /// </response>
        /// <response code="400">
        /// Bad request. Returned when the provided model is invalid (e.g., missing or invalid email).
        /// Example Response (Invalid Model):
        /// <code>
        /// {
        ///     "errors": {
        ///         "email": ["The email field is required."]
        ///     }
        /// }
        /// </code>
        /// </response>
        /// <response code="404">
        /// Not found. Returned when the email is not registered or the reset process fails (e.g., email sending failed).
        /// Example Response (Email Not Registered):
        /// <code>
        /// "Email is not registered!"
        /// </code>
        /// </response>
        /// <response code="500">
        /// Server error. Returned when an unexpected error occurs on the server, with a JSON object containing a message.
        /// Example Response (Server Error):
        /// <code>
        /// "An error occurred while processing the request: Database connection failed"
        /// </code>
        /// </response>
        [HttpPost]
        [Route("~/Account/ForgetPassword")]
        public async Task<IActionResult> ForgetPassword([FromBody] ForgetPasswordDTO model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var result = await _authenticationServices.ForgetPassword(model);

                    if (result != "success")
                        return NotFound(result);

                    return Ok(result);
                }
                return BadRequest(ModelState);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while processing the request: {ex.Message}");
            }
        }

        #endregion


        #region Reset Password
        /// <summary>
        /// Resets a user's password using a reset token.
        /// This endpoint allows users to reset their password by providing the necessary details, including the reset token, email, and new password.
        /// </summary>
        /// <param name="model">
        /// The data containing the reset token, email, and new password (as defined in ResetPasswordDTO).
        /// Example Request (ResetPasswordDTO):
        /// <code>
        /// {
        ///     "email": "user@example.com",
        ///     "token": "abc123xyz456",
        ///     "newPassword": "NewP@ssw0rd123"
        /// }
        /// </code>
        /// </param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> indicating the result of the password reset operation.
        /// </returns>
        /// <response code="200">
        /// Password reset successful. Returns the <see cref="AuthenticationDTO"/> with updated user details.
        /// Example Response (Success):
        /// <code>
        /// {
        ///     "message": "Password reset successful",
        ///     "id": "12345",
        ///     "name": "John Doe",
        ///     "email": "user@example.com",
        ///     "userName": "johndoe",
        ///     "phoneNumber": "123-456-7890",
        ///     "address": "123 Main St",
        ///     "nationalId": "987654321",
        ///     "salary": 50000,
        ///     "timeOfAttend": "09:00",
        ///     "timeOfLeave": "17:00",
        ///     "gender": "Male",
        ///     "dateOfWork": "2023-01-15",
        ///     "dateOfBirth": "1990-05-20",
        ///     "roles": ["User"]
        /// }
        /// </code>
        /// </response>
        /// <response code="400">
        /// Bad request. Returned when the provided model is invalid (e.g., missing token, email, or new password).
        /// Example Response (Invalid Model):
        /// <code>
        /// {
        ///     "errors": {
        ///         "email": ["The email field is required."],
        ///         "token": ["The token field is required."],
        ///         "newPassword": ["The new password field is required."]
        ///     }
        /// }
        /// </code>
        /// </response>
        /// <response code="404">
        /// Not found. Returned when the reset token is invalid, the email is not registered, or the reset process fails.
        /// Example Response (Invalid Token):
        /// <code>
        /// "Invalid or expired token."
        /// </code>
        /// </response>
        /// <response code="500">
        /// Server error. Returned when an unexpected error occurs on the server, with a JSON object containing a message.
        /// Example Response (Server Error):
        /// <code>
        /// {
        ///     "message": "An error occurred while resetting the password",
        ///     "error": "Database connection failed"
        /// }
        /// </code>
        /// </response>        
        [HttpPost]
        [Route("~/Account/ResetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var result = await _authenticationServices.ResetPassword(model);
                    if (result.IsAuthenticated)
                        return Ok(result); // Returns AuthenticationDTO on success
                    return NotFound(result.Message); // Returns failure message
                }
                return BadRequest(ModelState); // Returns validation errors
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while resetting the password: {ex.Message}");
            }
        }

        #endregion

    }
}
