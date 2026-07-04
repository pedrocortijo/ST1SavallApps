using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace ST1Savall.Web.Services
{
    public class WebAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ProtectedLocalStorage _localStorage;
        private ClaimsPrincipal _currentUser = new ClaimsPrincipal(new ClaimsIdentity());

        public WebAuthenticationStateProvider(ProtectedLocalStorage localStorage)
        {
            _localStorage = localStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var emailResult = await _localStorage.GetAsync<string>("auth_email");
                var tokenResult = await _localStorage.GetAsync<string>("auth_token");

                if (emailResult.Success && tokenResult.Success && !string.IsNullOrEmpty(emailResult.Value) && !string.IsNullOrEmpty(tokenResult.Value))
                {
                    var claims = new[] { 
                        new Claim(ClaimTypes.Name, emailResult.Value),
                        new Claim(ClaimTypes.Email, emailResult.Value),
                        new Claim("token", tokenResult.Value)
                    };
                    var identity = new ClaimsIdentity(claims, "Bearer");
                    _currentUser = new ClaimsPrincipal(identity);
                }
                else
                {
                    _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
                }
            }
            catch
            {
                // ProtectedLocalStorage may throw during prerendering or if JS Interop is not yet ready.
                _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            }

            return new AuthenticationState(_currentUser);
        }

        public async Task MarkUserAsAuthenticated(string email, string token)
        {
            try
            {
                await _localStorage.SetAsync("auth_email", email);
                await _localStorage.SetAsync("auth_token", token);
            }
            catch { }

            var claims = new[] { 
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Email, email),
                new Claim("token", token)
            };
            var identity = new ClaimsIdentity(claims, "Bearer");
            _currentUser = new ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }

        public async Task MarkUserAsLoggedOut()
        {
            try
            {
                await _localStorage.DeleteAsync("auth_email");
                await _localStorage.DeleteAsync("auth_token");
            }
            catch { }

            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        }
    }
}
