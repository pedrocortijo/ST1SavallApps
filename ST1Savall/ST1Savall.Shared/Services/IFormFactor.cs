using System.Threading.Tasks;

namespace ST1Savall.Shared.Services
{
    public interface IFormFactor
    {
        public string GetFormFactor();
        public string GetPlatform();
        public Task OpenUrlAsync(string url);
        public Task<DeviceLocation?> GetCurrentLocationAsync();
    }

    public sealed record DeviceLocation(double Latitude, double Longitude);
}
