using System.Net;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Services;

/// <summary>Publica los PDF DeCA en el almacenamiento FTPS configurado.</summary>
public sealed class FtpsPdfStorageService(IConfiguration configuration, ILogger<FtpsPdfStorageService> logger)
{
    private const string PasswordConfigurationKey = "FtpsPdfStorage:Password";

    public bool EstaConfigurado(Parametro parametro) =>
        !string.IsNullOrWhiteSpace(parametro.FtpsHost) &&
        !string.IsNullOrWhiteSpace(parametro.FtpsRutaRemota) &&
        !string.IsNullOrWhiteSpace(parametro.FtpsUsuario) &&
        !string.IsNullOrWhiteSpace(configuration[PasswordConfigurationKey]);

    public async Task SubirAsync(Parametro parametro, string rutaLocal, string nombreArchivo, CancellationToken cancellationToken = default)
    {
        if (!EstaConfigurado(parametro))
            throw new InvalidOperationException(
                "La publicación FTPS no está completamente configurada. Configure host, carpeta, usuario y el secreto FtpsPdfStorage:Password en la API.");

        var rutaRemota = $"/{parametro.FtpsRutaRemota!.Trim().Trim('/')}/{nombreArchivo}";
        var uri = new UriBuilder(Uri.UriSchemeFtp, parametro.FtpsHost!.Trim(), parametro.FtpsPuerto.GetValueOrDefault(21), rutaRemota).Uri;
        var request = (FtpWebRequest)WebRequest.Create(uri);
        request.Method = WebRequestMethods.Ftp.UploadFile;
        request.EnableSsl = true;
        request.UsePassive = true;
        request.UseBinary = true;
        request.Credentials = new NetworkCredential(parametro.FtpsUsuario!.Trim(), configuration[PasswordConfigurationKey]);

        await using var origen = File.OpenRead(rutaLocal);
        await using var destino = await request.GetRequestStreamAsync();
        await origen.CopyToAsync(destino, cancellationToken);

        using var respuesta = (FtpWebResponse)await request.GetResponseAsync();
        logger.LogInformation("PDF DeCA {NombreArchivo} publicado por FTPS. Estado: {Estado}", nombreArchivo, respuesta.StatusDescription.Trim());
    }
}
