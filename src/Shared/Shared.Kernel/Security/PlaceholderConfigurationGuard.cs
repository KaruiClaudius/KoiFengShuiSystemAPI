using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KoiFengShuiSystem.Shared.Kernel.Security;

public static class PlaceholderConfigurationGuard
{
    public const string PlaceholderPrefix = "ROTATE_ME";

    private const string DevelopmentEnvironmentName = "Development";

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
