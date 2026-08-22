namespace KoiFengShuiSystem.Modules.Identity.Application.Abstractions;

public interface IIdentityEmailSender
{
    Task<bool> SendPasswordResetEmailAsync(string email, string fullName, string newPassword);

    Task<bool> SendDefaultPasswordAsync(string email, string fullName, string defaultPassword);
}
