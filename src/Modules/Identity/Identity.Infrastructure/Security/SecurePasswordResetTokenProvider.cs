using System.Security.Cryptography;
using System.Text;
using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;

namespace KoiFengShuiSystem.Modules.Identity.Infrastructure.Security;

public class SecurePasswordResetTokenProvider : IPasswordResetTokenProvider
{
    private const int TokenByteSize = 32;

    public string Generate()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(TokenByteSize);
        return Convert.ToBase64String(randomBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public string Hash(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)))
            .ToLowerInvariant();
    }
}
