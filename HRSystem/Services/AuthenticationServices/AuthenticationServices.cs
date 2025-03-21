using HRSystem.DTO;
using HRSystem.Extend;
using HRSystem.Services.EmailServices;
using HRSystem.Services.UsersServices;
using HRSystem.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HRSystem.Services.AuthenticationServices
{
    public class AuthenticationServices : IAuthenticationServices
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailServices _emailServices;
        private readonly AdminLogin _adminLogin;
        private readonly JWT _jwt;
        private readonly IUsersServices _usersServices;
        public AuthenticationServices(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailServices emailServices, IOptions<AdminLogin> adminLogin, IOptions<JWT> jwt, IUsersServices usersServices)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailServices = emailServices;
            _adminLogin = adminLogin.Value;
            _jwt = jwt.Value;
            _usersServices = usersServices;
        }

        #region Login
        public async Task<AuthenticationDTO> Login(LoginDTO data)
        {
            var user = await _userManager.FindByEmailAsync(data.Email);

            // If admin credentials are provided and user is not found, create the admin user
            if (user == null && data.Email == _adminLogin.Email && data.Password == _adminLogin.Password)
            {
                var admin = new CreateUserDTO
                {
                    Name = "Admin",
                    UserName = "admin",
                    Email = _adminLogin.Email,
                    Password = _adminLogin.Password,
                    Address = "Egypt",
                    DateOfBarth = DateTime.Now,
                    PhoneNumber = "+201098684485",
                    Nationalid = "string",
                    Salary = 10000,
                    TimeOfAttend = "09:00:00",
                    TimeOfLeave = "17:00:00",
                    Gender = Gender.male.ToString(),
                    DateOfWork =DateTime.Now
                };
                await _usersServices.Create(admin);
            }

            // Validate the user and password
            if (user == null || !await _userManager.CheckPasswordAsync(user, data.Password))
            {
                return new AuthenticationDTO
                {
                    Message = "Email or Password is incorrect!"
                };
            }

            // Generate JWT token for the authenticated user
            var jwtToken = await CreateJWTToken(user);

            // Populate the AuthenticationDTO with user details
            return new AuthenticationDTO
            {
                Message = "Login Successful",
                Id = user.Id,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Name = user.Name,
                UserName = user.UserName,
                IsAuthenticated = true,
                Token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                Roles = await _userManager.GetRolesAsync(user),
                Address = user.Address,
                DateOfBarth = user.DateOfBarth.ToLongDateString(),
                DateOfWork = user.DateOfWork.ToLongDateString(),
                TimeOfAttend = user.TimeOfAttend.ToShortTimeString(),
                TimeOfLeave = user.TimeOfLeave.ToShortTimeString(),
                Gender = user.Gender.ToString(),
                Nationalid = user.Nationalid,
                Salary = user.Salary
            };
        }

        #endregion


        #region Forget Password

        public async Task<string> ForgetPassword(ForgetPasswordDTO dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return "Email is not registered!";

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var Message = await _emailServices.SendEmailAsync(user.Name, user.Email, token);

            return Message;
        }

        #endregion


        #region Reset Password
        public async Task<AuthenticationDTO> ResetPassword(ResetPasswordDTO dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null)
                return new AuthenticationDTO
                {
                    Message = "Email is not registered!"
                };

            var resetResult = await _userManager.ResetPasswordAsync(user, dto.Token, dto.Password);
            if (!resetResult.Succeeded)
            {
                var errors = string.Join("; ", resetResult.Errors.Select(e => e.Description));
                return new AuthenticationDTO
                {
                    Message = $"Failed to reset password: {errors}"
                };
            }
            var jwtSecurityToken = await CreateJWTToken(user);

            return new AuthenticationDTO
            {
                Message = "Password Reseted Successful",
                Id = user.Id,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Name = user.Name,
                UserName = user.UserName,
                IsAuthenticated = true,
                Token = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken),
                Roles = await _userManager.GetRolesAsync(user),
                Address = user.Address,
                DateOfBarth = user.DateOfBarth.ToLongDateString(),
                DateOfWork = user.DateOfWork.ToLongDateString(),
                TimeOfAttend = user.TimeOfAttend.ToShortTimeString(),
                TimeOfLeave = user.TimeOfLeave.ToShortTimeString(),
                Gender = user.Gender.ToString(),
                Nationalid = user.Nationalid,
                Salary = user.Salary
            };
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
