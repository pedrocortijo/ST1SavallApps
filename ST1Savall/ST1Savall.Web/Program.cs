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

// HTTP Client pointing to the REST API project
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://localhost:7284/") });

builder.Services.AddScoped<ST1Savall.Web.Services.IPortalAccessRequestService, ST1Savall.Web.Services.PortalAccessRequestService>();
builder.Services.AddScoped<ST1Savall.Shared.Services.IUserDisplayService, ST1Savall.Web.Services.ServerUserDisplayService>();
builder.Services.AddScoped<ST1Savall.Shared.Services.IAuthService, ST1Savall.Web.Services.WebAuthService>();
builder.Services.AddScoped<ST1Savall.Shared.Services.ObrasMntoGridState>();

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
