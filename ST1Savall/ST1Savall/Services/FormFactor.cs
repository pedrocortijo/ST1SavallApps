using ST1Savall.Shared.Services;
using System.Threading.Tasks;
using Microsoft.Maui.Devices;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

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

        public async Task<DeviceLocation?> GetCurrentLocationAsync()
        {
            var permission = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (permission != PermissionStatus.Granted)
                permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (permission != PermissionStatus.Granted)
                throw new InvalidOperationException("El permiso de ubicación no ha sido concedido.");

            var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(15));
            var location = await Geolocation.Default.GetLocationAsync(request);
            return location == null ? null : new DeviceLocation(location.Latitude, location.Longitude);
        }
    }
}
