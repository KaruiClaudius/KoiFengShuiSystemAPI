using System.IO;

namespace UnitTests.Identity;

public class JwtConsumerMigrationTests
{
    [Theory]
    [InlineData("src/Host/Middleware/JwtMiddleware.cs")]
    [InlineData("KoiFengShuiSystem.Api/Authorization/JwtMiddleware.cs")]
    // AuthController was removed from this gate once it stopped consuming JWT services
    // directly: all issuance now routes through SessionIssuer, which inherits the gate.
    [InlineData("src/Modules/Identity/Identity.Application/Services/SessionIssuer.cs")]
    public void JwtConsumers_DoNotDependOnLegacyJwtUtils(string relativePath)
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var filePath = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

        var source = File.ReadAllText(filePath);

        Assert.DoesNotContain("IJwtUtils", source, StringComparison.Ordinal);
        Assert.Contains("IJwtTokenService", source, StringComparison.Ordinal);
    }
}
