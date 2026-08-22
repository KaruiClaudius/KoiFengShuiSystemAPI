using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;

namespace KoiFengShuiSystem.Modules.Identity.Infrastructure.Persistence;

public class EfIdentityWriteStore : IIdentityWriteStore
{
    private readonly KoiFengShuiContext _context;

    public EfIdentityWriteStore(KoiFengShuiContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public Task<Account> CreateAccountAsync(Account account)
    {
        _context.Accounts.Add(account);
        return Task.FromResult(account);
    }

    public Task UpdateAccountAsync(Account account)
    {
        _context.Accounts.Update(account);
        return Task.CompletedTask;
    }

    public Task DeleteAccountAsync(Account account)
    {
        _context.Accounts.Remove(account);
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync()
        => _context.SaveChangesAsync();
}
