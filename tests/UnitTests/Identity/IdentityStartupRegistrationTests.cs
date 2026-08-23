using System.IO;

namespace UnitTests.Identity;

public class IdentityStartupRegistrationTests
{
    [Theory]
    [InlineData("src/Host/Program.cs")]
    public void Program_DoesNotManuallyRegisterJwtUtils(string relativePath)
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var filePath = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

        var programContents = File.ReadAllText(filePath);

        Assert.DoesNotContain("AddScoped<IJwtUtils, JwtUtils>()", programContents, StringComparison.Ordinal);
    }
}
