namespace KoiFengShuiSystem.Shared.Kernel.Security;

/// <summary>
/// Centralizes role identifiers used by authorization policies.
/// Values mirror Account.RoleId in the identity store: JwtTokenService
/// mints ClaimTypes.Role = RoleId.ToString(), so these strings are what
/// [Authorize(Roles = ...)] must match against.
/// </summary>
public static class AuthorizationDefaults
{
    public static class Roles
    {
        public const string Admin = "1";

        public const string Member = "2";
    }
}
