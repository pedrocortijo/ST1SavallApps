using DevExpress.Blazor.Localization;

namespace ST1Savall.Shared.Services;

public class CustomDxLocalizationService : DxLocalizationService
{
    protected override string? GetString(string key)
    {
        var val = base.GetString(key);
        if (string.Equals(val, "Recursos", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(val, "Resources", StringComparison.OrdinalIgnoreCase))
        {
            return "Conductores";
        }
        return val;
    }
}
