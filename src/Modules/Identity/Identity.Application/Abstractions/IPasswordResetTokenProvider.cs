namespace KoiFengShuiSystem.Modules.Identity.Application.Abstractions;

public interface IPasswordResetTokenProvider
{
    string Generate();

    string Hash(string token);
}
