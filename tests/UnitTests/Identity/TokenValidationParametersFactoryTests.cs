using System.Text;
using KoiFengShuiSystem.Modules.Identity.Infrastructure.Security;
using KoiFengShuiSystem.Shared.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace UnitTests.Identity;

public class TokenValidationParametersFactoryTests
{
    private const string Secret = "test-secret-key-that-is-at-least-32-bytes-long-for-hmac";
    private const string Issuer = "KoiFengShuiSystem";
    private const string Audience = "KoiFengShuiSystemClients";

    // --- strict configuration-based creation (host bearer semantics) ---

    [Fact]
    public void Create_FromConfiguration_MatchesHostBearerSemantics()
    {
        var configuration = BuildConfiguration(Secret, Issuer, Audience);

        var parameters = TokenValidationParametersFactory.Create(configuration);

        Assert.True(parameters.ValidateIssuerSigningKey);
        Assert.True(parameters.ValidateIssuer);
        Assert.Equal(Issuer, parameters.ValidIssuer);
        Assert.True(parameters.ValidateAudience);
        Assert.Equal(Audience, parameters.ValidAudience);
        Assert.True(parameters.ValidateLifetime);
        Assert.Equal(TimeSpan.Zero, parameters.ClockSkew);

        var key = Assert.IsType<SymmetricSecurityKey>(parameters.IssuerSigningKey);
        Assert.Equal(Encoding.UTF8.GetBytes(Secret), key.Key.ToArray());
    }

    [Fact]
    public void Create_FromConfiguration_WithMissingSecret_Throws()
    {
        var configuration = BuildConfiguration(secret: null, Issuer, Audience);

        Assert.Throws<InvalidOperationException>(
            () => TokenValidationParametersFactory.Create(configuration));
    }

    [Fact]
    public void Create_FromConfiguration_WithShortSecret_Throws()
    {
        var configuration = BuildConfiguration("too-short", Issuer, Audience);

        Assert.Throws<InvalidOperationException>(
            () => TokenValidationParametersFactory.Create(configuration));
    }

    [Fact]
    public void Create_FromConfiguration_WithMissingIssuer_Throws()
    {
        var configuration = BuildConfiguration(Secret, issuer: null, Audience);

        Assert.Throws<InvalidOperationException>(
            () => TokenValidationParametersFactory.Create(configuration));
    }

    [Fact]
    public void Create_FromConfiguration_WithMissingAudience_Throws()
    {
        var configuration = BuildConfiguration(Secret, Issuer, audience: null);

        Assert.Throws<InvalidOperationException>(
            () => TokenValidationParametersFactory.Create(configuration));
    }

    // --- app-settings based creation (in-process token validation semantics) ---

    [Fact]
    public void Create_FromAppSettings_WithIssuerAndAudience_MatchesExpectedValues()
    {
        var settings = new AppSettings { Secret = Secret, Issuer = Issuer, Audience = Audience };

        var parameters = TokenValidationParametersFactory.Create(settings);

        Assert.True(parameters.ValidateIssuerSigningKey);
        Assert.True(parameters.ValidateIssuer);
        Assert.Equal(Issuer, parameters.ValidIssuer);
        Assert.True(parameters.ValidateAudience);
        Assert.Equal(Audience, parameters.ValidAudience);
        Assert.True(parameters.ValidateLifetime);
        Assert.Equal(TimeSpan.Zero, parameters.ClockSkew);

        var key = Assert.IsType<SymmetricSecurityKey>(parameters.IssuerSigningKey);
        Assert.Equal(Encoding.UTF8.GetBytes(Secret), key.Key.ToArray());
    }

    [Fact]
    public void Create_FromAppSettings_WithoutIssuerAndAudience_DisablesTheirValidation()
    {
        var settings = new AppSettings { Secret = Secret };

        var parameters = TokenValidationParametersFactory.Create(settings);

        Assert.False(parameters.ValidateIssuer);
        Assert.Null(parameters.ValidIssuer);
        Assert.False(parameters.ValidateAudience);
        Assert.Null(parameters.ValidAudience);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_FromAppSettings_WithMissingSecret_Throws(string? secret)
    {
        var settings = new AppSettings { Secret = secret };

        Assert.ThrowsAny<Exception>(
            () => TokenValidationParametersFactory.Create(settings));
    }

    [Fact]
    public void Create_FromAppSettings_WithShortSecret_Throws()
    {
        var settings = new AppSettings { Secret = "too-short" };

        Assert.ThrowsAny<Exception>(
            () => TokenValidationParametersFactory.Create(settings));
    }

    private static IConfiguration BuildConfiguration(string? secret, string? issuer, string? audience)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:Secret"] = secret,
                ["AppSettings:Issuer"] = issuer,
                ["AppSettings:Audience"] = audience
            })
            .Build();
}
