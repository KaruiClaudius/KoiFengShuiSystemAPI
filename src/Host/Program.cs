using KoiFengShuiSystem.Shared.Kernel.Modules;
using KoiFengShuiSystem.Modules.Identity.Application.Services;
using KoiFengShuiSystem.Modules.Identity.Infrastructure.Security;
using KoiFengShuiSystem.Host.Middleware;
using KoiFengShuiSystem.Shared.Helpers;
using KoiFengShuiSystem.Shared.Infrastructure;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using KoiFengShuiSystem.Shared.Kernel.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);
builder.Configuration.AddEnvironmentVariables();

// Fail fast if placeholder credentials leaked into configuration outside development
PlaceholderConfigurationGuard.Validate(builder.Configuration, builder.Environment.EnvironmentName);

// Fail fast on a weak JWT signing secret in every environment
PlaceholderConfigurationGuard.ValidateJwtSecret(builder.Configuration);
PlaceholderConfigurationGuard.ValidateJwtIssuerAudience(builder.Configuration);

// Fail fast outside development when the admin seed password is missing or a placeholder
PlaceholderConfigurationGuard.ValidateAdminSeed(builder.Configuration, builder.Environment.EnvironmentName);

// Authentication and Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = TokenValidationParametersFactory.Create(builder.Configuration);
    });

builder.Services.AddAuthorization();

// Controller configuration - discover controllers from module API assemblies.
// All controllers now live in the module API assemblies; the legacy Api
// assembly contributes no controllers anymore.
builder.Services.AddControllers()
    .AddApplicationPart(typeof(KoiFengShuiSystem.Modules.FengShui.Api.Controllers.CompatibilityController).Assembly)
    .AddApplicationPart(typeof(KoiFengShuiSystem.Modules.Identity.Api.IdentityApiAssemblyMarker).Assembly)
    .AddApplicationPart(typeof(KoiFengShuiSystem.Modules.Community.Api.CommunityApiAssemblyMarker).Assembly)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.MaxDepth = 32;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// CORS configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Response caching
builder.Services.AddResponseCaching();
builder.Services.AddMemoryCache();

// Rate limiting (per-IP fixed windows, config-driven)
builder.Services.AddConfiguredRateLimiting(builder.Configuration);

// Database context
builder.Services.AddSharedInfrastructure(builder.Configuration);

// AppSettings and MailSettings configuration
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));

// Service registrations
builder.Services.AddHttpContextAccessor();

builder.Services.AddModuleInstallersFromAssemblies(
    builder.Configuration,
    typeof(Program).Assembly,
    typeof(KoiFengShuiSystem.Modules.FengShui.Infrastructure.FengShuiModuleInstaller).Assembly,
    typeof(KoiFengShuiSystem.Modules.Identity.Infrastructure.IdentityModuleInstaller).Assembly,
    typeof(KoiFengShuiSystem.Modules.Community.Infrastructure.CommunityModuleInstaller).Assembly);

builder.Services.AddHttpClient();

// Swagger/OpenAPI configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "KoiFengShuiSystem API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// Logging: the "Serilog" section of appsettings*.json is the single source of truth.
builder.Host.UseSerilog((context, loggerConfiguration) =>
    loggerConfiguration.ReadFrom.Configuration(context.Configuration));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "KoiFengShuiSystem API v1"));
}
else
{
    app.UseHsts();
}

// Global exception handling
app.UseMiddleware<ExceptionMiddleware>();

app.UseCors("AllowedOrigins");

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<JwtMiddleware>();
app.UseMiddleware<TrafficLoggingMiddleware>();
app.UseResponseCaching();
app.UseConfiguredRateLimiter();

app.MapControllers();

// Seed the guarded administrator account before accepting traffic. Deliberate decision:
// a database failure while seeding aborts startup loudly instead of degrading silently,
// because the admin system depends on this account being provisioned — no catch-and-continue.
using (var scope = app.Services.CreateScope())
{
    var adminAccountService = scope.ServiceProvider.GetRequiredService<AdminAccountService>();
    await adminAccountService.EnsureAdminAccountExistsAsync();
}

app.Run();

public partial class Program { }
