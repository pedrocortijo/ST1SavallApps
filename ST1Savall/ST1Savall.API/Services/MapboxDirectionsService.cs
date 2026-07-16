using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Services;

public sealed class MapboxDirectionsService
{
    private readonly HttpClient _httpClient;
    private readonly ApplicationDbContext _context;
    private readonly MapboxDirectionsOptions _options;
    private readonly ILogger<MapboxDirectionsService> _logger;

    public MapboxDirectionsService(
        HttpClient httpClient,
        ApplicationDbContext context,
        IOptions<MapboxDirectionsOptions> options,
        ILogger<MapboxDirectionsService> logger)
    {
        _httpClient = httpClient;
        _context = context;
        _options = options.Value;
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

        var precision = Math.Clamp(_options.CoordinatePrecision, 4, 6);
        latitudOrigen = decimal.Round(latitudOrigen, precision, MidpointRounding.AwayFromZero);
        longitudOrigen = decimal.Round(longitudOrigen, precision, MidpointRounding.AwayFromZero);
        latitudDestino = decimal.Round(latitudDestino, precision, MidpointRounding.AwayFromZero);
        longitudDestino = decimal.Round(longitudDestino, precision, MidpointRounding.AwayFromZero);

        if (latitudOrigen == latitudDestino && longitudOrigen == longitudDestino)
            return new ResultadoTramoRuta(0, 0, true);

        var profile = NormalizarProfile(_options.Profile);
        var clave = CrearClave(latitudOrigen, longitudOrigen, latitudDestino, longitudDestino, profile);
        var ahora = DateTime.UtcNow;

        var cache = await _context.RutasCache.FirstOrDefaultAsync(r => r.ClaveRuta == clave, cancellationToken);
        if (!forzarActualizacion && cache?.FechaExpiracionUtc > ahora)
        {
            cache.UltimoUsoUtc = ahora;
            cache.NumeroUsos++;
            await _context.SaveChangesAsync(cancellationToken);
            return new ResultadoTramoRuta(cache.DistanciaMetros, cache.DuracionSegundos, true);
        }

        if (string.IsNullOrWhiteSpace(_options.AccessToken))
            throw new ProveedorRutasException(
                "Mapbox no está configurado. Defina Mapbox:AccessToken mediante secretos de usuario o la variable Mapbox__AccessToken.");

        var coordenadas = string.Join(';',
            $"{longitudOrigen.ToString(CultureInfo.InvariantCulture)},{latitudOrigen.ToString(CultureInfo.InvariantCulture)}",
            $"{longitudDestino.ToString(CultureInfo.InvariantCulture)},{latitudDestino.ToString(CultureInfo.InvariantCulture)}");
        var url = $"directions/v5/{profile}/{coordenadas}?alternatives=false&overview=false&steps=false&access_token={Uri.EscapeDataString(_options.AccessToken)}";

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ProveedorRutasException("Mapbox no respondió dentro del tiempo permitido.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "No se pudo conectar con Mapbox Directions");
            throw new ProveedorRutasException("No se pudo conectar con Mapbox. Compruebe la red y vuelva a intentarlo.");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var detalle = await LeerErrorAsync(response, cancellationToken);
                _logger.LogWarning("Mapbox Directions devolvió {StatusCode}: {Detalle}", response.StatusCode, detalle);
                throw new ProveedorRutasException($"Mapbox no pudo calcular el tramo: {detalle}");
            }

            var result = await response.Content.ReadFromJsonAsync<MapboxDirectionsResponse>(cancellationToken: cancellationToken);
            var route = result?.Routes?.FirstOrDefault();
            if (route == null || route.Distance < 0 || route.Duration < 0)
                throw new ProveedorRutasException("Mapbox no devolvió una ruta válida para las coordenadas indicadas.");

            var distanciaMetros = (int)Math.Ceiling(route.Distance);
            var duracionSegundos = (int)Math.Ceiling(route.Duration);
            var duracionHoras = Math.Clamp(_options.CacheDurationHours, 1, 24 * 30);

            var esNuevo = cache == null;
            cache ??= new RutaCache
            {
                ClaveRuta = clave,
                LatitudOrigen = latitudOrigen,
                LongitudOrigen = longitudOrigen,
                LatitudDestino = latitudDestino,
                LongitudDestino = longitudDestino,
                ModoViaje = profile,
                PreferenciaRuta = "MAPBOX_DIRECTIONS_V5"
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

    private static string CrearClave(decimal latOrigen, decimal lonOrigen, decimal latDestino, decimal lonDestino, string profile)
    {
        var raw = string.Join('|',
            "MAPBOX_DIRECTIONS_V5",
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
            var json = await response.Content.ReadFromJsonAsync<MapboxErrorResponse>(cancellationToken: cancellationToken);
            return json?.Message ?? $"HTTP {(int)response.StatusCode}";
        }
        catch (JsonException)
        {
            return $"HTTP {(int)response.StatusCode}";
        }
    }

    private sealed class MapboxDirectionsResponse
    {
        [JsonPropertyName("routes")] public List<MapboxRoute>? Routes { get; init; }
    }

    private sealed class MapboxRoute
    {
        [JsonPropertyName("distance")] public double Distance { get; init; }
        [JsonPropertyName("duration")] public double Duration { get; init; }
    }

    private sealed class MapboxErrorResponse
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
