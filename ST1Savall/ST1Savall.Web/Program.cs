using ST1Savall.Shared.Services;
using ST1Savall.Web.Components;
using ST1Savall.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options => options.DetailedErrors = true);

builder.Services.AddControllersWithViews();

builder.Services.AddLocalization();

builder.Services.AddDevExpressBlazor(options =>
{
    options.BootstrapVersion = DevExpress.Blazor.BootstrapVersion.v5;
    options.SizeMode = DevExpress.Blazor.SizeMode.Medium;
});
// Add device-specific services used by the ST1Savall.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();

// Configure authentication state provider using local storage (decoupled from direct DB access)
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, WebAuthenticationStateProvider>();

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
})
.AddCookie("Cookies", options =>
{
    options.LoginPath = "/login";
});

// HTTP Client pointing to the REST API project.
var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://192.168.1.230:4040/";
if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var apiBaseAddress))
{
    throw new InvalidOperationException("La configuración Api:BaseUrl no contiene una dirección válida.");
}

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = apiBaseAddress,
    // Evita que una API detenida bloquee el circuito interactivo de Blazor.
    Timeout = TimeSpan.FromSeconds(12)
});

builder.Services.AddScoped<ST1Savall.Web.Services.IPortalAccessRequestService, ST1Savall.Web.Services.PortalAccessRequestService>();
builder.Services.AddScoped<ST1Savall.Shared.Services.IUserDisplayService, ST1Savall.Web.Services.ServerUserDisplayService>();
builder.Services.AddScoped<ST1Savall.Shared.Services.IAuthService, ST1Savall.Web.Services.WebAuthService>();
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

var app = builder.Build();

app.UseRequestLocalization("es-ES");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(ST1Savall.Shared._Imports).Assembly);

app.Run();
