namespace KoiFengShuiSystem.Modules.Identity.Application.Abstractions;

public interface IRefreshTokenPort
{
    /// <summary>
    /// Generates a new opaque raw refresh token for the account and persists only its hash.
    /// Returns the raw token exactly once; it cannot be recovered afterwards.
    /// </summary>
    Task<string> CreateForAccountAsync(int accountId);

    /// <summary>
    /// Consumes a raw refresh token, revoking it and issuing a successor. Reuse of an
    /// already-revoked token is treated as a breach: every active token of the owning
    /// account is revoked.
    /// </summary>
    Task<RotateResult> RotateAsync(string rawToken);

    Task RevokeAllForAccountAsync(int accountId);
}
