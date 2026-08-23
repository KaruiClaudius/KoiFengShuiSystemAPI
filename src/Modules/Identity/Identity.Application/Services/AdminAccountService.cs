using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Shared.Kernel.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KoiFengShuiSystem.Modules.Identity.Application.Services;

/// <summary>
/// Idempotently provisions the bootstrap administrator account at application startup.
/// The seeded password is hashed via <see cref="IPasswordHasher"/> before it ever reaches
/// persistence, and seeding is skipped (with a warning) when no real credentials are
/// configured. Production fail-fast for placeholder credentials lives in
/// <see cref="PlaceholderConfigurationGuard.ValidateAdminSeed"/>.
/// </summary>
public class AdminAccountService
{
    private const string AdminAccountSectionKey = "AdminAccount";

    private readonly IAccountService _accountService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AdminAccountService> _logger;

    public AdminAccountService(
        IAccountService accountService,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger<AdminAccountService> logger)
    {
        ArgumentNullException.ThrowIfNull(accountService);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);

        _accountService = accountService;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task EnsureAdminAccountExistsAsync()
    {
        var adminSection = _configuration.GetSection(AdminAccountSectionKey);

        if (!adminSection.Exists())
        {
            // Seeding is opt-in by configuration: hosts without an AdminAccount section
            // (e.g. contract-test factories) boot without provisioning an administrator.
            _logger.LogWarning(
                "Admin account seeding skipped: no '{AdminAccountSectionKey}' configuration section is present.",
                AdminAccountSectionKey);
            return;
        }

        var adminEmail = adminSection["Email"];
        var adminPassword = adminSection["Password"];
        var adminFullName = adminSection["FullName"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            throw new InvalidOperationException(
                $"Configuration key(s) '{AdminAccountSectionKey}:Email' and/or '{AdminAccountSectionKey}:Password' must be set to non-empty values to enable admin account seeding.");
        }

        if (IsPlaceholder(adminEmail) || IsPlaceholder(adminPassword))
        {
            _logger.LogWarning(
                "Admin account seeding skipped: '{AdminAccountSectionKey}' credentials still contain {PlaceholderPrefix} placeholders. " +
                "Supply real credentials via user-secrets or environment variables to enable seeding.",
                AdminAccountSectionKey, PlaceholderConfigurationGuard.PlaceholderPrefix);
            return;
        }

        var existingAdmin = await _accountService.GetAccountByEmailAsync(adminEmail);

        if (existingAdmin != null)
        {
            _logger.LogInformation("Admin account {Email} already present; seeding skipped.", adminEmail);
            return;
        }

        var newAdmin = new Account
        {
            Email = adminEmail,
            Password = _passwordHasher.Hash(adminPassword),
            FullName = string.IsNullOrWhiteSpace(adminFullName) ? "System Administrator" : adminFullName,
            RoleId = 1,
            Dob = DateTime.Now,
            Gender = "male",
            CreateAt = DateTime.Now,
            UpdateAt = DateTime.Now,
            Phone = "0379499630"
        };

        await _accountService.CreateAsync(newAdmin);

        _logger.LogInformation("Admin account {Email} created with a hashed seed password.", adminEmail);
    }

    private static bool IsPlaceholder(string value) =>
        value.StartsWith(PlaceholderConfigurationGuard.PlaceholderPrefix, StringComparison.OrdinalIgnoreCase);
}
