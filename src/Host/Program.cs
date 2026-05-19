using KoiFengShuiSystem.Api.Extensions;
using KoiFengShuiSystem.BusinessLogic.Services;
using KoiFengShuiSystem.BusinessLogic.Services.Implement;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.DataAccess.Repositories.Implement;
using KoiFengShuiSystem.DataAccess.Repositories.Interface;
using KoiFengShuiSystem.Host.Middleware;
using KoiFengShuiSystem.Shared.Helpers;
using KoiFengShuiSystem.Shared.Infrastructure;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using KoiFengShuiSystem.Shared.Helpers.Photos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Net.payOS;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);
builder.Configuration.AddEnvironmentVariables();

// PayOS configuration
PayOS payOS = new PayOS(
    builder.Configuration["Environment:PAYOS_CLIENT_ID"] ?? throw new Exception("Cannot find PAYOS_CLIENT_ID"),
    builder.Configuration["Environment:PAYOS_API_KEY"] ?? throw new Exception("Cannot find PAYOS_API_KEY"),
    builder.Configuration["Environment:PAYOS_CHECKSUM_KEY"] ?? throw new Exception("Cannot find PAYOS_CHECKSUM_KEY")
);
builder.Services.AddSingleton(payOS);

// Authentication and Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["AppSettings:Secret"] ?? throw new Exception("Cannot find AppSettings:Secret"))),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Controller configuration - discover controllers from API assembly
builder.Services.AddControllers()
    .AddApplicationPart(typeof(KoiFengShuiSystem.Api.Controllers.AuthController).Assembly)
    .AddApplicationPart(typeof(KoiFengShuiSystem.Modules.FengShui.Api.Controllers.CompatibilityController).Assembly)
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

// Database context
builder.Services.AddSharedInfrastructure(builder.Configuration);

// AppSettings and MailSettings configuration
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
builder.Services.Configure<CloundSettings>(builder.Configuration.GetSection(nameof(CloundSettings)));

// Service registrations
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<IMarketplaceListingService, MarketplaceListingService>();
builder.Services.AddScoped<IJwtUtils, JwtUtils>();
builder.Services.AddScoped<IFAQService, FAQService>();
builder.Services.AddScoped<IAdminPostService, AdminPostService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IAdminPostImageService, AdminPostImageService>();
builder.Services.AddScoped<ICloudService, CloudService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<ICompatibilityService, CompatibilityService>();
builder.Services.AddScoped<IConsultationService, ConsultationService>();
builder.Services.AddScoped(typeof(GenericRepository<>));
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AdminAccountService>();
builder.Services.AddScoped<UnitOfWorkRepository>();
builder.Services.AddScoped<IUnitOfWorkRepository, UnitOfWorkRepository>();
builder.Services.AddScoped<IElementService, ElementService>();
builder.Services.AddScoped<IMarketCategoryService, MarketCategoryService>();
builder.Services.AddScoped<ISubcriptionTiersService, SubcriptionTiersService>();

builder.Services.AddHostedService<TransactionSyncService>();

builder.Services.AddScoped<CloudService>();

builder.Services.AddModuleInstallersFromAssemblies(
    builder.Configuration,
    typeof(Program).Assembly,
    typeof(KoiFengShuiSystem.Modules.FengShui.Infrastructure.FengShuiModuleInstaller).Assembly);

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

// Logging configuration with Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

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

app.MapControllers();

app.Run();

public partial class Program { }
