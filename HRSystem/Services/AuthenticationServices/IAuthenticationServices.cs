using HRSystem.DTO;

namespace HRSystem.Services.AuthenticationServices
{
    public interface IAuthenticationServices
    {
        Task<AuthenticationDTO> Login(LoginDTO data);
        Task<string> ForgetPassword(ForgetPasswordDTO dto);
        Task<AuthenticationDTO> ResetPassword(ResetPasswordDTO dto);
    }
}
