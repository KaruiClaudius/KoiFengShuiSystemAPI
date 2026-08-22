using KoiFengShuiSystem.Shared.Kernel.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace UnitTests.Security;

public class PlaceholderConfigurationGuardTests
{
    private const string PlaceholderValue = "ROTATE_ME__SET_VIA_USER_SECRETS_OR_ENV";
    private const string Issuer = "KoiFengShuiSystem";
    private const string Audience = "KoiFengShuiSystemClients";

    [Fact]
    public void Validate_ProductionEnvironmentWithPlaceholderKey_ThrowsInvalidOperationExceptionNamingTheKey()
    {
        var configuration = BuildConfig(("AppSettings:Secret", PlaceholderValue));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            PlaceholderConfigurationGuard.Validate(configuration, "Production"));

        Assert.Contains("AppSettings:Secret", exception.Message);
        Assert.Contains("user-secrets", exception.Message);
    }

    [Fact]
    public void Validate_DevelopmentEnvironmentWithPlaceholderKeys_DoesNotThrow()
    {
        var configuration = BuildConfig(
            ("AppSettings:Secret", PlaceholderValue),
            ("MailSettings:Password", PlaceholderValue));

        PlaceholderConfigurationGuard.Validate(configuration, "Development");
    }

    [Fact]
    public void Validate_NoPlaceholderValues_PassesSilentlyInProduction()
    {
        var configuration = BuildConfig(
            ("AppSettings:Secret", "real-secret-value"),
            ("MailSettings:Password", ""),
            ("AppSettings:Missing", null),
            ("AllowedOrigins:0", "https://example.com"));

        PlaceholderConfigurationGuard.Validate(configuration, "Production");
    }

    [Fact]
    public void Validate_MultipleNestedPlaceholderKeys_ListsAllFullKeyPaths()
    {
        var configuration = BuildConfig(
            ("AppSettings:Secret", PlaceholderValue),
            ("MailSettings:Smtp:Password", PlaceholderValue));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            PlaceholderConfigurationGuard.Validate(configuration, "Staging"));

        Assert.Contains("AppSettings:Secret", exception.Message);
        Assert.Contains("MailSettings:Smtp:Password", exception.Message);
    }

    [Fact]
    public void Validate_DevelopmentEnvironment_LogsWarningThroughLoggerInsteadOfThrowing()
    {
        var configuration = BuildConfig(("MailSettings:Smtp:Password", PlaceholderValue));
        var logger = new CapturingLogger();

        PlaceholderConfigurationGuard.Validate(configuration, "Development", logger);

        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Warning && entry.Message.Contains("MailSettings:Smtp:Password"));
    }

    // --- ValidateJwtSecret ---

    [Fact]
    public void ValidateJwtSecret_SecretShorterThan32Chars_ThrowsInvalidOperationException()
    {
        var configuration = BuildConfig(("AppSettings:Secret", "short-secret"));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            PlaceholderConfigurationGuard.ValidateJwtSecret(configuration));

        Assert.Contains("AppSettings:Secret", exception.Message);
        Assert.Contains("32", exception.Message);
    }

    [Fact]
    public void ValidateJwtSecret_Exactly32CharSecret_Passes()
    {
        var configuration = BuildConfig(("AppSettings:Secret", new string('a', 32)));

        PlaceholderConfigurationGuard.ValidateJwtSecret(configuration);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateJwtSecret_MissingOrBlankSecret_ThrowsInvalidOperationException(string? secret)
    {
        var configuration = BuildConfig(("AppSettings:Secret", secret));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            PlaceholderConfigurationGuard.ValidateJwtSecret(configuration));

        Assert.Contains("AppSettings:Secret", exception.Message);
    }

    [Fact]
    public void ValidateJwtSecret_LongStrongSecret_Passes()
    {
        var configuration = BuildConfig(("AppSettings:Secret", "test-secret-key-that-is-at-least-32-bytes-long-for-hmac"));

        PlaceholderConfigurationGuard.ValidateJwtSecret(configuration);
    }

    // --- ValidateJwtIssuerAudience ---

    [Fact]
    public void ValidateJwtIssuerAudience_MissingIssuer_ThrowsInvalidOperationExceptionNamingTheKey()
    {
        var configuration = BuildConfig(("AppSettings:Issuer", null), ("AppSettings:Audience", Audience));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            PlaceholderConfigurationGuard.ValidateJwtIssuerAudience(configuration));

        Assert.Contains("AppSettings:Issuer", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateJwtIssuerAudience_EmptyOrWhitespaceAudience_ThrowsInvalidOperationExceptionNamingTheKey(string? audience)
    {
        var configuration = BuildConfig(("AppSettings:Issuer", Issuer), ("AppSettings:Audience", audience));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            PlaceholderConfigurationGuard.ValidateJwtIssuerAudience(configuration));

        Assert.Contains("AppSettings:Audience", exception.Message);
    }

    [Fact]
    public void ValidateJwtIssuerAudience_MissingIssuerAndAudience_ListsBothOffendingKeys()
    {
        var configuration = BuildConfig();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            PlaceholderConfigurationGuard.ValidateJwtIssuerAudience(configuration));

        Assert.Contains("AppSettings:Issuer", exception.Message);
        Assert.Contains("AppSettings:Audience", exception.Message);
    }

    [Fact]
    public void ValidateJwtIssuerAudience_BothPresent_Passes()
    {
        var configuration = BuildConfig(("AppSettings:Issuer", Issuer), ("AppSettings:Audience", Audience));

        PlaceholderConfigurationGuard.ValidateJwtIssuerAudience(configuration);
    }

    private static IConfiguration BuildConfig(params (string Key, string? Value)[] settings) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(s => new KeyValuePair<string, string?>(s.Key, s.Value)))
            .Build();

    private sealed class CapturingLogger : ILogger
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
