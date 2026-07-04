using System.Threading.Tasks;

namespace ST1Savall.Shared.Services
{
    public interface IFormFactor
    {
        public string GetFormFactor();
        public string GetPlatform();
        public Task OpenUrlAsync(string url);
    }
}
