using Hangfire.Dashboard;
using System.Text;

namespace HRSystem
{
    public class BasicAuthAuthorizationFilter : IDashboardAuthorizationFilter
    {
        private readonly string _username;
        private readonly string _password;

        public BasicAuthAuthorizationFilter(string username, string password)
        {
            _username = username;
            _password = password;
        }

        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            string authHeader = httpContext.Request.Headers["Authorization"];

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Basic "))
            {
                string encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
                string credentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
                string[] parts = credentials.Split(':', 2);
                string username = parts[0];
                string password = parts.Length > 1 ? parts[1] : null;

                if (username == _username && password == _password)
                {
                    return true;
                }
            }

            httpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire Dashboard\"";
            httpContext.Response.StatusCode = 401;
            return false;
        }
    }
}