using Microsoft.Extensions.Logging;
using ST1Savall.Services;
using ST1Savall.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace ST1Savall
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
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
#if ANDROID
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://192.168.18.32:5077/") });
#else
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7284/") });
#endif
            builder.Services.AddScoped<IAuthService, DesktopAuthService>();
            builder.Services.AddScoped<ST1Savall.Shared.Services.ObrasMntoGridState>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
