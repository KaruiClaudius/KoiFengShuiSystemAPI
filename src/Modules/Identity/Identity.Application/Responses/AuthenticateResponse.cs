using KoiFengShuiSystem.Modules.Identity.Domain.Entities;

namespace KoiFengShuiSystem.Modules.Identity.Application.Responses;

public class AuthenticateResponse
{
    public int Id { get; set; }

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Opaque refresh token handed to the client exactly once; only its hash is stored server-side.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Access-token lifetime in minutes, advertised so clients can refresh proactively.
    /// </summary>
    public int ExpiresInMinutes { get; set; }

    public AuthenticateResponse(Account account, string token)
    {
        Id = account.AccountId;
        FullName = account.FullName;
        Email = account.Email;
        Token = token;
    }
}
