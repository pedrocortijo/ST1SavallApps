using System.Threading.Tasks;

namespace ST1Savall.Shared.Services
{
    public interface IAuthService
    {
        Task LoginAsync(string email, string token);
        Task LogoutAsync();
    }
}
