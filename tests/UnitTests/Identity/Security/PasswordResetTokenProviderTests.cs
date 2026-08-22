using System.Security.Cryptography;
using System.Text;
using KoiFengShuiSystem.Modules.Identity.Infrastructure.Security;

namespace UnitTests.Identity.Security;

public class PasswordResetTokenProviderTests
{
    private const int TokenByteSize = 32;

    private readonly SecurePasswordResetTokenProvider _provider = new();

    // --- Generate ---

    [Fact]
    public void Generate_ReturnsNonEmptyToken()
    {
        var token = _provider.Generate();

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void Generate_TokenEncodes32Bytes_Base64UrlLength()
    {
        var token = _provider.Generate();

        Assert.Equal(Base64UrlEncodeLength(TokenByteSize), token.Length);
    }

    [Fact]
    public void Generate_TokenIsUrlSafe()
    {
        for (var i = 0; i < 20; i++)
        {
            var token = _provider.Generate();
            Assert.Matches("^[A-Za-z0-9_-]+$", token);
        }
    }

    [Fact]
    public void Generate_RepeatedCalls_ProducesUniqueTokens()
    {
        const int generationCount = 100;
        var tokens = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < generationCount; i++)
        {
            tokens.Add(_provider.Generate());
        }

        Assert.Equal(generationCount, tokens.Count);
    }

    [Fact]
    public void Generate_RoundTripsThroughBase64UrlDecode()
    {
        var token = _provider.Generate();

        var base64 = token.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2:
                base64 += "==";
                break;
            case 3:
                base64 += "=";
                break;
        }

        var decoded = Convert.FromBase64String(base64);

        Assert.Equal(TokenByteSize, decoded.Length);
    }

    // --- Hash ---

    [Fact]
    public void Hash_SameToken_ProducesIdenticalHash()
    {
        var token = _provider.Generate();

        var first = _provider.Hash(token);
        var second = _provider.Hash(token);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Hash_DifferentTokens_ProduceDifferentHashes()
    {
        var first = _provider.Hash(_provider.Generate());
        var second = _provider.Hash(_provider.Generate());

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Hash_Returns64CharacterLowercaseHex()
    {
        var hash = _provider.Hash(_provider.Generate());

        Assert.Equal(64, hash.Length);
        Assert.Matches("^[a-f0-9]{64}$", hash);
    }

    [Theory]
    [InlineData("test")]
    [InlineData("known-vector-token")]
    public void Hash_MatchesSha256OfUtf8TokenBytes(string token)
    {
        var expected = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)))
            .ToLowerInvariant();

        Assert.Equal(expected, _provider.Hash(token));
    }

    [Fact]
    public void Hash_NullToken_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _provider.Hash(null!));
    }

    private static int Base64UrlEncodeLength(int byteCount)
        => (int)Math.Ceiling(byteCount * 4d / 3d);
}
