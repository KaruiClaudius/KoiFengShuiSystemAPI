using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KoiFengShuiSystem.Modules.Identity.Infrastructure.Persistence;

public class EfIdentityReadStore : IIdentityReadStore
{
    private readonly KoiFengShuiContext _context;

    public EfIdentityReadStore(KoiFengShuiContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public Task<Account?> GetAccountByEmailAsync(string email)
        => _context.Accounts.FirstOrDefaultAsync(account => account.Email == email);

    public Task<Account?> GetAccountByResetTokenHashAsync(string resetTokenHash)
        => _context.Accounts.FirstOrDefaultAsync(account => account.ResetTokenHash == resetTokenHash);

    public Task<Account?> GetAccountByIdAsync(int accountId)
        => _context.Accounts.FirstOrDefaultAsync(account => account.AccountId == accountId);

    public async Task<IReadOnlyList<Account>> GetAllAccountsAsync()
        => await _context.Accounts.AsNoTracking().ToListAsync();

    public Task<Role?> GetRoleByIdAsync(int roleId)
        => _context.Roles.FirstOrDefaultAsync(role => role.RoleId == roleId);
}
