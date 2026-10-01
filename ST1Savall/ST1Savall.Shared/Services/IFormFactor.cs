using System.Threading.Tasks;

namespace ST1Savall.Shared.Services
{
    public interface IFormFactor
    {
        public string GetFormFactor();
        public string GetPlatform();
        public Task OpenUrlAsync(string url);
        public Task OpenFileAsync(string path);
        public Task<DeviceLocation?> GetCurrentLocationAsync();
        public Task<CapturedPhoto?> CapturePhotoAsync();
        public Task<CapturedPhoto?> PickPhotoAsync();
        public Task<string?> PickPdfPathAsync();
        public Task<string?> PickFolderPathAsync();
        public Task<string?> MoveFileToDirectoryAsync(string sourcePath, string destinationDirectory);
        public Task<string> CopyFileToDirectoryAsync(string sourcePath, string destinationDirectory);
        public Task DeleteFileAsync(string path);
        public Task<(string Text, decimal? Confidence)> RecognizeTextAsync(byte[] imageData);
    }

    public sealed record DeviceLocation(double Latitude, double Longitude);
    public sealed record CapturedPhoto(string FileName, string? ContentType, byte[] Content);
}
