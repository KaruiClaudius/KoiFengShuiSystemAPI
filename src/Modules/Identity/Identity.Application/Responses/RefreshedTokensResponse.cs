namespace KoiFengShuiSystem.Modules.Identity.Application.Responses;

/// <summary>
/// Wire contract of <c>POST api/Auth/refresh</c>: the replacement access-token /
/// refresh-token pair issued after a successful rotation. Property declaration order
/// and names serialize (camelCase) byte-identically to the pre-consolidation payload:
/// <c>{ "token", "refreshToken", "expiresIn" }</c>.
/// </summary>
public sealed record RefreshedTokensResponse(string Token, string RefreshToken, int ExpiresIn);
