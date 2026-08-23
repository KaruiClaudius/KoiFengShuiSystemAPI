namespace KoiFengShuiSystem.Modules.Identity.Application.Responses;

/// <summary>
/// Stable machine-readable error codes for the auth surface (council D1).
/// Shipped as { code, message } - the human-readable message stays in the
/// payload during the transition so legacy clients keep working, but callers
/// must branch on <see cref="AuthenticationResult"/> codes, not strings.
/// RATE_LIMITED is not emitted by handlers: it is the documented convention
/// for HTTP 429 responses produced by the auth rate-limiting policy.
/// </summary>
public static class AuthErrorCodes
{
    public const string AccountNotFound = "ACCOUNT_NOT_FOUND";
    public const string InvalidPassword = "INVALID_PASSWORD";
    public const string EmailTaken = "EMAIL_TAKEN";
    public const string RateLimited = "RATE_LIMITED";
}
