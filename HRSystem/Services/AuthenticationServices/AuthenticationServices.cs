using HRSystem.DataBase;
using HRSystem.DTO;
using HRSystem.DTO.AuthenticationDTOs;
using HRSystem.DTO.BranchDTOs;
using HRSystem.DTO.ShiftDTOs;
using HRSystem.DTO.UserDTOs;
using HRSystem.Extend;
using HRSystem.Models;
using HRSystem.Services.EmailServices;
using HRSystem.Services.UsersServices;
using HRSystem.Settings;
using HRSystem.UnitOfWork;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Web;

namespace HRSystem.Services.AuthenticationServices
{
    public class AuthenticationServices : IAuthenticationServices
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AdminLogin _adminLogin;
        private readonly JWT _jwt;
        //private readonly IUsersServices _usersServices;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthenticationServices> _logger;

        public AuthenticationServices(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IOptions<AdminLogin> adminLogin,
            IOptions<JWT> jwt,
            ApplicationDbContext context,
            ILogger<AuthenticationServices> logger,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _adminLogin = adminLogin.Value;
            _jwt = jwt.Value;
            //_usersServices = usersServices;
            _context = context;
            _logger = logger;
            _unitOfWork = unitOfWork;
        }


        #region Login
        public async Task<AuthenticationDTO> Login(LoginDTO data)
        {
            if (data == null || string.IsNullOrEmpty(data.Email) || string.IsNullOrEmpty(data.Password))
            {
                _logger.LogWarning("Invalid login data: Email or password is null or empty.");
                return new AuthenticationDTO
                {
                    Message = "Email and password are required.",
                    IsAuthenticated = false
                };
            }

            var user = await _userManager.FindByEmailAsync(data.Email);
            if (user == null)
            {
                _logger.LogWarning("No user found with email: {Email}", data.Email);

                if (data.Email == _adminLogin.Email && data.Password == _adminLogin.Password)
                {
                    _logger.LogInformation("Creating default admin user for email: {Email}", data.Email);

                    var admin = new CreateUserDTO
                    {
                        Name = "المدير",
                        Email = _adminLogin.Email,
                        Password = _adminLogin.Password,
                        Address = "Egypt",
                        DateOfBarth = DateTime.Now,
                        PhoneNumber = "+201098684485",
                        Nationalid = "string",
                        Salary = 10000,
                        Gender = Gender.Male.ToString(),
                        DateOfWork = DateTime.Now
                    };

                    try
                    {
                        await _unitOfWork.UsersServices.Create(admin);
                        _logger.LogInformation("Default admin user created successfully for email: {Email}", data.Email);
                        user = await _userManager.FindByEmailAsync(data.Email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create default admin user for email: {Email}. Error: {Message}", data.Email, ex.Message);
                        throw new Exception($"Failed to create default admin user: {ex.Message}", ex);
                    }
                }
            }

            if (user == null || !await _userManager.CheckPasswordAsync(user, data.Password))
            {
                _logger.LogWarning("Login failed for email: {Email}. Incorrect email or password.", data.Email);
                return new AuthenticationDTO
                {
                    Message = "Email or Password is incorrect!",
                    IsAuthenticated = false
                };
            }

            try
            {
                var jwtToken = await CreateJWTToken(user);
                var roles = await _userManager.GetRolesAsync(user);

                var employeeShifts = _context.EmployeeShifts
                    .Where(s => s.EmployeeId == user.Id)
                    .Select(s => new ShiftDTO
                    {
                        Id = s.Shift.Id,
                        StartTime = s.Shift.StartTime.ToString("H:mm", CultureInfo.InvariantCulture),
                        EndTime = s.Shift.EndTime.ToString("H:mm", CultureInfo.InvariantCulture),
                        EmployeeId = user.Id
                    })
                    .ToList();

                _logger.LogInformation("Login successful for email: {Email}", data.Email);
                return new AuthenticationDTO
                {
                    Message = "Login Successful",
                    Id = user.Id,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber ?? "Not specified",
                    Name = user.Name ?? "Not specified",
                    IsAuthenticated = true,
                    Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                    Roles = roles,
                    Address = user.Address ?? "Not specified",
                    DateOfBarth = user.DateOfBarth.ToString("yyyy/MM/dd"),
                    HiringDate = user.HiringDate.ToString("yyyy/MM/dd"),
                    Gender = user.Gender.ToString() ?? "Not specified",
                    NationalId = user.Nationalid ?? "Not specified",
                    Salary = (double)user.BaseSalary,
                    Branch = new BranchDTO
                    {
                        Id = user.Branch?.Id ?? 0,
                        Name = user.Branch?.Name ?? "Not specified",
                        Latitude = user.Branch?.Latitude,
                        Longitude = user.Branch?.Longitude,
                        Radius = user.Branch?.Radius
                    },
                    Shift = employeeShifts
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login process for email: {Email}. Error: {Message}", data.Email, ex.Message);
                throw new Exception($"Error during login process: {ex.Message}", ex);
            }
        }

        #endregion


        #region Forget Password

        public async Task<string> ForgetPassword(ForgetPasswordDTO dto)
        {

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                _logger.LogWarning("No user found with email: {Email}", dto.Email);
                return "Email is not registered!";
            }

            try
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                token = HttpUtility.UrlEncode(token);
                _logger.LogInformation("Sending password reset email to: {Email}", dto.Email);
                var message = await _unitOfWork.EmailServices.SendEmailAsync(user.Name, user.Email, token);

                _logger.LogInformation("Password reset email sent successfully to: {Email}", dto.Email);
                return message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing forget password request for email: {Email}", dto.Email);
                return "An error occurred while processing your request.";
            }
        }

        #endregion


        #region Reset Password
        public async Task<AuthenticationDTO> ResetPassword(ResetPasswordDTO dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Token) || string.IsNullOrEmpty(dto.Password))
            {
                _logger.LogWarning("Invalid reset password data: Email, token, or password is null or empty.");
                return new AuthenticationDTO
                {
                    Message = "Email, reset token, and new password are required.",
                    IsAuthenticated = false
                };
            }

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                _logger.LogWarning("No user found with email: {Email}", dto.Email);
                return new AuthenticationDTO
                {
                    Message = "Email is not registered!",
                    IsAuthenticated = false
                };
            }

            try
            {
                var resetResult = await _userManager.ResetPasswordAsync(user, dto.Token, dto.Password);
                if (!resetResult.Succeeded)
                {
                    var errors = string.Join("; ", resetResult.Errors.Select(e => e.Description));
                    _logger.LogWarning("Failed to reset password for email: {Email}. Errors: {Errors}", dto.Email, errors);
                    return new AuthenticationDTO
                    {
                        Message = $"Failed to reset password: {errors}",
                        IsAuthenticated = false
                    };
                }

                var jwtSecurityToken = await CreateJWTToken(user);
                var roles = await _userManager.GetRolesAsync(user);

                var employeeShifts = _context.EmployeeShifts  
                    .Select(s => new ShiftDTO
                    {
                        Id = s.Shift.Id,
                        StartTime = s.Shift.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                        EndTime = s.Shift.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                        EmployeeId = user.Id
                    })
                    .ToList();

                _logger.LogInformation("Password reset successful for email: {Email}", dto.Email);
                return new AuthenticationDTO
                {
                    Message = "Password Reset Successful",
                    Id = user.Id,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber ?? "Not specified",
                    Name = user.Name ?? "Not specified",
                    IsAuthenticated = true,
                    Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken),
                    Roles = roles,
                    Address = user.Address ?? "Not specified",
                    DateOfBarth = user.DateOfBarth.ToString("yyyy/MM/dd"),
                    HiringDate = user.HiringDate.ToString("yyyy/MM/dd"),
                    Gender = user.Gender.ToString() ?? "Not specified",
                    NationalId = user.Nationalid ?? "Not specified",
                    Salary = (double)user.BaseSalary,
                    Branch = new BranchDTO
                    {
                        Id = user.Branch?.Id ?? 0,
                        Name = user.Branch?.Name ?? "Not specified",
                        Latitude = user.Branch?.Latitude,
                        Longitude = user.Branch?.Longitude
                    },
                    Shift = employeeShifts.Any() ? employeeShifts : new List<ShiftDTO>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing reset password request for email: {Email}. Error: {Message}", dto.Email, ex.Message);
                return new AuthenticationDTO
                {
                    Message = $"An error occurred while resetting your password: {ex.Message}",
                    IsAuthenticated = false
                };
            }
        }

        #endregion


        #region Create JWT Token
        private async Task<JwtSecurityToken> CreateJWTToken(ApplicationUser user)
        {
            // Retrieve user claims
            var UserClaims = await _userManager.GetClaimsAsync(user);

            // Retrieve user roles and create role claims
            var Roles = await _userManager.GetRolesAsync(user);
            var RoleClaims = new List<Claim>();

            foreach (var role in Roles)
                RoleClaims.Add(new Claim(ClaimTypes.Role, role));

            // Combine user claims, role claims, and additional claims
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name,user.UserName),
                new Claim(ClaimTypes.Email,user.Email),
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
            }
            .Union(UserClaims)
            .Union(RoleClaims);

            // Define the security key and signing credentials
            SecurityKey securityKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));

            SigningCredentials signingCredentials =
                new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // Create the JWT token
            var JWTSecurityToken = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(_jwt.DurationInDays),
                signingCredentials: signingCredentials);
            return JWTSecurityToken;
        }

        #endregion

    }
}
