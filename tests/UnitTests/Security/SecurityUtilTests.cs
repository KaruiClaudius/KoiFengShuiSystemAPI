using KoiFengShuiSystem.BusinessLogic.Services.Implement;

namespace UnitTests.Security;

public class SecurityUtilTests
{
    [Fact]
    public void GenerateRandomPassword_ReturnsAtLeast12Characters()
    {
        var password = SecurityUtil.GenerateRandomPassword();

        Assert.True(password.Length >= 12, $"Expected at least 12 characters but got {password.Length}.");
    }

    [Fact]
    public void GenerateRandomPassword_ContainsUppercaseCharacter()
    {
        Assert.Contains(SecurityUtil.GenerateRandomPassword(), c => char.IsAsciiLetterUpper(c));
    }

    [Fact]
    public void GenerateRandomPassword_ContainsLowercaseCharacter()
    {
        Assert.Contains(SecurityUtil.GenerateRandomPassword(), c => char.IsAsciiLetterLower(c));
    }

    [Fact]
    public void GenerateRandomPassword_ContainsDigitCharacter()
    {
        Assert.Contains(SecurityUtil.GenerateRandomPassword(), c => char.IsAsciiDigit(c));
    }

    [Fact]
    public void GenerateRandomPassword_ContainsSpecialCharacter()
    {
        const string specials = "!@#$%^&*()-_=+";
        Assert.Contains(SecurityUtil.GenerateRandomPassword(), c => specials.Contains(c));
    }

    [Fact]
    public void GenerateRandomPassword_RepeatedCalls_ProducesUniquePasswords()
    {
        const int generationCount = 100;
        var passwords = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < generationCount; i++)
        {
            passwords.Add(SecurityUtil.GenerateRandomPassword());
        }

        Assert.Equal(generationCount, passwords.Count);
    }
}
