using KoiFengShuiSystem.Modules.Identity.Domain.Entities;

namespace KoiFengShuiSystem.Modules.Identity.Application.Responses;

public class AuthenticateResponse
{
    public int Id { get; set; }

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string Token { get; set; } = string.Empty;

    public AuthenticateResponse(Account account, string token)
    {
        Id = account.AccountId;
        FullName = account.FullName;
        Email = account.Email;
        Token = token;
    }
}
