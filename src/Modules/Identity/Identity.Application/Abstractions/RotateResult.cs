namespace KoiFengShuiSystem.Modules.Identity.Application.Abstractions;

/// <summary>
/// Outcome of a refresh-token rotation attempt.
/// </summary>
public sealed record RotateResult(bool Success, int? AccountId, string? NewRawToken, string FailureReason)
{
    public const string MissingTokenReason = "MissingRefreshToken";
    public const string UnknownTokenReason = "UnknownRefreshToken";
    public const string ReuseDetectedReason = "ReuseDetectedAllTokensRevoked";
    public const string ExpiredTokenReason = "ExpiredRefreshToken";

    public static RotateResult Successful(int accountId, string newRawToken)
        => new(true, accountId, newRawToken, string.Empty);

    public static RotateResult Failed(string failureReason)
        => new(false, null, null, failureReason);
}
