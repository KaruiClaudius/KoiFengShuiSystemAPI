namespace KoiFengShuiSystem.Modules.Identity.Application.Responses;

public class AuthenticationResult
{
    public AuthenticateResponse? Response { get; set; }

    public string? ErrorMessage { get; set; }

    public bool Success => ErrorMessage == null;
}
