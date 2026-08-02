using ST1Savall.Shared.Services;
using System;
using System.Threading.Tasks;

namespace ST1Savall.Web.Services
{
    public class FormFactor : IFormFactor
    {
        public string GetFormFactor()
        {
            return "Web";
        }

        public string GetPlatform()
        {
            return Environment.OSVersion.ToString();
        }

        public Task OpenUrlAsync(string url)
        {
            return Task.CompletedTask;
        }

        public Task<DeviceLocation?> GetCurrentLocationAsync()
        {
            // En web la ubicación se solicita desde el navegador mediante JS interop.
            return Task.FromResult<DeviceLocation?>(null);
        }

        public Task<CapturedPhoto?> CapturePhotoAsync() => Task.FromResult<CapturedPhoto?>(null);
    }
}
