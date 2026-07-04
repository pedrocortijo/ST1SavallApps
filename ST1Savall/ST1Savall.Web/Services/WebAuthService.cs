using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using ST1Savall.Shared.Services;

namespace ST1Savall.Web.Services
{
    public class WebAuthService : IAuthService
    {
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly NavigationManager _navigationManager;

        public WebAuthService(AuthenticationStateProvider authStateProvider, NavigationManager navigationManager)
        {
            _authStateProvider = authStateProvider;
            _navigationManager = navigationManager;
        }

        public async Task LoginAsync(string email, string token)
        {
            if (_authStateProvider is WebAuthenticationStateProvider webProvider)
            {
                await webProvider.MarkUserAsAuthenticated(email, token);
            }
        }

        public async Task LogoutAsync()
        {
            if (_authStateProvider is WebAuthenticationStateProvider webProvider)
            {
                await webProvider.MarkUserAsLoggedOut();
            }
            _navigationManager.NavigateTo("login");
        }
    }
}
