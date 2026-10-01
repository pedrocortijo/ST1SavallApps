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

        public async Task OpenFileAsync(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("No se ha encontrado el archivo indicado.", path);

            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(path)
            });
        }

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

        public async Task<string?> PickPdfPathAsync()
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Seleccionar documento OVP (PDF)",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "application/pdf" } },
                    { DevicePlatform.WinUI, new[] { ".pdf" } },
                    { DevicePlatform.iOS, new[] { "com.adobe.pdf" } },
                    { DevicePlatform.MacCatalyst, new[] { "com.adobe.pdf" } }
                })
            });

            return file?.FullPath;
        }

        public async Task<string?> PickFolderPathAsync()
        {
#if WINDOWS
            var ventana = Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
            if (ventana is null)
                throw new InvalidOperationException("No se encuentra la ventana de la aplicación para seleccionar la carpeta.");

            var selector = new Windows.Storage.Pickers.FolderPicker();
            selector.FileTypeFilter.Add("*");
            WinRT.Interop.InitializeWithWindow.Initialize(selector, WinRT.Interop.WindowNative.GetWindowHandle(ventana));
            var carpeta = await selector.PickSingleFolderAsync();
            return carpeta?.Path;
#else
            throw new PlatformNotSupportedException("La selección de carpetas está disponible en la aplicación de Windows.");
#endif
        }

        public Task<string?> MoveFileToDirectoryAsync(string sourcePath, string destinationDirectory)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(destinationDirectory))
                return Task.FromResult<string?>(sourcePath);

            var origen = Path.GetFullPath(sourcePath);
            if (!File.Exists(origen))
                throw new FileNotFoundException("No se encuentra el archivo seleccionado.", origen);

            var destinoCarpeta = Path.GetFullPath(destinationDirectory);
            var prefijoDestino = destinoCarpeta.EndsWith(Path.DirectorySeparatorChar)
                ? destinoCarpeta
                : destinoCarpeta + Path.DirectorySeparatorChar;
            if (origen.StartsWith(prefijoDestino, StringComparison.OrdinalIgnoreCase))
                return Task.FromResult<string?>(origen);

            Directory.CreateDirectory(destinoCarpeta);
            var nombre = Path.GetFileName(origen);
            var destino = Path.Combine(destinoCarpeta, nombre);
            if (File.Exists(destino))
            {
                var nombreSinExtension = Path.GetFileNameWithoutExtension(nombre);
                destino = Path.Combine(destinoCarpeta, $"{nombreSinExtension}_{Guid.NewGuid():N}{Path.GetExtension(nombre)}");
            }

            File.Move(origen, destino);
            return Task.FromResult<string?>(destino);
        }


        public Task<string> CopyFileToDirectoryAsync(string sourcePath, string destinationDirectory)
        {
            var origen = Path.GetFullPath(sourcePath);
            if (!File.Exists(origen)) throw new FileNotFoundException("No se encuentra el PDF de factura.", origen);
            Directory.CreateDirectory(destinationDirectory);
            var destino = Path.Combine(destinationDirectory, Path.GetFileName(origen));
            File.Copy(origen, destino, true);
            return Task.FromResult(destino);
        }

        public Task DeleteFileAsync(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("No se encuentra el archivo indicado.", path);

            File.Delete(path);
            return Task.CompletedTask;
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
