using System.Security.Cryptography;
using System.Text;
using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace KoiFengShuiSystem.Modules.Identity.Infrastructure.Persistence;

/// <summary>
/// Persists refresh tokens as SHA-256 hashes only; raw tokens are returned to the client
/// exactly once and can never be recovered from storage. Reuse of an already-revoked token
/// is treated as evidence of token theft and revokes the whole token family of the account.
/// </summary>
public class EfRefreshTokenPort : IRefreshTokenPort
{
    private const int TokenByteSize = 64;
    private const int DefaultRefreshTokenDays = 30;

    private readonly KoiFengShuiContext _context;
    private readonly IConfiguration _configuration;

    public EfRefreshTokenPort(KoiFengShuiContext context, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(configuration);

        _context = context;
        _configuration = configuration;
    }

    public async Task<string> CreateForAccountAsync(int accountId)
    {
        var rawToken = GenerateRawToken();
        var now = DateTime.UtcNow;

        _context.RefreshTokens.Add(new RefreshToken
        {
            AccountId = accountId,
            TokenHash = Hash(rawToken),
            ExpiresAt = now.AddDays(GetRefreshTokenDays()),
            CreatedAt = now
        });
        await _context.SaveChangesAsync();

        return rawToken;
    }

    public async Task<RotateResult> RotateAsync(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return RotateResult.Failed(RotateResult.MissingTokenReason);
        }

        var tokenHash = Hash(rawToken);
        var storedToken = await _context.RefreshTokens
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash);

        if (storedToken == null)
        {
            return RotateResult.Failed(RotateResult.UnknownTokenReason);
        }

        if (storedToken.RevokedAt != null)
        {
            // Replayed token: assume theft and revoke every still-active token of the account.
            await RevokeAllActiveTokensAsync(storedToken.AccountId);
            return RotateResult.Failed(RotateResult.ReuseDetectedReason);
        }

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            _context.RefreshTokens.Remove(storedToken);
            await _context.SaveChangesAsync();
            return RotateResult.Failed(RotateResult.ExpiredTokenReason);
        }

        var successorRawToken = GenerateRawToken();
        var now = DateTime.UtcNow;

        storedToken.RevokedAt = now;
        storedToken.ReplacedByTokenHash = Hash(successorRawToken);

        _context.RefreshTokens.Add(new RefreshToken
        {
            AccountId = storedToken.AccountId,
            TokenHash = Hash(successorRawToken),
            ExpiresAt = now.AddDays(GetRefreshTokenDays()),
            CreatedAt = now
        });

        await _context.SaveChangesAsync();
        return RotateResult.Successful(storedToken.AccountId, successorRawToken);
    }

    public async Task RevokeAllForAccountAsync(int accountId)
    {
        await RevokeAllActiveTokensAsync(accountId);
    }

    private async Task RevokeAllActiveTokensAsync(int accountId)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(token => token.AccountId == accountId && token.RevokedAt == null)
            .ToListAsync();

        if (activeTokens.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }

        await _context.SaveChangesAsync();
    }

    private static string GenerateRawToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(TokenByteSize);
        return Convert.ToBase64String(randomBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string Hash(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)))
            .ToLowerInvariant();
    }

    private int GetRefreshTokenDays()
    {
        var configuredDays = _configuration["AppSettings:RefreshTokenDays"];
        return int.TryParse(configuredDays, out var days) && days > 0 ? days : DefaultRefreshTokenDays;
    }
}
