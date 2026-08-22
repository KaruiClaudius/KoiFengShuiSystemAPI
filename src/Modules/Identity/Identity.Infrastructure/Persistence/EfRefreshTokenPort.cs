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

        if (storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            _context.RefreshTokens.Remove(storedToken);
            await _context.SaveChangesAsync();
            return RotateResult.Failed(RotateResult.ExpiredTokenReason);
        }

        // INVARIANT: a raw token can ever yield at most one valid successor.
        //
        // The revoke step below is an atomic conditional claim that only succeeds
        // while the token is still active. Two concurrent rotations of the same raw
        // token cannot both win: exactly one request's claim matches a row; the loser
        // observes "not claimed" and is routed to the breach path. The successor row
        // is only persisted AFTER the claim succeeds, so a lost race never leaves an
        // orphaned active successor.
        var successorRawToken = GenerateRawToken();
        var now = DateTime.UtcNow;

        if (!await TryClaimActiveTokenAsync(storedToken, now, Hash(successorRawToken)))
        {
            // Lost the race or revoked between fetch and claim: assume theft and revoke
            // every still-active token of the account.
            await RevokeAllActiveTokensAsync(storedToken.AccountId);
            return RotateResult.Failed(RotateResult.ReuseDetectedReason);
        }

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

    /// <summary>
    /// Atomically claims a still-active refresh token by revoking it and linking it to
    /// its successor's hash. Returns false when the token was already consumed — the
    /// caller must then treat this as reuse of a revoked token.
    /// </summary>
    private async Task<bool> TryClaimActiveTokenAsync(RefreshToken storedToken, DateTime now, string successorHash)
    {
        if (_context.Database.IsRelational())
        {
            // Relational providers translate this into a single atomic UPDATE with the
            // predicate evaluated at the database, making concurrent rotations of the
            // same token mutually exclusive.
            var claimed = await _context.RefreshTokens
                .Where(active => active.Id == storedToken.Id && active.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(active => active.RevokedAt, now)
                    .SetProperty(active => active.ReplacedByTokenHash, successorHash));

            return claimed == 1;
        }

        // Non-relational providers (the EF InMemory database used by unit tests) cannot
        // execute set-based updates. Emulate the same conditional claim with a fresh
        // no-tracking read so the predicate is always evaluated against persisted state,
        // never against possibly stale tracked copies; the tracked instance is mutated
        // and persisted together with the successor row.
        var stillActive = await _context.RefreshTokens
            .AsNoTracking()
            .Where(active => active.Id == storedToken.Id && active.RevokedAt == null)
            .AnyAsync();

        if (!stillActive)
        {
            return false;
        }

        storedToken.RevokedAt = now;
        storedToken.ReplacedByTokenHash = successorHash;
        return true;
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
