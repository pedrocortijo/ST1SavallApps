using System.Net;
using Microsoft.Maui.Storage;
using ST1Savall.Shared.Services;

namespace ST1Savall.Services;

public sealed class ApiEndpointConfiguration : IApiEndpointConfiguration
{
    private const string PreferenceKey = "api_base_url";

    public string? BaseUrl => Preferences.Default.Get<string?>(PreferenceKey, null);
    public bool IsConfigured => TryGetBaseUri(out _);

    public void SetBaseUrl(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl?.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.UserInfo))
            throw new ArgumentException("Indique una URL HTTP o HTTPS válida.", nameof(baseUrl));

        Preferences.Default.Set(PreferenceKey, new Uri(uri.GetLeftPart(UriPartial.Authority) + uri.AbsolutePath.TrimEnd('/') + "/").AbsoluteUri);
    }

    internal bool TryGetBaseUri(out Uri? baseUri) => Uri.TryCreate(BaseUrl, UriKind.Absolute, out baseUri);
}

public sealed class ApiEndpointHandler : DelegatingHandler
{
    private readonly ApiEndpointConfiguration endpointConfiguration;

    public ApiEndpointHandler(ApiEndpointConfiguration endpointConfiguration)
        : base(new HttpClientHandler())
    {
        this.endpointConfiguration = endpointConfiguration;
    }
    private static readonly Uri PlaceholderBaseUri = new("http://api.local/");

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!endpointConfiguration.TryGetBaseUri(out var baseUri) || baseUri is null)
            throw new InvalidOperationException("La dirección de la API no está configurada.");

        if (request.RequestUri is { Host: "api.local" } requestUri)
            request.RequestUri = new Uri(baseUri, requestUri.PathAndQuery.TrimStart('/'));

        return base.SendAsync(request, cancellationToken);
    }

    public static Uri PlaceholderUri => PlaceholderBaseUri;
}