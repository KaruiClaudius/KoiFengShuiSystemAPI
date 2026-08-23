using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;

namespace KoiFengShuiSystem.Modules.Identity.Infrastructure.Security;

public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool Verify(string password, string hash)
    {
        ArgumentNullException.ThrowIfNull(password);

        if (string.IsNullOrEmpty(hash) || !IsBcryptHash(hash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }

    public bool NeedsRehash(string hash)
    {
        if (string.IsNullOrEmpty(hash) || !IsBcryptHash(hash))
        {
            return true;
        }

        return GetWorkFactor(hash) < WorkFactor;
    }

    private static bool IsBcryptHash(string hash)
        => hash.StartsWith("$2", StringComparison.Ordinal);

    private static int GetWorkFactor(string hash)
    {
        var segments = hash.Split('$');
        return segments.Length > 2 && int.TryParse(segments[2], out var workFactor)
            ? workFactor
            : -1;
    }
}
