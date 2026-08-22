using KoiFengShuiSystem.Modules.Identity.Domain.Entities;

namespace KoiFengShuiSystem.Modules.Identity.Application.Abstractions;

public interface IJwtTokenService
{
    /// <summary>
    /// Lifetime of generated access tokens in minutes; the value advertised to clients
    /// as <c>expiresIn</c>.
    /// </summary>
    int AccessTokenLifetimeMinutes { get; }

    string GenerateJwtToken(Account account);
    int? ValidateJwtToken(string? token);
}
