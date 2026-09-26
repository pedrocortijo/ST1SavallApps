namespace ST1Savall.Shared.Services;

public interface IApiEndpointConfiguration
{
    string? BaseUrl { get; }
    bool IsConfigured { get; }
    void SetBaseUrl(string baseUrl);
}