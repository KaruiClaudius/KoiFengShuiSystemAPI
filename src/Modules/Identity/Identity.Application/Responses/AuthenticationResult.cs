namespace KoiFengShuiSystem.Modules.Identity.Application.Responses;

public class AuthenticationResult
{
    public AuthenticateResponse? Response { get; set; }

    public string? ErrorMessage { get; set; }

    /// <summary>Stable machine-readable code (council D1); see <see cref="AuthErrorCodes"/>.</summary>
    public string? ErrorCode { get; set; }

    public bool Success => ErrorMessage == null;
}
