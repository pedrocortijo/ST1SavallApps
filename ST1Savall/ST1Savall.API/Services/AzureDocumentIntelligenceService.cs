using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ST1Savall.API.Services;

public sealed class AzureDocumentIntelligenceOptions
{
    public string? Endpoint { get; init; }
    public string? Key { get; init; }
    public string ModelId { get; init; } = "prebuilt-read";
}

public sealed class AzureDocumentIntelligenceService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<AzureDocumentIntelligenceService> logger)
{
    private readonly AzureDocumentIntelligenceOptions options =
        configuration.GetSection("AzureDocumentIntelligence").Get<AzureDocumentIntelligenceOptions>() ?? new();

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(options.Endpoint) && !string.IsNullOrWhiteSpace(options.Key);

    public async Task<string?> ExtraerTextoAsync(byte[] imagen, CancellationToken cancellationToken)
    {
        if (!IsConfigured) return null;

        var endpoint = options.Endpoint!.TrimEnd('/');
        var url = $"{endpoint}/documentintelligence/documentModels/{options.ModelId}:analyze?api-version=2024-11-30&locale=es-ES";
        using var solicitud = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new { base64Source = Convert.ToBase64String(imagen) }),
                Encoding.UTF8,
                "application/json")
        };
        solicitud.Headers.Add("Ocp-Apim-Subscription-Key", options.Key);

        using var respuesta = await httpClient.SendAsync(solicitud, cancellationToken);
        if (!respuesta.IsSuccessStatusCode)
        {
            var detalle = await respuesta.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Azure Document Intelligence rechazó el tique ({(int)respuesta.StatusCode}): {detalle}");
        }

        var ubicacionOperacion = respuesta.Headers.Location?.ToString();
        if (string.IsNullOrWhiteSpace(ubicacionOperacion))
            throw new InvalidOperationException("Azure Document Intelligence no devolvió la ubicación de la operación.");

        for (var intento = 0; intento < 30; intento++)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            using var consulta = new HttpRequestMessage(HttpMethod.Get, ubicacionOperacion);
            consulta.Headers.Add("Ocp-Apim-Subscription-Key", options.Key);
            using var resultado = await httpClient.SendAsync(consulta, cancellationToken);
            resultado.EnsureSuccessStatusCode();

            using var documento = JsonDocument.Parse(await resultado.Content.ReadAsStreamAsync(cancellationToken));
            var raiz = documento.RootElement;
            var estado = raiz.TryGetProperty("status", out var estadoJson) ? estadoJson.GetString() : null;
            if (string.Equals(estado, "succeeded", StringComparison.OrdinalIgnoreCase))
            {
                var contenido = raiz.GetProperty("analyzeResult").GetProperty("content").GetString();
                logger.LogInformation("Azure Document Intelligence leyó {Caracteres} caracteres del tique.", contenido?.Length ?? 0);
                return contenido;
            }
            if (string.Equals(estado, "failed", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Azure Document Intelligence no pudo analizar el tique.");
        }

        throw new TimeoutException("Azure Document Intelligence tardó demasiado en analizar el tique.");
    }
}