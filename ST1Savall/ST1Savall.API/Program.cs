using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add DbContext
var connectionString = builder.Configuration.GetConnectionString("SavallAppsConnection") ?? throw new InvalidOperationException("Connection string 'SavallAppsConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add SageGestion DbContext
var sageGestionConnectionString = builder.Configuration.GetConnectionString("SageGestionConnection") 
    ?? throw new InvalidOperationException("Connection string 'SageGestionConnection' not found.");
builder.Services.AddDbContext<SageGestionDbContext>(options =>
    options.UseSqlServer(sageGestionConnectionString));

// Add SageComun DbContext
var sageComunConnectionString = builder.Configuration.GetConnectionString("SageComunConnection") 
    ?? throw new InvalidOperationException("Connection string 'SageComunConnection' not found.");
builder.Services.AddDbContext<SageComunDbContext>(options =>
    options.UseSqlServer(sageComunConnectionString));

// Add Identity Services with API Endpoints
builder.Services.AddAuthentication();
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
        await DbInitializer.InitializeAsync(context, sageGestionContext, userManager, roleManager);
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
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// Map Identity Endpoints
app.MapIdentityApi<ApplicationUser>();

app.MapControllers();

app.Run();