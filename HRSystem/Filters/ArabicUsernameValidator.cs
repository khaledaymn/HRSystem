using Microsoft.AspNetCore.Identity;
using System.Text.RegularExpressions;

namespace HRSystem.Filters
{
    public class ArabicUsernameValidator<TUser> : IUserValidator<TUser> where TUser : IdentityUser
    {
        public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user)
        {
            var errors = new List<IdentityError>();

            string pattern = @"^[\p{L}\p{N}_-]+$";
            if (!Regex.IsMatch(user.UserName, pattern))
            {
                return Task.FromResult(IdentityResult.Failed(errors.ToArray()));
            }

            return Task.FromResult(IdentityResult.Success);
        }

    }
}

