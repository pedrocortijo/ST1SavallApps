namespace ST1Savall.API.Services;

public sealed class GoogleRoutesOptions
{
    public const string SectionName = "GoogleRoutes";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://routes.googleapis.com/";
    public string TravelMode { get; set; } = "DRIVE";
    public string RoutingPreference { get; set; } = "TRAFFIC_UNAWARE";
    public int CacheDurationHours { get; set; } = 24;
    public int CoordinatePrecision { get; set; } = 5;
}
