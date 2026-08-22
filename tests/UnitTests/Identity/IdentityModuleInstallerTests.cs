using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Application.Services;
using KoiFengShuiSystem.Modules.Identity.Infrastructure;
using KoiFengShuiSystem.Modules.Identity.Infrastructure.Email;
using KoiFengShuiSystem.Modules.Identity.Infrastructure.Persistence;
using KoiFengShuiSystem.Modules.Identity.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests.Identity;

public class IdentityModuleInstallerTests
{
    [Fact]
    public void AddServices_RegistersIdentityServices()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        new IdentityModuleInstaller().AddServices(services, configuration);

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IIdentityReadStore) &&
            descriptor.ImplementationType == typeof(EfIdentityReadStore));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IIdentityWriteStore) &&
            descriptor.ImplementationType == typeof(EfIdentityWriteStore));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IIdentityElementLookup) &&
            descriptor.ImplementationType == typeof(EfIdentityElementLookup));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IJwtTokenService) &&
            descriptor.ImplementationType == typeof(JwtTokenService));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IPasswordHasher) &&
            descriptor.ImplementationType == typeof(BcryptPasswordHasher));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IPasswordResetTokenProvider) &&
            descriptor.ImplementationType == typeof(SecurePasswordResetTokenProvider));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IRefreshTokenPort) &&
            descriptor.ImplementationType == typeof(EfRefreshTokenPort));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IIdentityEmailSender) &&
            descriptor.ImplementationType == typeof(LegacyIdentityEmailSender));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IAccountService) &&
            descriptor.ImplementationType == typeof(AccountService));
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(AdminAccountService) &&
            descriptor.ImplementationType == typeof(AdminAccountService));
    }
}
