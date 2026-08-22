using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KoiFengShuiSystem.Shared.Kernel.Security;

public static class PlaceholderConfigurationGuard
{
    public const string PlaceholderPrefix = "ROTATE_ME";

    private const string DevelopmentEnvironmentName = "Development";

    public const string SecretConfigurationKey = "AppSettings:Secret";

    public const string IssuerConfigurationKey = "AppSettings:Issuer";

    public const string AudienceConfigurationKey = "AppSettings:Audience";

    private const int MinimumJwtSecretLength = 32;

    /// <summary>
    /// Enforces a minimum JWT signing-secret strength at startup. Unlike <see cref="Validate"/>,
    /// this check applies in every environment: a weak HMAC key silently downgrades token
    /// security even during local development, so it always fails fast.
    /// </summary>
    public static void ValidateJwtSecret(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var secret = configuration[SecretConfigurationKey];
        if (!string.IsNullOrWhiteSpace(secret) && secret.Length >= MinimumJwtSecretLength)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Configuration key '{SecretConfigurationKey}' must be set to a non-empty value of at least {MinimumJwtSecretLength} characters. " +
            "Supply a strong signing secret via user-secrets or environment variables before starting the application.");
    }

    /// <summary>
    /// Ensures the JWT issuer and audience are configured before JwtBearer validation
    /// is registered. Both hosts validate issuer/audience on every token, so a missing
    /// value would reject all authentication; like <see cref="ValidateJwtSecret"/>,
    /// this check fails fast in every environment.
    /// </summary>
    public static void ValidateJwtIssuerAudience(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var offendingKeys = new List<string>();
        if (string.IsNullOrWhiteSpace(configuration[IssuerConfigurationKey]))
        {
            offendingKeys.Add(IssuerConfigurationKey);
        }

        if (string.IsNullOrWhiteSpace(configuration[AudienceConfigurationKey]))
        {
            offendingKeys.Add(AudienceConfigurationKey);
        }

        if (offendingKeys.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Configuration key(s) {string.Join(", ", offendingKeys)} must be set to non-empty values. " +
            "Every issued token carries these values and JwtBearer rejects tokens without a matching issuer/audience.");
    }

    public static void Validate(IConfiguration configuration, string environmentName, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var offendingKeys = FindPlaceholderKeys(configuration);
        if (offendingKeys.Count == 0)
        {
            return;
        }

        var message = BuildMessage(offendingKeys);

        if (string.Equals(environmentName, DevelopmentEnvironmentName, StringComparison.OrdinalIgnoreCase))
        {
            if (logger is not null)
            {
                logger.LogWarning("{Message}", message);
            }
            else
            {
                Console.WriteLine("WARNING: " + message);
            }

            return;
        }

        throw new InvalidOperationException(message);
    }

    public static IReadOnlyList<string> FindPlaceholderKeys(IConfiguration configuration)
    {
        var offendingKeys = new List<string>();
        CollectPlaceholderKeys(configuration, parentPath: string.Empty, offendingKeys);
        return offendingKeys;
    }

    private static void CollectPlaceholderKeys(IConfiguration configuration, string parentPath, List<string> offendingKeys)
    {
        foreach (var section in configuration.GetChildren())
        {
            var path = string.IsNullOrEmpty(parentPath) ? section.Key : $"{parentPath}:{section.Key}";
            var value = section.Value;

            if (!string.IsNullOrEmpty(value) && value.StartsWith(PlaceholderPrefix, StringComparison.OrdinalIgnoreCase))
            {
                offendingKeys.Add(path);
            }

            CollectPlaceholderKeys(section, path, offendingKeys);
        }
    }

    private static string BuildMessage(IReadOnlyList<string> offendingKeys) =>
        $"Placeholder credential(s) detected for configuration key(s): {string.Join(", ", offendingKeys)}. " +
        "Replace every ROTATE_ME placeholder with a real value supplied via user-secrets or environment variables before running outside development.";
}
