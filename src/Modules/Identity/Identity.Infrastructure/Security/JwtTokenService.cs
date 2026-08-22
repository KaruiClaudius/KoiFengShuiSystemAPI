using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Shared.Helpers;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace KoiFengShuiSystem.Modules.Identity.Infrastructure.Security;

public class JwtTokenService : IJwtTokenService
{
    private const int DefaultAccessTokenMinutes = 15;

    private readonly AppSettings _appSettings;

    public JwtTokenService(IOptions<AppSettings> appSettings)
    {
        _appSettings = appSettings.Value;

        if (string.IsNullOrWhiteSpace(_appSettings.Secret))
        {
            throw new Exception("JWT secret not configured");
        }
    }

    public int AccessTokenLifetimeMinutes => ResolveAccessTokenMinutes();

    private int ResolveAccessTokenMinutes()
        => _appSettings.AccessTokenMinutes is > 0 ? _appSettings.AccessTokenMinutes.Value : DefaultAccessTokenMinutes;

    public string GenerateJwtToken(Account account)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_appSettings.Secret!);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("id", account.AccountId.ToString()),
                new Claim(ClaimTypes.Email, account.Email!),
                new Claim(ClaimTypes.Role, account.RoleId?.ToString() ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }),
            Expires = DateTime.UtcNow.AddMinutes(ResolveAccessTokenMinutes()),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        if (!string.IsNullOrWhiteSpace(_appSettings.Issuer))
        {
            tokenDescriptor.Issuer = _appSettings.Issuer;
        }

        if (!string.IsNullOrWhiteSpace(_appSettings.Audience))
        {
            tokenDescriptor.Audience = _appSettings.Audience;
        }

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public int? ValidateJwtToken(string? token)
    {
        if (token == null)
        {
            return null;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_appSettings.Secret!);

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = !string.IsNullOrWhiteSpace(_appSettings.Issuer),
                ValidIssuer = _appSettings.Issuer,
                ValidateAudience = !string.IsNullOrWhiteSpace(_appSettings.Audience),
                ValidAudience = _appSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;
            return int.Parse(jwtToken.Claims.First(claim => claim.Type == "id").Value);
        }
        catch
        {
            return null;
        }
    }
}
