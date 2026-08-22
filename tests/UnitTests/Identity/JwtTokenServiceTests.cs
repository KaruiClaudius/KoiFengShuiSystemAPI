using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Modules.Identity.Infrastructure.Security;
using KoiFengShuiSystem.Shared.Helpers;
using Microsoft.Extensions.Options;

namespace UnitTests.Identity;

public class JwtTokenServiceTests
{
    private const string Secret = "test-secret-key-that-is-at-least-32-bytes-long-for-hmac";

    private static JwtTokenService CreateService()
    {
        var options = Options.Create(new AppSettings { Secret = Secret });
        return new JwtTokenService(options);
    }

    [Fact]
    public void GenerateJwtToken_IncludesExpectedClaims()
    {
        var service = CreateService();
        var account = new Account
        {
            AccountId = 123,
            Email = "identity@test.com",
            RoleId = 2
        };

        var token = service.GenerateJwtToken(account);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("123", jwt.Claims.Single(c => c.Type == "id").Value);
        Assert.Equal("identity@test.com", jwt.Claims.Single(c => c.Type == JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap[ClaimTypes.Email]).Value);
        Assert.Equal("2", jwt.Claims.Single(c => c.Type == JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap[ClaimTypes.Role]).Value);
    }

    [Fact]
    public void ValidateJwtToken_WithValidToken_ReturnsAccountId()
    {
        var service = CreateService();
        var account = new Account
        {
            AccountId = 55,
            Email = "valid@test.com",
            RoleId = 1
        };

        var token = service.GenerateJwtToken(account);

        var result = service.ValidateJwtToken(token);

        Assert.Equal(55, result);
    }

    [Fact]
    public void ValidateJwtToken_WithInvalidToken_ReturnsNull()
    {
        var service = CreateService();

        var result = service.ValidateJwtToken("not-a-real-token");

        Assert.Null(result);
    }
}
