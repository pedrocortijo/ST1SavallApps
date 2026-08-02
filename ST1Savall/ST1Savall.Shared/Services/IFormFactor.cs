using System.Threading.Tasks;

namespace ST1Savall.Shared.Services
{
    public interface IFormFactor
    {
        public string GetFormFactor();
        public string GetPlatform();
        public Task OpenUrlAsync(string url);
        public Task<DeviceLocation?> GetCurrentLocationAsync();
        public Task<CapturedPhoto?> CapturePhotoAsync();
    }

    public sealed record DeviceLocation(double Latitude, double Longitude);
    public sealed record CapturedPhoto(string FileName, string? ContentType, byte[] Content);
}
