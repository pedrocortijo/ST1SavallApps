using Microsoft.Extensions.Logging;
using ST1Savall.Services;
using ST1Savall.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Plugin.Maui.OCR;

namespace ST1Savall
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseOcr()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            // Add device-specific services used by the ST1Savall.Shared project
            builder.Services.AddSingleton<IFormFactor, FormFactor>();

            builder.Services.AddMauiBlazorWebView();

            builder.Services.AddDevExpressBlazor(options => {
                options.BootstrapVersion = DevExpress.Blazor.BootstrapVersion.v5;
                options.SizeMode = DevExpress.Blazor.SizeMode.Medium;
            });

            builder.Services.AddSingleton<IUserDisplayService, DesktopUserDisplayService>();
            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<AuthenticationStateProvider, DesktopAuthenticationStateProvider>();
            builder.Services.AddSingleton<ApiEndpointConfiguration>();
            builder.Services.AddSingleton<ST1Savall.Shared.Services.IApiEndpointConfiguration>(sp => sp.GetRequiredService<ApiEndpointConfiguration>());
            builder.Services.AddTransient<ApiEndpointHandler>();
            builder.Services.AddScoped(sp => new HttpClient(sp.GetRequiredService<ApiEndpointHandler>())
            {
                BaseAddress = ApiEndpointHandler.PlaceholderUri
            });
            builder.Services.AddScoped<IAuthService, DesktopAuthService>();
            builder.Services.AddScoped<ST1Savall.Shared.Services.ObrasMntoGridState>();
            builder.Services.AddScoped<ST1Savall.Shared.Services.SolicitudesGridState>();
            builder.Services.AddScoped<ST1Savall.Shared.Services.HomeGridState>();
            builder.Services.AddScoped<DevExpress.Blazor.Localization.IDxLocalizationService, ST1Savall.Shared.Services.CustomDxLocalizationService>();

            DevExpress.Utils.Localization.XtraLocalizer.QueryLocalizedString += (sender, e) =>
            {
                if (string.Equals(e.Value, "Recursos", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(e.Value, "Resources", StringComparison.OrdinalIgnoreCase))
                {
                    e.Value = "Conductores";
                }
            };

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
