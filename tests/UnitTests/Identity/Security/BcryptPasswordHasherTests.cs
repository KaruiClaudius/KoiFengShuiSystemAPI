using KoiFengShuiSystem.Modules.Identity.Infrastructure.Security;

namespace UnitTests.Identity.Security;

public class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _hasher = new();

    // --- Hash ---

    [Fact]
    public void Hash_Plaintext_ReturnsValueDifferentFromPlaintext()
    {
        var hash = _hasher.Hash("mySecret123!");

        Assert.NotEqual("mySecret123!", hash);
    }

    [Fact]
    public void Hash_Plaintext_ReturnsBcryptFormattedHash()
    {
        var hash = _hasher.Hash("mySecret123!");

        Assert.StartsWith("$2", hash);
        Assert.Equal(60, hash.Length);
    }

    [Fact]
    public void Hash_SamePlaintext_TwiceProducesDifferentSalts()
    {
        var first = _hasher.Hash("mySecret123!");
        var second = _hasher.Hash("mySecret123!");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Hash_NullPassword_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _hasher.Hash(null!));
    }

    // --- Verify ---

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("mySecret123!");

        Assert.True(_hasher.Verify("mySecret123!", hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("mySecret123!");

        Assert.False(_hasher.Verify("wrongPassword", hash));
    }

    [Fact]
    public void Verify_NonBcryptStoredValue_ReturnsFalse()
    {
        Assert.False(_hasher.Verify("mySecret123!", "plaintext-stored-value"));
    }

    [Fact]
    public void Verify_EmptyStoredValue_ReturnsFalse()
    {
        Assert.False(_hasher.Verify("mySecret123!", string.Empty));
    }

    [Fact]
    public void Verify_NullStoredValue_ReturnsFalse()
    {
        Assert.False(_hasher.Verify("mySecret123!", null!));
    }

    // --- NeedsRehash ---

    [Fact]
    public void NeedsRehash_FreshlyGeneratedHash_ReturnsFalse()
    {
        var hash = _hasher.Hash("mySecret123!");

        Assert.False(_hasher.NeedsRehash(hash));
    }

    [Fact]
    public void NeedsRehash_NonBcryptLegacyPlaintext_ReturnsTrue()
    {
        Assert.True(_hasher.NeedsRehash("legacy-plaintext-password"));
    }

    [Fact]
    public void NeedsRehash_EmptyValue_ReturnsTrue()
    {
        Assert.True(_hasher.NeedsRehash(string.Empty));
    }

    [Fact]
    public void NeedsRehash_LowerWorkFactorHash_ReturnsTrue()
    {
        var weakHash = BCrypt.Net.BCrypt.HashPassword("mySecret123!", workFactor: 10);

        Assert.True(_hasher.NeedsRehash(weakHash));
    }
}
