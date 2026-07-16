namespace ST1Savall.API.Services;

public sealed class MapboxDirectionsOptions
{
    public const string SectionName = "Mapbox";

    public string AccessToken { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.mapbox.com/";
    public string Profile { get; set; } = "mapbox/driving";
    public int CacheDurationHours { get; set; } = 24;
    public int CoordinatePrecision { get; set; } = 5;
}
