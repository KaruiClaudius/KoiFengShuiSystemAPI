using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Application.Responses;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;

namespace KoiFengShuiSystem.Modules.Identity.Application.Services;

/// <summary>
/// Single home for session lifecycle inside the module: issues the standard access-token +
/// refresh-token pair with the advertised access-token lifetime, and revokes every
/// outstanding session of an account (used by sign-in, password change and password reset).
/// </summary>
public class SessionIssuer
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenPort _refreshTokenPort;

    public SessionIssuer(IJwtTokenService jwtTokenService, IRefreshTokenPort refreshTokenPort)
    {
        ArgumentNullException.ThrowIfNull(jwtTokenService);
        ArgumentNullException.ThrowIfNull(refreshTokenPort);

        _jwtTokenService = jwtTokenService;
        _refreshTokenPort = refreshTokenPort;
    }

    /// <summary>
    /// Issues the token pair for an authenticated account: an access token from the JWT
    /// service paired with a freshly created opaque refresh token, advertising the access
    /// token lifetime so clients can refresh proactively.
    /// </summary>
    public async Task<AuthenticateResponse> IssueForAccountAsync(Account account)
    {
        var token = _jwtTokenService.GenerateJwtToken(account);
        var refreshToken = await _refreshTokenPort.CreateForAccountAsync(account.AccountId);

        return new AuthenticateResponse(account, token)
        {
            RefreshToken = refreshToken,
            ExpiresInMinutes = _jwtTokenService.AccessTokenLifetimeMinutes
        };
    }

    /// <summary>Revokes every outstanding session (refresh token) of the account.</summary>
    public Task RevokeAllForAccountAsync(int accountId)
        => _refreshTokenPort.RevokeAllForAccountAsync(accountId);
}
