
namespace HRSystem.Services.EmailServices
{
    public interface IEmailServices
    {
        Task<string> SendEmailAsync(string Name, string Email,string Token);
    }
}
