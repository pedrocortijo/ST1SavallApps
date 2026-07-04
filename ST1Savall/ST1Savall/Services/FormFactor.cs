using ST1Savall.Shared.Services;
using System.Threading.Tasks;
using Microsoft.Maui.Devices;
using Microsoft.Maui.ApplicationModel;

namespace ST1Savall.Services
{
    public class FormFactor : IFormFactor
    {
        public string GetFormFactor()
        {
            return DeviceInfo.Idiom.ToString();
        }

        public string GetPlatform()
        {
            return DeviceInfo.Platform.ToString() + " - " + DeviceInfo.VersionString;
        }

        public async Task OpenUrlAsync(string url)
        {
            await Launcher.Default.OpenAsync(url);
        }
    }
}
