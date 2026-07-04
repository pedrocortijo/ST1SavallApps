using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Maui.Storage;

namespace ST1Savall.Services
{
    public class DesktopAuthenticationStateProvider : AuthenticationStateProvider
    {
        private ClaimsPrincipal _currentUser = new ClaimsPrincipal(new ClaimsIdentity());

        public DesktopAuthenticationStateProvider()
        {
            try
            {
                var email = Preferences.Get("auth_email", null);
                var token = Preferences.Get("auth_token", null);
                if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(token))
                {
                    var claims = new[] { 
                        new Claim(ClaimTypes.Name, email),
                        new Claim(ClaimTypes.Email, email),
                        new Claim("token", token)
                    };
                    var identity = new ClaimsIdentity(claims, "Bearer");
                    _currentUser = new ClaimsPrincipal(identity);
                }
            }
            catch { }
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return Task.FromResult(new AuthenticationState(_currentUser));
        }

        public void MarkUserAsAuthenticated(string email, string token)
        {
            Preferences.Set("auth_email", email);
            Preferences.Set("auth_token", token);

            var claims = new[] { 
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Email, email),
                new Claim("token", token)
            };
            var identity = new ClaimsIdentity(claims, "Bearer");
            _currentUser = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }

        public void MarkUserAsLoggedOut()
        {
            Preferences.Remove("auth_email");
            Preferences.Remove("auth_token");

            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }
    }
}
