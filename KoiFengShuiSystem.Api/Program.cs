
using KoiFengShuiSystem.Api.Authorization;
using KoiFengShuiSystem.BusinessLogic.Services;
using KoiFengShuiSystem.BusinessLogic.Services.Implement;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.DataAccess.Repositories.Implement;
using KoiFengShuiSystem.DataAccess.Repositories.Interface;
using KoiFengShuiSystem.Shared.Helpers;
using KoiFengShuiSystem.Shared.Helpers.Photos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Net.payOS;
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["AppSettings:Secret"])),
            ValidateIssuer = false,
            ValidateAudience = false,
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

// Database context
builder.Services.AddDbContext<KoiFengShuiContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<ICloudService, CloudService>();

//builder.Services.AddScoped<ITransactionService, TransactionService>();
//builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ICompatibilityService, CompatibilityService>();
builder.Services.AddScoped<IConsultationService, ConsultationService>();
builder.Services.AddScoped(typeof(GenericRepository<>));
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<AdminAccountService>();
builder.Services.AddScoped<IUnitOfWorkRepository, UnitOfWorkRepository>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddHostedService<TransactionSyncService>();

builder.Services.AddScoped<CloudService>();

builder.Services.AddHttpClient();
//builder.Services.AddSingleton<IVnPayService, VnPayService>();

// Controller configuration
builder.Services.AddControllers()
    .AddJsonOptions(x => x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);


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

app.UseCors(x => x
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<JwtMiddleware>();
app.UseMiddleware<TrafficLoggingMiddleware>();

app.MapControllers();

app.Run();
