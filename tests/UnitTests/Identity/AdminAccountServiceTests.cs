using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Application.Services;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Modules.Identity.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.Identity;

public class AdminAccountServiceTests
{
    private const string DevAdminEmail = "admin@localhost.dev";
    private const string DevAdminPassword = "DevAdmin_123!";
    private const string PlaceholderValue = "ROTATE_ME__SET_VIA_USER_SECRETS_OR_ENV";

    private readonly Mock<IAccountService> _accountService = new();
    private readonly BcryptPasswordHasher _passwordHasher = new();

    [Fact]
    public async Task EnsureAdminAccountExists_NoExistingAdmin_StoresHashedPasswordNotRaw()
    {
        Account? stored = null;
        _accountService
            .Setup(s => s.GetAccountByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((Account?)null);
        _accountService
            .Setup(s => s.CreateAsync(It.IsAny<Account>()))
            .Callback<Account>(a => stored = a)
            .ReturnsAsync((Account a) => a);

        var service = CreateService(
            ("AdminAccount:Email", DevAdminEmail),
            ("AdminAccount:Password", DevAdminPassword),
            ("AdminAccount:FullName", "Ops Administrator"));

        await service.EnsureAdminAccountExistsAsync();

        Assert.NotNull(stored);
        Assert.Equal(DevAdminEmail, stored!.Email);
        Assert.Equal("Ops Administrator", stored.FullName);
        Assert.StartsWith("$2", stored.Password);
        Assert.NotEqual(DevAdminPassword, stored.Password);
    }

    [Fact]
    public async Task EnsureAdminAccountExists_AdminAlreadyPresent_DoesNotCreateAgain()
    {
        _accountService
            .Setup(s => s.GetAccountByEmailAsync(DevAdminEmail))
            .ReturnsAsync(new Account { Email = DevAdminEmail, Password = "$2existing$hash" });

        var service = CreateService(
            ("AdminAccount:Email", DevAdminEmail),
            ("AdminAccount:Password", DevAdminPassword));

        await service.EnsureAdminAccountExistsAsync();

        _accountService.Verify(s => s.CreateAsync(It.IsAny<Account>()), Times.Never);
    }

    [Fact]
    public async Task EnsureAdminAccountExists_SeedsExactlyOneAdminOnSecondCall()
    {
        Account? seeded = null;
        _accountService
            .Setup(s => s.GetAccountByEmailAsync(DevAdminEmail))
            .ReturnsAsync(() => seeded);
        _accountService
            .Setup(s => s.CreateAsync(It.IsAny<Account>()))
            .Callback<Account>(a => seeded = a)
            .ReturnsAsync((Account a) => a);

        var service = CreateService(
            ("AdminAccount:Email", DevAdminEmail),
            ("AdminAccount:Password", DevAdminPassword));

        await service.EnsureAdminAccountExistsAsync();
        await service.EnsureAdminAccountExistsAsync();

        _accountService.Verify(s => s.CreateAsync(It.IsAny<Account>()), Times.Once);
    }

    [Fact]
    public async Task EnsureAdminAccountExists_SectionAbsentEntirely_SkipsWithoutThrowing()
    {
        var service = CreateService(("AppSettings:Issuer", "KoiFengShuiSystem"));

        await service.EnsureAdminAccountExistsAsync();

        _accountService.Verify(s => s.CreateAsync(It.IsAny<Account>()), Times.Never);
    }

    [Fact]
    public async Task EnsureAdminAccountExists_PlaceholderCredentials_SkipsWithoutCreating()
    {
        var service = CreateService(
            ("AdminAccount:Email", PlaceholderValue),
            ("AdminAccount:Password", PlaceholderValue));

        await service.EnsureAdminAccountExistsAsync();

        _accountService.Verify(s => s.CreateAsync(It.IsAny<Account>()), Times.Never);
    }

    [Fact]
    public async Task EnsureAdminAccountExists_SectionPresentButCredentialsMissing_ThrowsInvalidOperationException()
    {
        var service = CreateService(
            ("AdminAccount:Email", ""),
            ("AdminAccount:Password", ""));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.EnsureAdminAccountExistsAsync());

        Assert.Contains("AdminAccount", exception.Message);
    }

    [Fact]
    public async Task EnsureAdminAccountExists_AdminAlreadyPresent_LogsOutcome()
    {
        _accountService
            .Setup(s => s.GetAccountByEmailAsync(DevAdminEmail))
            .ReturnsAsync(new Account { Email = DevAdminEmail });
        var logger = new CapturingLogger();

        var service = CreateService(logger,
            ("AdminAccount:Email", DevAdminEmail),
            ("AdminAccount:Password", DevAdminPassword));

        await service.EnsureAdminAccountExistsAsync();

        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Information && entry.Message.Contains(DevAdminEmail));
    }

    [Fact]
    public async Task EnsureAdminAccountExists_CreatesAdmin_LogsCreatedOutcome()
    {
        _accountService
            .Setup(s => s.GetAccountByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((Account?)null);
        _accountService
            .Setup(s => s.CreateAsync(It.IsAny<Account>()))
            .ReturnsAsync((Account a) => a);
        var logger = new CapturingLogger();

        var service = CreateService(logger,
            ("AdminAccount:Email", DevAdminEmail),
            ("AdminAccount:Password", DevAdminPassword));

        await service.EnsureAdminAccountExistsAsync();

        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Information && entry.Message.Contains("created"));
    }

    private AdminAccountService CreateService(params (string Key, string? Value)[] settings) =>
        CreateService(Mock.Of<ILogger<AdminAccountService>>(), settings);

    private AdminAccountService CreateService(
        ILogger<AdminAccountService> logger, params (string Key, string? Value)[] settings) =>
        new AdminAccountService(
            _accountService.Object,
            _passwordHasher,
            new ConfigurationBuilder()
                .AddInMemoryCollection(settings.Select(s => new KeyValuePair<string, string?>(s.Key, s.Value)))
                .Build(),
            logger);

    private sealed class CapturingLogger : ILogger<AdminAccountService>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
