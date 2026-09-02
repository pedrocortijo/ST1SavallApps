namespace ST1Savall.API.Services;

public sealed class WialonOptions
{
    public const string SectionName = "Wialon";
    public string Host { get; set; } = "hst-api.wialon.com";
    public string AccessToken { get; set; } = string.Empty;
}
