using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Application.Requests;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KoiFengShuiSystem.Modules.Identity.Application.Services;

/// <summary>
/// Owns the self-service password reset flow: issuing and hashing reset tokens for
/// forgot-password requests, building the reset link against the configured frontend
/// origin, and consuming a valid, unexpired token into a new password (which also
/// invalidates every outstanding session of the account).
/// </summary>
public class PasswordResetService
{
    private const int ResetTokenLifetimeMinutes = 15;
    private const string DefaultFrontendBaseUrl = "http://localhost:3000";

    private readonly IIdentityReadStore _readStore;
    private readonly IIdentityWriteStore _writeStore;
    private readonly IPasswordResetTokenProvider _passwordResetTokenProvider;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IIdentityEmailSender _identityEmailSender;
    private readonly SessionIssuer _sessionIssuer;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        IIdentityReadStore readStore,
        IIdentityWriteStore writeStore,
        IPasswordResetTokenProvider passwordResetTokenProvider,
        IPasswordHasher passwordHasher,
        IIdentityEmailSender identityEmailSender,
        SessionIssuer sessionIssuer,
        IConfiguration configuration,
        ILogger<PasswordResetService> logger)
    {
        ArgumentNullException.ThrowIfNull(readStore);
        ArgumentNullException.ThrowIfNull(writeStore);
        ArgumentNullException.ThrowIfNull(passwordResetTokenProvider);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(identityEmailSender);
        ArgumentNullException.ThrowIfNull(sessionIssuer);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);

        _readStore = readStore;
        _writeStore = writeStore;
        _passwordResetTokenProvider = passwordResetTokenProvider;
        _passwordHasher = passwordHasher;
        _identityEmailSender = identityEmailSender;
        _sessionIssuer = sessionIssuer;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return true;
        }

        var account = await _readStore.GetAccountByEmailAsync(email);
        if (account == null)
        {
            _logger.LogInformation("Password reset requested for an unknown email; no action taken");
            return true;
        }

        var token = _passwordResetTokenProvider.Generate();
        account.ResetTokenHash = _passwordResetTokenProvider.Hash(token);
        account.ResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(ResetTokenLifetimeMinutes);

        await _writeStore.UpdateAccountAsync(account);
        await _writeStore.SaveChangesAsync();

        var resetLink = BuildResetLink(token);

        return await _identityEmailSender.SendPasswordResetEmailAsync(email, account.FullName, resetLink);
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrEmpty(request.NewPassword))
        {
            return false;
        }

        var tokenHash = _passwordResetTokenProvider.Hash(request.Token);
        var account = await _readStore.GetAccountByResetTokenHashAsync(tokenHash);

        if (account == null)
        {
            return false;
        }

        if (account.ResetTokenExpiresAt is not { } expiresAt || expiresAt <= DateTime.UtcNow)
        {
            return false;
        }

        account.Password = _passwordHasher.Hash(request.NewPassword);
        account.ResetTokenHash = null;
        account.ResetTokenExpiresAt = null;

        await _writeStore.UpdateAccountAsync(account);
        await _writeStore.SaveChangesAsync();

        // A password reset invalidates every outstanding session of the account.
        await _sessionIssuer.RevokeAllForAccountAsync(account.AccountId);

        return true;
    }

    private string BuildResetLink(string token)
    {
        var baseUrl = _configuration["AppSettings:FrontendBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogWarning(
                "AppSettings:FrontendBaseUrl is not configured; falling back to {DefaultBaseUrl} for password-reset links. Configure it to point at the frontend origin.",
                DefaultFrontendBaseUrl);
            baseUrl = DefaultFrontendBaseUrl;
        }

        return $"{baseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";
    }
}
