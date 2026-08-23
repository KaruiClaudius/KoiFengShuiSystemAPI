using KoiFengShuiSystem.Modules.Identity.Domain.Entities;

namespace KoiFengShuiSystem.Modules.Identity.Application.Abstractions;

public interface IIdentityWriteStore
{
    Task<Account> CreateAccountAsync(Account account);

    Task UpdateAccountAsync(Account account);

    Task DeleteAccountAsync(Account account);

    Task<int> SaveChangesAsync();
}
