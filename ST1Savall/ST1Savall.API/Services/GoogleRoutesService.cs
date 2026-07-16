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

public sealed class GoogleRoutesService
{
    private const string FieldMask = "routes.distanceMeters,routes.duration";
    private readonly HttpClient _httpClient;
    private readonly ApplicationDbContext _context;
    private readonly GoogleRoutesOptions _options;
    private readonly ILogger<GoogleRoutesService> _logger;

    public GoogleRoutesService(
        HttpClient httpClient,
        ApplicationDbContext context,
        IOptions<GoogleRoutesOptions> options,
        ILogger<GoogleRoutesService> logger)
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

        var modoViaje = NormalizarModoViaje(_options.TravelMode);
        var preferencia = NormalizarPreferencia(_options.RoutingPreference);
        var clave = CrearClave(latitudOrigen, longitudOrigen, latitudDestino, longitudDestino, modoViaje, preferencia);
        var ahora = DateTime.UtcNow;

        var cache = await _context.RutasCache.FirstOrDefaultAsync(r => r.ClaveRuta == clave, cancellationToken);
        if (!forzarActualizacion && cache?.FechaExpiracionUtc > ahora)
        {
            cache.UltimoUsoUtc = ahora;
            cache.NumeroUsos++;
            await _context.SaveChangesAsync(cancellationToken);
            return new ResultadoTramoRuta(cache.DistanciaMetros, cache.DuracionSegundos, true);
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new GoogleRoutesException(
                "Google Routes no está configurado. Defina GoogleRoutes:ApiKey mediante secretos de usuario o la variable GoogleRoutes__ApiKey.");

        var requestBody = new ComputeRoutesRequest
        {
            Origin = CrearWaypoint(latitudOrigen, longitudOrigen),
            Destination = CrearWaypoint(latitudDestino, longitudDestino),
            TravelMode = modoViaje,
            RoutingPreference = preferencia,
            ComputeAlternativeRoutes = false,
            LanguageCode = "es-ES",
            Units = "METRIC"
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "directions/v2:computeRoutes")
        {
            Content = JsonContent.Create(requestBody)
        };
        request.Headers.TryAddWithoutValidation("X-Goog-Api-Key", _options.ApiKey);
        request.Headers.TryAddWithoutValidation("X-Goog-FieldMask", FieldMask);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new GoogleRoutesException("Google Routes no respondió dentro del tiempo permitido.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "No se pudo conectar con Google Routes");
            throw new GoogleRoutesException("No se pudo conectar con Google Routes. Compruebe la red y vuelva a intentarlo.");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var detalle = await LeerErrorAsync(response, cancellationToken);
                _logger.LogWarning("Google Routes devolvió {StatusCode}: {Detalle}", response.StatusCode, detalle);
                throw new GoogleRoutesException($"Google Routes no pudo calcular el tramo: {detalle}");
            }

            var result = await response.Content.ReadFromJsonAsync<ComputeRoutesResponse>(cancellationToken: cancellationToken);
            var route = result?.Routes?.FirstOrDefault();
            if (route == null || route.DistanceMeters < 0 || string.IsNullOrWhiteSpace(route.Duration))
                throw new GoogleRoutesException("Google Routes no devolvió una ruta válida para las coordenadas indicadas.");

            var duracionSegundos = ParseDurationSeconds(route.Duration);
            var duracionHoras = Math.Clamp(_options.CacheDurationHours, 1, 24 * 30);

            var esNuevo = cache == null;
            cache ??= new RutaCache
            {
                ClaveRuta = clave,
                LatitudOrigen = latitudOrigen,
                LongitudOrigen = longitudOrigen,
                LatitudDestino = latitudDestino,
                LongitudDestino = longitudDestino,
                ModoViaje = modoViaje,
                PreferenciaRuta = preferencia
            };

            if (cache.IdRutaCache == 0)
                _context.RutasCache.Add(cache);

            cache.DistanciaMetros = route.DistanceMeters;
            cache.DuracionSegundos = duracionSegundos;
            cache.FechaCalculoUtc = ahora;
            cache.FechaExpiracionUtc = ahora.AddHours(duracionHoras);
            cache.UltimoUsoUtc = ahora;
            cache.NumeroUsos++;
            await GuardarCacheAsync(cache, esNuevo, cancellationToken);

            return new ResultadoTramoRuta(route.DistanceMeters, duracionSegundos, false);
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
            // Otra petición pudo insertar la misma ruta entre la lectura y el guardado.
            _context.Entry(cache).State = EntityState.Detached;
            var existente = await _context.RutasCache.FirstOrDefaultAsync(r => r.ClaveRuta == cache.ClaveRuta, cancellationToken);
            if (existente == null)
                throw;
        }
    }

    private static void ValidarCoordenadas(decimal latitud, decimal longitud, string punto)
    {
        if (latitud is < -90 or > 90 || longitud is < -180 or > 180)
            throw new GoogleRoutesException($"Las coordenadas de {punto} no son válidas.");
    }

    private static string CrearClave(decimal latOrigen, decimal lonOrigen, decimal latDestino, decimal lonDestino, string modo, string preferencia)
    {
        var raw = string.Join('|',
            latOrigen.ToString(CultureInfo.InvariantCulture),
            lonOrigen.ToString(CultureInfo.InvariantCulture),
            latDestino.ToString(CultureInfo.InvariantCulture),
            lonDestino.ToString(CultureInfo.InvariantCulture),
            modo,
            preferencia);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    }

    private static string NormalizarModoViaje(string value) => value.Trim().ToUpperInvariant() switch
    {
        "DRIVE" or "BICYCLE" or "WALK" or "TWO_WHEELER" => value.Trim().ToUpperInvariant(),
        _ => "DRIVE"
    };

    private static string NormalizarPreferencia(string value) => value.Trim().ToUpperInvariant() switch
    {
        "TRAFFIC_AWARE" or "TRAFFIC_AWARE_OPTIMAL" or "TRAFFIC_UNAWARE" => value.Trim().ToUpperInvariant(),
        _ => "TRAFFIC_UNAWARE"
    };

    private static Waypoint CrearWaypoint(decimal latitud, decimal longitud) => new()
    {
        Location = new Location
        {
            LatLng = new LatLng { Latitude = (double)latitud, Longitude = (double)longitud }
        }
    };

    private static int ParseDurationSeconds(string duration)
    {
        var raw = duration.EndsWith('s') ? duration[..^1] : duration;
        if (!decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) || seconds < 0)
            throw new GoogleRoutesException("Google Routes devolvió una duración no válida.");
        return (int)Math.Ceiling(seconds);
    }

    private static async Task<string> LeerErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var json = await response.Content.ReadFromJsonAsync<GoogleErrorResponse>(cancellationToken: cancellationToken);
            return json?.Error?.Message ?? $"HTTP {(int)response.StatusCode}";
        }
        catch (JsonException)
        {
            return $"HTTP {(int)response.StatusCode}";
        }
    }

    private sealed class ComputeRoutesRequest
    {
        [JsonPropertyName("origin")] public Waypoint Origin { get; init; } = new();
        [JsonPropertyName("destination")] public Waypoint Destination { get; init; } = new();
        [JsonPropertyName("travelMode")] public string TravelMode { get; init; } = "DRIVE";
        [JsonPropertyName("routingPreference")] public string RoutingPreference { get; init; } = "TRAFFIC_UNAWARE";
        [JsonPropertyName("computeAlternativeRoutes")] public bool ComputeAlternativeRoutes { get; init; }
        [JsonPropertyName("languageCode")] public string LanguageCode { get; init; } = "es-ES";
        [JsonPropertyName("units")] public string Units { get; init; } = "METRIC";
    }

    private sealed class Waypoint
    {
        [JsonPropertyName("location")] public Location Location { get; init; } = new();
    }

    private sealed class Location
    {
        [JsonPropertyName("latLng")] public LatLng LatLng { get; init; } = new();
    }

    private sealed class LatLng
    {
        [JsonPropertyName("latitude")] public double Latitude { get; init; }
        [JsonPropertyName("longitude")] public double Longitude { get; init; }
    }

    private sealed class ComputeRoutesResponse
    {
        [JsonPropertyName("routes")] public List<RouteResponse>? Routes { get; init; }
    }

    private sealed class RouteResponse
    {
        [JsonPropertyName("distanceMeters")] public int DistanceMeters { get; init; }
        [JsonPropertyName("duration")] public string? Duration { get; init; }
    }

    private sealed class GoogleErrorResponse
    {
        [JsonPropertyName("error")] public GoogleError? Error { get; init; }
    }

    private sealed class GoogleError
    {
        [JsonPropertyName("message")] public string? Message { get; init; }
    }
}

public readonly record struct ResultadoTramoRuta(int DistanciaMetros, int DuracionSegundos, bool DesdeCache);

public sealed class GoogleRoutesException : Exception
{
    public GoogleRoutesException(string message) : base(message)
    {
    }
}
