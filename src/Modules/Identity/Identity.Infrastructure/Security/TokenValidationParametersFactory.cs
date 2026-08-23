using System.Text;
using KoiFengShuiSystem.Shared.Helpers;
using KoiFengShuiSystem.Shared.Kernel.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;

namespace KoiFengShuiSystem.Modules.Identity.Infrastructure.Security;

/// <summary>
/// Single source of <see cref="TokenValidationParameters"/> for every JWT consumer in the
/// solution: both host <c>AddJwtBearer</c> registrations and in-process token validation.
/// </summary>
public static class TokenValidationParametersFactory
{
    /// <summary>
    /// Creates parameters matching the hosts' bearer semantics: issuer and audience are always
    /// validated against fail-closed configuration reads enforced by the shared startup guards.
    /// </summary>
    public static TokenValidationParameters Create(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        PlaceholderConfigurationGuard.ValidateJwtSecret(configuration);
        PlaceholderConfigurationGuard.ValidateJwtIssuerAudience(configuration);

        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = CreateSigningKey(configuration[PlaceholderConfigurationGuard.SecretConfigurationKey]!),
            ValidateIssuer = true,
            ValidIssuer = configuration[PlaceholderConfigurationGuard.IssuerConfigurationKey],
            ValidateAudience = true,
            ValidAudience = configuration[PlaceholderConfigurationGuard.AudienceConfigurationKey],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }

    /// <summary>
    /// Creates parameters for in-process validation from bound <see cref="AppSettings"/>.
    /// Unlike the host registration, issuer and audience remain optional: when absent their
    /// validation is disabled so tokens minted without them still validate. The signing secret
    /// must still satisfy the shared strength guard.
    /// </summary>
    public static TokenValidationParameters Create(AppSettings appSettings)
    {
        ArgumentNullException.ThrowIfNull(appSettings);

        if (string.IsNullOrWhiteSpace(appSettings.Secret))
        {
            throw new InvalidOperationException("JWT secret not configured");
        }

        PlaceholderConfigurationGuard.ValidateJwtSecret(new SecretOnlyConfiguration(appSettings.Secret));

        var hasIssuer = !string.IsNullOrWhiteSpace(appSettings.Issuer);
        var hasAudience = !string.IsNullOrWhiteSpace(appSettings.Audience);

        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = CreateSigningKey(appSettings.Secret),
            ValidateIssuer = hasIssuer,
            ValidIssuer = hasIssuer ? appSettings.Issuer : null,
            ValidateAudience = hasAudience,
            ValidAudience = hasAudience ? appSettings.Audience : null,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }

    private static SymmetricSecurityKey CreateSigningKey(string secret)
        => new(Encoding.UTF8.GetBytes(secret));

    /// <summary>
    /// Minimal read-only <see cref="IConfiguration"/> exposing one value so the shared
    /// <see cref="PlaceholderConfigurationGuard"/> secret-strength rule can be reused without
    /// duplicating the minimum-length threshold or its error message.
    /// </summary>
    private sealed class SecretOnlyConfiguration(string? value) : IConfiguration
    {
        public string? this[string key]
        {
            get => string.Equals(key, PlaceholderConfigurationGuard.SecretConfigurationKey, StringComparison.OrdinalIgnoreCase)
                ? value
                : null;
            set => throw new NotSupportedException();
        }

        public IConfigurationSection GetSection(string key)
            => new SecretOnlyConfigurationSection(key, this[key]);

        public IEnumerable<IConfigurationSection> GetChildren()
            => Array.Empty<IConfigurationSection>();

        public IChangeToken GetReloadToken()
            => new NeverReloadChangeToken();
    }

    private sealed class NeverReloadChangeToken : IChangeToken
    {
        public bool HasChanged => false;

        public bool ActiveChangeCallbacks => false;

        public IDisposable? RegisterChangeCallback(Action<object?> callback, object? state)
            => null;
    }

    private sealed class SecretOnlyConfigurationSection(string key, string? value) : IConfigurationSection
    {
        public string Key => key;

        public string Path => key;

        public string? Value { get => value; set => throw new NotSupportedException(); }

        public string? this[string key] { get => null; set => throw new NotSupportedException(); }

        public IConfigurationSection GetSection(string key)
            => new SecretOnlyConfigurationSection($"{Path}:{key}", null);

        public IEnumerable<IConfigurationSection> GetChildren()
            => Array.Empty<IConfigurationSection>();

        public IChangeToken GetReloadToken()
            => new NeverReloadChangeToken();
    }
}
