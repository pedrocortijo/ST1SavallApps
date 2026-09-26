using ST1Savall.Shared.Services;
using System.Threading.Tasks;
using Microsoft.Maui.Devices;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Plugin.Maui.OCR;


namespace ST1Savall.Services
{
    public class FormFactor(IOcrService ocrService) : IFormFactor
    {
        public string GetFormFactor() => DeviceInfo.Idiom.ToString();
        public string GetPlatform() => DeviceInfo.Platform + " - " + DeviceInfo.VersionString;
        public async Task OpenUrlAsync(string url) => await Launcher.Default.OpenAsync(url);

        public async Task<DeviceLocation?> GetCurrentLocationAsync()
        {
            var permission = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (permission != PermissionStatus.Granted)
                permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (permission != PermissionStatus.Granted)
                throw new InvalidOperationException("El permiso de ubicación no ha sido concedido.");

            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(15)));
            return location == null ? null : new DeviceLocation(location.Latitude, location.Longitude);
        }

        public async Task<CapturedPhoto?> CapturePhotoAsync()
        {
            var permission = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (permission != PermissionStatus.Granted)
                permission = await Permissions.RequestAsync<Permissions.Camera>();
            if (permission != PermissionStatus.Granted)
                throw new InvalidOperationException("El permiso de cámara no ha sido concedido.");
            if (!MediaPicker.Default.IsCaptureSupported)
                throw new InvalidOperationException("Este dispositivo no dispone de cámara.");

            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo == null) return null;
            await using var source = await photo.OpenReadAsync();
            using var memory = new MemoryStream();
            await source.CopyToAsync(memory);
            return new CapturedPhoto(photo.FileName, photo.ContentType, memory.ToArray());
        }

        public async Task<CapturedPhoto?> PickPhotoAsync()
        {
            var photo = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Seleccionar tique de báscula",
                FileTypes = FilePickerFileType.Images
            });
            if (photo is null) return null;

            await using var source = await photo.OpenReadAsync();
            using var memory = new MemoryStream();
            await source.CopyToAsync(memory);
            return new CapturedPhoto(photo.FileName, photo.ContentType, memory.ToArray());
        }

        public async Task<(string Text, decimal? Confidence)> RecognizeTextAsync(byte[] imageData)
        {
#if WINDOWS
            try
            {
                imageData = TiqueImageCropper.RecortarSiSeDetecta(imageData);
            }
            catch
            {
                // Si el recorte no puede detectar un tique válido, el OCR usa la foto original.
            }
#endif
            await ocrService.InitAsync();

            // En Windows el motor OCR usa los idiomas instalados en el sistema. Preferimos
            // español y solicitamos el modo exhaustivo para texto pequeño de los tiques.
            var idioma = ocrService.SupportedLanguages.FirstOrDefault(language =>
                language.StartsWith("es", StringComparison.OrdinalIgnoreCase))
                ?? ocrService.SupportedLanguages.FirstOrDefault();
            var opciones = new OcrOptions.Builder().SetTryHard(true);
            if (!string.IsNullOrWhiteSpace(idioma))
                opciones.SetLanguage(idioma);

            var result = await ocrService.RecognizeTextAsync(imageData, opciones.Build());
            if (!result.Success || string.IsNullOrWhiteSpace(result.AllText))
                throw new InvalidOperationException("No se ha podido leer texto en el tique. Repita la foto con el tique completo, enfocado y bien iluminado.");

            var confidences = result.Elements
                .Where(element => element.Confidence > 0)
                .Select(element => (decimal)element.Confidence)
                .ToList();
            return (result.AllText, confidences.Count == 0 ? null : decimal.Round(confidences.Average(), 4));
        }
    }
}
