using System.Security.Claims;
using System.Threading.Tasks;

namespace ST1Savall.Shared.Services
{
    public interface IUserDisplayService
    {
        Task<string> GetUserDisplayNameAsync(ClaimsPrincipal user);
    }
}
