using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.API.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add persistent DataProtection keys
var dataProtectionKeysFolder = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys");
Directory.CreateDirectory(dataProtectionKeysFolder);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysFolder))
    .SetApplicationName("ST1Savall");

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("SavallAppsConnection") ?? throw new InvalidOperationException("Connection string 'SavallAppsConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddScoped<PlanificacionService>();
builder.Services.AddScoped<PeriodicidadObraService>();
builder.Services.AddHostedService<PeriodicidadObraHostedService>();
builder.Services.AddScoped<EstadoOperariosAusenciasService>();
builder.Services.AddScoped<ParametrosIntegracionesService>();
builder.Services.AddHttpClient<MapboxDirectionsService>(client => client.Timeout = TimeSpan.FromSeconds(20));
builder.Logging.AddFilter("System.Net.Http.HttpClient.MapboxDirectionsService", LogLevel.Warning);
builder.Services.AddScoped<CalculoRutaSolicitudService>();
builder.Services.AddHttpClient<WialonTrackingService>(client => client.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddSingleton<TiquePesajeParser>();
builder.Services.AddHttpClient<AzureDocumentIntelligenceService>(client => client.Timeout = TimeSpan.FromSeconds(30));

// Add SageGestion DbContext
var sageGestionConnectionString = builder.Configuration.GetConnectionString("SageGestionConnection") 
    ?? throw new InvalidOperationException("Connection string 'SageGestionConnection' not found.");
builder.Services.AddDbContext<SageGestionDbContext>(options =>
    options.UseSqlServer(sageGestionConnectionString));
builder.Services.AddScoped<ArticulosSage50Service>();
builder.Services.AddScoped<DatosAlbaranPlantaExcelService>();
builder.Services.AddScoped<GeneracionAlbaranServicioService>();
builder.Services.AddScoped<AlbaranPdfService>();
builder.Services.AddScoped<SmtpAlbaranService>();
builder.Services.AddScoped<DeCaPdfService>();
builder.Services.AddScoped<DeCaEmisionService>();
builder.Services.AddScoped<FtpsPdfStorageService>();

// Add SageComun DbContext
var sageComunConnectionString = builder.Configuration.GetConnectionString("SageComunConnection") 
    ?? throw new InvalidOperationException("Connection string 'SageComunConnection' not found.");
builder.Services.AddDbContext<SageComunDbContext>(options =>
    options.UseSqlServer(sageComunConnectionString));

// Add Identity Services with API Endpoints
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = IdentityConstants.BearerScheme;
    options.DefaultChallengeScheme = IdentityConstants.BearerScheme;
});
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var sageGestionContext = services.GetRequiredService<SageGestionDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        // Las columnas de Parametros deben existir antes de que DbInitializer
        // consulte la tabla en instalaciones actualizadas desde versiones previas.
        await context.Database.ExecuteSqlRawAsync(@"
            IF COL_LENGTH('Parametros', 'PathDocumentos') IS NULL
                ALTER TABLE Parametros ADD PathDocumentos VARCHAR(255) NULL;
            IF COL_LENGTH('Parametros', 'AutorizacionTransporte') IS NULL
                ALTER TABLE Parametros ADD AutorizacionTransporte NVARCHAR(50) NULL;
            IF COL_LENGTH('Parametros', 'UrlBasePublicaDeCa') IS NULL
                ALTER TABLE Parametros ADD UrlBasePublicaDeCa NVARCHAR(255) NULL;
            IF COL_LENGTH('Parametros', 'FtpsHost') IS NULL
                ALTER TABLE Parametros ADD FtpsHost NVARCHAR(255) NULL;
            IF COL_LENGTH('Parametros', 'FtpsPuerto') IS NULL
                ALTER TABLE Parametros ADD FtpsPuerto INT NULL;
            IF COL_LENGTH('Parametros', 'FtpsRutaRemota') IS NULL
                ALTER TABLE Parametros ADD FtpsRutaRemota NVARCHAR(255) NULL;
            IF COL_LENGTH('Parametros', 'FtpsUsuario') IS NULL
                ALTER TABLE Parametros ADD FtpsUsuario NVARCHAR(100) NULL;");
        // Esta columna la utiliza el modelo de Solicitud; debe existir antes de que DbInitializer consulte solicitudes.
        await context.Database.ExecuteSqlRawAsync(@"
            IF COL_LENGTH('Solicitudes', 'IdPeriodicidadObraEjecucion') IS NULL
                ALTER TABLE Solicitudes ADD IdPeriodicidadObraEjecucion INT NULL;
            IF COL_LENGTH('Solicitudes', 'UrlPublicaDeCa') IS NULL
                ALTER TABLE Solicitudes ADD UrlPublicaDeCa NVARCHAR(500) NULL;");
        await DbInitializer.InitializeAsync(context, sageGestionContext, userManager, roleManager);
        await services.GetRequiredService<ParametrosIntegracionesService>().MigrarDesdeConfiguracionAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar/v1"));
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Map Identity Endpoints
app.MapIdentityApi<ApplicationUser>();

app.MapControllers();

app.Run();
