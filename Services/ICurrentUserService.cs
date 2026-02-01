using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace KNQASelfService.Services
{
    public interface ICurrentUserService
    {
        Task<string> GetEmployeeNoAsync();
        Task<string> GetEmployeeNameAsync();
        Task<bool> IsAuthenticatedAsync();
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly AuthenticationStateProvider _authStateProvider;

        public CurrentUserService(AuthenticationStateProvider authStateProvider)
        {
            _authStateProvider = authStateProvider;
        }

        public async Task<string> GetEmployeeNoAsync()
        {
            try
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user.Identity?.IsAuthenticated == true)
                {
                    var employeeNo = user.FindFirst("EmployeeNo")?.Value
                                   ?? user.FindFirst("EmployeeNumber")?.Value
                                   ?? user.FindFirst("Employee_Id")?.Value
                                   ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    return employeeNo ?? string.Empty;
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<string> GetEmployeeNameAsync()
        {
            try
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user.Identity?.IsAuthenticated == true)
                {
                    return user.Identity.Name
                           ?? user.FindFirst(ClaimTypes.Name)?.Value
                           ?? user.FindFirst("FullName")?.Value
                           ?? string.Empty;
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            try
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                return authState.User.Identity?.IsAuthenticated ?? false;
            }
            catch
            {
                return false;
            }
        }
    }
}