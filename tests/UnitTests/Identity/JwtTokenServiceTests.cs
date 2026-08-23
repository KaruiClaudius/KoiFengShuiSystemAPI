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
    private const string Issuer = "KoiFengShuiSystem";
    private const string Audience = "KoiFengShuiSystemClients";

    private static JwtTokenService CreateService(AppSettings? settings = null)
    {
        var options = Options.Create(settings ?? new AppSettings { Secret = Secret });
        return new JwtTokenService(options);
    }

    private static Account CreateAccount(int accountId = 123) => new()
    {
        AccountId = accountId,
        Email = "identity@test.com",
        RoleId = 2
    };

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

    // --- Access-token lifetime ---

    [Fact]
    public void AccessTokenLifetimeMinutes_WithoutConfiguration_DefaultsTo15()
    {
        var service = CreateService();

        Assert.Equal(15, service.AccessTokenLifetimeMinutes);
    }

    [Fact]
    public void AccessTokenLifetimeMinutes_ConfiguredValue_IsUsed()
    {
        var service = CreateService(new AppSettings { Secret = Secret, AccessTokenMinutes = 5 });

        Assert.Equal(5, service.AccessTokenLifetimeMinutes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-10)]
    public void GenerateJwtToken_MissingOrNonPositiveAccessTokenMinutes_FallsBackToDefault15(int? accessTokenMinutes)
    {
        var service = CreateService(new AppSettings { Secret = Secret, AccessTokenMinutes = accessTokenMinutes });

        var token = service.GenerateJwtToken(CreateAccount());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var lifetimeMinutes = (jwt.ValidTo - DateTime.UtcNow).TotalMinutes;
        Assert.InRange(lifetimeMinutes, 14.9, 15.1);
    }

    [Fact]
    public void GenerateJwtToken_ConfiguredAccessTokenMinutes_SetsExpiryAccordingly()
    {
        var service = CreateService(new AppSettings { Secret = Secret, AccessTokenMinutes = 5 });

        var token = service.GenerateJwtToken(CreateAccount());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var lifetimeMinutes = (jwt.ValidTo - DateTime.UtcNow).TotalMinutes;
        Assert.InRange(lifetimeMinutes, 4.9, 5.1);
    }

    // --- jti claim ---

    [Fact]
    public void GenerateJwtToken_IncludesJtiClaim()
    {
        var service = CreateService();

        var token = service.GenerateJwtToken(CreateAccount());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.False(string.IsNullOrWhiteSpace(jwt.Claims.Single(c => c.Type == "jti").Value));
    }

    [Fact]
    public void GenerateJwtToken_RepeatedInvocations_ProduceUniqueJtiValues()
    {
        var service = CreateService();

        var first = new JwtSecurityTokenHandler().ReadJwtToken(service.GenerateJwtToken(CreateAccount()));
        var second = new JwtSecurityTokenHandler().ReadJwtToken(service.GenerateJwtToken(CreateAccount()));

        Assert.NotEqual(
            first.Claims.Single(claim => claim.Type == "jti").Value,
            second.Claims.Single(claim => claim.Type == "jti").Value);
    }

    // --- issuer / audience ---

    [Fact]
    public void GenerateJwtToken_WithConfiguredIssuerAndAudience_EmitsBothAndValidatesRoundTrip()
    {
        var service = CreateService(new AppSettings { Secret = Secret, Issuer = Issuer, Audience = Audience });

        var token = service.GenerateJwtToken(CreateAccount());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(Issuer, jwt.Issuer);
        Assert.Equal(Audience, jwt.Audiences.Single());
        Assert.Equal(123, service.ValidateJwtToken(token));
    }

    [Fact]
    public void ValidateJwtToken_TokenFromDifferentIssuer_ReturnsNullWhenIssuerIsEnforced()
    {
        var enforcingService = CreateService(new AppSettings { Secret = Secret, Issuer = "other-issuer", Audience = Audience });
        var mintingService = CreateService(new AppSettings { Secret = Secret, Issuer = Issuer, Audience = Audience });

        var foreignToken = mintingService.GenerateJwtToken(CreateAccount());

        Assert.Null(enforcingService.ValidateJwtToken(foreignToken));
    }

    [Fact]
    public void ValidateJwtToken_TokenWithoutIssuer_PassesWhenIssuerNotConfigured()
    {
        var service = CreateService();

        var token = service.GenerateJwtToken(CreateAccount());

        Assert.Equal(123, service.ValidateJwtToken(token));
    }
}
