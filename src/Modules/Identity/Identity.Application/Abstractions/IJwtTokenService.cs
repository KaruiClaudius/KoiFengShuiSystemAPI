using KoiFengShuiSystem.Modules.Identity.Domain.Entities;

namespace KoiFengShuiSystem.Modules.Identity.Application.Abstractions;

public interface IJwtTokenService
{
    string GenerateJwtToken(Account account);
    int? ValidateJwtToken(string? token);
}
