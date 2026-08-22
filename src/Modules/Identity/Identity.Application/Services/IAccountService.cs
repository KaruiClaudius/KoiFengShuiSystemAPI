using KoiFengShuiSystem.Modules.Identity.Application.Requests;
using KoiFengShuiSystem.Modules.Identity.Application.Responses;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;

namespace KoiFengShuiSystem.Modules.Identity.Application.Services;

public interface IAccountService
{
    Task<AuthenticationResult> AuthenticateAsync(AuthenticateRequest model);

    Task<IEnumerable<Account>> GetAllAsync();

    Task<Account?> GetByIdAsync(int id);

    Task<Account> RegisterAsync(RegisterRequest model);

    Task UpdateAsync(int id, UpdateRequest model);

    Task DeleteAsync(int id);

    Task<Account?> GetAccountByEmailAsync(string email);

    Task<bool> SendPasswordResetEmailAsync(string email, string fullName, string newPassword);

    Task UpdateUserPasswordAsync(Account account, string newPassword);

    Task<Account> CreateAsync(Account account);

    Task<AccountResponse?> GetAccountResponseByEmailAsync(string email);

    Task<bool> SendDefaultPasswordAsync(string email, string fullName, string defaultPassword);

    Task<bool> ChangePasswordAsync(int accountId, string currentPassword, string newPassword);
}
