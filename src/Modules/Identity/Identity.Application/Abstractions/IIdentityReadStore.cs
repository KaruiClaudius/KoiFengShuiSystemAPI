using KoiFengShuiSystem.Modules.Identity.Domain.Entities;

namespace KoiFengShuiSystem.Modules.Identity.Application.Abstractions;

public interface IIdentityReadStore
{
    Task<Account?> GetAccountByEmailAsync(string email);

    Task<Account?> GetAccountByIdAsync(int accountId);

    Task<IReadOnlyList<Account>> GetAllAccountsAsync();

    Task<Role?> GetRoleByIdAsync(int roleId);
}
