using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;

namespace ST1Savall.API.Services;

public sealed class ParametrosIntegracionesService(
    ApplicationDbContext context,
    IConfiguration configuration,
    ILogger<ParametrosIntegracionesService> logger)
{

    public async Task MigrarDesdeConfiguracionAsync(CancellationToken ct = default)
    {
        var p = await context.Parametros.OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        if (p is null) return;

        var mapbox = configuration.GetSection("Mapbox");
        var wialon = configuration.GetSection("Wialon");
        var cambios = false;
        if (string.IsNullOrWhiteSpace(p.MapboxBaseUrl) && !string.IsNullOrWhiteSpace(mapbox["BaseUrl"])) { p.MapboxBaseUrl = mapbox["BaseUrl"]!.Trim(); cambios = true; }
        if (string.IsNullOrWhiteSpace(p.MapboxProfile) && !string.IsNullOrWhiteSpace(mapbox["Profile"])) { p.MapboxProfile = mapbox["Profile"]!.Trim(); cambios = true; }
        if (!p.MapboxCacheDurationHours.HasValue && mapbox.GetValue<int?>("CacheDurationHours") is int cache) { p.MapboxCacheDurationHours = cache; cambios = true; }
        if (!p.MapboxCoordinatePrecision.HasValue && mapbox.GetValue<int?>("CoordinatePrecision") is int precision) { p.MapboxCoordinatePrecision = precision; cambios = true; }
        if (string.IsNullOrWhiteSpace(p.WialonHost) && !string.IsNullOrWhiteSpace(wialon["Host"])) { p.WialonHost = wialon["Host"]!.Trim(); cambios = true; }
        if (string.IsNullOrWhiteSpace(p.MapboxAccessTokenProtegido) && !string.IsNullOrWhiteSpace(mapbox["AccessToken"])) { p.MapboxAccessTokenProtegido = mapbox["AccessToken"]!.Trim(); cambios = true; }
        if (string.IsNullOrWhiteSpace(p.WialonAccessTokenProtegido) && !string.IsNullOrWhiteSpace(wialon["AccessToken"])) { p.WialonAccessTokenProtegido = wialon["AccessToken"]!.Trim(); cambios = true; }
        if (!cambios) return;

        await context.SaveChangesAsync(ct);
        logger.LogInformation("Configuración de Mapbox/Wialon migrada a Parámetros.");
    }

    public async Task<MapboxParametros> ObtenerMapboxAsync(CancellationToken ct)
    {
        var p = await context.Parametros.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        return new MapboxParametros(p?.MapboxAccessTokenProtegido, p?.MapboxBaseUrl ?? "https://api.mapbox.com/", p?.MapboxProfile ?? "mapbox/driving", Math.Clamp(p?.MapboxCacheDurationHours ?? 24, 1, 720), Math.Clamp(p?.MapboxCoordinatePrecision ?? 5, 4, 6));
    }

    public async Task<WialonParametros> ObtenerWialonAsync(CancellationToken ct)
    {
        var p = await context.Parametros.AsNoTracking().OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        return new WialonParametros(p?.WialonAccessTokenProtegido, p?.WialonHost ?? "hst-api.wialon.com");
    }

}

public sealed record MapboxParametros(string? AccessToken, string BaseUrl, string Profile, int CacheDurationHours, int CoordinatePrecision);
public sealed record WialonParametros(string? AccessToken, string Host);