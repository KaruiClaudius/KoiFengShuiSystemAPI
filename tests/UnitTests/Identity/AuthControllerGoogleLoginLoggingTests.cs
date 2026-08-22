using System.IO;

namespace UnitTests.Identity;

public class AuthControllerGoogleLoginLoggingTests
{
    [Fact]
    public void GoogleLogin_DoesNotLogSensitiveTokenOrGooglePayloadData()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var filePath = Path.Combine(repositoryRoot, "src", "Modules", "Identity", "Identity.Api", "Controllers", "AuthController.cs");

        var source = File.ReadAllText(filePath);

        Assert.DoesNotContain("tokenPreview", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Received Google login request for token:", source, StringComparison.Ordinal);
        Assert.DoesNotContain("JsonSerializer.Serialize(googleUser)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Creating new account for email:", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Email sent successfully to", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Existing account found for email:", source, StringComparison.Ordinal);
    }
}
