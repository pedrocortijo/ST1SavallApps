using System.Text.Json;
using Microsoft.Extensions.Options;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Services;

/// <summary>Cliente de solo lectura para la Remote API clásica de Wialon.</summary>
public sealed class WialonTrackingService(
    HttpClient httpClient,
    ParametrosIntegracionesService integraciones,
    ILogger<WialonTrackingService> logger)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ParametrosIntegracionesService _integraciones = integraciones;
    private readonly ILogger<WialonTrackingService> _logger = logger;
    private readonly SemaphoreSlim _sessionLock = new(1, 1);
    private string? _sessionId;
    private DateTime _lastActivityUtc;
    private string? _host;

    public async Task<bool> ConfiguradoAsync(CancellationToken cancellationToken = default) => !string.IsNullOrWhiteSpace((await _integraciones.ObtenerWialonAsync(cancellationToken)).AccessToken);

    public async Task<WialonUnitPositionResult> ObtenerPosicionAsync(string unitUniqueId, CancellationToken cancellationToken = default)
    {
        var unit = await BuscarUnidadAsync(unitUniqueId, cancellationToken);
        if (unit.Error is not null || unit.Item is not { } item)
            return new WialonUnitPositionResult(null, unit.Error);

        if (!item.Element.TryGetProperty("pos", out var pos) || pos.ValueKind != JsonValueKind.Object)
            return new WialonUnitPositionResult(new WialonUnitPosition(item.Nombre, null, null, null, null, null), null);

        return new WialonUnitPositionResult(new WialonUnitPosition(
            item.Nombre, GetDouble(pos, "y"), GetDouble(pos, "x"), GetDouble(pos, "s"),
            GetInt(pos, "c"), GetUnixDate(pos, "t")), null);
    }

    private async Task<WialonUnitLookupResult> BuscarUnidadAsync(string unitUniqueId, CancellationToken cancellationToken)
    {
        await AsegurarSesionAsync(cancellationToken);
        var response = await EnviarAsync("core/search_items", new
        {
            spec = new { itemsType = "avl_unit", propName = "sys_unique_id", propValueMask = unitUniqueId.Trim(), sortType = "sys_name", propType = "property", or_logic = false },
            force = 1, flags = 1025, from = 0, to = 1
        }, cancellationToken);
        if (response.Error is not null)
            return new WialonUnitLookupResult(null, response.Error);

        if (response.Document?.RootElement.TryGetProperty("items", out var items) != true || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0)
            return new WialonUnitLookupResult(null, "No se ha encontrado una unidad Wialon con este IMEI.");

        var element = items[0];
        if (!element.TryGetProperty("id", out var idElement) || !idElement.TryGetInt64(out var id))
            return new WialonUnitLookupResult(null, "Wialon no devolvió el identificador interno de la unidad.");
        var nombre = element.TryGetProperty("nm", out var nameElement) ? nameElement.GetString() : null;
        return new WialonUnitLookupResult(new WialonUnitItem(id, nombre, element), null);
    }

    private async Task AsegurarSesionAsync(CancellationToken cancellationToken)
    {
        var configuracion = await _integraciones.ObtenerWialonAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(configuracion.AccessToken))
            throw new InvalidOperationException("Wialon no está configurado en Parámetros.");
        _host = configuracion.Host;
        if (!string.IsNullOrWhiteSpace(_sessionId) && DateTime.UtcNow - _lastActivityUtc < TimeSpan.FromMinutes(4.5)) return;

        await _sessionLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_sessionId) && DateTime.UtcNow - _lastActivityUtc < TimeSpan.FromMinutes(4.5)) return;
            var login = await EnviarAsync("token/login", new { token = configuracion.AccessToken }, cancellationToken, false, configuracion.Host);
            if (login.Error is not null || login.Document?.RootElement.TryGetProperty("eid", out var session) != true)
                throw new InvalidOperationException(login.Error ?? "Wialon no devolvió una sesión válida.");
            _sessionId = session.GetString();
            if (string.IsNullOrWhiteSpace(_sessionId)) throw new InvalidOperationException("Wialon no devolvió una sesión válida.");
            _lastActivityUtc = DateTime.UtcNow;
        }
        finally { _sessionLock.Release(); }
    }

    private async Task<WialonResponse> EnviarAsync(string service, object parameters, CancellationToken cancellationToken, bool incluirSesion = true, string? host = null)
    {
        var url = $"wialon/ajax.html?svc={service}&params={JsonSerializer.Serialize(parameters)}";
        if (incluirSesion && !string.IsNullOrWhiteSpace(_sessionId)) url += $"&sid={_sessionId}";
        host ??= _host;
        if (!string.IsNullOrWhiteSpace(host)) { if (!host.StartsWith("http", StringComparison.OrdinalIgnoreCase)) host = $"https://{host}"; url = $"{host.TrimEnd('/')}/{url}"; }
        using var response = await _httpClient.PostAsync(url, null, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new WialonResponse(null, $"Wialon respondió HTTP {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync(cancellationToken)}");

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind == JsonValueKind.Object && document.RootElement.TryGetProperty("error", out var error))
                return new WialonResponse(document, $"Wialon devolvió el error {error.GetRawText()}.");
            _lastActivityUtc = DateTime.UtcNow;
            return new WialonResponse(document, null);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Respuesta no válida de Wialon para {Service}", service);
            return new WialonResponse(null, "Wialon devolvió una respuesta no válida.");
        }
    }

    private static double? GetDouble(JsonElement element, string property) => element.TryGetProperty(property, out var value) && value.TryGetDouble(out var result) ? result : null;
    private static int? GetInt(JsonElement element, string property) => element.TryGetProperty(property, out var value) && value.TryGetInt32(out var result) ? result : null;
    private static DateTime? GetUnixDate(JsonElement element, string property) => element.TryGetProperty(property, out var value) && value.TryGetInt64(out var result) ? DateTimeOffset.FromUnixTimeSeconds(result).UtcDateTime : null;

    private sealed record WialonResponse(JsonDocument? Document, string? Error);
    private sealed record WialonUnitItem(long Id, string? Nombre, JsonElement Element);
    private sealed record WialonUnitLookupResult(WialonUnitItem? Item, string? Error);
}

public sealed record WialonUnitPosition(string? Nombre, double? Latitud, double? Longitud, double? Velocidad, int? Rumbo, DateTime? FechaUtc);
public sealed record WialonUnitPositionResult(WialonUnitPosition? Posicion, string? Error);
