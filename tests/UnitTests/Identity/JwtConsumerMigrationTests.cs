using System.IO;

namespace UnitTests.Identity;

public class JwtConsumerMigrationTests
{
    [Theory]
    [InlineData("src/Host/Middleware/JwtMiddleware.cs")]
    [InlineData("KoiFengShuiSystem.Api/Authorization/JwtMiddleware.cs")]
[InlineData("src/Modules/Identity/Identity.Api/Controllers/AuthController.cs")]
    public void JwtConsumers_DoNotDependOnLegacyJwtUtils(string relativePath)
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var filePath = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

        var source = File.ReadAllText(filePath);

        Assert.DoesNotContain("IJwtUtils", source, StringComparison.Ordinal);
        Assert.Contains("IJwtTokenService", source, StringComparison.Ordinal);
    }
}
