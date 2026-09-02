using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Services;

public sealed class MapboxDirectionsService
{
    private readonly HttpClient _httpClient;
    private readonly ApplicationDbContext _context;
    private readonly ParametrosIntegracionesService _integraciones;
    private readonly ILogger<MapboxDirectionsService> _logger;

    public MapboxDirectionsService(
        HttpClient httpClient,
        ApplicationDbContext context,
        ParametrosIntegracionesService integraciones,
        ILogger<MapboxDirectionsService> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _integraciones = integraciones;
        _logger = logger;
    }

    public async Task<ResultadoTramoRuta> CalcularTramoAsync(
        decimal latitudOrigen,
        decimal longitudOrigen,
        decimal latitudDestino,
        decimal longitudDestino,
        bool forzarActualizacion = false,
        CancellationToken cancellationToken = default)
    {
        ValidarCoordenadas(latitudOrigen, longitudOrigen, "origen");
        ValidarCoordenadas(latitudDestino, longitudDestino, "destino");

        var configuracion = await _integraciones.ObtenerMapboxAsync(cancellationToken);
        var precision = configuracion.CoordinatePrecision;
        latitudOrigen = decimal.Round(latitudOrigen, precision, MidpointRounding.AwayFromZero);
        longitudOrigen = decimal.Round(longitudOrigen, precision, MidpointRounding.AwayFromZero);
        latitudDestino = decimal.Round(latitudDestino, precision, MidpointRounding.AwayFromZero);
        longitudDestino = decimal.Round(longitudDestino, precision, MidpointRounding.AwayFromZero);

        if (latitudOrigen == latitudDestino && longitudOrigen == longitudDestino)
            return new ResultadoTramoRuta(0, 0, true);

        var profile = NormalizarProfile(configuracion.Profile);
        var esMapbox = !string.IsNullOrWhiteSpace(configuracion.AccessToken) && 
                       (configuracion.BaseUrl?.Contains("mapbox.com", StringComparison.OrdinalIgnoreCase) ?? false);

        var motorRuta = esMapbox ? "MAPBOX_DIRECTIONS_V5" : "OSRM_ROUTING_V1";
        var clave = CrearClave(latitudOrigen, longitudOrigen, latitudDestino, longitudDestino, profile, motorRuta);
        var ahora = DateTime.UtcNow;

        var cache = await _context.RutasCache.FirstOrDefaultAsync(r => r.ClaveRuta == clave, cancellationToken);
        if (!forzarActualizacion && cache?.FechaExpiracionUtc > ahora)
        {
            cache.UltimoUsoUtc = ahora;
            cache.NumeroUsos++;
            await _context.SaveChangesAsync(cancellationToken);
            return new ResultadoTramoRuta(cache.DistanciaMetros, cache.DuracionSegundos, true);
        }

        var lonOrigenStr = longitudOrigen.ToString(CultureInfo.InvariantCulture);
        var latOrigenStr = latitudOrigen.ToString(CultureInfo.InvariantCulture);
        var lonDestinoStr = longitudDestino.ToString(CultureInfo.InvariantCulture);
        var latDestinoStr = latitudDestino.ToString(CultureInfo.InvariantCulture);

        string url;
        if (esMapbox)
        {
            var coordenadas = $"{lonOrigenStr},{latOrigenStr};{lonDestinoStr},{latDestinoStr}";
            url = $"{configuracion.BaseUrl.TrimEnd('/')}/directions/v5/{profile}/{coordenadas}?alternatives=false&overview=false&steps=false&access_token={Uri.EscapeDataString(configuracion.AccessToken!)}";
        }
        else
        {
            // OSRM (Open Source Routing Machine) - 100% libre y gratuito sin tokens
            var baseUrl = string.IsNullOrWhiteSpace(configuracion.BaseUrl) || configuracion.BaseUrl.Contains("mapbox.com", StringComparison.OrdinalIgnoreCase)
                ? "https://router.project-osrm.org"
                : configuracion.BaseUrl.TrimEnd('/');

            var osrmProfile = profile.Contains("walk", StringComparison.OrdinalIgnoreCase) ? "foot"
                : profile.Contains("cycl", StringComparison.OrdinalIgnoreCase) ? "bike"
                : "driving";

            url = $"{baseUrl}/route/v1/{osrmProfile}/{lonOrigenStr},{latOrigenStr};{lonDestinoStr},{latDestinoStr}?overview=false&alternatives=false&steps=false";
        }

        HttpResponseMessage response;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "ST1Savall-App/1.0 (Routing)");
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ProveedorRutasException("El servicio de cálculo de rutas no respondió dentro del tiempo permitido.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "No se pudo conectar con el servicio de rutas {Url}", url);
            throw new ProveedorRutasException("No se pudo conectar con el servicio de rutas. Compruebe la conexión de red.");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var detalle = await LeerErrorAsync(response, cancellationToken);
                _logger.LogWarning("El servicio de rutas devolvió {StatusCode}: {Detalle}", response.StatusCode, detalle);
                throw new ProveedorRutasException($"No se pudo calcular el tramo de ruta: {detalle}");
            }

            var result = await response.Content.ReadFromJsonAsync<DirectionsResponse>(cancellationToken: cancellationToken);
            var route = result?.Routes?.FirstOrDefault();
            if (route == null || route.Distance < 0 || route.Duration < 0)
                throw new ProveedorRutasException("El servicio de rutas no devolvió una trayectoria válida para las coordenadas indicadas.");

            var distanciaMetros = (int)Math.Ceiling(route.Distance);
            var duracionSegundos = (int)Math.Ceiling(route.Duration);
            var duracionHoras = configuracion.CacheDurationHours;

            var esNuevo = cache == null;
            cache ??= new RutaCache
            {
                ClaveRuta = clave,
                LatitudOrigen = latitudOrigen,
                LongitudOrigen = longitudOrigen,
                LatitudDestino = latitudDestino,
                LongitudDestino = longitudDestino,
                ModoViaje = profile,
                PreferenciaRuta = motorRuta
            };

            if (cache.IdRutaCache == 0)
                _context.RutasCache.Add(cache);

            cache.DistanciaMetros = distanciaMetros;
            cache.DuracionSegundos = duracionSegundos;
            cache.FechaCalculoUtc = ahora;
            cache.FechaExpiracionUtc = ahora.AddHours(duracionHoras);
            cache.UltimoUsoUtc = ahora;
            cache.NumeroUsos++;
            await GuardarCacheAsync(cache, esNuevo, cancellationToken);

            return new ResultadoTramoRuta(distanciaMetros, duracionSegundos, false);
        }
    }

    private async Task GuardarCacheAsync(RutaCache cache, bool esNuevo, CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException) when (esNuevo)
        {
            _context.Entry(cache).State = EntityState.Detached;
            var existente = await _context.RutasCache.FirstOrDefaultAsync(r => r.ClaveRuta == cache.ClaveRuta, cancellationToken);
            if (existente == null)
                throw;
        }
    }

    private static void ValidarCoordenadas(decimal latitud, decimal longitud, string punto)
    {
        if (latitud is < -90 or > 90 || longitud is < -180 or > 180)
            throw new ProveedorRutasException($"Las coordenadas de {punto} no son válidas.");
    }

    private static string CrearClave(decimal latOrigen, decimal lonOrigen, decimal latDestino, decimal lonDestino, string profile, string motor)
    {
        var raw = string.Join('|',
            motor,
            latOrigen.ToString(CultureInfo.InvariantCulture),
            lonOrigen.ToString(CultureInfo.InvariantCulture),
            latDestino.ToString(CultureInfo.InvariantCulture),
            lonDestino.ToString(CultureInfo.InvariantCulture),
            profile);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }

    private static string NormalizarProfile(string value) => value.Trim().ToLowerInvariant() switch
    {
        "mapbox/driving" or "mapbox/driving-traffic" or "mapbox/walking" or "mapbox/cycling" => value.Trim().ToLowerInvariant(),
        _ => "mapbox/driving"
    };

    private static async Task<string> LeerErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var json = await response.Content.ReadFromJsonAsync<DirectionsErrorResponse>(cancellationToken: cancellationToken);
            return json?.Message ?? $"HTTP {(int)response.StatusCode}";
        }
        catch (JsonException)
        {
            return $"HTTP {(int)response.StatusCode}";
        }
    }

    private sealed class DirectionsResponse
    {
        [JsonPropertyName("routes")] public List<RouteInfo>? Routes { get; init; }
    }

    private sealed class RouteInfo
    {
        [JsonPropertyName("distance")] public double Distance { get; init; }
        [JsonPropertyName("duration")] public double Duration { get; init; }
    }

    private sealed class DirectionsErrorResponse
    {
        [JsonPropertyName("message")] public string? Message { get; init; }
    }
}

public readonly record struct ResultadoTramoRuta(int DistanciaMetros, int DuracionSegundos, bool DesdeCache);

public sealed class ProveedorRutasException : Exception
{
    public ProveedorRutasException(string message) : base(message)
    {
    }
}
