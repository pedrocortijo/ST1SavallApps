using System.Security.Claims;
using System.Threading.Tasks;
using ST1Savall.Shared.Services;

namespace ST1Savall.Web.Services
{
    public class ServerUserDisplayService : IUserDisplayService
    {
        public Task<string> GetUserDisplayNameAsync(ClaimsPrincipal user)
        {
            if (user.Identity?.IsAuthenticated == true)
            {
                return Task.FromResult($"Usuario: {user.Identity.Name}");
            }
            return Task.FromResult("Empresa: Freecom Free Computers S.L. (Web)");
        }
    }
}
