using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Application.Services;
using KoiFengShuiSystem.Modules.Identity.Infrastructure.Email;
using KoiFengShuiSystem.Modules.Identity.Infrastructure.FengShui;
using KoiFengShuiSystem.Modules.Identity.Infrastructure.Persistence;
using KoiFengShuiSystem.Modules.Identity.Infrastructure.Security;
using KoiFengShuiSystem.Shared.Kernel.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KoiFengShuiSystem.Modules.Identity.Infrastructure;

public class IdentityModuleInstaller : IModuleInstaller
{
    public void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IIdentityReadStore, EfIdentityReadStore>();
        services.AddScoped<IIdentityWriteStore, EfIdentityWriteStore>();
        services.AddScoped<IIdentityElementLookup, EfIdentityElementLookup>();
        services.AddSingleton<IElementCalculator, FengShuiElementCalculator>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IPasswordResetTokenProvider, SecurePasswordResetTokenProvider>();
        services.AddScoped<IRefreshTokenPort, EfRefreshTokenPort>();
        services.AddScoped<IIdentityEmailSender, LegacyIdentityEmailSender>();
        services.AddScoped<SessionIssuer>();
        services.AddScoped<PasswordResetService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<AdminAccountService>();
    }
}
