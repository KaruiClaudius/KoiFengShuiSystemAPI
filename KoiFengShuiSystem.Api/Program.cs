using KoiFengShuiSystem.Api.Authorization;
using KoiFengShuiSystem.Api.Extensions;
using KoiFengShuiSystem.BusinessLogic.Services;
using KoiFengShuiSystem.BusinessLogic.Services.Implement;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.DataAccess.Repositories.Implement;
using KoiFengShuiSystem.DataAccess.Repositories.Interface;
using KoiFengShuiSystem.Modules.Identity.Application.Services;
using KoiFengShuiSystem.Shared.Helpers;
using KoiFengShuiSystem.Shared.Infrastructure;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using KoiFengShuiSystem.Shared.Helpers.Photos;
using KoiFengShuiSystem.Shared.Kernel.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
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

// Authentication and Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["AppSettings:Secret"] ?? throw new Exception("Cannot find AppSettings:Secret"))),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["AppSettings:Issuer"] ?? throw new Exception("Cannot find AppSettings:Issuer"),
            ValidateAudience = true,
            ValidAudience = builder.Configuration["AppSettings:Audience"] ?? throw new Exception("Cannot find AppSettings:Audience"),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Controller configuration
builder.Services.AddControllers()
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
builder.Services.Configure<CloundSettings>(builder.Configuration.GetSection(nameof(CloundSettings)));
// Service registrations
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IAdminPostService, AdminPostService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IAdminPostImageService, AdminPostImageService>();
builder.Services.AddScoped<ICloudService, CloudService>();
builder.Services.AddScoped<ICompatibilityService, CompatibilityService>();
builder.Services.AddScoped<IConsultationService, ConsultationService>();
builder.Services.AddScoped(typeof(GenericRepository<>));
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<UnitOfWorkRepository>();
builder.Services.AddScoped<IUnitOfWorkRepository, UnitOfWorkRepository>();
builder.Services.AddScoped<IElementService, ElementService>();

builder.Services.AddScoped<CloudService>();

builder.Services.AddModuleInstallersFromAssemblies(
    builder.Configuration,
    typeof(Program).Assembly,
    typeof(KoiFengShuiSystem.Modules.Identity.Infrastructure.IdentityModuleInstaller).Assembly);

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

// Logging configuration
builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "KoiFengShuiSystem API v1"));
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

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

app.Run();
