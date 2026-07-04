using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using ST1Savall.Shared.Services;

namespace ST1Savall.Services
{
    public class DesktopAuthService : IAuthService
    {
        private readonly AuthenticationStateProvider _authStateProvider;

        public DesktopAuthService(AuthenticationStateProvider authStateProvider)
        {
            _authStateProvider = authStateProvider;
        }

        public Task LoginAsync(string email, string token)
        {
            if (_authStateProvider is DesktopAuthenticationStateProvider desktopProvider)
            {
                desktopProvider.MarkUserAsAuthenticated(email, token);
            }
            return Task.CompletedTask;
        }

        public Task LogoutAsync()
        {
            if (_authStateProvider is DesktopAuthenticationStateProvider desktopProvider)
            {
                desktopProvider.MarkUserAsLoggedOut();
            }
            return Task.CompletedTask;
        }
    }
}
